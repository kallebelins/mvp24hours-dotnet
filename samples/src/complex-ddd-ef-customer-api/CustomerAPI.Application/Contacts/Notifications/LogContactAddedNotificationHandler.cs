using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Notifications;

/// <summary>
/// In-process handler: logs when a contact is added to a customer.
/// </summary>
public sealed class LogContactAddedNotificationHandler(ILogger<LogContactAddedNotificationHandler> logger)
    : IMediatorNotificationHandler<ContactAddedNotification>
{
    public Task Handle(ContactAddedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DomainEvent → contact ({ContactType}: {Description}) added to customer {CustomerId} ({CustomerName})",
            notification.ContactType,
            notification.Description,
            notification.CustomerId,
            notification.CustomerName);
        return Task.CompletedTask;
    }
}
