using CustomerAPI.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Core.Ports
{
    /// <summary>
    /// Outbound port for contact write operations.
    /// </summary>
    public interface IContactWritePort
    {
        Task<Contact> AddAsync(Contact contact, CancellationToken cancellationToken = default);
        Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default);
        Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default);
    }
}
