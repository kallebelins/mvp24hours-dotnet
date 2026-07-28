using CustomerAPI.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Core.Ports
{
    /// <summary>
    /// Outbound port — driven by the application for customer write operations.
    /// Implementations live in CustomerAPI.Infrastructure (EF adapter).
    /// </summary>
    public interface ICustomerWritePort
    {
        Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
        Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
        Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default);
    }
}
