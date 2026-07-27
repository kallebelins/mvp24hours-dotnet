using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Base service for synchronizing Keycloak users with an application-owned user store.
/// </summary>
/// <remarks>
/// Derived classes implement the four local persistence operations. This base class implements
/// synchronization from the Keycloak Admin API.
/// </remarks>
public abstract class UserKeycloakSyncService(
    IKeycloakUserService keycloakUserService,
    ILogger<UserKeycloakSyncService> logger) : IUserKeycloakService
{
    public abstract Task<IBusinessResult<bool>> GetAnyLocalUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    public abstract Task<IBusinessResult<object>> GetLocalIdByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    public abstract Task<IBusinessResult<object>> GetLocalIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    public abstract Task<IBusinessResult<object>> CreateOrUpdateLocalUserAsync(
        UserToken dto,
        CancellationToken cancellationToken = default);

    public async Task<IBusinessResult<object>> SyncLocalUserFromKeycloakAsync(
        Guid keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        IBusinessResult<UserRepresentation> keycloakResult =
            await keycloakUserService.GetUserByIdAsync(
                keycloakUserId.ToString(),
                cancellationToken);

        if (keycloakResult.HasErrors)
        {
            logger.LogWarning(
                "Unable to load Keycloak user {KeycloakUserId} for local synchronization.",
                keycloakUserId);

            return keycloakResult.Messages is { Count: > 0 }
                ? BusinessResult.Failure<object>(keycloakResult.Messages, keycloakResult.Token)
                : BusinessResult.Failure<object>(
                    "Unable to load the Keycloak user.",
                    "KEYCLOAK_USER_LOOKUP_FAILED",
                    keycloakResult.Token);
        }

        if (keycloakResult.Data is null)
        {
            return BusinessResult.Failure<object>(
                $"Keycloak user '{keycloakUserId}' was not found.",
                "KEYCLOAK_USER_NOT_FOUND",
                keycloakResult.Token);
        }

        UserToken user = MapUser(keycloakResult.Data, keycloakUserId);
        return await CreateOrUpdateLocalUserAsync(user, cancellationToken);
    }

    private static UserToken MapUser(UserRepresentation user, Guid fallbackId)
    {
        Guid id = Guid.TryParse(user.Id, out Guid parsedId) ? parsedId : fallbackId;
        string? name = BuildName(user.FirstName, user.LastName);

        return new UserToken
        {
            Id = id,
            Name = name,
            PreferredUserName = user.Username,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            RealmRoles = user.RealmRoles,
            ClientRoles = user.ClientRoles,
            ResourceRoles = user.ClientRoles?
                .SelectMany(item => item.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Groups = user.Groups,
            Attributes = user.Attributes
        };
    }

    private static string? BuildName(string? firstName, string? lastName)
    {
        string name = string.Join(
            ' ',
            new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
