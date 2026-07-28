using CustomerAPI.Core.Enums;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Notifications;

/// <summary>
/// In-process notification dispatched after a Contact is successfully added to a Customer aggregate.
/// Maps from <c>ContactAddedDomainEvent</c>.
/// </summary>
public sealed record ContactAddedNotification(
    int CustomerId,
    string CustomerName,
    ContactType ContactType,
    string Description) : IMediatorNotification;
