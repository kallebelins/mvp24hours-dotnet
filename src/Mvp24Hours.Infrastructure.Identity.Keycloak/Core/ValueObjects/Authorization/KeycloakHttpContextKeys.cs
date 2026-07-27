namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

/// <summary>
/// Keys used to store Keycloak values in <c>HttpContext.Items</c>.
/// </summary>
public static class KeycloakHttpContextKeys
{
    public const string User = "KeycloakUser";
}
