using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Provides the Keycloak user parsed for the current HTTP request.
/// </summary>
public interface IKeycloakCurrentUser
{
    UserToken? User { get; }

    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
