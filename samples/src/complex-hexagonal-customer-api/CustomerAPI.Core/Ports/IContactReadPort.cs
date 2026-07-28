using CustomerAPI.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Core.Ports
{
    /// <summary>
    /// Outbound port for contact read operations.
    /// </summary>
    public interface IContactReadPort
    {
        Task<IList<Contact>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
