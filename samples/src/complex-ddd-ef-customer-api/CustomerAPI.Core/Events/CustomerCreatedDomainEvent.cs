using Mvp24Hours.Core.Contract.Domain.Entity;

namespace CustomerAPI.Core.Events
{
    /// <summary>
    /// Domain event raised when a new customer is successfully created.
    /// Subscribers may send welcome e-mails, audit logs, etc.
    /// </summary>
    public sealed record CustomerCreatedDomainEvent(int CustomerId, string CustomerName) : DomainEventBase;
}
