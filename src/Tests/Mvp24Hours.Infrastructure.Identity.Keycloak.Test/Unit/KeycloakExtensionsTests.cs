using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void AddKeycloakAuthentication_WithNullConfiguration_ShouldThrow()
    {
        ServiceCollection services = new();

        Action act = () => services.AddKeycloakAuthentication((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddKeycloakAuthentication_WithConfiguration_ShouldRegisterCoreServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthentication(CreateConfiguration());

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKeycloakJwtTokenParser));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKeycloakCurrentUser));
    }

    [Fact]
    public void AddKeycloakAuthentication_WithDelegates_ShouldValidateOnStart()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthentication(
            options =>
            {
                options.Authority = "https://identity.example/realms/test";
                options.Realm = "test";
                options.ClientId = "api";
                options.Audience = "api";
            },
            authorization => authorization.ResourceServerClientId = "api");

        ServiceProvider provider = services.BuildServiceProvider();
        KeycloakOptions options = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

        options.Authority.Should().Be("https://identity.example/realms/test");
    }

    [Fact]
    public void AddKeycloakUserSync_ShouldRegisterScopedService()
    {
        ServiceCollection services = new();
        services.AddKeycloakUserSync<StubUserKeycloakService>();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IUserKeycloakService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddKeycloakAuthorization_ShouldRegisterPoliciesAndHandlers()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthorization(
            roles: new Dictionary<string, List<string>>
            {
                ["AdminOnly"] = ["admin"],
                ["EmptyRoles"] = []
            },
            decisionRequirements: new Dictionary<string, List<DecisionRequirement>>
            {
                ["OrdersRead"] = [new DecisionRequirement("orders", "read")]
            },
            rptRequirements: new Dictionary<string, List<RptRequirement>>
            {
                ["ReportsView"] = [new RptRequirement("reports", "view")]
            });

        ServiceProvider provider = services.BuildServiceProvider();
        AuthorizationOptions authorization = provider
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        authorization.GetPolicy("AdminOnly").Should().NotBeNull();
        authorization.GetPolicy("EmptyRoles").Should().BeNull();
        authorization.GetPolicy("OrdersRead").Should().NotBeNull();
        authorization.GetPolicy("ReportsView").Should().NotBeNull();
        provider.GetServices<IAuthorizationHandler>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddKeycloakAuthorization_WithConfiguration_ShouldBindAuthorizationOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthorization(CreateConfiguration());

        ServiceProvider provider = services.BuildServiceProvider();
        KeycloakAuthorizationOptions options = provider
            .GetRequiredService<IOptions<KeycloakAuthorizationOptions>>()
            .Value;

        options.ResourceServerClientId.Should().Be("api");
    }

    [Fact]
    public void AddKeycloakPolicies_ShouldDiscoverAuthorizeAttributes()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakPolicies(typeof(PolicyDiscoverySample).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        AuthorizationOptions authorization = provider
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        authorization.GetPolicy(nameof(PolicyDiscoverySample)).Should().NotBeNull();
        authorization.GetPolicy("OrdersRead").Should().BeNull("decision policies use the type name as policy key");
    }

    [Fact]
    public void AddKeycloakServices_ShouldRegisterAuthenticationAuthorizationAndAdminClients()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakServices(CreateConfiguration(includeAdmin: true));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IKeycloakJwtTokenParser>().Should().NotBeNull();
        provider.GetService<IKeycloakUserService>().Should().NotBeNull();
        provider.GetService<IKeycloakRoleService>().Should().NotBeNull();
        provider.GetService<IKeycloakGroupService>().Should().NotBeNull();
    }

    [Fact]
    public void AddKeycloakAdminServices_ShouldRegisterAdminClientsWithoutAuthentication()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAdminServices(CreateConfiguration(includeAdmin: true));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IKeycloakTokenService>().Should().NotBeNull();
        provider.GetService<IKeycloakUserService>().Should().NotBeNull();
        provider.GetService<IKeycloakJwtTokenParser>().Should().BeNull();
    }

    [Fact]
    public void AddKeycloakService_ObsoleteAlias_ShouldRegisterSameServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
#pragma warning disable CS0618
        services.AddKeycloakService(CreateConfiguration(includeAdmin: true));
#pragma warning restore CS0618

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IKeycloakTokenService>().Should().NotBeNull();
    }

    [Fact]
    public void JwtBearerOptions_ShouldMapKeycloakSettings()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthentication(
            options =>
            {
                options.Authority = "https://identity.example/realms/test";
                options.Realm = "test";
                options.ClientId = "api";
                options.Audience = "api";
                options.MetadataAddress = "https://identity.example/realms/test/.well-known/openid-configuration";
                options.RequireHttpsMetadata = false;
                options.ValidateAudience = false;
                options.TokenClockSkew = TimeSpan.FromSeconds(15);
            },
            authorization => authorization.RealmRoleClaimType = "roles");

        ServiceProvider provider = services.BuildServiceProvider();
        JwtBearerOptions jwtOptions = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Authority.Should().Be("https://identity.example/realms/test");
        jwtOptions.Audience.Should().Be("api");
        jwtOptions.MetadataAddress.Should().Contain("openid-configuration");
        jwtOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(15));
        jwtOptions.TokenValidationParameters.RoleClaimType.Should().Be("roles");
    }

    [Fact]
    public async Task JwtBearerEvents_OnMessageReceived_ShouldComplete()
    {
        JwtBearerEvents events = await GetJwtBearerEventsAsync();
        MessageReceivedContext context = CreateMessageReceivedContext();

        await events.OnMessageReceived(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task JwtBearerEvents_OnChallenge_WithFailure_ShouldComplete()
    {
        JwtBearerEvents events = await GetJwtBearerEventsAsync();
        JwtBearerChallengeContext context = CreateChallengeContext(
            new SecurityTokenExpiredException("expired"));

        await events.OnChallenge(context);

        context.AuthenticateFailure.Should().NotBeNull();
    }

    [Fact]
    public async Task JwtBearerEvents_OnAuthenticationFailed_WithExpiredToken_ShouldSetHeader()
    {
        JwtBearerEvents events = await GetJwtBearerEventsAsync();
        AuthenticationFailedContext context = CreateAuthenticationFailedContext(
            new SecurityTokenExpiredException("expired"));

        await events.OnAuthenticationFailed(context);

        context.Response.Headers["Token-Expired"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task JwtBearerEvents_OnTokenValidated_ShouldCreateLocalUserWhenMissing()
    {
        Mock<IUserKeycloakService> sync = new();
        sync.Setup(service => service.GetAnyLocalUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessResult.Success(false));
        sync.Setup(service => service.CreateOrUpdateLocalUserAsync(
                It.IsAny<UserToken>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessResult.Success<object>(new object()));
        JwtBearerEvents events = await GetJwtBearerEventsAsync(sync.Object);
        TokenValidatedContext context = CreateTokenValidatedContext(sync.Object);

        await events.OnTokenValidated(context);

        sync.Verify(service => service.CreateOrUpdateLocalUserAsync(
            It.Is<UserToken>(user => user.Id == UserId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JwtBearerEvents_OnTokenValidated_ShouldSkipWhenLocalUserExists()
    {
        Mock<IUserKeycloakService> sync = new();
        sync.Setup(service => service.GetAnyLocalUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessResult.Success(true));
        JwtBearerEvents events = await GetJwtBearerEventsAsync(sync.Object);
        TokenValidatedContext context = CreateTokenValidatedContext(sync.Object);

        await events.OnTokenValidated(context);

        sync.Verify(service => service.CreateOrUpdateLocalUserAsync(
            It.IsAny<UserToken>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task JwtBearerEvents_OnTokenValidated_ShouldLogWhenSyncFails()
    {
        Mock<IUserKeycloakService> sync = new();
        sync.Setup(service => service.GetAnyLocalUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessResult.Success(false));
        sync.Setup(service => service.CreateOrUpdateLocalUserAsync(
                It.IsAny<UserToken>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessResult.Failure<object>("sync failed", "SYNC"));
        JwtBearerEvents events = await GetJwtBearerEventsAsync(sync.Object);
        TokenValidatedContext context = CreateTokenValidatedContext(sync.Object);

        await events.OnTokenValidated(context);

        sync.Verify(service => service.CreateOrUpdateLocalUserAsync(
            It.IsAny<UserToken>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AddKeycloakAuthorization_WithResourcePolicy_ShouldRegisterCustomRequirementPolicy()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAuthorization();
        services.AddKeycloakAuthorization(
            resourceRequirements: new Dictionary<string, List<IAuthorizationRequirement>>
            {
                ["ResourceGate"] = [new StubAuthorizationRequirement()]
            });

        ServiceProvider provider = services.BuildServiceProvider();
        AuthorizationOptions authorization = provider
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        authorization.GetPolicy("ResourceGate").Should().NotBeNull();
    }

    private static IConfiguration CreateConfiguration(bool includeAdmin = false)
    {
        Dictionary<string, string?> values = new()
        {
            ["Keycloak:Authority"] = "https://identity.example/realms/test",
            ["Keycloak:Realm"] = "test",
            ["Keycloak:ClientId"] = "api",
            ["Keycloak:Audience"] = "api",
            ["Keycloak:Authorization:ResourceServerClientId"] = "api"
        };
        if (includeAdmin)
        {
            values["Keycloak:Admin:AdminBaseUrl"] =
                "https://identity.example/admin/realms/test";
            values["Keycloak:Admin:Realm"] = "test";
            values["Keycloak:Admin:ClientId"] = "admin-client";
            values["Keycloak:Admin:ClientSecret"] = "secret";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static async Task<JwtBearerEvents> GetJwtBearerEventsAsync(
        IUserKeycloakService? syncService = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakAuthentication(
            options =>
            {
                options.Authority = "https://identity.example/realms/test";
                options.Realm = "test";
                options.ClientId = "api";
                options.Audience = "api";
            });
        if (syncService is not null)
        {
            services.AddSingleton(syncService);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        JwtBearerOptions jwtOptions = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        return jwtOptions.Events;
    }

    private static MessageReceivedContext CreateMessageReceivedContext()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        return new MessageReceivedContext(
            httpContext,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }

    private static JwtBearerChallengeContext CreateChallengeContext(Exception failure)
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        return new JwtBearerChallengeContext(
            httpContext,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions(),
            properties: new AuthenticationProperties())
        {
            AuthenticateFailure = failure
        };
    }

    private static AuthenticationFailedContext CreateAuthenticationFailedContext(Exception failure)
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        return new AuthenticationFailedContext(
            httpContext,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Exception = failure
        };
    }

    private static TokenValidatedContext CreateTokenValidatedContext(IUserKeycloakService syncService)
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        httpContext.RequestServices = CreateRequestServices(httpContext, syncService);
        string jwt = CreateJwt(UserId);
        Mock<IKeycloakJwtTokenParser> parser = new();
        parser.Setup(value => value.ParseUserToken(It.IsAny<string?>()))
            .Returns(new UserToken { Id = UserId, PreferredUserName = "sync-user" });
        httpContext.RequestServices = CreateRequestServices(httpContext, syncService, parser.Object);
        httpContext.Request.Headers.Authorization = $"Bearer {jwt}";
        return new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", UserId.ToString())], "Bearer"))
        };
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = CreateRequestServices(httpContext);
        return httpContext;
    }

    private static IServiceProvider CreateRequestServices(
        HttpContext httpContext,
        IUserKeycloakService? syncService = null,
        IKeycloakJwtTokenParser? parser = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(value => value.HttpContext).Returns(httpContext);
        services.AddSingleton(accessor.Object);
        if (syncService is not null)
        {
            services.AddSingleton(syncService);
        }

        if (parser is not null)
        {
            services.AddSingleton(parser);
        }

        return services.BuildServiceProvider();
    }

    private static string CreateJwt(Guid userId)
    {
        JwtSecurityToken token = new(claims: [new Claim("sub", userId.ToString())]);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize(Roles = "admin,operator")]
    private sealed class PolicyDiscoverySample
    {
        [Authorize(Policy = "orders#read")]
        public void ReadOrders() { }

        [Authorize(Policy = "reports#view")]
        public void ViewReports() { }
    }

    private sealed class StubUserKeycloakService : IUserKeycloakService
    {
        public Task<IBusinessResult<bool>> GetAnyLocalUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBusinessResult<bool>>(BusinessResult.Success(false));
        }

        public Task<IBusinessResult<object>> GetLocalIdByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBusinessResult<object>>(BusinessResult.Success<object>(id));
        }

        public Task<IBusinessResult<object>> GetLocalIdByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBusinessResult<object>>(BusinessResult.Success<object>(email));
        }

        public Task<IBusinessResult<object>> CreateOrUpdateLocalUserAsync(
            UserToken dto,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBusinessResult<object>>(BusinessResult.Success<object>(dto));
        }

        public Task<IBusinessResult<object>> SyncLocalUserFromKeycloakAsync(
            Guid keycloakUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBusinessResult<object>>(BusinessResult.Success<object>(keycloakUserId));
        }
    }

    private sealed class StubAuthorizationRequirement : IAuthorizationRequirement;
}
