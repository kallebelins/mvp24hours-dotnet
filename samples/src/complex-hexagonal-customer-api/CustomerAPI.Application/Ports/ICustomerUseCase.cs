using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Application.DTOs.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Ports
{
    /// <summary>
    /// Inbound (driving) port — exposes customer use cases to the HTTP adapter.
    /// Implemented by <see cref="UseCases.CustomerUseCase"/>.
    /// </summary>
    public interface ICustomerUseCase
    {
        Task<IBusinessResult<IList<CustomerResult>>> GetCustomersAsync(CustomerQuery query, CancellationToken cancellationToken = default);
        Task<IBusinessResult<CustomerIdResult>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IBusinessResult<int>> CreateCustomerAsync(CustomerCreate dto, CancellationToken cancellationToken = default);
        Task<IBusinessResult<bool>> UpdateCustomerAsync(int id, CustomerUpdate dto, CancellationToken cancellationToken = default);
        Task<IBusinessResult<bool>> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);

        Task<IBusinessResult<IList<ContactResult>>> GetContactsAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IBusinessResult<int>> CreateContactAsync(int customerId, ContactCreate dto, CancellationToken cancellationToken = default);
        Task<IBusinessResult<bool>> UpdateContactAsync(int customerId, int contactId, ContactUpdate dto, CancellationToken cancellationToken = default);
        Task<IBusinessResult<bool>> DeleteContactAsync(int customerId, int contactId, CancellationToken cancellationToken = default);
    }
}
