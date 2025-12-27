//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Enhanced structured logger for RabbitMQ that includes message envelope details.
/// </summary>
/// <remarks>
/// <para>
/// This logger provides rich, structured logging for RabbitMQ operations including:
/// <list type="bullet">
/// <item>Message envelope details (MessageId, CorrelationId, CausationId)</item>
/// <item>Trace context propagation (TraceId, SpanId)</item>
/// <item>Tenant and user information</item>
/// <item>Timing and performance data</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logger = new EnhancedStructuredLogger(loggerFactory.CreateLogger&lt;EnhancedStructuredLogger&gt;());
///
/// logger.LogMessagePublished(new MessageEnvelope
/// {
///     MessageId = "msg-123",
///     CorrelationId = "corr-456",
///     Exchange = "orders",
///     RoutingKey = "order.created",
///     MessageType = "OrderCreatedEvent",
///     PayloadSize = 1024,
///     TenantId = "tenant-abc"
/// }, TimeSpan.FromMilliseconds(5));
/// </code>
/// </example>
public class EnhancedStructuredLogger : IRabbitMQStructuredLogger
{
    private readonly ILogger<EnhancedStructuredLogger> _logger;
    private readonly bool _logPayload;
    private readonly int _maxPayloadLength;
    private readonly HashSet<string> _sensitiveHeaders;

    /// <summary>
    /// Creates a new EnhancedStructuredLogger.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="logPayload">Whether to log message payloads (use with caution in production).</param>
    /// <param name="maxPayloadLength">Maximum payload length to log.</param>
    /// <param name="sensitiveHeaders">Headers that should be masked in logs.</param>
    public EnhancedStructuredLogger(
        ILogger<EnhancedStructuredLogger> logger,
        bool logPayload = false,
        int maxPayloadLength = 1000,
        IEnumerable<string>? sensitiveHeaders = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logPayload = logPayload;
        _maxPayloadLength = maxPayloadLength;
        _sensitiveHeaders = new HashSet<string>(sensitiveHeaders ?? new[] { "Authorization", "x-api-key", "password" }, StringComparer.OrdinalIgnoreCase);
    }

    #region Enhanced Logging Methods

    /// <summary>
    /// Logs a message published event with full envelope details.
    /// </summary>
    public void LogMessagePublishedWithEnvelope(MessageEnvelope envelope, TimeSpan elapsed)
    {
        var activity = Activity.Current;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["MessageId"] = envelope.MessageId,
            ["CorrelationId"] = envelope.CorrelationId,
            ["CausationId"] = envelope.CausationId,
            ["TraceId"] = activity?.TraceId.ToString(),
            ["SpanId"] = activity?.SpanId.ToString(),
            ["TenantId"] = envelope.TenantId,
            ["UserId"] = envelope.UserId
        }))
        {
            _logger.LogInformation(
                "Message published. MessageId={MessageId}, Exchange={Exchange}, RoutingKey={RoutingKey}, " +
                "MessageType={MessageType}, PayloadSize={PayloadSize}bytes, Priority={Priority}, " +
                "Persistent={Persistent}, Expiration={Expiration}, ElapsedMs={ElapsedMs}",
                envelope.MessageId,
                envelope.Exchange,
                envelope.RoutingKey,
                envelope.MessageType,
                envelope.PayloadSize,
                envelope.Priority,
                envelope.Persistent,
                envelope.Expiration,
                elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Logs a message consumed event with full envelope details.
    /// </summary>
    public void LogMessageConsumedWithEnvelope(MessageEnvelope envelope, TimeSpan processingTime, bool success)
    {
        var activity = Activity.Current;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["MessageId"] = envelope.MessageId,
            ["CorrelationId"] = envelope.CorrelationId,
            ["CausationId"] = envelope.CausationId,
            ["TraceId"] = activity?.TraceId.ToString(),
            ["SpanId"] = activity?.SpanId.ToString(),
            ["TenantId"] = envelope.TenantId,
            ["UserId"] = envelope.UserId
        }))
        {
            if (success)
            {
                _logger.LogInformation(
                    "Message consumed successfully. MessageId={MessageId}, Queue={Queue}, MessageType={MessageType}, " +
                    "PayloadSize={PayloadSize}bytes, Redelivered={Redelivered}, RedeliveryCount={RedeliveryCount}, " +
                    "ProcessingTimeMs={ProcessingTimeMs}",
                    envelope.MessageId,
                    envelope.QueueName,
                    envelope.MessageType,
                    envelope.PayloadSize,
                    envelope.Redelivered,
                    envelope.RedeliveryCount,
                    processingTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Message consumption failed. MessageId={MessageId}, Queue={Queue}, MessageType={MessageType}, " +
                    "Redelivered={Redelivered}, RedeliveryCount={RedeliveryCount}, ProcessingTimeMs={ProcessingTimeMs}",
                    envelope.MessageId,
                    envelope.QueueName,
                    envelope.MessageType,
                    envelope.Redelivered,
                    envelope.RedeliveryCount,
                    processingTime.TotalMilliseconds);
            }
        }
    }

    /// <summary>
    /// Logs a message with full envelope as JSON for detailed debugging.
    /// </summary>
    public void LogMessageEnvelopeDebug(string operation, MessageEnvelope envelope)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        var sanitizedHeaders = SanitizeHeaders(envelope.Headers);

        _logger.LogDebug(
            "RabbitMQ {Operation} envelope: {Envelope}",
            operation,
            JsonSerializer.Serialize(new
            {
                envelope.MessageId,
                envelope.CorrelationId,
                envelope.CausationId,
                envelope.ConversationId,
                envelope.Exchange,
                envelope.RoutingKey,
                envelope.QueueName,
                envelope.MessageType,
                envelope.PayloadSize,
                envelope.Priority,
                envelope.Persistent,
                envelope.Expiration,
                envelope.Redelivered,
                envelope.RedeliveryCount,
                envelope.TenantId,
                envelope.UserId,
                Headers = sanitizedHeaders,
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString()
            }, new JsonSerializerOptions { WriteIndented = false }));
    }

    /// <summary>
    /// Logs a batch processing event.
    /// </summary>
    public void LogBatchProcessed(string queueName, int batchSize, int successCount, int failedCount, TimeSpan totalTime)
    {
        _logger.LogInformation(
            "Batch processed. Queue={Queue}, BatchSize={BatchSize}, Success={SuccessCount}, Failed={FailedCount}, " +
            "TotalTimeMs={TotalTimeMs}, AvgTimePerMessageMs={AvgTimeMs}",
            queueName,
            batchSize,
            successCount,
            failedCount,
            totalTime.TotalMilliseconds,
            batchSize > 0 ? totalTime.TotalMilliseconds / batchSize : 0);
    }

    /// <summary>
    /// Logs a saga step execution.
    /// </summary>
    public void LogSagaStep(string sagaType, string correlationId, string stepName, bool success, TimeSpan duration, string? errorMessage = null)
    {
        if (success)
        {
            _logger.LogInformation(
                "Saga step completed. Saga={SagaType}, CorrelationId={CorrelationId}, Step={StepName}, " +
                "DurationMs={DurationMs}",
                sagaType,
                correlationId,
                stepName,
                duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Saga step failed. Saga={SagaType}, CorrelationId={CorrelationId}, Step={StepName}, " +
                "DurationMs={DurationMs}, Error={ErrorMessage}",
                sagaType,
                correlationId,
                stepName,
                duration.TotalMilliseconds,
                errorMessage);
        }
    }

    #endregion

    #region IRabbitMQStructuredLogger Implementation

    /// <inheritdoc />
    public void LogMessagePublished(
        string messageId,
        string exchange,
        string routingKey,
        int bodySize,
        IDictionary<string, object>? headers = null,
        byte? priority = null,
        TimeSpan? elapsed = null)
    {
        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            Exchange = exchange,
            RoutingKey = routingKey,
            PayloadSize = bodySize,
            Priority = priority,
            Headers = headers
        };

        LogMessagePublishedWithEnvelope(envelope, elapsed ?? TimeSpan.Zero);
    }

    /// <inheritdoc />
    public void LogMessageReceived(
        string messageId,
        string exchange,
        string routingKey,
        string consumerTag,
        bool redelivered,
        int bodySize,
        IDictionary<string, object>? headers = null)
    {
        _logger.LogInformation(
            "Message received. MessageId={MessageId}, Exchange={Exchange}, RoutingKey={RoutingKey}, " +
            "ConsumerTag={ConsumerTag}, Redelivered={Redelivered}, BodySize={BodySize}bytes",
            messageId,
            exchange,
            routingKey,
            consumerTag,
            redelivered,
            bodySize);
    }

    /// <inheritdoc />
    public void LogMessageAcked(string messageId, ulong deliveryTag, TimeSpan processingTime)
    {
        _logger.LogInformation(
            "Message acknowledged. MessageId={MessageId}, DeliveryTag={DeliveryTag}, ProcessingTimeMs={ProcessingTimeMs}",
            messageId,
            deliveryTag,
            processingTime.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void LogMessageNacked(string messageId, ulong deliveryTag, bool requeue, string? reason = null)
    {
        _logger.LogWarning(
            "Message negatively acknowledged. MessageId={MessageId}, DeliveryTag={DeliveryTag}, " +
            "Requeue={Requeue}, Reason={Reason}",
            messageId,
            deliveryTag,
            requeue,
            reason);
    }

    /// <inheritdoc />
    public void LogMessageRejected(string messageId, ulong deliveryTag, string? reason = null)
    {
        _logger.LogWarning(
            "Message rejected. MessageId={MessageId}, DeliveryTag={DeliveryTag}, Reason={Reason}",
            messageId,
            deliveryTag,
            reason);
    }

    /// <inheritdoc />
    public void LogMessageRedelivered(string messageId, int redeliveryCount, int maxRedeliveries)
    {
        _logger.LogWarning(
            "Message being redelivered. MessageId={MessageId}, RedeliveryCount={RedeliveryCount}/{MaxRedeliveries}",
            messageId,
            redeliveryCount,
            maxRedeliveries);
    }

    /// <inheritdoc />
    public void LogDuplicateMessageSkipped(string messageId)
    {
        _logger.LogInformation(
            "Duplicate message skipped. MessageId={MessageId}",
            messageId);
    }

    /// <inheritdoc />
    public void LogPublisherConfirm(string messageId, ulong deliveryTag)
    {
        _logger.LogDebug(
            "Publisher confirm received. MessageId={MessageId}, DeliveryTag={DeliveryTag}",
            messageId,
            deliveryTag);
    }

    /// <inheritdoc />
    public void LogPublisherNack(string messageId, ulong deliveryTag)
    {
        _logger.LogWarning(
            "Publisher nack received. MessageId={MessageId}, DeliveryTag={DeliveryTag}",
            messageId,
            deliveryTag);
    }

    /// <inheritdoc />
    public void LogConnectionEvent(string eventType, string hostName, int port, string? reason = null)
    {
        var logLevel = eventType switch
        {
            "connected" => LogLevel.Information,
            "disconnected" => LogLevel.Warning,
            "reconnecting" => LogLevel.Warning,
            "blocked" => LogLevel.Warning,
            "unblocked" => LogLevel.Information,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "RabbitMQ connection event. Event={EventType}, Host={HostName}:{Port}, Reason={Reason}",
            eventType,
            hostName,
            port,
            reason);
    }

    /// <inheritdoc />
    public void LogChannelEvent(string eventType, int channelNumber, string? reason = null)
    {
        _logger.LogDebug(
            "RabbitMQ channel event. Event={EventType}, Channel={ChannelNumber}, Reason={Reason}",
            eventType,
            channelNumber,
            reason);
    }

    /// <inheritdoc />
    public void LogError(string operation, Exception exception, string? messageId = null)
    {
        _logger.LogError(exception,
            "RabbitMQ operation failed. Operation={Operation}, MessageId={MessageId}, " +
            "ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
            operation,
            messageId,
            exception.GetType().Name,
            exception.Message);
    }

    /// <inheritdoc />
    public void LogQueueDeclared(string queueName, bool durable, bool exclusive, bool autoDelete, int? messageCount = null)
    {
        _logger.LogDebug(
            "Queue declared. QueueName={QueueName}, Durable={Durable}, Exclusive={Exclusive}, " +
            "AutoDelete={AutoDelete}, MessageCount={MessageCount}",
            queueName,
            durable,
            exclusive,
            autoDelete,
            messageCount);
    }

    /// <inheritdoc />
    public void LogExchangeDeclared(string exchange, string exchangeType, bool durable, bool autoDelete)
    {
        _logger.LogDebug(
            "Exchange declared. Exchange={Exchange}, Type={ExchangeType}, Durable={Durable}, AutoDelete={AutoDelete}",
            exchange,
            exchangeType,
            durable,
            autoDelete);
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, object?>? SanitizeHeaders(IDictionary<string, object>? headers)
    {
        if (headers == null || headers.Count == 0) return null;

        var sanitized = new Dictionary<string, object?>();
        foreach (var kvp in headers)
        {
            if (_sensitiveHeaders.Contains(kvp.Key))
            {
                sanitized[kvp.Key] = "***REDACTED***";
            }
            else
            {
                sanitized[kvp.Key] = kvp.Value;
            }
        }

        return sanitized;
    }

    #endregion
}

/// <summary>
/// Represents the full envelope/metadata of a RabbitMQ message.
/// </summary>
public class MessageEnvelope
{
    /// <summary>Gets or sets the unique message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the correlation ID for request/response tracking.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the causation ID (ID of the message that caused this one).</summary>
    public string? CausationId { get; set; }

    /// <summary>Gets or sets the conversation ID for multi-message flows.</summary>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets the exchange name.</summary>
    public string? Exchange { get; set; }

    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    /// <summary>Gets or sets the queue name.</summary>
    public string? QueueName { get; set; }

    /// <summary>Gets or sets the message type name.</summary>
    public string? MessageType { get; set; }

    /// <summary>Gets or sets the payload size in bytes.</summary>
    public int PayloadSize { get; set; }

    /// <summary>Gets or sets the message priority.</summary>
    public byte? Priority { get; set; }

    /// <summary>Gets or sets whether the message is persistent.</summary>
    public bool Persistent { get; set; }

    /// <summary>Gets or sets the message expiration/TTL.</summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>Gets or sets whether this is a redelivered message.</summary>
    public bool Redelivered { get; set; }

    /// <summary>Gets or sets the redelivery count.</summary>
    public int RedeliveryCount { get; set; }

    /// <summary>Gets or sets the consumer tag.</summary>
    public string? ConsumerTag { get; set; }

    /// <summary>Gets or sets the tenant ID.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the user ID.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the source service name.</summary>
    public string? SourceService { get; set; }

    /// <summary>Gets or sets the message headers.</summary>
    public IDictionary<string, object>? Headers { get; set; }

    /// <summary>Gets or sets the timestamp when the message was created.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

