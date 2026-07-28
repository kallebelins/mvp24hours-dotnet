using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Notifications;

/// <summary>
/// Demonstrates in-process notification handling after a successful create command.
/// </summary>
public sealed class LogCustomerCreatedNotificationHandler(ILogger<LogCustomerCreatedNotificationHandler> logger)
    : IMediatorNotificationHandler<CustomerCreatedNotification>
{
    public Task Handle(CustomerCreatedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DomainEvent → customer {CustomerId} ({CustomerName}) was created",
            notification.CustomerId,
            notification.CustomerName);
        return Task.CompletedTask;
    }
}
