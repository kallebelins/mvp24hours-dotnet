using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Identity.Keycloak.Core.Contract.Logic
{
    /// <summary>
    /// Represents Keycloak User service
    /// </summary>
    public interface IUserKeycloakService
    {
        Task<IBusinessResult<bool>> GetAnyLocalUserById(Guid id, CancellationToken cancellationToken = default);
        Task<IBusinessResult<object>> GetLocalIdById(Guid id, CancellationToken cancellationToken = default);
        Task<IBusinessResult<object>> GetLocalIdByEmail(string email, CancellationToken cancellationToken = default);
        Task<IBusinessResult<object>> CreateOrUpdateLocalUser(UserToken dto, CancellationToken cancellationToken = default);
    }
}
