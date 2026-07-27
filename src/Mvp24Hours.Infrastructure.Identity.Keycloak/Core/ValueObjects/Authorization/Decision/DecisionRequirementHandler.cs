using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

public class DecisionRequirementHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<JwtBearerOptions> options) : AuthorizationHandler<DecisionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<JwtBearerOptions> _options = options;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DecisionRequirement requirement)
    {
        JwtBearerOptions options = _options.Get(JwtBearerDefaults.AuthenticationScheme);
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        Dictionary<string, string> data = new()
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["response_mode"] = "decision",
            ["audience"] = options.Audience ?? string.Empty,
            ["permission"] = $"{requirement.Resource}#{requirement.Scope}"
        };

        string? token = await httpContext.GetTokenAsync(
            JwtBearerDefaults.AuthenticationScheme,
            "access_token");
        if (string.IsNullOrWhiteSpace(token) || options.ConfigurationManager is null)
        {
            context.Fail();
            return;
        }

        HttpClient client = _httpClientFactory.CreateClient("KeycloakDecision");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        OpenIdConnectConfiguration configuration = await options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
        using HttpResponseMessage response = await client.PostAsync(
            configuration.TokenEndpoint,
            new FormUrlEncodedContent(data),
            CancellationToken.None);

        if (response.IsSuccessStatusCode)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
