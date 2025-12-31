//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;

namespace Mvp24Hours.Infrastructure.Cqrs.Logging;

/// <summary>
/// High-performance source-generated logger messages for the CQRS module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 3000-3999 (CQRS module range)
/// </remarks>
public static partial class CqrsLoggerMessages
{
    #region [ Event IDs - CQRS Module: 3000-3999 ]

    private const int CqrsEventIdBase = 3000;

    public const int CommandStartedEventId = CqrsEventIdBase + 1;
    public const int CommandCompletedEventId = CqrsEventIdBase + 2;
    public const int CommandFailedEventId = CqrsEventIdBase + 3;
    public const int QueryStartedEventId = CqrsEventIdBase + 4;
    public const int QueryCompletedEventId = CqrsEventIdBase + 5;
    public const int QueryFailedEventId = CqrsEventIdBase + 6;
    public const int NotificationPublishedEventId = CqrsEventIdBase + 7;
    public const int NotificationHandledEventId = CqrsEventIdBase + 8;
    public const int DomainEventRaisedEventId = CqrsEventIdBase + 9;
    public const int DomainEventDispatchedEventId = CqrsEventIdBase + 10;
    public const int IntegrationEventPublishedEventId = CqrsEventIdBase + 11;
    public const int BehaviorExecutedEventId = CqrsEventIdBase + 12;
    public const int ValidationEventId = CqrsEventIdBase + 13;
    public const int CachingEventId = CqrsEventIdBase + 14;
    public const int TransactionEventId = CqrsEventIdBase + 15;
    public const int IdempotencyEventId = CqrsEventIdBase + 16;
    public const int SagaEventId = CqrsEventIdBase + 17;
    public const int EventSourcingEventId = CqrsEventIdBase + 18;
    public const int ScheduledCommandEventId = CqrsEventIdBase + 19;
    public const int AuditEventId = CqrsEventIdBase + 20;

    #endregion

    #region [ Commands ]

    [LoggerMessage(
        EventId = CommandStartedEventId,
        Level = LogLevel.Information,
        Message = "Command '{CommandName}' started. CorrelationId: {CorrelationId}")]
    public static partial void CommandStarted(ILogger logger, string commandName, string correlationId);

    [LoggerMessage(
        EventId = CommandCompletedEventId,
        Level = LogLevel.Information,
        Message = "Command '{CommandName}' completed in {ElapsedMs}ms. Success: {IsSuccess}")]
    public static partial void CommandCompleted(ILogger logger, string commandName, long elapsedMs, bool isSuccess);

    [LoggerMessage(
        EventId = CommandFailedEventId,
        Level = LogLevel.Error,
        Message = "Command '{CommandName}' failed after {ElapsedMs}ms")]
    public static partial void CommandFailed(ILogger logger, Exception exception, string commandName, long elapsedMs);

    [LoggerMessage(
        EventId = CommandCompletedEventId,
        Level = LogLevel.Warning,
        Message = "Slow command '{CommandName}' detected: {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowCommand(ILogger logger, string commandName, long elapsedMs, long thresholdMs);

    #endregion

    #region [ Queries ]

    [LoggerMessage(
        EventId = QueryStartedEventId,
        Level = LogLevel.Debug,
        Message = "Query '{QueryName}' started. CorrelationId: {CorrelationId}")]
    public static partial void QueryStarted(ILogger logger, string queryName, string correlationId);

    [LoggerMessage(
        EventId = QueryCompletedEventId,
        Level = LogLevel.Debug,
        Message = "Query '{QueryName}' completed in {ElapsedMs}ms. Result count: {ResultCount}")]
    public static partial void QueryCompleted(ILogger logger, string queryName, long elapsedMs, int resultCount);

    [LoggerMessage(
        EventId = QueryFailedEventId,
        Level = LogLevel.Error,
        Message = "Query '{QueryName}' failed after {ElapsedMs}ms")]
    public static partial void QueryFailed(ILogger logger, Exception exception, string queryName, long elapsedMs);

    [LoggerMessage(
        EventId = QueryCompletedEventId,
        Level = LogLevel.Warning,
        Message = "Slow query '{QueryName}' detected: {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowQuery(ILogger logger, string queryName, long elapsedMs, long thresholdMs);

    #endregion

    #region [ Notifications ]

    [LoggerMessage(
        EventId = NotificationPublishedEventId,
        Level = LogLevel.Debug,
        Message = "Notification '{NotificationName}' published. Handlers: {HandlerCount}")]
    public static partial void NotificationPublished(ILogger logger, string notificationName, int handlerCount);

    [LoggerMessage(
        EventId = NotificationHandledEventId,
        Level = LogLevel.Debug,
        Message = "Notification '{NotificationName}' handled by '{HandlerName}' in {ElapsedMs}ms")]
    public static partial void NotificationHandled(ILogger logger, string notificationName, string handlerName, long elapsedMs);

    [LoggerMessage(
        EventId = NotificationHandledEventId,
        Level = LogLevel.Error,
        Message = "Notification handler '{HandlerName}' failed for '{NotificationName}'")]
    public static partial void NotificationHandlerFailed(ILogger logger, Exception exception, string handlerName, string notificationName);

    #endregion

    #region [ Domain Events ]

    [LoggerMessage(
        EventId = DomainEventRaisedEventId,
        Level = LogLevel.Debug,
        Message = "Domain event '{EventName}' raised by aggregate '{AggregateId}'")]
    public static partial void DomainEventRaised(ILogger logger, string eventName, string aggregateId);

    [LoggerMessage(
        EventId = DomainEventDispatchedEventId,
        Level = LogLevel.Debug,
        Message = "Domain event '{EventName}' dispatched. Handlers: {HandlerCount}")]
    public static partial void DomainEventDispatched(ILogger logger, string eventName, int handlerCount);

    [LoggerMessage(
        EventId = IntegrationEventPublishedEventId,
        Level = LogLevel.Information,
        Message = "Integration event '{EventName}' published to outbox. Id: {EventId}")]
    public static partial void IntegrationEventPublished(ILogger logger, string eventName, string eventId);

    #endregion

    #region [ Behaviors ]

    [LoggerMessage(
        EventId = BehaviorExecutedEventId,
        Level = LogLevel.Trace,
        Message = "Behavior '{BehaviorName}' executed for '{RequestName}' in {ElapsedMs}ms")]
    public static partial void BehaviorExecuted(ILogger logger, string behaviorName, string requestName, long elapsedMs);

    [LoggerMessage(
        EventId = BehaviorExecutedEventId,
        Level = LogLevel.Debug,
        Message = "Behavior pipeline for '{RequestName}': {BehaviorChain}")]
    public static partial void BehaviorPipelineStarted(ILogger logger, string requestName, string behaviorChain);

    #endregion

    #region [ Validation ]

    [LoggerMessage(
        EventId = ValidationEventId,
        Level = LogLevel.Warning,
        Message = "Validation failed for '{RequestName}': {ErrorCount} errors")]
    public static partial void ValidationFailed(ILogger logger, string requestName, int errorCount);

    [LoggerMessage(
        EventId = ValidationEventId,
        Level = LogLevel.Debug,
        Message = "Validation passed for '{RequestName}'")]
    public static partial void ValidationPassed(ILogger logger, string requestName);

    #endregion

    #region [ Caching ]

    [LoggerMessage(
        EventId = CachingEventId,
        Level = LogLevel.Debug,
        Message = "Cache hit for query '{QueryName}'. Key: {CacheKey}")]
    public static partial void CacheHit(ILogger logger, string queryName, string cacheKey);

    [LoggerMessage(
        EventId = CachingEventId,
        Level = LogLevel.Debug,
        Message = "Cache miss for query '{QueryName}'. Key: {CacheKey}")]
    public static partial void CacheMiss(ILogger logger, string queryName, string cacheKey);

    [LoggerMessage(
        EventId = CachingEventId,
        Level = LogLevel.Debug,
        Message = "Cache invalidated for key '{CacheKey}' due to command '{CommandName}'")]
    public static partial void CacheInvalidated(ILogger logger, string cacheKey, string commandName);

    #endregion

    #region [ Transactions ]

    [LoggerMessage(
        EventId = TransactionEventId,
        Level = LogLevel.Debug,
        Message = "Transaction started for command '{CommandName}'")]
    public static partial void TransactionStarted(ILogger logger, string commandName);

    [LoggerMessage(
        EventId = TransactionEventId,
        Level = LogLevel.Debug,
        Message = "Transaction committed for command '{CommandName}'. Events dispatched: {EventCount}")]
    public static partial void TransactionCommitted(ILogger logger, string commandName, int eventCount);

    [LoggerMessage(
        EventId = TransactionEventId,
        Level = LogLevel.Warning,
        Message = "Transaction rolled back for command '{CommandName}'")]
    public static partial void TransactionRolledBack(ILogger logger, Exception exception, string commandName);

    #endregion

    #region [ Idempotency ]

    [LoggerMessage(
        EventId = IdempotencyEventId,
        Level = LogLevel.Information,
        Message = "Duplicate command '{CommandName}' detected. IdempotencyKey: {IdempotencyKey}")]
    public static partial void DuplicateCommandDetected(ILogger logger, string commandName, string idempotencyKey);

    [LoggerMessage(
        EventId = IdempotencyEventId,
        Level = LogLevel.Debug,
        Message = "Idempotency key '{IdempotencyKey}' stored for command '{CommandName}'")]
    public static partial void IdempotencyKeyStored(ILogger logger, string idempotencyKey, string commandName);

    #endregion

    #region [ Saga ]

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' started. SagaId: {SagaId}")]
    public static partial void SagaStarted(ILogger logger, string sagaName, string sagaId);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' completed. SagaId: {SagaId}. Duration: {ElapsedMs}ms")]
    public static partial void SagaCompleted(ILogger logger, string sagaName, string sagaId, long elapsedMs);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Warning,
        Message = "Saga '{SagaName}' step '{StepName}' failed. Starting compensation...")]
    public static partial void SagaStepFailed(ILogger logger, string sagaName, string stepName);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' compensation completed. CompensatedSteps: {StepCount}")]
    public static partial void SagaCompensationCompleted(ILogger logger, string sagaName, int stepCount);

    #endregion

    #region [ Event Sourcing ]

    [LoggerMessage(
        EventId = EventSourcingEventId,
        Level = LogLevel.Debug,
        Message = "Event '{EventName}' appended to stream '{StreamId}'. Version: {Version}")]
    public static partial void EventAppended(ILogger logger, string eventName, string streamId, long version);

    [LoggerMessage(
        EventId = EventSourcingEventId,
        Level = LogLevel.Debug,
        Message = "Aggregate '{AggregateId}' loaded from {EventCount} events")]
    public static partial void AggregateLoaded(ILogger logger, string aggregateId, int eventCount);

    [LoggerMessage(
        EventId = EventSourcingEventId,
        Level = LogLevel.Debug,
        Message = "Snapshot created for '{AggregateId}' at version {Version}")]
    public static partial void SnapshotCreated(ILogger logger, string aggregateId, long version);

    [LoggerMessage(
        EventId = EventSourcingEventId,
        Level = LogLevel.Debug,
        Message = "Projection '{ProjectionName}' updated. Position: {Position}")]
    public static partial void ProjectionUpdated(ILogger logger, string projectionName, long position);

    #endregion

    #region [ Scheduled Commands ]

    [LoggerMessage(
        EventId = ScheduledCommandEventId,
        Level = LogLevel.Information,
        Message = "Command '{CommandName}' scheduled for {ScheduledTime}. Id: {ScheduledCommandId}")]
    public static partial void CommandScheduled(ILogger logger, string commandName, DateTimeOffset scheduledTime, string scheduledCommandId);

    [LoggerMessage(
        EventId = ScheduledCommandEventId,
        Level = LogLevel.Information,
        Message = "Scheduled command '{ScheduledCommandId}' executed. Duration: {ElapsedMs}ms")]
    public static partial void ScheduledCommandExecuted(ILogger logger, string scheduledCommandId, long elapsedMs);

    [LoggerMessage(
        EventId = ScheduledCommandEventId,
        Level = LogLevel.Warning,
        Message = "Scheduled command '{ScheduledCommandId}' failed. Retry: {RetryCount}/{MaxRetries}")]
    public static partial void ScheduledCommandFailed(ILogger logger, Exception exception, string scheduledCommandId, int retryCount, int maxRetries);

    #endregion

    #region [ Audit ]

    [LoggerMessage(
        EventId = AuditEventId,
        Level = LogLevel.Information,
        Message = "Audit: {Action} on '{EntityType}' by '{UserId}'. EntityId: {EntityId}")]
    public static partial void AuditEntry(ILogger logger, string action, string entityType, string userId, string entityId);

    #endregion
}

