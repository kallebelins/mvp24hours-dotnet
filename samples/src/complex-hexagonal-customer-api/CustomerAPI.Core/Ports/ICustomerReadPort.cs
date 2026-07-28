using CustomerAPI.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Core.Ports
{
    /// <summary>
    /// Outbound port — driven by the application for customer read operations.
    /// Implementations live in CustomerAPI.Infrastructure (EF adapter).
    /// </summary>
    public interface ICustomerReadPort
    {
        Task<IList<Customer>> GetAllAsync(string? name, bool? active, bool hasEmailContact = false, CancellationToken cancellationToken = default);
        Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    }
}
