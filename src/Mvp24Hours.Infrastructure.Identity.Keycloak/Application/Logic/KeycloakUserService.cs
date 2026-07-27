using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Keycloak Admin REST API operations for users.
/// </summary>
public sealed class KeycloakUserService(KeycloakAdminHttpClient client) : IKeycloakUserService
{
    public Task<IBusinessResult<IReadOnlyList<UserRepresentation>>> GetUsersAsync(
        string? search = null,
        string? username = null,
        string? email = null,
        int first = 0,
        int max = 100,
        CancellationToken cancellationToken = default)
    {
        if (first < 0 || max <= 0)
        {
            return Validation<IReadOnlyList<UserRepresentation>>(
                "First cannot be negative and max must be greater than zero.");
        }

        string path = BuildQuery(
            "users",
            ("search", search),
            ("username", username),
            ("email", email),
            ("first", first.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("max", max.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return client.GetAsync<IReadOnlyList<UserRepresentation>>(path, cancellationToken);
    }

    public Task<IBusinessResult<UserRepresentation>> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Validation<UserRepresentation>("User id is required.")
            : client.GetAsync<UserRepresentation>($"users/{Escape(userId)}", cancellationToken);
    }

    public Task<IBusinessResult<string>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.IsValid)
        {
            return Validation<string>(request.Validate());
        }

        UserRepresentation representation = new()
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Enabled = request.Enabled,
            EmailVerified = request.EmailVerified,
            RequiredActions = request.RequiredActions,
            Attributes = request.Attributes,
            Groups = request.Groups,
            Credentials = string.IsNullOrWhiteSpace(request.TemporaryPassword)
                ? null
                :
                [
                    new CredentialRepresentation
                    {
                        Type = "password",
                        Value = request.TemporaryPassword,
                        Temporary = request.TemporaryPasswordRequired
                    }
                ]
        };
        return client.PostForLocationAsync("users", representation, cancellationToken);
    }

    public Task<IBusinessResult<bool>> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.IsValid)
        {
            return Validation<bool>(request.Validate());
        }

        Dictionary<string, object?> body = new()
        {
            ["username"] = request.Username,
            ["email"] = request.Email,
            ["firstName"] = request.FirstName,
            ["lastName"] = request.LastName,
            ["enabled"] = request.Enabled,
            ["emailVerified"] = request.EmailVerified,
            ["requiredActions"] = request.RequiredActions,
            ["attributes"] = request.Attributes
        };
        return client.PutAsync($"users/{Escape(request.UserId)}", body, cancellationToken);
    }

    public Task<IBusinessResult<bool>> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Validation<bool>("User id is required.")
            : client.DeleteAsync($"users/{Escape(userId)}", cancellationToken: cancellationToken);
    }

    public Task<IBusinessResult<bool>> SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Validation<bool>("User id is required.")
            : client.PutAsync(
                $"users/{Escape(userId)}",
                new Dictionary<string, bool> { ["enabled"] = enabled },
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.IsValid)
        {
            return Validation<bool>(request.Validate());
        }

        CredentialRepresentation credential = new()
        {
            Type = request.Type,
            Value = request.Password,
            Temporary = request.Temporary
        };
        return client.PutAsync(
            $"users/{Escape(request.UserId)}/reset-password",
            credential,
            cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Validation<IReadOnlyList<RoleRepresentation>>("User id is required.")
            : client.GetAsync<IReadOnlyList<RoleRepresentation>>(
                $"users/{Escape(userId)}/role-mappings/realm",
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> AssignRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeRolesAsync(request, client.PostAsync, false, cancellationToken);
    }

    public Task<IBusinessResult<bool>> RemoveRealmRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeRolesAsync(request, client.DeleteAsync, false, cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string userId,
        string clientUuid,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(clientUuid)
            ? Validation<IReadOnlyList<RoleRepresentation>>(
                "User id and client UUID are required.")
            : client.GetAsync<IReadOnlyList<RoleRepresentation>>(
                $"users/{Escape(userId)}/role-mappings/clients/{Escape(clientUuid)}",
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> AssignClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeRolesAsync(request, client.PostAsync, true, cancellationToken);
    }

    public Task<IBusinessResult<bool>> RemoveClientRolesAsync(
        AssignRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeRolesAsync(request, client.DeleteAsync, true, cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<GroupRepresentation>>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Validation<IReadOnlyList<GroupRepresentation>>("User id is required.")
            : client.GetAsync<IReadOnlyList<GroupRepresentation>>(
                $"users/{Escape(userId)}/groups",
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> AddUserToGroupAsync(
        AddUserToGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return !request.IsValid
            ? Validation<bool>(request.Validate())
            : client.PutAsync(
                $"users/{Escape(request.UserId)}/groups/{Escape(request.GroupId)}",
                null,
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(groupId)
            ? Validation<bool>("User id and group id are required.")
            : client.DeleteAsync(
                $"users/{Escape(userId)}/groups/{Escape(groupId)}",
                cancellationToken: cancellationToken);
    }

    private Task<IBusinessResult<bool>> ChangeRolesAsync(
        AssignRolesRequest request,
        Func<string, object?, CancellationToken, Task<IBusinessResult<bool>>> operation,
        bool clientRoles,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.IsValid)
        {
            return Validation<bool>(request.Validate());
        }

        if (clientRoles != !string.IsNullOrWhiteSpace(request.ClientUuid))
        {
            return Validation<bool>(
                clientRoles
                    ? "Client UUID is required for client role mappings."
                    : "Client UUID must be empty for realm role mappings.");
        }

        string suffix = clientRoles
            ? $"clients/{Escape(request.ClientUuid!)}"
            : "realm";
        return operation(
            $"users/{Escape(request.UserId)}/role-mappings/{suffix}",
            request.Roles,
            cancellationToken);
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
