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
/// Each logging method has a unique event ID.
/// </remarks>
public static partial class CqrsLoggerMessages
{
    #region [ Event IDs - CQRS Module: 3000-3999 ]

    private const int CqrsEventIdBase = 3000;

    // Commands (3001-3010)
    /// <summary>Event ID for when a command execution starts.</summary>
    public const int CommandStartedEventId = CqrsEventIdBase + 1;
    /// <summary>Event ID for when a command execution completes successfully.</summary>
    public const int CommandCompletedEventId = CqrsEventIdBase + 2;
    /// <summary>Event ID for when a command execution fails.</summary>
    public const int CommandFailedEventId = CqrsEventIdBase + 3;
    /// <summary>Event ID for when a slow command is detected.</summary>
    public const int SlowCommandEventId = CqrsEventIdBase + 4;

    // Queries (3011-3020)
    /// <summary>Event ID for when a query execution starts.</summary>
    public const int QueryStartedEventId = CqrsEventIdBase + 11;
    /// <summary>Event ID for when a query execution completes successfully.</summary>
    public const int QueryCompletedEventId = CqrsEventIdBase + 12;
    /// <summary>Event ID for when a query execution fails.</summary>
    public const int QueryFailedEventId = CqrsEventIdBase + 13;
    /// <summary>Event ID for when a slow query is detected.</summary>
    public const int SlowQueryEventId = CqrsEventIdBase + 14;

    // Notifications (3021-3030)
    /// <summary>Event ID for when a notification is published to handlers.</summary>
    public const int NotificationPublishedEventId = CqrsEventIdBase + 21;
    /// <summary>Event ID for when a notification is handled by a handler.</summary>
    public const int NotificationHandledEventId = CqrsEventIdBase + 22;
    /// <summary>Event ID for when a notification handler fails.</summary>
    public const int NotificationHandlerFailedEventId = CqrsEventIdBase + 23;

    // Domain Events (3031-3040)
    /// <summary>Event ID for when a domain event is raised by an aggregate.</summary>
    public const int DomainEventRaisedEventId = CqrsEventIdBase + 31;
    /// <summary>Event ID for when a domain event is dispatched to handlers.</summary>
    public const int DomainEventDispatchedEventId = CqrsEventIdBase + 32;
    /// <summary>Event ID for when an integration event is published to the outbox.</summary>
    public const int IntegrationEventPublishedEventId = CqrsEventIdBase + 33;

    // Behaviors (3041-3050)
    /// <summary>Event ID for when a pipeline behavior is executed.</summary>
    public const int BehaviorExecutedEventId = CqrsEventIdBase + 41;
    /// <summary>Event ID for when a behavior pipeline starts.</summary>
    public const int BehaviorPipelineStartedEventId = CqrsEventIdBase + 42;

    // Validation (3051-3060)
    /// <summary>Event ID for when validation fails.</summary>
    public const int ValidationFailedEventId = CqrsEventIdBase + 51;
    /// <summary>Event ID for when validation passes.</summary>
    public const int ValidationPassedEventId = CqrsEventIdBase + 52;

    // Caching (3061-3070)
    /// <summary>Event ID for cache hit.</summary>
    public const int CacheHitEventId = CqrsEventIdBase + 61;
    /// <summary>Event ID for cache miss.</summary>
    public const int CacheMissEventId = CqrsEventIdBase + 62;
    /// <summary>Event ID for cache invalidation.</summary>
    public const int CacheInvalidatedEventId = CqrsEventIdBase + 63;

    // Transactions (3071-3080)
    /// <summary>Event ID for when a transaction starts.</summary>
    public const int TransactionStartedEventId = CqrsEventIdBase + 71;
    /// <summary>Event ID for when a transaction is committed.</summary>
    public const int TransactionCommittedEventId = CqrsEventIdBase + 72;
    /// <summary>Event ID for when a transaction is rolled back.</summary>
    public const int TransactionRolledBackEventId = CqrsEventIdBase + 73;

    // Idempotency (3081-3090)
    /// <summary>Event ID for duplicate command detection.</summary>
    public const int DuplicateCommandDetectedEventId = CqrsEventIdBase + 81;
    /// <summary>Event ID for when an idempotency key is stored.</summary>
    public const int IdempotencyKeyStoredEventId = CqrsEventIdBase + 82;

    // Saga (3091-3100)
    /// <summary>Event ID for when a saga starts.</summary>
    public const int SagaStartedEventId = CqrsEventIdBase + 91;
    /// <summary>Event ID for when a saga completes.</summary>
    public const int SagaCompletedEventId = CqrsEventIdBase + 92;
    /// <summary>Event ID for when a saga step fails.</summary>
    public const int SagaStepFailedEventId = CqrsEventIdBase + 93;
    /// <summary>Event ID for when saga compensation completes.</summary>
    public const int SagaCompensationCompletedEventId = CqrsEventIdBase + 94;

    // Event Sourcing (3101-3110)
    /// <summary>Event ID for when an event is appended to a stream.</summary>
    public const int EventAppendedEventId = CqrsEventIdBase + 101;
    /// <summary>Event ID for when an aggregate is loaded from events.</summary>
    public const int AggregateLoadedEventId = CqrsEventIdBase + 102;
    /// <summary>Event ID for when a snapshot is created.</summary>
    public const int SnapshotCreatedEventId = CqrsEventIdBase + 103;
    /// <summary>Event ID for when a projection is updated.</summary>
    public const int ProjectionUpdatedEventId = CqrsEventIdBase + 104;

    // Scheduled Commands (3111-3120)
    /// <summary>Event ID for when a command is scheduled.</summary>
    public const int CommandScheduledEventId = CqrsEventIdBase + 111;
    /// <summary>Event ID for when a scheduled command is executed.</summary>
    public const int ScheduledCommandExecutedEventId = CqrsEventIdBase + 112;
    /// <summary>Event ID for when a scheduled command fails.</summary>
    public const int ScheduledCommandFailedEventId = CqrsEventIdBase + 113;

    // Audit (3121-3130)
    /// <summary>Event ID for audit trail entries.</summary>
    public const int AuditEventId = CqrsEventIdBase + 121;

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
        EventId = SlowCommandEventId,
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
        EventId = SlowQueryEventId,
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
        EventId = NotificationHandlerFailedEventId,
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
        EventId = BehaviorPipelineStartedEventId,
        Level = LogLevel.Debug,
        Message = "Behavior pipeline for '{RequestName}': {BehaviorChain}")]
    public static partial void BehaviorPipelineStarted(ILogger logger, string requestName, string behaviorChain);

    #endregion

    #region [ Validation ]

    [LoggerMessage(
        EventId = ValidationFailedEventId,
        Level = LogLevel.Warning,
        Message = "Validation failed for '{RequestName}': {ErrorCount} errors")]
    public static partial void ValidationFailed(ILogger logger, string requestName, int errorCount);

    [LoggerMessage(
        EventId = ValidationPassedEventId,
        Level = LogLevel.Debug,
        Message = "Validation passed for '{RequestName}'")]
    public static partial void ValidationPassed(ILogger logger, string requestName);

    #endregion

    #region [ Caching ]

    [LoggerMessage(
        EventId = CacheHitEventId,
        Level = LogLevel.Debug,
        Message = "Cache hit for query '{QueryName}'. Key: {CacheKey}")]
    public static partial void CacheHit(ILogger logger, string queryName, string cacheKey);

    [LoggerMessage(
        EventId = CacheMissEventId,
        Level = LogLevel.Debug,
        Message = "Cache miss for query '{QueryName}'. Key: {CacheKey}")]
    public static partial void CacheMiss(ILogger logger, string queryName, string cacheKey);

    [LoggerMessage(
        EventId = CacheInvalidatedEventId,
        Level = LogLevel.Debug,
        Message = "Cache invalidated for key '{CacheKey}' due to command '{CommandName}'")]
    public static partial void CacheInvalidated(ILogger logger, string cacheKey, string commandName);

    #endregion

    #region [ Transactions ]

    [LoggerMessage(
        EventId = TransactionStartedEventId,
        Level = LogLevel.Debug,
        Message = "Transaction started for command '{CommandName}'")]
    public static partial void TransactionStarted(ILogger logger, string commandName);

    [LoggerMessage(
        EventId = TransactionCommittedEventId,
        Level = LogLevel.Debug,
        Message = "Transaction committed for command '{CommandName}'. Events dispatched: {EventCount}")]
    public static partial void TransactionCommitted(ILogger logger, string commandName, int eventCount);

    [LoggerMessage(
        EventId = TransactionRolledBackEventId,
        Level = LogLevel.Warning,
        Message = "Transaction rolled back for command '{CommandName}'")]
    public static partial void TransactionRolledBack(ILogger logger, Exception exception, string commandName);

    #endregion

    #region [ Idempotency ]

    [LoggerMessage(
        EventId = DuplicateCommandDetectedEventId,
        Level = LogLevel.Information,
        Message = "Duplicate command '{CommandName}' detected. IdempotencyKey: {IdempotencyKey}")]
    public static partial void DuplicateCommandDetected(ILogger logger, string commandName, string idempotencyKey);

    [LoggerMessage(
        EventId = IdempotencyKeyStoredEventId,
        Level = LogLevel.Debug,
        Message = "Idempotency key '{IdempotencyKey}' stored for command '{CommandName}'")]
    public static partial void IdempotencyKeyStored(ILogger logger, string idempotencyKey, string commandName);

    #endregion

    #region [ Saga ]

    [LoggerMessage(
        EventId = SagaStartedEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' started. SagaId: {SagaId}")]
    public static partial void SagaStarted(ILogger logger, string sagaName, string sagaId);

    [LoggerMessage(
        EventId = SagaCompletedEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' completed. SagaId: {SagaId}. Duration: {ElapsedMs}ms")]
    public static partial void SagaCompleted(ILogger logger, string sagaName, string sagaId, long elapsedMs);

    [LoggerMessage(
        EventId = SagaStepFailedEventId,
        Level = LogLevel.Warning,
        Message = "Saga '{SagaName}' step '{StepName}' failed. Starting compensation...")]
    public static partial void SagaStepFailed(ILogger logger, string sagaName, string stepName);

    [LoggerMessage(
        EventId = SagaCompensationCompletedEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' compensation completed. CompensatedSteps: {StepCount}")]
    public static partial void SagaCompensationCompleted(ILogger logger, string sagaName, int stepCount);

    #endregion

    #region [ Event Sourcing ]

    [LoggerMessage(
        EventId = EventAppendedEventId,
        Level = LogLevel.Debug,
        Message = "Event '{EventName}' appended to stream '{StreamId}'. Version: {Version}")]
    public static partial void EventAppended(ILogger logger, string eventName, string streamId, long version);

    [LoggerMessage(
        EventId = AggregateLoadedEventId,
        Level = LogLevel.Debug,
        Message = "Aggregate '{AggregateId}' loaded from {EventCount} events")]
    public static partial void AggregateLoaded(ILogger logger, string aggregateId, int eventCount);

    [LoggerMessage(
        EventId = SnapshotCreatedEventId,
        Level = LogLevel.Debug,
        Message = "Snapshot created for '{AggregateId}' at version {Version}")]
    public static partial void SnapshotCreated(ILogger logger, string aggregateId, long version);

    [LoggerMessage(
        EventId = ProjectionUpdatedEventId,
        Level = LogLevel.Debug,
        Message = "Projection '{ProjectionName}' updated. Position: {Position}")]
    public static partial void ProjectionUpdated(ILogger logger, string projectionName, long position);

    #endregion

    #region [ Scheduled Commands ]

    [LoggerMessage(
        EventId = CommandScheduledEventId,
        Level = LogLevel.Information,
        Message = "Command '{CommandName}' scheduled for {ScheduledTime}. Id: {ScheduledCommandId}")]
    public static partial void CommandScheduled(ILogger logger, string commandName, DateTimeOffset scheduledTime, string scheduledCommandId);

    [LoggerMessage(
        EventId = ScheduledCommandExecutedEventId,
        Level = LogLevel.Information,
        Message = "Scheduled command '{ScheduledCommandId}' executed. Duration: {ElapsedMs}ms")]
    public static partial void ScheduledCommandExecuted(ILogger logger, string scheduledCommandId, long elapsedMs);

    [LoggerMessage(
        EventId = ScheduledCommandFailedEventId,
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
