using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class DecisionRequirementHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_WhenEnforcementDisabled_ShouldSucceed()
    {
        DecisionRequirementHandler handler = CreateHandler(
            authorizationOptions: new KeycloakAuthorizationOptions
            {
                PolicyEnforcementMode = KeycloakPolicyEnforcementMode.Disabled
            });

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenDecisionEndpointReturnsSuccess_ShouldSucceed()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        DecisionRequirementHandler handler = CreateHandler(
            httpResponse: response,
            authenticated: true,
            authorizationHeader: "Bearer access-token");

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUnauthenticated_ShouldFailInEnforcingMode()
    {
        DecisionRequirementHandler handler = CreateHandler(authenticated: false);

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUnauthenticatedInPermissiveMode_ShouldSucceed()
    {
        DecisionRequirementHandler handler = CreateHandler(
            authenticated: false,
            authorizationOptions: new KeycloakAuthorizationOptions
            {
                PolicyEnforcementMode = KeycloakPolicyEnforcementMode.Permissive,
                UseDecisionEndpoint = true
            });

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenBearerTokenMissing_ShouldFail()
    {
        DecisionRequirementHandler handler = CreateHandler(
            authenticated: true,
            authorizationHeader: "Basic credentials");

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenDecisionEndpointFails_ShouldFail()
    {
        using HttpResponseMessage response = new(HttpStatusCode.Forbidden);
        DecisionRequirementHandler handler = CreateHandler(
            httpResponse: response,
            authenticated: true,
            authorizationHeader: "Bearer access-token");

        AuthorizationHandlerContext context = CreateContext(new DecisionRequirement("orders", "read"));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    private static DecisionRequirementHandler CreateHandler(
        HttpResponseMessage? httpResponse = null,
        bool authenticated = false,
        string? authorizationHeader = null,
        KeycloakAuthorizationOptions? authorizationOptions = null)
    {
        DefaultHttpContext httpContext = new();
        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], "Bearer"));
        }

        if (authorizationHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(value => value.HttpContext).Returns(httpContext);

        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetTokenEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://identity.example/token");

        HttpMessageHandler innerHandler = Mock.Of<HttpMessageHandler>();
        if (httpResponse is not null)
        {
            Mock<HttpMessageHandler> handlerMock = new();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);
            innerHandler = handlerMock.Object;
        }

        HttpClient client = new(innerHandler);
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient("KeycloakDecision")).Returns(client);

        return new DecisionRequirementHandler(
            accessor.Object,
            factory.Object,
            discovery.Object,
            Options.Create(new KeycloakOptions
            {
                Authority = "https://identity.example/realms/test",
                Realm = "test",
                ClientId = "api",
                Audience = "api"
            }),
            Options.Create(authorizationOptions ?? new KeycloakAuthorizationOptions
            {
                UseDecisionEndpoint = true,
                ResourceServerClientId = "api"
            }));
    }

    private static AuthorizationHandlerContext CreateContext(DecisionRequirement requirement)
    {
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(),
            null);
    }
}
