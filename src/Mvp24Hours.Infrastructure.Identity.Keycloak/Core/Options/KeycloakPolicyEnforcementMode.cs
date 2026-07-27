namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

/// <summary>
/// Keycloak authorization policy enforcement mode.
/// </summary>
public enum KeycloakPolicyEnforcementMode
{
    /// <summary>
    /// Fail authorization when no matching permission is found.
    /// </summary>
    Enforcing = 0,

    /// <summary>
    /// Allow the request when permission evaluation fails.
    /// </summary>
    Permissive = 1,

    /// <summary>
    /// Skip Keycloak permission evaluation and succeed.
    /// </summary>
    Disabled = 2
}
