using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// Represents a service that synchronizes Keycloak users with local users.
/// </summary>
public interface IUserKeycloakService
{
    Task<IBusinessResult<bool>> GetAnyLocalUserById(Guid id, CancellationToken cancellationToken = default);

    Task<IBusinessResult<object>> GetLocalIdById(Guid id, CancellationToken cancellationToken = default);

    Task<IBusinessResult<object>> GetLocalIdByEmail(string email, CancellationToken cancellationToken = default);

    Task<IBusinessResult<object>> CreateOrUpdateLocalUser(UserToken dto, CancellationToken cancellationToken = default);
}
