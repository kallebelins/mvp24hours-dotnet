using System.Collections.Concurrent;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Repositories;

namespace CustomerAPI.Application.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of ICustomerRepository.
/// Sufficient for the saga teaching sample; replace with EF Core for production use.
/// </summary>
public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _store = new();

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _store[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out Customer? customer);
        return Task.FromResult(customer);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Customer> result = _store.Values.ToList().AsReadOnly();
        return Task.FromResult(result);
    }
}
