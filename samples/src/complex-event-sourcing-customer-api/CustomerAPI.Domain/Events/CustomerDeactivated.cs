using Mvp24Hours.Core.Contract.Domain.Entity;

namespace CustomerAPI.Domain.Events;

/// <summary>
/// Raised when a customer account is deactivated.
/// </summary>
public record CustomerDeactivated : DomainEventBase
{
    public Guid CustomerId { get; init; }

    public CustomerDeactivated() { }

    public CustomerDeactivated(Guid customerId)
    {
        CustomerId = customerId;
    }
}
