namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// Standard OpenID Connect / Keycloak claim type names.
/// </summary>
public static class KeycloakClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string PreferredUserName = "preferred_username";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Role = "role";
    public const string RealmAccess = "realm_access";
    public const string ResourceAccess = "resource_access";
    public const string Scope = "scope";
    public const string SessionId = "sid";
    public const string SessionState = "session_state";
    public const string AuthorizedParty = "azp";
    public const string Groups = "groups";
}
