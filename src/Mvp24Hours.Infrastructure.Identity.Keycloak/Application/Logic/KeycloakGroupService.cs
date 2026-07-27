using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Keycloak Admin REST API operations for groups.
/// </summary>
public sealed class KeycloakGroupService(KeycloakAdminHttpClient client) : IKeycloakGroupService
{
    public Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetGroupsAsync(
        string? search = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default)
    {
        if (first < 0 || max <= 0)
        {
            return Validation<IReadOnlyList<GroupRepresentation>>(
                "First cannot be negative and max must be greater than zero.");
        }

        string path = BuildQuery(
            "groups",
            ("search", search),
            ("first", first.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("max", max.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return client.GetAsync<IReadOnlyList<GroupRepresentation>>(path, cancellationToken);
    }

    public Task<IBusinessResult<GroupRepresentation>> GetGroupByIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(groupId)
            ? Validation<GroupRepresentation>("Group id is required.")
            : client.GetAsync<GroupRepresentation>(
                $"groups/{Escape(groupId)}",
                cancellationToken);
    }

    public Task<IBusinessResult<string>> CreateGroupAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.IsValid)
        {
            return Validation<string>(request.Validate());
        }

        GroupRepresentation group = new()
        {
            Name = request.Name,
            Attributes = request.Attributes,
            RealmRoles = request.RealmRoles,
            ClientRoles = request.ClientRoles
        };
        string path = string.IsNullOrWhiteSpace(request.ParentGroupId)
            ? "groups"
            : $"groups/{Escape(request.ParentGroupId)}/children";
        return client.PostForLocationAsync(path, group, cancellationToken);
    }

    public Task<IBusinessResult<bool>> UpdateGroupAsync(
        string groupId,
        GroupRepresentation group,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(groupId) || group is null
            ? Validation<bool>("Group id and group representation are required.")
            : client.PutAsync($"groups/{Escape(groupId)}", group, cancellationToken);
    }

    public Task<IBusinessResult<bool>> DeleteGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(groupId)
            ? Validation<bool>("Group id is required.")
            : client.DeleteAsync(
                $"groups/{Escape(groupId)}",
                cancellationToken: cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetSubGroupsAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(groupId)
            ? Validation<IReadOnlyList<GroupRepresentation>>("Group id is required.")
            : client.GetAsync<IReadOnlyList<GroupRepresentation>>(
                $"groups/{Escape(groupId)}/children",
                cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetGroupMembersAsync(
        string groupId,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Validation<IReadOnlyList<UserRepresentation>>("Group id is required.");
        }

        if (first < 0 || max <= 0)
        {
            return Validation<IReadOnlyList<UserRepresentation>>(
                "First cannot be negative and max must be greater than zero.");
        }

        string path = BuildQuery(
            $"groups/{Escape(groupId)}/members",
            ("first", first.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("max", max.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return client.GetAsync<IReadOnlyList<UserRepresentation>>(path, cancellationToken);
    }

    private static Task<IBusinessResult<T>> Validation<T>(string message)
    {
        return Task.FromResult(BusinessResult.Failure<T>(message, "KEYCLOAK_VALIDATION"));
    }

    private static Task<IBusinessResult<T>> Validation<T>(IEnumerable<string> messages)
    {
        return Task.FromResult(
            BusinessResult.Failure<T>(
                messages.Select(message => ("KEYCLOAK_VALIDATION", message))));
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value);
    }

    private static string BuildQuery(
        string path,
        params (string Name, string? Value)[] values)
    {
        string query = string.Join(
            "&",
            values
                .Where(value => !string.IsNullOrWhiteSpace(value.Value))
                .Select(value =>
                    $"{Uri.EscapeDataString(value.Name)}={Uri.EscapeDataString(value.Value!)}"));
        return string.IsNullOrEmpty(query) ? path : $"{path}?{query}";
    }
}
