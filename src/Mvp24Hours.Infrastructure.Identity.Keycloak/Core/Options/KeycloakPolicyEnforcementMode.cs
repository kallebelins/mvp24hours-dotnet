namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

/// <summary>
/// Keycloak authorization policy enforcement mode.
/// </summary>
public enum KeycloakPolicyEnforcementMode
{
    Enforcing = 0,
    Permissive = 1,
    Disabled = 2
}
