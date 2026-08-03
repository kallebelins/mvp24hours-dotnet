using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class RptRequirementHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_WhenEnforcementDisabled_ShouldSucceed()
    {
        RptRequirementHandler handler = CreateHandler(new KeycloakAuthorizationOptions
        {
            PolicyEnforcementMode = KeycloakPolicyEnforcementMode.Disabled,
            UseRptEndpoint = true,
            ResourceServerClientId = "api"
        });
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            claims: []);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenRptEndpointDisabled_ShouldFail()
    {
        RptRequirementHandler handler = CreateHandler(new KeycloakAuthorizationOptions
        {
            UseRptEndpoint = false
        });
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            claims: []);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenPermissionMatchesArrayPayload_ShouldSucceed()
    {
        const string payload =
            """[{"rsname":"orders","scopes":["read","write"]},{"rsname":"reports","scopes":["view"]}]""";
        RptRequirementHandler handler = CreateHandler(CreateRptOptions());
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            [new Claim("authorization", payload)]);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenPermissionMatchesObjectPayload_ShouldSucceed()
    {
        const string payload =
            """{"permissions":[{"rsname":"orders","scopes":["read"]}]}""";
        RptRequirementHandler handler = CreateHandler(CreateRptOptions());
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            [new Claim("authorization", payload)]);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenScopeDoesNotMatch_ShouldFail()
    {
        const string payload =
            """[{"rsname":"orders","scopes":["write"]}]""";
        RptRequirementHandler handler = CreateHandler(CreateRptOptions());
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            [new Claim("authorization", payload)]);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenClaimIsInvalidJson_ShouldFail()
    {
        RptRequirementHandler handler = CreateHandler(CreateRptOptions());
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            [new Claim("authorization", "{invalid")]);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenClaimMissing_ShouldSucceedInPermissiveMode()
    {
        RptRequirementHandler handler = CreateHandler(new KeycloakAuthorizationOptions
        {
            PolicyEnforcementMode = KeycloakPolicyEnforcementMode.Permissive,
            UseRptEndpoint = true,
            ResourceServerClientId = "api"
        });
        AuthorizationHandlerContext context = CreateContext(
            new RptRequirement("orders", "read"),
            claims: []);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    private static KeycloakAuthorizationOptions CreateRptOptions()
    {
        return new KeycloakAuthorizationOptions
        {
            UseRptEndpoint = true,
            ResourceServerClientId = "api"
        };
    }

    private static RptRequirementHandler CreateHandler(KeycloakAuthorizationOptions options)
    {
        return new RptRequirementHandler(Options.Create(options));
    }

    private static AuthorizationHandlerContext CreateContext(
        RptRequirement requirement,
        IEnumerable<Claim> claims)
    {
        ClaimsPrincipal user = new(new ClaimsIdentity(claims, "Bearer"));
        return new AuthorizationHandlerContext([requirement], user, null);
    }
}
