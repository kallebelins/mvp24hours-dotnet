using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for realm and client roles.
/// </summary>
public interface IKeycloakRoleService
{
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<RoleRepresentation>> GetRealmRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<RoleRepresentation>> CreateRealmRoleAsync(
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> UpdateRealmRoleAsync(
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> DeleteRealmRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string clientUuid,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<RoleRepresentation>> GetClientRoleByNameAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<RoleRepresentation>> CreateClientRoleAsync(
        string clientUuid,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> UpdateClientRoleAsync(
        string clientUuid,
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> DeleteClientRoleAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRoleCompositesAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> AddRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> RemoveRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default);
}
