using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

public class DecisionRequirementHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IKeycloakDiscoveryService discoveryService,
    IOptions<KeycloakOptions> keycloakOptions,
    IOptions<KeycloakAuthorizationOptions> authorizationOptions)
    : AuthorizationHandler<DecisionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions = keycloakOptions.Value;
    private readonly KeycloakAuthorizationOptions _authorizationOptions =
        authorizationOptions.Value;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DecisionRequirement requirement)
    {
        if (_authorizationOptions.PolicyEnforcementMode
            == KeycloakPolicyEnforcementMode.Disabled)
        {
            context.Succeed(requirement);
            return;
        }

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (!_authorizationOptions.UseDecisionEndpoint
            || httpContext?.User.Identity?.IsAuthenticated != true)
        {
            CompleteFailure(context, requirement);
            return;
        }

        Dictionary<string, string> data = new()
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["response_mode"] = "decision",
            ["audience"] = _authorizationOptions.ResourceServerClientId
                ?? _keycloakOptions.Audience
                ?? _keycloakOptions.ClientId,
            ["permission"] = $"{requirement.Resource}#{requirement.Scope}"
        };

        string? authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();
        string? token = authorization?.StartsWith(
            "Bearer ",
            StringComparison.OrdinalIgnoreCase) == true
                ? authorization["Bearer ".Length..].Trim()
                : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            CompleteFailure(context, requirement);
            return;
        }

        HttpClient client = _httpClientFactory.CreateClient("KeycloakDecision");
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            await discoveryService.GetTokenEndpointAsync(
                httpContext.RequestAborted))
        {
            Content = new FormUrlEncodedContent(data)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await client.SendAsync(
            request,
            httpContext.RequestAborted);
        if (response.IsSuccessStatusCode)
        {
            context.Succeed(requirement);
        }
        else
        {
            CompleteFailure(context, requirement);
        }
    }

    private void CompleteFailure(
        AuthorizationHandlerContext context,
        DecisionRequirement requirement)
    {
        if (_authorizationOptions.PolicyEnforcementMode
            == KeycloakPolicyEnforcementMode.Permissive)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
