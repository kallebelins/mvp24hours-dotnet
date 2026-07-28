using Mvp24Hours.Core.Contract.Domain.Entity;

namespace CustomerAPI.Domain.Events;

/// <summary>
/// Raised when a new customer is created.
/// </summary>
public record CustomerCreated : DomainEventBase
{
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    // Parameterless constructor required for JSON deserialization by InMemoryEventStore
    public CustomerCreated() { }

    public CustomerCreated(Guid customerId, string name, string email)
    {
        CustomerId = customerId;
        Name = name;
        Email = email;
    }
}
