using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Keycloak Admin API operations for groups.
/// </summary>
public interface IKeycloakGroupService
{
    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetGroupsAsync(
        string? search = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<GroupRepresentation>> GetGroupByIdAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<string>> CreateGroupAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> UpdateGroupAsync(
        string groupId,
        GroupRepresentation group,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<bool>> DeleteGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetSubGroupsAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetGroupMembersAsync(
        string groupId,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default);
}
