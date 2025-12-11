//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Test notification for order created event.
/// </summary>
public class OrderCreatedNotification : IMediatorNotification
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// First handler for OrderCreatedNotification.
/// </summary>
public class OrderCreatedEmailHandler : IMediatorNotificationHandler<OrderCreatedNotification>
{
    public static List<string> HandledNotifications { get; } = new();

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        HandledNotifications.Add($"Email sent for order {notification.OrderId} to {notification.CustomerName}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for OrderCreatedNotification.
/// </summary>
public class OrderCreatedAuditHandler : IMediatorNotificationHandler<OrderCreatedNotification>
{
    public static List<string> HandledNotifications { get; } = new();

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        HandledNotifications.Add($"Audit logged for order {notification.OrderId} with amount {notification.Amount}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Notification with no handlers (valid case).
/// </summary>
public class NoHandlerNotification : IMediatorNotification
{
    public string Message { get; set; } = string.Empty;
}

