using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for realm and client roles.
/// </summary>
public interface IKeycloakRoleService
{
    /// <summary>Gets all realm roles.</summary>
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Gets a realm role by name.</summary>
    Task<IBusinessResult<RoleRepresentation>> GetRealmRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a realm role.</summary>
    Task<IBusinessResult<RoleRepresentation>> CreateRealmRoleAsync(
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a realm role.</summary>
    Task<IBusinessResult<bool>> UpdateRealmRoleAsync(
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a realm role.</summary>
    Task<IBusinessResult<bool>> DeleteRealmRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all roles for a client.</summary>
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string clientUuid,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a client role by name.</summary>
    Task<IBusinessResult<RoleRepresentation>> GetClientRoleByNameAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a client role.</summary>
    Task<IBusinessResult<RoleRepresentation>> CreateClientRoleAsync(
        string clientUuid,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a client role.</summary>
    Task<IBusinessResult<bool>> UpdateClientRoleAsync(
        string clientUuid,
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a client role.</summary>
    Task<IBusinessResult<bool>> DeleteClientRoleAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the composite roles of a realm role.</summary>
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRoleCompositesAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Adds composite roles to a realm role.</summary>
    Task<IBusinessResult<bool>> AddRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default);

    /// <summary>Removes composite roles from a realm role.</summary>
    Task<IBusinessResult<bool>> RemoveRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default);
}
