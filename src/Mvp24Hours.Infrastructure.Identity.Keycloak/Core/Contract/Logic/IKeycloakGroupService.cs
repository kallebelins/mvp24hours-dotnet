using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for groups.
/// </summary>
public interface IKeycloakGroupService
{
    /// <summary>Searches and pages realm groups.</summary>
    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetGroupsAsync(
        string? search = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a group by its Keycloak identifier.</summary>
    Task<IBusinessResult<GroupRepresentation>> GetGroupByIdAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a group and returns its identifier.</summary>
    Task<IBusinessResult<string>> CreateGroupAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a group.</summary>
    Task<IBusinessResult<bool>> UpdateGroupAsync(
        string groupId,
        GroupRepresentation group,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a group.</summary>
    Task<IBusinessResult<bool>> DeleteGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets direct child groups.</summary>
    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetSubGroupsAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a page of group members.</summary>
    Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetGroupMembersAsync(
        string groupId,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);
}
