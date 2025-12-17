//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;

/// <summary>
/// Observer interface for monitoring message consumption operations.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to observe message consumption lifecycle events.
/// This is useful for metrics collection, logging, alerting, and custom telemetry.
/// </para>
/// <para>
/// <strong>Lifecycle:</strong>
/// <list type="bullet">
/// <item>PreConsume - Called before a message is processed</item>
/// <item>PostConsume - Called after a message is successfully processed</item>
/// <item>ConsumeFault - Called when message processing fails</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MetricsConsumeObserver : IConsumeObserver
/// {
///     private readonly IMetrics _metrics;
///
///     public async Task PreConsumeAsync(ConsumeObserverContext context, CancellationToken ct)
///     {
///         _metrics.IncrementCounter("rabbitmq_messages_received_total",
///             new[] { ("message_type", context.MessageType), ("queue", context.QueueName) });
///     }
///
///     public async Task PostConsumeAsync(ConsumeObserverContext context, CancellationToken ct)
///     {
///         _metrics.RecordHistogram("rabbitmq_message_processing_duration_seconds",
///             context.Duration.TotalSeconds,
///             new[] { ("message_type", context.MessageType), ("queue", context.QueueName) });
///     }
///
///     public async Task ConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken ct)
///     {
///         _metrics.IncrementCounter("rabbitmq_messages_failed_total",
///             new[] { ("message_type", context.MessageType), ("error_type", exception.GetType().Name) });
///     }
/// }
/// </code>
/// </example>
public interface IConsumeObserver
{
    /// <summary>
    /// Called before a message is consumed/processed.
    /// </summary>
    /// <param name="context">The context containing message metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PreConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a message is successfully consumed/processed.
    /// </summary>
    /// <param name="context">The context containing message metadata and processing results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PostConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when message consumption fails with an exception.
    /// </summary>
    /// <param name="context">The context containing message metadata.</param>
    /// <param name="exception">The exception that occurred during processing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken cancellationToken = default);
}

/// <summary>
/// Observer interface for monitoring message publishing operations.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to observe message publishing lifecycle events.
/// This is useful for metrics collection, logging, alerting, and custom telemetry.
/// </para>
/// <para>
/// <strong>Lifecycle:</strong>
/// <list type="bullet">
/// <item>PrePublish - Called before a message is published</item>
/// <item>PostPublish - Called after a message is successfully published</item>
/// <item>PublishFault - Called when message publishing fails</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MetricsPublishObserver : IPublishObserver
/// {
///     private readonly IMetrics _metrics;
///
///     public async Task PrePublishAsync(PublishObserverContext context, CancellationToken ct)
///     {
///         // Track publish attempts
///     }
///
///     public async Task PostPublishAsync(PublishObserverContext context, CancellationToken ct)
///     {
///         _metrics.IncrementCounter("rabbitmq_messages_published_total",
///             new[] { ("message_type", context.MessageType), ("exchange", context.Exchange) });
///     }
///
///     public async Task PublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken ct)
///     {
///         _metrics.IncrementCounter("rabbitmq_messages_publish_failed_total",
///             new[] { ("message_type", context.MessageType), ("error_type", exception.GetType().Name) });
///     }
/// }
/// </code>
/// </example>
public interface IPublishObserver
{
    /// <summary>
    /// Called before a message is published.
    /// </summary>
    /// <param name="context">The context containing message metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PrePublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a message is successfully published.
    /// </summary>
    /// <param name="context">The context containing message metadata and publish results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PostPublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when message publishing fails with an exception.
    /// </summary>
    /// <param name="context">The context containing message metadata.</param>
    /// <param name="exception">The exception that occurred during publishing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken cancellationToken = default);
}

/// <summary>
/// Observer interface for monitoring send operations (direct queue sends).
/// </summary>
public interface ISendObserver
{
    /// <summary>
    /// Called before a message is sent to a specific queue.
    /// </summary>
    Task PreSendAsync(SendObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a message is successfully sent to a specific queue.
    /// </summary>
    Task PostSendAsync(SendObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when message sending fails with an exception.
    /// </summary>
    Task SendFaultAsync(SendObserverContext context, Exception exception, CancellationToken cancellationToken = default);
}

/// <summary>
/// Observer interface for monitoring connection lifecycle events.
/// </summary>
public interface IConnectionObserver
{
    /// <summary>
    /// Called when a connection is established.
    /// </summary>
    Task OnConnectedAsync(ConnectionObserverContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a connection is lost or closed.
    /// </summary>
    Task OnDisconnectedAsync(ConnectionObserverContext context, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a reconnection attempt is made.
    /// </summary>
    Task OnReconnectingAsync(ConnectionObserverContext context, int attemptNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a connection error occurs.
    /// </summary>
    Task OnConnectionErrorAsync(ConnectionObserverContext context, Exception exception, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for consume observer events.
/// </summary>
public class ConsumeObserverContext
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange name.
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Gets or sets the routing key.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the consumer tag.
    /// </summary>
    public string? ConsumerTag { get; set; }

    /// <summary>
    /// Gets or sets whether this is a redelivered message.
    /// </summary>
    public bool Redelivered { get; set; }

    /// <summary>
    /// Gets or sets the redelivery count.
    /// </summary>
    public int RedeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets the message payload size in bytes.
    /// </summary>
    public int PayloadSize { get; set; }

    /// <summary>
    /// Gets or sets the processing duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID (for multi-tenant scenarios).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the message headers.
    /// </summary>
    public IDictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets custom properties for the context.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}

/// <summary>
/// Context for publish observer events.
/// </summary>
public class PublishObserverContext
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target exchange name.
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing key.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the message payload size in bytes.
    /// </summary>
    public int PayloadSize { get; set; }

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public byte? Priority { get; set; }

    /// <summary>
    /// Gets or sets whether the message is persistent.
    /// </summary>
    public bool Persistent { get; set; }

    /// <summary>
    /// Gets or sets the message expiration/TTL.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the publishing duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when publishing started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when publishing ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID (for multi-tenant scenarios).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets whether publisher confirm was received.
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Gets or sets the message headers.
    /// </summary>
    public IDictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets custom properties for the context.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}

/// <summary>
/// Context for send observer events.
/// </summary>
public class SendObserverContext
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination queue name.
    /// </summary>
    public string DestinationQueue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message payload size in bytes.
    /// </summary>
    public int PayloadSize { get; set; }

    /// <summary>
    /// Gets or sets the sending duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when sending started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets custom properties for the context.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}

/// <summary>
/// Context for connection observer events.
/// </summary>
public class ConnectionObserverContext
{
    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string? VirtualHost { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets custom properties for the context.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}

