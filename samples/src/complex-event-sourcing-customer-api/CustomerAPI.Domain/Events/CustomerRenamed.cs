using Mvp24Hours.Core.Contract.Domain.Entity;

namespace CustomerAPI.Domain.Events;

/// <summary>
/// Raised when a customer's name is changed.
/// </summary>
public record CustomerRenamed : DomainEventBase
{
    public Guid CustomerId { get; init; }
    public string NewName { get; init; } = string.Empty;

    public CustomerRenamed() { }

    public CustomerRenamed(Guid customerId, string newName)
    {
        CustomerId = customerId;
        NewName = newName;
    }
}
