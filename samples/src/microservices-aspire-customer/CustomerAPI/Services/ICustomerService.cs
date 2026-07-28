using CustomerAPI.Entities;
using CustomerAPI.Models;

namespace CustomerAPI.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
}
