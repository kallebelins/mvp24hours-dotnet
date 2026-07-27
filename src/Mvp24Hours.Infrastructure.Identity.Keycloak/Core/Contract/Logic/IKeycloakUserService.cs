using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for users.
/// </summary>
public interface IKeycloakUserService
{
    /// <summary>Searches and pages realm users.</summary>
    Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetUsersAsync(
        string? search = null,
        string? username = null,
        string? email = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a user by its Keycloak identifier.</summary>
    Task<IBusinessResult<UserRepresentation>> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a realm user and returns its identifier.</summary>
    Task<IBusinessResult<string>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a realm user.</summary>
    Task<IBusinessResult<bool>> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a realm user.</summary>
    Task<IBusinessResult<bool>> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Enables or disables a realm user.</summary>
    Task<IBusinessResult<bool>> SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>Resets a user's password.</summary>
    Task<IBusinessResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets realm roles assigned to a user.</summary>
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns realm roles to a user.</summary>
    Task<IBusinessResult<bool>> AssignRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Removes realm roles from a user.</summary>
    Task<IBusinessResult<bool>> RemoveRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets client roles assigned to a user.</summary>
    Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string userId,
        string clientUuid,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns client roles to a user.</summary>
    Task<IBusinessResult<bool>> AssignClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Removes client roles from a user.</summary>
    Task<IBusinessResult<bool>> RemoveClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets groups to which a user belongs.</summary>
    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a user to a group.</summary>
    Task<IBusinessResult<bool>> AddUserToGroupAsync(
        AddUserToGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a user from a group.</summary>
    Task<IBusinessResult<bool>> RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);
}
