using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for users.
/// </summary>
public interface IKeycloakUserService
{
    Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetUsersAsync(
        string? search = null,
        string? username = null,
        string? email = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<UserRepresentation>> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<string>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> AssignRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> RemoveRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string userId,
        string clientUuid,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> AssignClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> RemoveClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> AddUserToGroupAsync(
        AddUserToGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);
}
