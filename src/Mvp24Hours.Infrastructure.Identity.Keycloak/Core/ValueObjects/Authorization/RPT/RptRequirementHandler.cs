using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;

public class RptRequirementHandler(
    IOptions<KeycloakAuthorizationOptions> options)
    : AuthorizationHandler<RptRequirement>
{
    private readonly KeycloakAuthorizationOptions _options = options.Value;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RptRequirement requirement)
    {
        if (_options.PolicyEnforcementMode == KeycloakPolicyEnforcementMode.Disabled)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!_options.UseRptEndpoint)
        {
            CompleteFailure(context, requirement);
            return Task.CompletedTask;
        }

        string? authorizationClaim = context.User.FindFirstValue("authorization");
        if (string.IsNullOrWhiteSpace(authorizationClaim))
        {
            CompleteFailure(context, requirement);
            return Task.CompletedTask;
        }

        try
        {
            using var json = JsonDocument.Parse(authorizationClaim);
            JsonElement permissions;
            if (json.RootElement.ValueKind == JsonValueKind.Array)
            {
                permissions = json.RootElement;
            }
            else if (!json.RootElement.TryGetProperty(
                _options.PermissionClaimType,
                out permissions))
            {
                CompleteFailure(context, requirement);
                return Task.CompletedTask;
            }

            foreach (JsonElement permission in permissions.EnumerateArray())
            {
                if (!permission.TryGetProperty("rsname", out JsonElement resource)
                    || resource.GetString() != requirement.Resource
                    || !permission.TryGetProperty("scopes", out JsonElement scopes))
                {
                    continue;
                }

                if (scopes.EnumerateArray().Any(
                    scope => scope.GetString() == requirement.Scope))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }
        catch (JsonException)
        {
            // Invalid permission claims are treated as an authorization failure.
        }

        CompleteFailure(context, requirement);
        return Task.CompletedTask;
    }

    private void CompleteFailure(
        AuthorizationHandlerContext context,
        RptRequirement requirement)
    {
        if (_options.PolicyEnforcementMode == KeycloakPolicyEnforcementMode.Permissive)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
