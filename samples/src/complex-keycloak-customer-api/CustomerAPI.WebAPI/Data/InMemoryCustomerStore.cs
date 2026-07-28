using CustomerAPI.Core.Entities;

namespace CustomerAPI.WebAPI.Data;

/// <summary>
/// Thread-safe in-memory store for customer data.
/// Replaces a real database to keep this sample focused on Keycloak identity.
/// </summary>
public sealed class InMemoryCustomerStore
{
    private readonly List<Customer> _customers =
    [
        new Customer { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Alice Smith", Email = "alice@example.com", Active = true },
        new Customer { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Bob Jones", Email = "bob@example.com", Active = true },
        new Customer { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Carol White", Email = "carol@example.com", Active = false }
    ];

    private readonly Lock _lock = new();

    public IReadOnlyList<Customer> GetAll()
    {
        lock (_lock)
        {
            return [.. _customers];
        }
    }

    public Customer? GetById(Guid id)
    {
        lock (_lock)
        {
            return _customers.Find(c => c.Id == id);
        }
    }

    public Customer Add(Customer customer)
    {
        lock (_lock)
        {
            _customers.Add(customer);
            return customer;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            Customer? existing = _customers.Find(c => c.Id == id);
            if (existing is null) return false;
            _customers.Remove(existing);
            return true;
        }
    }
}
