//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Logging;

/// <summary>
/// High-performance source-generated logger messages for the RabbitMQ module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 4000-4999 (RabbitMQ module range). Each message has a unique EventId.
/// </remarks>
public static partial class RabbitMQLoggerMessages
{
    #region [ Event IDs - RabbitMQ Module: 4000-4999 ]

    private const int RabbitMQEventIdBase = 4000;

    // Connection
    public const int ConnectionEstablishedEventId = RabbitMQEventIdBase + 1;
    public const int ConnectionLostEventId = RabbitMQEventIdBase + 2;
    public const int ConnectionRecoveredEventId = RabbitMQEventIdBase + 3;
    public const int ConnectionFailedEventId = RabbitMQEventIdBase + 4;
    public const int ConnectionClosedEventId = RabbitMQEventIdBase + 5;

    // Channel
    public const int ChannelCreatedEventId = RabbitMQEventIdBase + 6;
    public const int ChannelClosedEventId = RabbitMQEventIdBase + 7;

    // Publishing
    public const int MessagePublishedEventId = RabbitMQEventIdBase + 8;
    public const int PublisherConfirmReceivedEventId = RabbitMQEventIdBase + 9;
    public const int PublisherNackReceivedEventId = RabbitMQEventIdBase + 10;
    public const int PublishFailedEventId = RabbitMQEventIdBase + 11;

    // Consuming
    public const int MessageReceivedEventId = RabbitMQEventIdBase + 12;
    public const int MessageProcessedEventId = RabbitMQEventIdBase + 13;
    public const int MessageProcessingFailedEventId = RabbitMQEventIdBase + 14;
    public const int ConsumerStartedEventId = RabbitMQEventIdBase + 15;
    public const int ConsumerStoppedEventId = RabbitMQEventIdBase + 16;

    // Acknowledgment
    public const int MessageAcknowledgedEventId = RabbitMQEventIdBase + 17;
    public const int MessageRejectedEventId = RabbitMQEventIdBase + 18;
    public const int MessageNackedEventId = RabbitMQEventIdBase + 19;

    // Exchange/Queue
    public const int ExchangeDeclaredEventId = RabbitMQEventIdBase + 20;
    public const int QueueDeclaredEventId = RabbitMQEventIdBase + 21;
    public const int QueueBoundEventId = RabbitMQEventIdBase + 22;

    // Request/Response
    public const int RequestSentEventId = RabbitMQEventIdBase + 23;
    public const int ResponseReceivedEventId = RabbitMQEventIdBase + 24;
    public const int RequestTimedOutEventId = RabbitMQEventIdBase + 25;

    // Batch
    public const int BatchStartedEventId = RabbitMQEventIdBase + 26;
    public const int BatchCompletedEventId = RabbitMQEventIdBase + 27;
    public const int BatchPartiallyFailedEventId = RabbitMQEventIdBase + 28;

    // Scheduling
    public const int MessageScheduledEventId = RabbitMQEventIdBase + 29;
    public const int ScheduledMessageDeliveredEventId = RabbitMQEventIdBase + 30;
    public const int ScheduledMessageCancelledEventId = RabbitMQEventIdBase + 31;
    public const int RecurringMessageTriggeredEventId = RabbitMQEventIdBase + 32;

    // Saga
    public const int SagaMessageReceivedEventId = RabbitMQEventIdBase + 33;
    public const int SagaStateTransitionedEventId = RabbitMQEventIdBase + 34;

    // Filters
    public const int FilterExecutedEventId = RabbitMQEventIdBase + 35;
    public const int MessageRejectedByFilterEventId = RabbitMQEventIdBase + 36;

    // Multi-tenancy
    public const int TenantContextResolvedEventId = RabbitMQEventIdBase + 37;
    public const int TenantQueueUsedEventId = RabbitMQEventIdBase + 38;

    // Dead Letter
    public const int MessageSentToDeadLetterEventId = RabbitMQEventIdBase + 39;
    public const int DeadLetterMessageReprocessedEventId = RabbitMQEventIdBase + 40;

    // Resiliency
    public const int MessageRetryScheduledEventId = RabbitMQEventIdBase + 41;
    public const int CircuitBreakerOpenedEventId = RabbitMQEventIdBase + 42;
    public const int CircuitBreakerClosedEventId = RabbitMQEventIdBase + 43;

    #endregion

    #region [ Connection ]

    [LoggerMessage(
        EventId = ConnectionEstablishedEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection established. Host: {Host}, Port: {Port}")]
    public static partial void ConnectionEstablished(ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = ConnectionLostEventId,
        Level = LogLevel.Warning,
        Message = "RabbitMQ connection lost. Attempting reconnection...")]
    public static partial void ConnectionLost(ILogger logger);

    [LoggerMessage(
        EventId = ConnectionRecoveredEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection recovered. Reconnection attempts: {Attempts}")]
    public static partial void ConnectionRecovered(ILogger logger, int attempts);

    [LoggerMessage(
        EventId = ConnectionFailedEventId,
        Level = LogLevel.Error,
        Message = "RabbitMQ connection failed after {Attempts} attempts")]
    public static partial void ConnectionFailed(ILogger logger, Exception exception, int attempts);

    [LoggerMessage(
        EventId = ConnectionClosedEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection closed gracefully")]
    public static partial void ConnectionClosed(ILogger logger);

    #endregion

    #region [ Channel ]

    [LoggerMessage(
        EventId = ChannelCreatedEventId,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel created. ChannelNumber: {ChannelNumber}")]
    public static partial void ChannelCreated(ILogger logger, int channelNumber);

    [LoggerMessage(
        EventId = ChannelClosedEventId,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel closed. ChannelNumber: {ChannelNumber}")]
    public static partial void ChannelClosed(ILogger logger, int channelNumber);

    #endregion

    #region [ Publishing ]

    [LoggerMessage(
        EventId = MessagePublishedEventId,
        Level = LogLevel.Debug,
        Message = "Message published to exchange '{Exchange}' with routing key '{RoutingKey}'. MessageId: {MessageId}")]
    public static partial void MessagePublished(ILogger logger, string exchange, string routingKey, string messageId);

    [LoggerMessage(
        EventId = PublisherConfirmReceivedEventId,
        Level = LogLevel.Debug,
        Message = "Publisher confirm received. DeliveryTag: {DeliveryTag}, Multiple: {Multiple}")]
    public static partial void PublisherConfirmReceived(ILogger logger, ulong deliveryTag, bool multiple);

    [LoggerMessage(
        EventId = PublisherNackReceivedEventId,
        Level = LogLevel.Warning,
        Message = "Publisher nack received. DeliveryTag: {DeliveryTag}, Multiple: {Multiple}")]
    public static partial void PublisherNackReceived(ILogger logger, ulong deliveryTag, bool multiple);

    [LoggerMessage(
        EventId = PublishFailedEventId,
        Level = LogLevel.Error,
        Message = "Message publish failed to exchange '{Exchange}'. MessageId: {MessageId}")]
    public static partial void PublishFailed(ILogger logger, Exception exception, string exchange, string messageId);

    #endregion

    #region [ Consuming ]

    [LoggerMessage(
        EventId = MessageReceivedEventId,
        Level = LogLevel.Debug,
        Message = "Message received from queue '{Queue}'. MessageId: {MessageId}, Type: {MessageType}")]
    public static partial void MessageReceived(ILogger logger, string queue, string messageId, string messageType);

    [LoggerMessage(
        EventId = MessageProcessedEventId,
        Level = LogLevel.Debug,
        Message = "Message '{MessageId}' processed by consumer '{ConsumerType}' in {ElapsedMs}ms")]
    public static partial void MessageProcessed(ILogger logger, string messageId, string consumerType, long elapsedMs);

    [LoggerMessage(
        EventId = MessageProcessingFailedEventId,
        Level = LogLevel.Error,
        Message = "Message '{MessageId}' processing failed by consumer '{ConsumerType}'")]
    public static partial void MessageProcessingFailed(ILogger logger, Exception exception, string messageId, string consumerType);

    [LoggerMessage(
        EventId = ConsumerStartedEventId,
        Level = LogLevel.Information,
        Message = "Consumer '{ConsumerType}' started on queue '{Queue}'. PrefetchCount: {PrefetchCount}")]
    public static partial void ConsumerStarted(ILogger logger, string consumerType, string queue, int prefetchCount);

    [LoggerMessage(
        EventId = ConsumerStoppedEventId,
        Level = LogLevel.Information,
        Message = "Consumer '{ConsumerType}' stopped on queue '{Queue}'")]
    public static partial void ConsumerStopped(ILogger logger, string consumerType, string queue);

    #endregion

    #region [ Acknowledgment ]

    [LoggerMessage(
        EventId = MessageAcknowledgedEventId,
        Level = LogLevel.Trace,
        Message = "Message acknowledged. DeliveryTag: {DeliveryTag}")]
    public static partial void MessageAcknowledged(ILogger logger, ulong deliveryTag);

    [LoggerMessage(
        EventId = MessageRejectedEventId,
        Level = LogLevel.Warning,
        Message = "Message rejected. DeliveryTag: {DeliveryTag}, Requeue: {Requeue}")]
    public static partial void MessageRejected(ILogger logger, ulong deliveryTag, bool requeue);

    [LoggerMessage(
        EventId = MessageNackedEventId,
        Level = LogLevel.Warning,
        Message = "Message nacked. DeliveryTag: {DeliveryTag}, Requeue: {Requeue}")]
    public static partial void MessageNacked(ILogger logger, ulong deliveryTag, bool requeue);

    #endregion

    #region [ Exchange/Queue ]

    [LoggerMessage(
        EventId = ExchangeDeclaredEventId,
        Level = LogLevel.Debug,
        Message = "Exchange '{Exchange}' declared. Type: {ExchangeType}")]
    public static partial void ExchangeDeclared(ILogger logger, string exchange, string exchangeType);

    [LoggerMessage(
        EventId = QueueDeclaredEventId,
        Level = LogLevel.Debug,
        Message = "Queue '{Queue}' declared. Durable: {Durable}")]
    public static partial void QueueDeclared(ILogger logger, string queue, bool durable);

    [LoggerMessage(
        EventId = QueueBoundEventId,
        Level = LogLevel.Debug,
        Message = "Queue '{Queue}' bound to exchange '{Exchange}' with routing key '{RoutingKey}'")]
    public static partial void QueueBound(ILogger logger, string queue, string exchange, string routingKey);

    #endregion

    #region [ Request/Response ]

    [LoggerMessage(
        EventId = RequestSentEventId,
        Level = LogLevel.Debug,
        Message = "Request sent. CorrelationId: {CorrelationId}, RequestType: {RequestType}")]
    public static partial void RequestSent(ILogger logger, string correlationId, string requestType);

    [LoggerMessage(
        EventId = ResponseReceivedEventId,
        Level = LogLevel.Debug,
        Message = "Response received. CorrelationId: {CorrelationId}, Duration: {ElapsedMs}ms")]
    public static partial void ResponseReceived(ILogger logger, string correlationId, long elapsedMs);

    [LoggerMessage(
        EventId = RequestTimedOutEventId,
        Level = LogLevel.Warning,
        Message = "Request timed out. CorrelationId: {CorrelationId}, Timeout: {TimeoutMs}ms")]
    public static partial void RequestTimedOut(ILogger logger, string correlationId, int timeoutMs);

    #endregion

    #region [ Batch ]

    [LoggerMessage(
        EventId = BatchStartedEventId,
        Level = LogLevel.Debug,
        Message = "Batch started. Messages: {MessageCount}")]
    public static partial void BatchStarted(ILogger logger, int messageCount);

    [LoggerMessage(
        EventId = BatchCompletedEventId,
        Level = LogLevel.Debug,
        Message = "Batch completed. Messages: {MessageCount}, Duration: {ElapsedMs}ms")]
    public static partial void BatchCompleted(ILogger logger, int messageCount, long elapsedMs);

    [LoggerMessage(
        EventId = BatchPartiallyFailedEventId,
        Level = LogLevel.Warning,
        Message = "Batch partially failed. Success: {SuccessCount}/{TotalCount}")]
    public static partial void BatchPartiallyFailed(ILogger logger, int successCount, int totalCount);

    #endregion

    #region [ Scheduling ]

    [LoggerMessage(
        EventId = MessageScheduledEventId,
        Level = LogLevel.Information,
        Message = "Message scheduled. MessageId: {MessageId}, ScheduledTime: {ScheduledTime}")]
    public static partial void MessageScheduled(ILogger logger, string messageId, DateTimeOffset scheduledTime);

    [LoggerMessage(
        EventId = ScheduledMessageDeliveredEventId,
        Level = LogLevel.Debug,
        Message = "Scheduled message delivered. MessageId: {MessageId}")]
    public static partial void ScheduledMessageDelivered(ILogger logger, string messageId);

    [LoggerMessage(
        EventId = ScheduledMessageCancelledEventId,
        Level = LogLevel.Information,
        Message = "Scheduled message cancelled. MessageId: {MessageId}")]
    public static partial void ScheduledMessageCancelled(ILogger logger, string messageId);

    [LoggerMessage(
        EventId = RecurringMessageTriggeredEventId,
        Level = LogLevel.Debug,
        Message = "Recurring message triggered. ScheduleId: {ScheduleId}, Occurrence: {OccurrenceCount}")]
    public static partial void RecurringMessageTriggered(ILogger logger, string scheduleId, int occurrenceCount);

    #endregion

    #region [ Saga ]

    [LoggerMessage(
        EventId = SagaMessageReceivedEventId,
        Level = LogLevel.Information,
        Message = "Saga message received. SagaId: {SagaId}, MessageType: {MessageType}")]
    public static partial void SagaMessageReceived(ILogger logger, string sagaId, string messageType);

    [LoggerMessage(
        EventId = SagaStateTransitionedEventId,
        Level = LogLevel.Debug,
        Message = "Saga state transitioned. SagaId: {SagaId}, From: {FromState}, To: {ToState}")]
    public static partial void SagaStateTransitioned(ILogger logger, string sagaId, string fromState, string toState);

    #endregion

    #region [ Filters ]

    [LoggerMessage(
        EventId = FilterExecutedEventId,
        Level = LogLevel.Trace,
        Message = "Filter '{FilterName}' executed for message '{MessageType}'. Duration: {ElapsedMs}ms")]
    public static partial void FilterExecuted(ILogger logger, string filterName, string messageType, long elapsedMs);

    [LoggerMessage(
        EventId = MessageRejectedByFilterEventId,
        Level = LogLevel.Debug,
        Message = "Message rejected by filter '{FilterName}'. Reason: {Reason}")]
    public static partial void MessageRejectedByFilter(ILogger logger, string filterName, string reason);

    #endregion

    #region [ Multi-tenancy ]

    [LoggerMessage(
        EventId = TenantContextResolvedEventId,
        Level = LogLevel.Debug,
        Message = "Tenant context resolved. TenantId: {TenantId}, VirtualHost: {VirtualHost}")]
    public static partial void TenantContextResolved(ILogger logger, string tenantId, string virtualHost);

    [LoggerMessage(
        EventId = TenantQueueUsedEventId,
        Level = LogLevel.Debug,
        Message = "Using tenant-specific queue: {Queue}")]
    public static partial void TenantQueueUsed(ILogger logger, string queue);

    #endregion

    #region [ Dead Letter ]

    [LoggerMessage(
        EventId = MessageSentToDeadLetterEventId,
        Level = LogLevel.Warning,
        Message = "Message sent to dead letter queue. MessageId: {MessageId}, Reason: {Reason}")]
    public static partial void MessageSentToDeadLetter(ILogger logger, string messageId, string reason);

    [LoggerMessage(
        EventId = DeadLetterMessageReprocessedEventId,
        Level = LogLevel.Debug,
        Message = "Dead letter message reprocessed. MessageId: {MessageId}")]
    public static partial void DeadLetterMessageReprocessed(ILogger logger, string messageId);

    #endregion

    #region [ Resiliency ]

    [LoggerMessage(
        EventId = MessageRetryScheduledEventId,
        Level = LogLevel.Warning,
        Message = "Message retry scheduled. MessageId: {MessageId}, Attempt: {Attempt}/{MaxAttempts}, Delay: {DelayMs}ms")]
    public static partial void MessageRetryScheduled(ILogger logger, string messageId, int attempt, int maxAttempts, int delayMs);

    [LoggerMessage(
        EventId = CircuitBreakerOpenedEventId,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for queue '{Queue}'. Failures: {FailureCount}")]
    public static partial void CircuitBreakerOpened(ILogger logger, string queue, int failureCount);

    [LoggerMessage(
        EventId = CircuitBreakerClosedEventId,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed for queue '{Queue}'")]
    public static partial void CircuitBreakerClosed(ILogger logger, string queue);

    #endregion
}
