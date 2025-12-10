//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines the strategy for publishing notifications to multiple handlers.
/// </summary>
public enum NotificationPublishingStrategy
{
    /// <summary>
    /// Execute handlers sequentially, one after another.
    /// This is the safest option as it maintains order and simplifies debugging.
    /// If one handler throws an exception, subsequent handlers are not executed.
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute all handlers in parallel using Task.WhenAll.
    /// This is faster for independent handlers but order is not guaranteed.
    /// If any handler throws, the aggregate exception contains all failures.
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute all handlers regardless of failures (fire-and-forget pattern).
    /// Exceptions in one handler don't prevent other handlers from executing.
    /// Exceptions are logged but not thrown.
    /// </summary>
    ParallelNoWait,

    /// <summary>
    /// Execute handlers sequentially but continue even if one fails.
    /// All handlers are executed regardless of failures.
    /// Exceptions are collected and thrown as an aggregate exception at the end.
    /// </summary>
    SequentialContinueOnException
}

/// <summary>
/// Interface for implementing custom notification publishing strategies.
/// </summary>
/// <remarks>
/// Implement this interface to create custom publishing behavior for notifications.
/// </remarks>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers using the implementation's strategy.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="handlers">The handlers to invoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<IMediatorNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification;
}

/// <summary>
/// Sequential notification publisher - executes handlers one by one.
/// </summary>
public sealed class SequentialNotificationPublisher : INotificationPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<IMediatorNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}

/// <summary>
/// Parallel notification publisher - executes all handlers concurrently.
/// </summary>
public sealed class ParallelNotificationPublisher : INotificationPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<IMediatorNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification
    {
        var tasks = handlers.Select(handler => handler.Handle(notification, cancellationToken));
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Fire-and-forget notification publisher - starts all handlers but doesn't wait for completion.
/// </summary>
public sealed class ParallelNoWaitNotificationPublisher : INotificationPublisher
{
    private readonly Microsoft.Extensions.Logging.ILogger<ParallelNoWaitNotificationPublisher>? _logger;

    /// <summary>
    /// Creates a new instance of the ParallelNoWaitNotificationPublisher.
    /// </summary>
    /// <param name="logger">Optional logger for recording exceptions.</param>
    public ParallelNoWaitNotificationPublisher(Microsoft.Extensions.Logging.ILogger<ParallelNoWaitNotificationPublisher>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<IMediatorNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification
    {
        foreach (var handler in handlers)
        {
            // Fire and forget - don't await
            _ = ExecuteSafeAsync(handler, notification, cancellationToken);
        }
        return Task.CompletedTask;
    }

    private async Task ExecuteSafeAsync<TNotification>(
        IMediatorNotificationHandler<TNotification> handler,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification
    {
        try
        {
            await handler.Handle(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[Notification] Handler {HandlerType} failed for {NotificationType}: {Message}",
                handler.GetType().Name,
                typeof(TNotification).Name,
                ex.Message);
        }
    }
}

/// <summary>
/// Sequential notification publisher that continues execution even if a handler fails.
/// </summary>
public sealed class SequentialContinueOnExceptionPublisher : INotificationPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<IMediatorNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
        where TNotification : IMediatorNotification
    {
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(
                $"Multiple handlers failed for notification {typeof(TNotification).Name}",
                exceptions);
        }
    }
}

