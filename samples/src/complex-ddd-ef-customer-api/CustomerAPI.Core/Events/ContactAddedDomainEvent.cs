using CustomerAPI.Core.Enums;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace CustomerAPI.Core.Events
{
    /// <summary>
    /// Domain event raised when a contact is added to a customer aggregate.
    /// </summary>
    public sealed record ContactAddedDomainEvent(
        int CustomerId,
        string CustomerName,
        ContactType ContactType,
        string Description) : DomainEventBase;
}
