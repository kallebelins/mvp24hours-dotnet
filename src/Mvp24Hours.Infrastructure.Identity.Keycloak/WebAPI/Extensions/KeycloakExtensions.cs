using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Extensions;
using KeycloakTokenClient =
    Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients.TokenClient;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

public static class KeycloakExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtBearerOptions jwtOptions = configuration.GetSection("JwtBearer").Get<JwtBearerOptions>()
            ?? throw new InvalidOperationException("The JwtBearer configuration section is required.");
        KeycloakAuthorizationOptions authorizationOptions =
            configuration.GetSection(KeycloakAuthorizationOptions.SectionName)
                .Get<KeycloakAuthorizationOptions>()
            ?? new KeycloakAuthorizationOptions();

        services.AddHttpContextAccessor();
        services.Configure<KeycloakOptions>(
            configuration.GetSection(KeycloakOptions.SectionName));
        services.Configure<KeycloakAuthorizationOptions>(
            configuration.GetSection(KeycloakAuthorizationOptions.SectionName));
        services.TryAddSingleton<IKeycloakJwtTokenParser, KeycloakJwtTokenParser>();
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = jwtOptions.Authority;
                options.Audience = jwtOptions.Audience;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    NameClaimType = KeycloakClaimTypes.PreferredUserName,
                    RoleClaimType = authorizationOptions.RealmRoleClaimType
                };
                options.Events = CreateJwtBearerEvents();
            });

        services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

        return services;
    }

    /// <summary>
    /// Registers an application-specific Keycloak user synchronization service.
    /// </summary>
    public static IServiceCollection AddKeycloakUserSync<TService>(
        this IServiceCollection services)
        where TService : class, IUserKeycloakService
    {
        services.AddScoped<IUserKeycloakService, TService>();
        return services;
    }

    public static IServiceCollection AddKeycloakAuthorization(
        this IServiceCollection services,
        Dictionary<string, List<string>>? roles = null,
        Dictionary<string, List<DecisionRequirement>>? decisionRequirements = null,
        Dictionary<string, List<RptRequirement>>? rptRequirements = null,
        Dictionary<string, List<IAuthorizationRequirement>>? resourceRequirements = null)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient("KeycloakDecision");
        services.AddAuthorization(options =>
        {
            AddRolePolicies(options, roles);
            AddDecisionPolicies(services, options, decisionRequirements);
            AddRptPolicies(services, options, rptRequirements);
            AddResourcePolicies(options, resourceRequirements);
        });

        return services;
    }

    public static IServiceCollection AddKeycloakPolicies(
        this IServiceCollection services,
        Assembly assembly,
        Dictionary<string, List<IAuthorizationRequirement>>? resourceRequirements = null)
    {
        return services.AddKeycloakAuthorization(
            assembly.GetRolePolicies(),
            assembly.GetDecisionPolicies(),
            assembly.GetRptPolicies(),
            resourceRequirements);
    }

    public static IServiceCollection AddKeycloakService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient<KeycloakService>(client =>
        {
            string resourceUrl = configuration["KeycloakResourceUrl"]
                ?? throw new InvalidOperationException("KeycloakResourceUrl is required.");
            client.BaseAddress = new Uri(resourceUrl);
        })
            .AddHttpMessageHandler<PropagationAuthorizationDelegatingHandler>();
        services.TryAddTransient<PropagationAuthorizationDelegatingHandler>();
        services.AddHttpClient<KeycloakTokenClient>();
        services.AddSingleton(_ =>
            configuration.GetSection("ClientCredentialsTokenRequest")
                .Get<ClientCredentialsTokenRequest>()
            ?? throw new InvalidOperationException(
                "The ClientCredentialsTokenRequest configuration section is required."));

        return services;
    }

    private static JwtBearerEvents CreateJwtBearerEvents()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                GetLogger(context.HttpContext).LogDebug("JWT Bearer: Message received");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                if (context.AuthenticateFailure is not null)
                {
                    GetLogger(context.HttpContext).LogWarning(
                        context.AuthenticateFailure,
                        "JWT Bearer: Challenge failed");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                GetLogger(context.HttpContext).LogError(
                    context.Exception,
                    "JWT Bearer: Authentication failed");
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = SynchronizeLocalUser
        };
    }

    private static async Task SynchronizeLocalUser(TokenValidatedContext context)
    {
        ILogger logger = GetLogger(context.HttpContext);
        IUserKeycloakService? localUserService =
            context.HttpContext.RequestServices.GetService<IUserKeycloakService>();
        UserToken? user = context.HttpContext.RequestServices.GetUserToken();

        if (localUserService is not null && user?.Id is Guid userId)
        {
            logger.LogDebug("JWT Bearer: Starting user integration");
            IBusinessResult<bool> anyUserResult = await localUserService.GetAnyLocalUserByIdAsync(
                userId,
                context.HttpContext.RequestAborted);
            if (!anyUserResult.GetDataValue() || anyUserResult.HasErrors)
            {
                IBusinessResult<object> localUserResult = await localUserService.CreateOrUpdateLocalUserAsync(
                    user,
                    context.HttpContext.RequestAborted);
                if (localUserResult.HasErrors)
                {
                    logger.LogError(
                        "JWT Bearer: Failed to create or update the local user: {Error}",
                        localUserResult.Messages?.FirstOrDefault()?.Message);
                }
            }
        }

        logger.LogDebug(
            "JWT Bearer: Token validated with claims: {Claims}",
            string.Join("; ", context.Principal?.Claims.Select(claim => claim.Type) ?? []));
    }

    private static ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(KeycloakExtensions).FullName!);
    }

    private static void AddRolePolicies(
        AuthorizationOptions options,
        Dictionary<string, List<string>>? roles)
    {
        if (roles is null)
        {
            return;
        }

        foreach ((string? name, List<string>? policyRoles) in roles)
        {
            if (policyRoles.Count > 0)
            {
                options.AddPolicy(
                    name,
                    policy => policy.RequireRole([.. policyRoles]));
            }
        }
    }

    private static void AddDecisionPolicies(
        IServiceCollection services,
        AuthorizationOptions options,
        Dictionary<string, List<DecisionRequirement>>? requirements)
    {
        if (requirements is null)
        {
            return;
        }

        services.TryAddScoped<IAuthorizationHandler, DecisionRequirementHandler>();
        foreach ((string? name, List<DecisionRequirement>? policyRequirements) in requirements)
        {
            if (policyRequirements.Count > 0)
            {
                options.AddPolicy(
                    name,
                    policy => policy.AddRequirements([.. policyRequirements]));
            }
        }
    }

    private static void AddRptPolicies(
        IServiceCollection services,
        AuthorizationOptions options,
        Dictionary<string, List<RptRequirement>>? requirements)
    {
        if (requirements is null)
        {
            return;
        }

        services.TryAddScoped<IAuthorizationHandler, RptRequirementHandler>();
        foreach ((string? name, List<RptRequirement>? policyRequirements) in requirements)
        {
            if (policyRequirements.Count > 0)
            {
                options.AddPolicy(
                    name,
                    policy => policy.AddRequirements([.. policyRequirements]));
            }
        }
    }

    private static void AddResourcePolicies(
        AuthorizationOptions options,
        Dictionary<string, List<IAuthorizationRequirement>>? requirements)
    {
        if (requirements is null)
        {
            return;
        }

        foreach ((string? name, List<IAuthorizationRequirement>? policyRequirements) in requirements)
        {
            if (policyRequirements.Count == 0)
            {
                continue;
            }

            options.AddPolicy(name, policy => policy.RequireAssertion(async context =>
            {
                if (context.Resource is not HttpContext httpContext)
                {
                    return false;
                }

                IAuthorizationService authorizationService =
                    httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                AuthorizationPolicy resourcePolicy =
                    new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                        .AddRequirements([.. policyRequirements])
                        .Build();
                AuthorizationResult result = await authorizationService.AuthorizeAsync(
                    httpContext.User,
                    resourcePolicy);
                return result.Succeeded;
            }));
        }
    }

    private static Dictionary<string, List<string>> GetRolePolicies(this Assembly assembly)
    {
        var result = new Dictionary<string, List<string>>();
        foreach (TypeInfo type in GetAuthorizedTypes(assembly))
        {
            var roles = type.GetCustomAttributes<AuthorizeAttribute>()
                .Concat(type.GetMethods()
                    .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>()))
                .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            result[type.Name] = roles;
        }

        return result;
    }

    private static Dictionary<string, List<RptRequirement>> GetRptPolicies(this Assembly assembly)
    {
        return GetAuthorizationRequirements(
            assembly,
            (resource, scope) => new RptRequirement(resource, scope));
    }

    private static Dictionary<string, List<DecisionRequirement>> GetDecisionPolicies(
        this Assembly assembly)
    {
        return GetAuthorizationRequirements(
            assembly,
            (resource, scope) => new DecisionRequirement(resource, scope));
    }

    private static Dictionary<string, List<TRequirement>> GetAuthorizationRequirements<TRequirement>(
        Assembly assembly,
        Func<string, string, TRequirement> factory)
    {
        var result = new Dictionary<string, List<TRequirement>>();
        foreach (TypeInfo type in GetAuthorizedTypes(assembly))
        {
            var requirements = type.GetMethods()
                .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>())
                .SelectMany(attribute => (attribute.Policy ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(policy => policy.Split('#', StringSplitOptions.TrimEntries))
                .Where(values => values.Length == 2)
                .Select(values => factory(values[0], values[1]))
                .ToList();
            result[type.Name] = requirements;
        }

        return result;
    }

    private static IEnumerable<TypeInfo> GetAuthorizedTypes(Assembly assembly)
    {
        return assembly.DefinedTypes.Where(type =>
            type.IsDefined(typeof(AuthorizeAttribute), true)
            || type.GetMethods().Any(method =>
                method.IsDefined(typeof(AuthorizeAttribute), true)));
    }
}
