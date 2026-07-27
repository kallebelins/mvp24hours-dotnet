using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Extensions;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

public static class KeycloakExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        ConfigureKeycloakOptions(
            services,
            configuration.GetSection(sectionName));
        ConfigureAuthorizationOptions(
            services,
            configuration.GetSection($"{sectionName}:Authorization"));
        AddAuthenticationServices(services);

        return services;
    }

    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        Action<KeycloakOptions> configure,
        Action<KeycloakAuthorizationOptions>? configureAuthorization = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<KeycloakOptions>()
            .Configure(configure)
            .Validate(
                options => options.Validate().Count == 0,
                "Invalid Keycloak authentication configuration.")
            .ValidateOnStart();
        services.AddOptions<KeycloakAuthorizationOptions>()
            .Configure(configureAuthorization ?? (_ => { }))
            .Validate(
                options => options.Validate().Count == 0,
                "Invalid Keycloak authorization configuration.")
            .ValidateOnStart();
        AddAuthenticationServices(services);

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
        AddCoreServices(services);
        services.AddHttpClient("KeycloakDecision").AddStandardResilienceHandler();
        services.AddHttpClient("KeycloakRpt").AddStandardResilienceHandler();
        services.TryAddScoped<IAuthorizationHandler, DecisionRequirementHandler>();
        services.TryAddScoped<IAuthorizationHandler, RptRequirementHandler>();
        services.AddAuthorization(options =>
        {
            AddRolePolicies(options, roles);
            AddDecisionPolicies(options, decisionRequirements);
            AddRptPolicies(options, rptRequirements);
            AddResourcePolicies(options, resourceRequirements);
        });

        return services;
    }

    public static IServiceCollection AddKeycloakAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakAuthorizationOptions.SectionName,
        Dictionary<string, List<string>>? roles = null,
        Dictionary<string, List<DecisionRequirement>>? decisionRequirements = null,
        Dictionary<string, List<RptRequirement>>? rptRequirements = null,
        Dictionary<string, List<IAuthorizationRequirement>>? resourceRequirements = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        ConfigureAuthorizationOptions(
            services,
            configuration.GetSection(sectionName));
        return services.AddKeycloakAuthorization(
            roles,
            decisionRequirements,
            rptRequirements,
            resourceRequirements);
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

    public static IServiceCollection AddKeycloakServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddKeycloakAuthentication(configuration, sectionName);
        services.AddKeycloakAuthorization(
            configuration,
            $"{sectionName}:Authorization");
        ConfigureAdminOptions(
            services,
            configuration.GetSection($"{sectionName}:Admin"));
        AddAdminServices(services);
        return services;
    }

    public static IServiceCollection AddKeycloakAdminServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        ConfigureKeycloakOptions(services, configuration.GetSection(sectionName));
        ConfigureAdminOptions(
            services,
            configuration.GetSection($"{sectionName}:Admin"));
        AddCoreServices(services);
        AddAdminServices(services);
        return services;
    }

    [Obsolete("Use AddKeycloakServices instead.")]
    public static IServiceCollection AddKeycloakService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddKeycloakServices(configuration);
    }

    private static void AddAuthenticationServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IKeycloakJwtTokenParser, KeycloakJwtTokenParser>();
        services.TryAddScoped<IKeycloakCurrentUser, KeycloakCurrentUser>();
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakOptions>, IOptions<KeycloakAuthorizationOptions>>(
                (jwtOptions, keycloakOptions, authorizationOptions) =>
                {
                    KeycloakOptions keycloak = keycloakOptions.Value;
                    jwtOptions.Authority = keycloak.Authority;
                    jwtOptions.Audience = keycloak.Audience;
                    if (!string.IsNullOrWhiteSpace(keycloak.MetadataAddress))
                    {
                        jwtOptions.MetadataAddress = keycloak.MetadataAddress;
                    }

                    jwtOptions.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                    jwtOptions.MapInboundClaims = false;
                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = keycloak.ValidateAudience,
                        ValidateIssuer = keycloak.ValidateIssuer,
                        ValidateLifetime = true,
                        ClockSkew = keycloak.TokenClockSkew,
                        NameClaimType = KeycloakClaimTypes.PreferredUserName,
                        RoleClaimType = authorizationOptions.Value.RealmRoleClaimType
                    };
                    jwtOptions.Events = CreateJwtBearerEvents();
                });
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<
                IClaimsTransformation,
                KeycloakRolesClaimsTransformation>());
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient("KeycloakDiscovery").AddStandardResilienceHandler();
        services.AddHttpClient("KeycloakToken").AddStandardResilienceHandler();
        services.TryAddSingleton<IKeycloakDiscoveryService, KeycloakDiscoveryService>();
        services.TryAddSingleton<KeycloakTokenClient>();
        services.TryAddSingleton<IKeycloakTokenService, KeycloakTokenService>();
    }

    private static void AddAdminServices(IServiceCollection services)
    {
        services.TryAddTransient<KeycloakAdminBearerDelegatingHandler>();
        services.AddHttpClient(
                "KeycloakAdmin",
                (serviceProvider, client) =>
                {
                    KeycloakAdminOptions options = serviceProvider
                        .GetRequiredService<IOptions<KeycloakAdminOptions>>()
                        .Value;
                    client.BaseAddress = new Uri(
                        $"{options.AdminBaseUrl.TrimEnd('/')}/");
                    client.Timeout = options.Timeout;
                })
            .AddHttpMessageHandler<KeycloakAdminBearerDelegatingHandler>()
            .AddStandardResilienceHandler(
                options => options.Retry.MaxRetryAttempts = 3);
        services.TryAddScoped<KeycloakAdminHttpClient>();
        services.TryAddScoped<IKeycloakUserService, KeycloakUserService>();
        services.TryAddScoped<IKeycloakRoleService, KeycloakRoleService>();
        services.TryAddScoped<IKeycloakGroupService, KeycloakGroupService>();
    }

    private static void ConfigureKeycloakOptions(
        IServiceCollection services,
        IConfiguration section)
    {
        services.AddOptions<KeycloakOptions>()
            .Bind(section)
            .Validate(
                options => options.Validate().Count == 0,
                "Invalid Keycloak authentication configuration.")
            .ValidateOnStart();
    }

    private static void ConfigureAuthorizationOptions(
        IServiceCollection services,
        IConfiguration section)
    {
        services.AddOptions<KeycloakAuthorizationOptions>()
            .Bind(section)
            .Validate(
                options => options.Validate().Count == 0,
                "Invalid Keycloak authorization configuration.")
            .ValidateOnStart();
    }

    private static void ConfigureAdminOptions(
        IServiceCollection services,
        IConfiguration section)
    {
        services.AddOptions<KeycloakAdminOptions>()
            .Bind(section)
            .Validate(
                options => options.Validate().Count == 0,
                "Invalid Keycloak Admin API configuration.")
            .ValidateOnStart();
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
        AuthorizationOptions options,
        Dictionary<string, List<DecisionRequirement>>? requirements)
    {
        if (requirements is null)
        {
            return;
        }

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
        AuthorizationOptions options,
        Dictionary<string, List<RptRequirement>>? requirements)
    {
        if (requirements is null)
        {
            return;
        }

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
