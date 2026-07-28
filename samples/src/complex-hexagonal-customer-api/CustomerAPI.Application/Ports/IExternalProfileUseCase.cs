using CustomerAPI.Application.DTOs.ExternalProfiles;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Ports
{
    /// <summary>
    /// Inbound (driving) port — exposes external profile enrichment use cases to the HTTP adapter.
    /// Implemented by <see cref="UseCases.ExternalProfileUseCase"/>.
    /// </summary>
    public interface IExternalProfileUseCase
    {
        Task<IBusinessResult<IList<ExternalProfileResult>>> GetProfilesAsync(CancellationToken cancellationToken = default);
        Task<IBusinessResult<ExternalProfileResult>> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
