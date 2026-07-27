using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Integration;

[Collection(KeycloakTestConstants.CollectionName)]
[Trait("Category", "Integration")]
public sealed class AuthorizationIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task DecisionHandler_ShouldAllowAdminAndDenyRegularUser()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakTokenService tokens =
            services.GetRequiredService<IKeycloakTokenService>();
        string adminToken = (await tokens.GetPasswordTokenAsync(
            KeycloakTestConstants.AdminUsername,
            KeycloakTestConstants.Password)).Data!.AccessToken!;
        string userToken = (await tokens.GetPasswordTokenAsync(
            KeycloakTestConstants.Username,
            KeycloakTestConstants.Password)).Data!.AccessToken!;

        (await AuthorizeDecisionAsync(services, adminToken, "orders", "read"))
            .Should()
            .BeTrue();
        (await AuthorizeDecisionAsync(services, userToken, "orders", "read"))
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData("orders", "read", true)]
    [InlineData("orders", "write", false)]
    [InlineData("customers", "read", false)]
    public async Task RptHandler_ShouldEvaluateResourceAndScopePermissions(
        string resource,
        string scope,
        bool expected)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(
                "authorization",
                "{\"permissions\":[{\"rsname\":\"orders\",\"scopes\":[\"read\"]}]}")
        ],
        "Bearer"));
        RptRequirement requirement = new(resource, scope);
        AuthorizationHandlerContext context = new([requirement], principal, null);
        RptRequirementHandler handler = new(Options.Create(
            new KeycloakAuthorizationOptions
            {
                UseDecisionEndpoint = false,
                UseRptEndpoint = true,
                ResourceServerClientId = KeycloakTestConstants.ClientId
            }));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(expected);
    }

    private static async Task<bool> AuthorizeDecisionAsync(
        IServiceProvider services,
        string token,
        string resource,
        string scope)
    {
        DefaultHttpContext httpContext = new()
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "integration-user")],
                "Bearer"))
        };
        httpContext.Request.Headers.Authorization = $"Bearer {token}";
        services.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        DecisionRequirement requirement = new(resource, scope);
        AuthorizationHandlerContext context = new(
            [requirement],
            httpContext.User,
            httpContext);
        DecisionRequirementHandler handler = new(
            services.GetRequiredService<IHttpContextAccessor>(),
            services.GetRequiredService<IHttpClientFactory>(),
            services.GetRequiredService<IKeycloakDiscoveryService>(),
            services.GetRequiredService<IOptions<KeycloakOptions>>(),
            services.GetRequiredService<IOptions<KeycloakAuthorizationOptions>>());

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }
}
