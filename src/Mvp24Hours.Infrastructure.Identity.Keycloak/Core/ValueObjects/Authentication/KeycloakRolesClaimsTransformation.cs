using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// Transforms Keycloak realm roles into role claims.
/// </summary>
public class KeycloakRolesClaimsTransformation(string roleClaimType, string realmScope) : IClaimsTransformation
{
    private readonly string _roleClaimType = roleClaimType;
    private readonly string _realmScope = realmScope;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsPrincipal result = principal.Clone();
        if (result.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(result);
        }

        string? realmAccessValue = principal.FindFirst(_realmScope)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessValue))
        {
            return Task.FromResult(result);
        }

        using var realmAccess = JsonDocument.Parse(realmAccessValue);
        if (!realmAccess.RootElement.TryGetProperty("roles", out JsonElement roles))
        {
            return Task.FromResult(result);
        }

        foreach (JsonElement role in roles.EnumerateArray())
        {
            string? value = role.GetString();
            if (!string.IsNullOrWhiteSpace(value)
                && !identity.HasClaim(_roleClaimType, value))
            {
                identity.AddClaim(new Claim(_roleClaimType, value));
            }
        }

        return Task.FromResult(result);
    }
}
