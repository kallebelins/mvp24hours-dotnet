using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Synchronizes Keycloak users with the application's local user store.
/// </summary>
public interface IUserKeycloakService
{
    /// <summary>
    /// Returns whether a local user exists for the given Keycloak subject id.
    /// </summary>
    Task<IBusinessResult<bool>> GetAnyLocalUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the local user identifier from a Keycloak subject id.
    /// </summary>
    Task<IBusinessResult<object>> GetLocalIdByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the local user identifier from email.
    /// </summary>
    Task<IBusinessResult<object>> GetLocalIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the local user from Keycloak token claims.
    /// </summary>
    Task<IBusinessResult<object>> CreateOrUpdateLocalUserAsync(
        UserToken dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the local user by loading Admin API data for the subject.
    /// </summary>
    Task<IBusinessResult<object>> SyncLocalUserFromKeycloakAsync(
        Guid keycloakUserId,
        CancellationToken cancellationToken = default);
}
