using CustomerAPI.Core.ValueObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Core.Ports
{
    /// <summary>
    /// Outbound port for fetching external user profiles.
    /// Implemented by the Typicode HTTP adapter in CustomerAPI.Infrastructure.
    /// </summary>
    public interface IExternalProfilePort
    {
        Task<IList<ExternalProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
        Task<ExternalProfile?> GetProfileByIdAsync(int externalId, CancellationToken cancellationToken = default);
    }
}
