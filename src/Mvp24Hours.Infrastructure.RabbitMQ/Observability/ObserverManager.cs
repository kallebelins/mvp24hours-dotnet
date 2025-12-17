//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Manages and coordinates all RabbitMQ observers for centralized observability.
/// </summary>
/// <remarks>
/// <para>
/// This class acts as a coordinator for all registered observers, ensuring that
/// observer events are dispatched correctly and errors in observers don't affect
/// message processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register observers
/// services.AddSingleton&lt;IConsumeObserver, MetricsConsumeObserver&gt;();
/// services.AddSingleton&lt;IPublishObserver, LoggingPublishObserver&gt;();
///
/// // The ObserverManager will automatically coordinate all registered observers
/// </code>
/// </example>
public interface IObserverManager
{
    /// <summary>
    /// Notifies all consume observers of a pre-consume event.
    /// </summary>
    Task NotifyPreConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all consume observers of a post-consume event.
    /// </summary>
    Task NotifyPostConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all consume observers of a consume fault event.
    /// </summary>
    Task NotifyConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all publish observers of a pre-publish event.
    /// </summary>
    Task NotifyPrePublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all publish observers of a post-publish event.
    /// </summary>
    Task NotifyPostPublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all publish observers of a publish fault event.
    /// </summary>
    Task NotifyPublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all send observers of a pre-send event.
    /// </summary>
    Task NotifyPreSendAsync(SendObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all send observers of a post-send event.
    /// </summary>
    Task NotifyPostSendAsync(SendObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all send observers of a send fault event.
    /// </summary>
    Task NotifySendFaultAsync(SendObserverContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all connection observers of a connected event.
    /// </summary>
    Task NotifyConnectedAsync(ConnectionObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all connection observers of a disconnected event.
    /// </summary>
    Task NotifyDisconnectedAsync(ConnectionObserverContext context, string? reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of the observer manager.
/// </summary>
public class ObserverManager : IObserverManager
{
    private readonly IEnumerable<IConsumeObserver> _consumeObservers;
    private readonly IEnumerable<IPublishObserver> _publishObservers;
    private readonly IEnumerable<ISendObserver> _sendObservers;
    private readonly IEnumerable<IConnectionObserver> _connectionObservers;
    private readonly ILogger<ObserverManager>? _logger;

    /// <summary>
    /// Creates a new ObserverManager with the specified observers.
    /// </summary>
    public ObserverManager(
        IEnumerable<IConsumeObserver>? consumeObservers = null,
        IEnumerable<IPublishObserver>? publishObservers = null,
        IEnumerable<ISendObserver>? sendObservers = null,
        IEnumerable<IConnectionObserver>? connectionObservers = null,
        ILogger<ObserverManager>? logger = null)
    {
        _consumeObservers = consumeObservers ?? Enumerable.Empty<IConsumeObserver>();
        _publishObservers = publishObservers ?? Enumerable.Empty<IPublishObserver>();
        _sendObservers = sendObservers ?? Enumerable.Empty<ISendObserver>();
        _connectionObservers = connectionObservers ?? Enumerable.Empty<IConnectionObserver>();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyPreConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _consumeObservers)
        {
            try
            {
                await observer.PreConsumeAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IConsumeObserver), nameof(observer.PreConsumeAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPostConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _consumeObservers)
        {
            try
            {
                await observer.PostConsumeAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IConsumeObserver), nameof(observer.PostConsumeAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _consumeObservers)
        {
            try
            {
                await observer.ConsumeFaultAsync(context, exception, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IConsumeObserver), nameof(observer.ConsumeFaultAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPrePublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _publishObservers)
        {
            try
            {
                await observer.PrePublishAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IPublishObserver), nameof(observer.PrePublishAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPostPublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _publishObservers)
        {
            try
            {
                await observer.PostPublishAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IPublishObserver), nameof(observer.PostPublishAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _publishObservers)
        {
            try
            {
                await observer.PublishFaultAsync(context, exception, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IPublishObserver), nameof(observer.PublishFaultAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPreSendAsync(SendObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _sendObservers)
        {
            try
            {
                await observer.PreSendAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(ISendObserver), nameof(observer.PreSendAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyPostSendAsync(SendObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _sendObservers)
        {
            try
            {
                await observer.PostSendAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(ISendObserver), nameof(observer.PostSendAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifySendFaultAsync(SendObserverContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _sendObservers)
        {
            try
            {
                await observer.SendFaultAsync(context, exception, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(ISendObserver), nameof(observer.SendFaultAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyConnectedAsync(ConnectionObserverContext context, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _connectionObservers)
        {
            try
            {
                await observer.OnConnectedAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IConnectionObserver), nameof(observer.OnConnectedAsync), ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyDisconnectedAsync(ConnectionObserverContext context, string? reason, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _connectionObservers)
        {
            try
            {
                await observer.OnDisconnectedAsync(context, reason, cancellationToken);
            }
            catch (Exception ex)
            {
                LogObserverError(nameof(IConnectionObserver), nameof(observer.OnDisconnectedAsync), ex);
            }
        }
    }

    private void LogObserverError(string observerType, string methodName, Exception exception)
    {
        _logger?.LogWarning(exception,
            "Observer error in {ObserverType}.{MethodName}: {ErrorMessage}",
            observerType, methodName, exception.Message);
    }
}

/// <summary>
/// A no-op observer manager that does nothing (for scenarios where observers are not configured).
/// </summary>
public class NullObserverManager : IObserverManager
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullObserverManager Instance = new();

    private NullObserverManager() { }

    /// <inheritdoc />
    public Task NotifyPreConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPostConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPrePublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPostPublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPreSendAsync(SendObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyPostSendAsync(SendObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifySendFaultAsync(SendObserverContext context, Exception exception, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyConnectedAsync(ConnectionObserverContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyDisconnectedAsync(ConnectionObserverContext context, string? reason, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

