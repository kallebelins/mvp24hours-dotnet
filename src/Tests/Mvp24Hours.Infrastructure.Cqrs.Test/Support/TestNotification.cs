//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

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
/// Uses <see cref="AsyncLocal{T}"/> so parallel test classes (e.g. BenchmarkTest)
/// do not pollute notification assertions.
/// </summary>
public class OrderCreatedEmailHandler : IMediatorNotificationHandler<OrderCreatedNotification>
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>
    /// Starts capturing handled notifications for the current async flow.
    /// </summary>
    public static List<string> BeginCapture()
    {
        var list = new List<string>();
        Current.Value = list;
        return list;
    }

    /// <summary>
    /// Stops capturing for the current async flow.
    /// </summary>
    public static void EndCapture()
    {
        Current.Value = null;
    }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        Current.Value?.Add($"Email sent for order {notification.OrderId} to {notification.CustomerName}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for OrderCreatedNotification.
/// Uses <see cref="AsyncLocal{T}"/> so parallel test classes do not pollute assertions.
/// </summary>
public class OrderCreatedAuditHandler : IMediatorNotificationHandler<OrderCreatedNotification>
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>
    /// Starts capturing handled notifications for the current async flow.
    /// </summary>
    public static List<string> BeginCapture()
    {
        var list = new List<string>();
        Current.Value = list;
        return list;
    }

    /// <summary>
    /// Stops capturing for the current async flow.
    /// </summary>
    public static void EndCapture()
    {
        Current.Value = null;
    }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        Current.Value?.Add($"Audit logged for order {notification.OrderId} with amount {notification.Amount}");
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
