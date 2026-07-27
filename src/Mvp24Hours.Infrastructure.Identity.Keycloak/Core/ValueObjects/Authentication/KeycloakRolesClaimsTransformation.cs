using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// Transforms Keycloak realm and client roles and groups into authorization claims.
/// </summary>
public sealed class KeycloakRolesClaimsTransformation(
    IOptions<KeycloakAuthorizationOptions> options,
    ILogger<KeycloakRolesClaimsTransformation> logger) : IClaimsTransformation
{
    private readonly KeycloakAuthorizationOptions _options = options.Value;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsPrincipal result = new(
            principal.Identities.Select(identity => identity.Clone()));
        if (result.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(result);
        }

        AddRealmRoles(principal, identity);
        AddClientRoles(principal, identity);
        AddGroups(principal, identity);

        return Task.FromResult(result);
    }

    private void AddRealmRoles(ClaimsPrincipal principal, ClaimsIdentity identity)
    {
        string? realmAccessValue = principal.FindFirst(KeycloakClaimTypes.RealmAccess)?.Value;
        if (!TryParseJson(realmAccessValue, KeycloakClaimTypes.RealmAccess, out JsonDocument realmAccess))
        {
            return;
        }

        using (realmAccess)
        {
            if (realmAccess.RootElement.TryGetProperty("roles", out JsonElement roles))
            {
                AddStringArrayClaims(identity, roles, _options.RealmRoleClaimType);
            }
        }
    }

    private void AddClientRoles(ClaimsPrincipal principal, ClaimsIdentity identity)
    {
        string? resourceAccessValue = principal.FindFirst(KeycloakClaimTypes.ResourceAccess)?.Value;
        if (!TryParseJson(
                resourceAccessValue,
                KeycloakClaimTypes.ResourceAccess,
                out JsonDocument resourceAccess))
        {
            return;
        }

        using (resourceAccess)
        {
            IEnumerable<JsonProperty> clients = resourceAccess.RootElement.ValueKind == JsonValueKind.Object
                ? resourceAccess.RootElement.EnumerateObject()
                : [];

            foreach (JsonProperty client in clients)
            {
                if (!string.IsNullOrWhiteSpace(_options.ResourceServerClientId)
                    && !string.Equals(
                        client.Name,
                        _options.ResourceServerClientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (client.Value.ValueKind == JsonValueKind.Object
                    && client.Value.TryGetProperty("roles", out JsonElement roles))
                {
                    AddStringArrayClaims(identity, roles, _options.RealmRoleClaimType);
                }
            }
        }
    }

    private void AddGroups(ClaimsPrincipal principal, ClaimsIdentity identity)
    {
        foreach (Claim groupClaim in principal.FindAll(KeycloakClaimTypes.Groups).ToArray())
        {
            if (TryParseJson(groupClaim.Value, KeycloakClaimTypes.Groups, out JsonDocument groups))
            {
                using (groups)
                {
                    AddStringArrayClaims(identity, groups.RootElement, KeycloakClaimTypes.Groups);
                }
            }
            else if (!string.IsNullOrWhiteSpace(groupClaim.Value)
                && !identity.HasClaim(KeycloakClaimTypes.Groups, groupClaim.Value))
            {
                identity.AddClaim(new Claim(KeycloakClaimTypes.Groups, groupClaim.Value));
            }
        }
    }

    private bool TryParseJson(
        string? value,
        string claimType,
        out JsonDocument document)
    {
        document = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            document = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException exception)
        {
            logger.LogDebug(
                exception,
                "The Keycloak claim {ClaimType} does not contain JSON.",
                claimType);
            return false;
        }
    }

    private static void AddStringArrayClaims(
        ClaimsIdentity identity,
        JsonElement values,
        string claimType)
    {
        if (values.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in values.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!string.IsNullOrWhiteSpace(value) && !identity.HasClaim(claimType, value))
            {
                identity.AddClaim(new Claim(claimType, value));
            }
        }
    }
}
