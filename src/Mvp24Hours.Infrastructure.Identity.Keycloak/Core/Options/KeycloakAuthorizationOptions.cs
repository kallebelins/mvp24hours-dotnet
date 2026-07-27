using System.Security.Claims;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

/// <summary>
/// Configuration for Keycloak UMA / RPT / role-based authorization.
/// Default configuration section: <c>Keycloak:Authorization</c>.
/// </summary>
public class KeycloakAuthorizationOptions
{
    public const string SectionName = "Keycloak:Authorization";

    public KeycloakPolicyEnforcementMode PolicyEnforcementMode { get; set; }
        = KeycloakPolicyEnforcementMode.Enforcing;

    public bool UseDecisionEndpoint { get; set; } = true;

    public bool UseRptEndpoint { get; set; }

    public string PermissionClaimType { get; set; } = "permissions";

    public string RealmRoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// Resource server client id used when mapping <c>resource_access</c> roles.
    /// </summary>
    public string? ResourceServerClientId { get; set; }

    /// <summary>
    /// Validates required settings and returns a list of error messages (empty when valid).
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(PermissionClaimType))
        {
            errors.Add($"{nameof(PermissionClaimType)} is required.");
        }

        if (string.IsNullOrWhiteSpace(RealmRoleClaimType))
        {
            errors.Add($"{nameof(RealmRoleClaimType)} is required.");
        }

        if (UseDecisionEndpoint && UseRptEndpoint)
        {
            errors.Add(
                $"{nameof(UseDecisionEndpoint)} and {nameof(UseRptEndpoint)} cannot both be true; choose one enforcement path.");
        }

        if (UseRptEndpoint && string.IsNullOrWhiteSpace(ResourceServerClientId))
        {
            errors.Add(
                $"{nameof(ResourceServerClientId)} is required when {nameof(UseRptEndpoint)} is true.");
        }

        if (!Enum.IsDefined(PolicyEnforcementMode))
        {
            errors.Add($"{nameof(PolicyEnforcementMode)} has an invalid value.");
        }

        return errors;
    }
}
