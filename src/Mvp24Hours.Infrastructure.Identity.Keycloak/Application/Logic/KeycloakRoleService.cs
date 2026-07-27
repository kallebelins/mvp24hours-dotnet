using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Keycloak Admin REST API operations for realm and client roles.
/// </summary>
public sealed class KeycloakRoleService(KeycloakAdminHttpClient client) : IKeycloakRoleService
{
    public Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return client.GetAsync<IReadOnlyList<RoleRepresentation>>("roles", cancellationToken);
    }

    public Task<IBusinessResult<RoleRepresentation>> GetRealmRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(roleName)
            ? Validation<RoleRepresentation>("Role name is required.")
            : client.GetAsync<RoleRepresentation>(
                $"roles/{Escape(roleName)}",
                cancellationToken);
    }

    public async Task<IBusinessResult<RoleRepresentation>> CreateRealmRoleAsync(
        RoleRepresentation role,
        CancellationToken cancellationToken = default)
    {
        IBusinessResult<bool>? validation = ValidateRole(role);
        if (validation is not null)
        {
            return Failure<RoleRepresentation>(validation);
        }

        IBusinessResult<bool> created = await client.PostAsync("roles", role, cancellationToken);
        return created.HasErrors
            ? Failure<RoleRepresentation>(created)
            : await GetRealmRoleByNameAsync(role.Name!, cancellationToken);
    }

    public Task<IBusinessResult<bool>> UpdateRealmRoleAsync(
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync($"roles/{Escape(roleName)}", roleName, role, cancellationToken);
    }

    public Task<IBusinessResult<bool>> DeleteRealmRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(roleName)
            ? Validation<bool>("Role name is required.")
            : client.DeleteAsync(
                $"roles/{Escape(roleName)}",
                cancellationToken: cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetClientRolesAsync(
        string clientUuid,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(clientUuid)
            ? Validation<IReadOnlyList<RoleRepresentation>>("Client UUID is required.")
            : client.GetAsync<IReadOnlyList<RoleRepresentation>>(
                $"clients/{Escape(clientUuid)}/roles",
                cancellationToken);
    }

    public Task<IBusinessResult<RoleRepresentation>> GetClientRoleByNameAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(clientUuid) || string.IsNullOrWhiteSpace(roleName)
            ? Validation<RoleRepresentation>("Client UUID and role name are required.")
            : client.GetAsync<RoleRepresentation>(
                $"clients/{Escape(clientUuid)}/roles/{Escape(roleName)}",
                cancellationToken);
    }

    public async Task<IBusinessResult<RoleRepresentation>> CreateClientRoleAsync(
        string clientUuid,
        RoleRepresentation role,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientUuid))
        {
            return await Validation<RoleRepresentation>("Client UUID is required.");
        }

        IBusinessResult<bool>? validation = ValidateRole(role);
        if (validation is not null)
        {
            return Failure<RoleRepresentation>(validation);
        }

        IBusinessResult<bool> created = await client.PostAsync(
            $"clients/{Escape(clientUuid)}/roles",
            role,
            cancellationToken);
        return created.HasErrors
            ? Failure<RoleRepresentation>(created)
            : await GetClientRoleByNameAsync(clientUuid, role.Name!, cancellationToken);
    }

    public Task<IBusinessResult<bool>> UpdateClientRoleAsync(
        string clientUuid,
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(clientUuid)
            ? Validation<bool>("Client UUID is required.")
            : UpdateAsync(
                $"clients/{Escape(clientUuid)}/roles/{Escape(roleName)}",
                roleName,
                role,
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> DeleteClientRoleAsync(
        string clientUuid,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(clientUuid) || string.IsNullOrWhiteSpace(roleName)
            ? Validation<bool>("Client UUID and role name are required.")
            : client.DeleteAsync(
                $"clients/{Escape(clientUuid)}/roles/{Escape(roleName)}",
                cancellationToken: cancellationToken);
    }

    public Task<IBusinessResult<IReadOnlyList<RoleRepresentation>>> GetRealmRoleCompositesAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(roleName)
            ? Validation<IReadOnlyList<RoleRepresentation>>("Role name is required.")
            : client.GetAsync<IReadOnlyList<RoleRepresentation>>(
                $"roles/{Escape(roleName)}/composites",
                cancellationToken);
    }

    public Task<IBusinessResult<bool>> AddRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default)
    {
        return ChangeCompositesAsync(roleName, composites, client.PostAsync, cancellationToken);
    }

    public Task<IBusinessResult<bool>> RemoveRealmRoleCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        CancellationToken cancellationToken = default)
    {
        return ChangeCompositesAsync(roleName, composites, client.DeleteAsync, cancellationToken);
    }

    private async Task<IBusinessResult<bool>> UpdateAsync(
        string path,
        string roleName,
        RoleRepresentation role,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return await Validation<bool>("Role name is required.");
        }

        IBusinessResult<bool>? validation = ValidateRole(role);
        return validation ?? await client.PutAsync(path, role, cancellationToken);
    }

    private Task<IBusinessResult<bool>> ChangeCompositesAsync(
        string roleName,
        IReadOnlyList<RoleRepresentation> composites,
        Func<string, object?, CancellationToken, Task<IBusinessResult<bool>>> operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Validation<bool>("Role name is required.");
        }

        if (composites is null || composites.Count == 0)
        {
            return Validation<bool>("At least one composite role is required.");
        }

        return operation(
            $"roles/{Escape(roleName)}/composites",
            composites,
            cancellationToken);
    }

    private static IBusinessResult<bool>? ValidateRole(RoleRepresentation role)
    {
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            return BusinessResult.Failure<bool>(
                "Role name is required.",
                "KEYCLOAK_VALIDATION");
        }

        return null;
    }

    private static IBusinessResult<T> Failure<T>(IBusinessResult<bool> result)
    {
        return result.Messages is { Count: > 0 }
            ? BusinessResult.Failure<T>(result.Messages, result.Token)
            : BusinessResult.Failure<T>("Keycloak role operation failed.", token: result.Token);
    }

    private static Task<IBusinessResult<T>> Validation<T>(string message)
    {
        return Task.FromResult(BusinessResult.Failure<T>(message, "KEYCLOAK_VALIDATION"));
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value);
    }
}
