//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Logging;

/// <summary>
/// High-performance source-generated logger messages for the RabbitMQ module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 4000-4999 (RabbitMQ module range)
/// </remarks>
public static partial class RabbitMQLoggerMessages
{
    #region [ Event IDs - RabbitMQ Module: 4000-4999 ]

    private const int RabbitMQEventIdBase = 4000;

    public const int ConnectionEventId = RabbitMQEventIdBase + 1;
    public const int ChannelEventId = RabbitMQEventIdBase + 2;
    public const int PublishEventId = RabbitMQEventIdBase + 3;
    public const int ConsumeEventId = RabbitMQEventIdBase + 4;
    public const int AckNackEventId = RabbitMQEventIdBase + 5;
    public const int ExchangeQueueEventId = RabbitMQEventIdBase + 6;
    public const int RequestResponseEventId = RabbitMQEventIdBase + 7;
    public const int BatchEventId = RabbitMQEventIdBase + 8;
    public const int SchedulingEventId = RabbitMQEventIdBase + 9;
    public const int SagaEventId = RabbitMQEventIdBase + 10;
    public const int FilterEventId = RabbitMQEventIdBase + 11;
    public const int TenantEventId = RabbitMQEventIdBase + 12;
    public const int DeadLetterEventId = RabbitMQEventIdBase + 13;
    public const int ResiliencyEventId = RabbitMQEventIdBase + 14;

    #endregion

    #region [ Connection ]

    [LoggerMessage(
        EventId = ConnectionEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection established. Host: {Host}, Port: {Port}")]
    public static partial void ConnectionEstablished(ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = ConnectionEventId,
        Level = LogLevel.Warning,
        Message = "RabbitMQ connection lost. Attempting reconnection...")]
    public static partial void ConnectionLost(ILogger logger);

    [LoggerMessage(
        EventId = ConnectionEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection recovered. Reconnection attempts: {Attempts}")]
    public static partial void ConnectionRecovered(ILogger logger, int attempts);

    [LoggerMessage(
        EventId = ConnectionEventId,
        Level = LogLevel.Error,
        Message = "RabbitMQ connection failed after {Attempts} attempts")]
    public static partial void ConnectionFailed(ILogger logger, Exception exception, int attempts);

    [LoggerMessage(
        EventId = ConnectionEventId,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection closed gracefully")]
    public static partial void ConnectionClosed(ILogger logger);

    #endregion

    #region [ Channel ]

    [LoggerMessage(
        EventId = ChannelEventId,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel created. ChannelNumber: {ChannelNumber}")]
    public static partial void ChannelCreated(ILogger logger, int channelNumber);

    [LoggerMessage(
        EventId = ChannelEventId,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel closed. ChannelNumber: {ChannelNumber}")]
    public static partial void ChannelClosed(ILogger logger, int channelNumber);

    #endregion

    #region [ Publishing ]

    [LoggerMessage(
        EventId = PublishEventId,
        Level = LogLevel.Debug,
        Message = "Message published to exchange '{Exchange}' with routing key '{RoutingKey}'. MessageId: {MessageId}")]
    public static partial void MessagePublished(ILogger logger, string exchange, string routingKey, string messageId);

    [LoggerMessage(
        EventId = PublishEventId,
        Level = LogLevel.Debug,
        Message = "Publisher confirm received. DeliveryTag: {DeliveryTag}, Multiple: {Multiple}")]
    public static partial void PublisherConfirmReceived(ILogger logger, ulong deliveryTag, bool multiple);

    [LoggerMessage(
        EventId = PublishEventId,
        Level = LogLevel.Warning,
        Message = "Publisher nack received. DeliveryTag: {DeliveryTag}, Multiple: {Multiple}")]
    public static partial void PublisherNackReceived(ILogger logger, ulong deliveryTag, bool multiple);

    [LoggerMessage(
        EventId = PublishEventId,
        Level = LogLevel.Error,
        Message = "Message publish failed to exchange '{Exchange}'. MessageId: {MessageId}")]
    public static partial void PublishFailed(ILogger logger, Exception exception, string exchange, string messageId);

    #endregion

    #region [ Consuming ]

    [LoggerMessage(
        EventId = ConsumeEventId,
        Level = LogLevel.Debug,
        Message = "Message received from queue '{Queue}'. MessageId: {MessageId}, Type: {MessageType}")]
    public static partial void MessageReceived(ILogger logger, string queue, string messageId, string messageType);

    [LoggerMessage(
        EventId = ConsumeEventId,
        Level = LogLevel.Debug,
        Message = "Message '{MessageId}' processed by consumer '{ConsumerType}' in {ElapsedMs}ms")]
    public static partial void MessageProcessed(ILogger logger, string messageId, string consumerType, long elapsedMs);

    [LoggerMessage(
        EventId = ConsumeEventId,
        Level = LogLevel.Error,
        Message = "Message '{MessageId}' processing failed by consumer '{ConsumerType}'")]
    public static partial void MessageProcessingFailed(ILogger logger, Exception exception, string messageId, string consumerType);

    [LoggerMessage(
        EventId = ConsumeEventId,
        Level = LogLevel.Information,
        Message = "Consumer '{ConsumerType}' started on queue '{Queue}'. PrefetchCount: {PrefetchCount}")]
    public static partial void ConsumerStarted(ILogger logger, string consumerType, string queue, int prefetchCount);

    [LoggerMessage(
        EventId = ConsumeEventId,
        Level = LogLevel.Information,
        Message = "Consumer '{ConsumerType}' stopped on queue '{Queue}'")]
    public static partial void ConsumerStopped(ILogger logger, string consumerType, string queue);

    #endregion

    #region [ Acknowledgment ]

    [LoggerMessage(
        EventId = AckNackEventId,
        Level = LogLevel.Trace,
        Message = "Message acknowledged. DeliveryTag: {DeliveryTag}")]
    public static partial void MessageAcknowledged(ILogger logger, ulong deliveryTag);

    [LoggerMessage(
        EventId = AckNackEventId,
        Level = LogLevel.Warning,
        Message = "Message rejected. DeliveryTag: {DeliveryTag}, Requeue: {Requeue}")]
    public static partial void MessageRejected(ILogger logger, ulong deliveryTag, bool requeue);

    [LoggerMessage(
        EventId = AckNackEventId,
        Level = LogLevel.Warning,
        Message = "Message nacked. DeliveryTag: {DeliveryTag}, Requeue: {Requeue}")]
    public static partial void MessageNacked(ILogger logger, ulong deliveryTag, bool requeue);

    #endregion

    #region [ Exchange/Queue ]

    [LoggerMessage(
        EventId = ExchangeQueueEventId,
        Level = LogLevel.Debug,
        Message = "Exchange '{Exchange}' declared. Type: {ExchangeType}")]
    public static partial void ExchangeDeclared(ILogger logger, string exchange, string exchangeType);

    [LoggerMessage(
        EventId = ExchangeQueueEventId,
        Level = LogLevel.Debug,
        Message = "Queue '{Queue}' declared. Durable: {Durable}")]
    public static partial void QueueDeclared(ILogger logger, string queue, bool durable);

    [LoggerMessage(
        EventId = ExchangeQueueEventId,
        Level = LogLevel.Debug,
        Message = "Queue '{Queue}' bound to exchange '{Exchange}' with routing key '{RoutingKey}'")]
    public static partial void QueueBound(ILogger logger, string queue, string exchange, string routingKey);

    #endregion

    #region [ Request/Response ]

    [LoggerMessage(
        EventId = RequestResponseEventId,
        Level = LogLevel.Debug,
        Message = "Request sent. CorrelationId: {CorrelationId}, RequestType: {RequestType}")]
    public static partial void RequestSent(ILogger logger, string correlationId, string requestType);

    [LoggerMessage(
        EventId = RequestResponseEventId,
        Level = LogLevel.Debug,
        Message = "Response received. CorrelationId: {CorrelationId}, Duration: {ElapsedMs}ms")]
    public static partial void ResponseReceived(ILogger logger, string correlationId, long elapsedMs);

    [LoggerMessage(
        EventId = RequestResponseEventId,
        Level = LogLevel.Warning,
        Message = "Request timed out. CorrelationId: {CorrelationId}, Timeout: {TimeoutMs}ms")]
    public static partial void RequestTimedOut(ILogger logger, string correlationId, int timeoutMs);

    #endregion

    #region [ Batch ]

    [LoggerMessage(
        EventId = BatchEventId,
        Level = LogLevel.Debug,
        Message = "Batch started. Messages: {MessageCount}")]
    public static partial void BatchStarted(ILogger logger, int messageCount);

    [LoggerMessage(
        EventId = BatchEventId,
        Level = LogLevel.Debug,
        Message = "Batch completed. Messages: {MessageCount}, Duration: {ElapsedMs}ms")]
    public static partial void BatchCompleted(ILogger logger, int messageCount, long elapsedMs);

    [LoggerMessage(
        EventId = BatchEventId,
        Level = LogLevel.Warning,
        Message = "Batch partially failed. Success: {SuccessCount}/{TotalCount}")]
    public static partial void BatchPartiallyFailed(ILogger logger, int successCount, int totalCount);

    #endregion

    #region [ Scheduling ]

    [LoggerMessage(
        EventId = SchedulingEventId,
        Level = LogLevel.Information,
        Message = "Message scheduled. MessageId: {MessageId}, ScheduledTime: {ScheduledTime}")]
    public static partial void MessageScheduled(ILogger logger, string messageId, DateTimeOffset scheduledTime);

    [LoggerMessage(
        EventId = SchedulingEventId,
        Level = LogLevel.Debug,
        Message = "Scheduled message delivered. MessageId: {MessageId}")]
    public static partial void ScheduledMessageDelivered(ILogger logger, string messageId);

    [LoggerMessage(
        EventId = SchedulingEventId,
        Level = LogLevel.Information,
        Message = "Scheduled message cancelled. MessageId: {MessageId}")]
    public static partial void ScheduledMessageCancelled(ILogger logger, string messageId);

    [LoggerMessage(
        EventId = SchedulingEventId,
        Level = LogLevel.Debug,
        Message = "Recurring message triggered. ScheduleId: {ScheduleId}, Occurrence: {OccurrenceCount}")]
    public static partial void RecurringMessageTriggered(ILogger logger, string scheduleId, int occurrenceCount);

    #endregion

    #region [ Saga ]

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga message received. SagaId: {SagaId}, MessageType: {MessageType}")]
    public static partial void SagaMessageReceived(ILogger logger, string sagaId, string messageType);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Debug,
        Message = "Saga state transitioned. SagaId: {SagaId}, From: {FromState}, To: {ToState}")]
    public static partial void SagaStateTransitioned(ILogger logger, string sagaId, string fromState, string toState);

    #endregion

    #region [ Filters ]

    [LoggerMessage(
        EventId = FilterEventId,
        Level = LogLevel.Trace,
        Message = "Filter '{FilterName}' executed for message '{MessageType}'. Duration: {ElapsedMs}ms")]
    public static partial void FilterExecuted(ILogger logger, string filterName, string messageType, long elapsedMs);

    [LoggerMessage(
        EventId = FilterEventId,
        Level = LogLevel.Debug,
        Message = "Message rejected by filter '{FilterName}'. Reason: {Reason}")]
    public static partial void MessageRejectedByFilter(ILogger logger, string filterName, string reason);

    #endregion

    #region [ Multi-tenancy ]

    [LoggerMessage(
        EventId = TenantEventId,
        Level = LogLevel.Debug,
        Message = "Tenant context resolved. TenantId: {TenantId}, VirtualHost: {VirtualHost}")]
    public static partial void TenantContextResolved(ILogger logger, string tenantId, string virtualHost);

    [LoggerMessage(
        EventId = TenantEventId,
        Level = LogLevel.Debug,
        Message = "Using tenant-specific queue: {Queue}")]
    public static partial void TenantQueueUsed(ILogger logger, string queue);

    #endregion

    #region [ Dead Letter ]

    [LoggerMessage(
        EventId = DeadLetterEventId,
        Level = LogLevel.Warning,
        Message = "Message sent to dead letter queue. MessageId: {MessageId}, Reason: {Reason}")]
    public static partial void MessageSentToDeadLetter(ILogger logger, string messageId, string reason);

    [LoggerMessage(
        EventId = DeadLetterEventId,
        Level = LogLevel.Debug,
        Message = "Dead letter message reprocessed. MessageId: {MessageId}")]
    public static partial void DeadLetterMessageReprocessed(ILogger logger, string messageId);

    #endregion

    #region [ Resiliency ]

    [LoggerMessage(
        EventId = ResiliencyEventId,
        Level = LogLevel.Warning,
        Message = "Message retry scheduled. MessageId: {MessageId}, Attempt: {Attempt}/{MaxAttempts}, Delay: {DelayMs}ms")]
    public static partial void MessageRetryScheduled(ILogger logger, string messageId, int attempt, int maxAttempts, int delayMs);

    [LoggerMessage(
        EventId = ResiliencyEventId,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for queue '{Queue}'. Failures: {FailureCount}")]
    public static partial void CircuitBreakerOpened(ILogger logger, string queue, int failureCount);

    [LoggerMessage(
        EventId = ResiliencyEventId,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed for queue '{Queue}'")]
    public static partial void CircuitBreakerClosed(ILogger logger, string queue);

    #endregion
}

