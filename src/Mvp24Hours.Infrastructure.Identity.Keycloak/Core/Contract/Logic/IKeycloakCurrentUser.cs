using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Provides the Keycloak user parsed for the current HTTP request.
/// </summary>
public interface IKeycloakCurrentUser
{
    /// <summary>
    /// Gets the parsed Keycloak user for the current request.
    /// </summary>
    UserToken? User { get; }

    /// <summary>
    /// Gets the Keycloak subject as a GUID when available.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets whether the current request has an authenticated Keycloak user.
    /// </summary>
    bool IsAuthenticated { get; }
}
