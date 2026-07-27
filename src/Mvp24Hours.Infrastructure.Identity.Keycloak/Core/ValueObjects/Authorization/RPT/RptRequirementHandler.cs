using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;

public class RptRequirementHandler : AuthorizationHandler<RptRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RptRequirement requirement)
    {
        string? authorizationClaim = context.User.FindFirstValue("authorization");
        if (string.IsNullOrWhiteSpace(authorizationClaim))
        {
            return Task.CompletedTask;
        }

        using var json = JsonDocument.Parse(authorizationClaim);
        if (!json.RootElement.TryGetProperty("permissions", out JsonElement permissions))
        {
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

            if (scopes.EnumerateArray().Any(scope => scope.GetString() == requirement.Scope))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
