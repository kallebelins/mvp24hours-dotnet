//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;

namespace Mvp24Hours.Core.Logging;

/// <summary>
/// High-performance source-generated logger messages for the Core module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// <para>
/// Source-generated logging provides:
/// <list type="bullet">
/// <item><description>Zero allocation at runtime (no boxing)</description></item>
/// <item><description>Better performance than string interpolation</description></item>
/// <item><description>Compile-time validation of log message templates</description></item>
/// <item><description>Native AOT compatibility</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your class:
/// private readonly ILogger&lt;MyService&gt; _logger;
/// 
/// // Usage:
/// CoreLoggerMessages.OperationStarted(_logger, "ProcessOrder", orderId.ToString());
/// </code>
/// </example>
public static partial class CoreLoggerMessages
{
    #region [ Event IDs - Core Module: 1000-1999 ]

    /// <summary>Event ID base for Core module operations.</summary>
    private const int CoreEventIdBase = 1000;

    /// <summary>Event ID for operation started.</summary>
    public const int OperationStartedEventId = CoreEventIdBase + 1;

    /// <summary>Event ID for operation completed.</summary>
    public const int OperationCompletedEventId = CoreEventIdBase + 2;

    /// <summary>Event ID for operation failed.</summary>
    public const int OperationFailedEventId = CoreEventIdBase + 3;

    /// <summary>Event ID for validation errors.</summary>
    public const int ValidationErrorEventId = CoreEventIdBase + 4;

    /// <summary>Event ID for guard clause violations.</summary>
    public const int GuardClauseViolationEventId = CoreEventIdBase + 5;

    /// <summary>Event ID for configuration loaded.</summary>
    public const int ConfigurationLoadedEventId = CoreEventIdBase + 6;

    /// <summary>Event ID for service registered.</summary>
    public const int ServiceRegisteredEventId = CoreEventIdBase + 7;

    /// <summary>Event ID for cache hit.</summary>
    public const int CacheHitEventId = CoreEventIdBase + 8;

    /// <summary>Event ID for cache miss.</summary>
    public const int CacheMissEventId = CoreEventIdBase + 9;

    /// <summary>Event ID for slow operation warning.</summary>
    public const int SlowOperationWarningEventId = CoreEventIdBase + 10;

    /// <summary>Event ID for retry attempt.</summary>
    public const int RetryAttemptEventId = CoreEventIdBase + 11;

    /// <summary>Event ID for circuit breaker state change.</summary>
    public const int CircuitBreakerStateChangedEventId = CoreEventIdBase + 12;

    /// <summary>Event ID for rate limit exceeded.</summary>
    public const int RateLimitExceededEventId = CoreEventIdBase + 13;

    /// <summary>Event ID for entity created.</summary>
    public const int EntityCreatedEventId = CoreEventIdBase + 14;

    /// <summary>Event ID for entity updated.</summary>
    public const int EntityUpdatedEventId = CoreEventIdBase + 15;

    /// <summary>Event ID for entity deleted.</summary>
    public const int EntityDeletedEventId = CoreEventIdBase + 16;

    /// <summary>Event ID for specification evaluated.</summary>
    public const int SpecificationEvaluatedEventId = CoreEventIdBase + 17;

    /// <summary>Event ID for channel message sent.</summary>
    public const int ChannelMessageSentEventId = CoreEventIdBase + 18;

    /// <summary>Event ID for channel message received.</summary>
    public const int ChannelMessageReceivedEventId = CoreEventIdBase + 19;

    /// <summary>Event ID for timer tick.</summary>
    public const int TimerTickEventId = CoreEventIdBase + 20;

    #endregion

    #region [ Operation Lifecycle ]

    /// <summary>
    /// Logs when an operation starts.
    /// </summary>
    [LoggerMessage(
        EventId = OperationStartedEventId,
        Level = LogLevel.Information,
        Message = "Operation {OperationName} started for {EntityId}")]
    public static partial void OperationStarted(ILogger logger, string operationName, string entityId);

    /// <summary>
    /// Logs when an operation starts without entity ID.
    /// </summary>
    [LoggerMessage(
        EventId = OperationStartedEventId,
        Level = LogLevel.Information,
        Message = "Operation {OperationName} started")]
    public static partial void OperationStartedSimple(ILogger logger, string operationName);

    /// <summary>
    /// Logs when an operation completes successfully.
    /// </summary>
    [LoggerMessage(
        EventId = OperationCompletedEventId,
        Level = LogLevel.Information,
        Message = "Operation {OperationName} completed in {ElapsedMs}ms")]
    public static partial void OperationCompleted(ILogger logger, string operationName, long elapsedMs);

    /// <summary>
    /// Logs when an operation completes with result.
    /// </summary>
    [LoggerMessage(
        EventId = OperationCompletedEventId,
        Level = LogLevel.Information,
        Message = "Operation {OperationName} completed in {ElapsedMs}ms with result: {ResultType}")]
    public static partial void OperationCompletedWithResult(ILogger logger, string operationName, long elapsedMs, string resultType);

    /// <summary>
    /// Logs when an operation fails with an exception.
    /// </summary>
    [LoggerMessage(
        EventId = OperationFailedEventId,
        Level = LogLevel.Error,
        Message = "Operation {OperationName} failed after {ElapsedMs}ms")]
    public static partial void OperationFailed(ILogger logger, Exception exception, string operationName, long elapsedMs);

    /// <summary>
    /// Logs when an operation fails with details.
    /// </summary>
    [LoggerMessage(
        EventId = OperationFailedEventId,
        Level = LogLevel.Error,
        Message = "Operation {OperationName} failed for {EntityId}: {ErrorMessage}")]
    public static partial void OperationFailedWithDetails(ILogger logger, string operationName, string entityId, string errorMessage);

    #endregion

    #region [ Validation ]

    /// <summary>
    /// Logs validation errors.
    /// </summary>
    [LoggerMessage(
        EventId = ValidationErrorEventId,
        Level = LogLevel.Warning,
        Message = "Validation failed for {TypeName}: {ErrorCount} errors found")]
    public static partial void ValidationFailed(ILogger logger, string typeName, int errorCount);

    /// <summary>
    /// Logs a specific validation error.
    /// </summary>
    [LoggerMessage(
        EventId = ValidationErrorEventId,
        Level = LogLevel.Warning,
        Message = "Validation error on {PropertyName}: {ErrorMessage}")]
    public static partial void ValidationError(ILogger logger, string propertyName, string errorMessage);

    /// <summary>
    /// Logs guard clause violations.
    /// </summary>
    [LoggerMessage(
        EventId = GuardClauseViolationEventId,
        Level = LogLevel.Warning,
        Message = "Guard clause '{GuardType}' violated for parameter '{ParameterName}': {Message}")]
    public static partial void GuardClauseViolation(ILogger logger, string guardType, string parameterName, string message);

    #endregion

    #region [ Configuration ]

    /// <summary>
    /// Logs when configuration is loaded.
    /// </summary>
    [LoggerMessage(
        EventId = ConfigurationLoadedEventId,
        Level = LogLevel.Information,
        Message = "Configuration '{ConfigurationName}' loaded from {Source}")]
    public static partial void ConfigurationLoaded(ILogger logger, string configurationName, string source);

    /// <summary>
    /// Logs when a service is registered.
    /// </summary>
    [LoggerMessage(
        EventId = ServiceRegisteredEventId,
        Level = LogLevel.Debug,
        Message = "Service {ServiceType} registered with lifetime {Lifetime}")]
    public static partial void ServiceRegistered(ILogger logger, string serviceType, string lifetime);

    #endregion

    #region [ Caching ]

    /// <summary>
    /// Logs a cache hit.
    /// </summary>
    [LoggerMessage(
        EventId = CacheHitEventId,
        Level = LogLevel.Debug,
        Message = "Cache hit for key '{CacheKey}'")]
    public static partial void CacheHit(ILogger logger, string cacheKey);

    /// <summary>
    /// Logs a cache miss.
    /// </summary>
    [LoggerMessage(
        EventId = CacheMissEventId,
        Level = LogLevel.Debug,
        Message = "Cache miss for key '{CacheKey}'")]
    public static partial void CacheMiss(ILogger logger, string cacheKey);

    /// <summary>
    /// Logs cache entry created.
    /// </summary>
    [LoggerMessage(
        EventId = CacheHitEventId,
        Level = LogLevel.Debug,
        Message = "Cache entry created for key '{CacheKey}' with TTL {TtlSeconds}s")]
    public static partial void CacheEntryCreated(ILogger logger, string cacheKey, int ttlSeconds);

    /// <summary>
    /// Logs cache entry evicted.
    /// </summary>
    [LoggerMessage(
        EventId = CacheMissEventId,
        Level = LogLevel.Debug,
        Message = "Cache entry evicted for key '{CacheKey}'. Reason: {EvictionReason}")]
    public static partial void CacheEntryEvicted(ILogger logger, string cacheKey, string evictionReason);

    #endregion

    #region [ Performance ]

    /// <summary>
    /// Logs a slow operation warning.
    /// </summary>
    [LoggerMessage(
        EventId = SlowOperationWarningEventId,
        Level = LogLevel.Warning,
        Message = "Slow operation detected: {OperationName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowOperationWarning(ILogger logger, string operationName, long elapsedMs, long thresholdMs);

    #endregion

    #region [ Resiliency ]

    /// <summary>
    /// Logs a retry attempt.
    /// </summary>
    [LoggerMessage(
        EventId = RetryAttemptEventId,
        Level = LogLevel.Warning,
        Message = "Retry attempt {AttemptNumber}/{MaxRetries} for {OperationName}. Waiting {DelayMs}ms before next attempt")]
    public static partial void RetryAttempt(ILogger logger, int attemptNumber, int maxRetries, string operationName, int delayMs);

    /// <summary>
    /// Logs retry exhausted.
    /// </summary>
    [LoggerMessage(
        EventId = RetryAttemptEventId,
        Level = LogLevel.Error,
        Message = "Retry exhausted after {Attempts} attempts for {OperationName}")]
    public static partial void RetryExhausted(ILogger logger, Exception exception, int attempts, string operationName);

    /// <summary>
    /// Logs circuit breaker state change.
    /// </summary>
    [LoggerMessage(
        EventId = CircuitBreakerStateChangedEventId,
        Level = LogLevel.Warning,
        Message = "Circuit breaker '{BreakerName}' state changed from {FromState} to {ToState}")]
    public static partial void CircuitBreakerStateChanged(ILogger logger, string breakerName, string fromState, string toState);

    /// <summary>
    /// Logs rate limit exceeded.
    /// </summary>
    [LoggerMessage(
        EventId = RateLimitExceededEventId,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for {ResourceKey}. Limit: {Limit}, Retry after: {RetryAfterSeconds}s")]
    public static partial void RateLimitExceeded(ILogger logger, string resourceKey, int limit, int retryAfterSeconds);

    #endregion

    #region [ Entity Operations ]

    /// <summary>
    /// Logs entity created.
    /// </summary>
    [LoggerMessage(
        EventId = EntityCreatedEventId,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} created with ID {EntityId}")]
    public static partial void EntityCreated(ILogger logger, string entityType, string entityId);

    /// <summary>
    /// Logs entity updated.
    /// </summary>
    [LoggerMessage(
        EventId = EntityUpdatedEventId,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} with ID {EntityId} updated")]
    public static partial void EntityUpdated(ILogger logger, string entityType, string entityId);

    /// <summary>
    /// Logs entity deleted.
    /// </summary>
    [LoggerMessage(
        EventId = EntityDeletedEventId,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} with ID {EntityId} deleted (soft: {IsSoftDelete})")]
    public static partial void EntityDeleted(ILogger logger, string entityType, string entityId, bool isSoftDelete);

    #endregion

    #region [ Specification ]

    /// <summary>
    /// Logs specification evaluation.
    /// </summary>
    [LoggerMessage(
        EventId = SpecificationEvaluatedEventId,
        Level = LogLevel.Debug,
        Message = "Specification {SpecificationName} evaluated. Matches: {MatchCount}")]
    public static partial void SpecificationEvaluated(ILogger logger, string specificationName, int matchCount);

    #endregion

    #region [ Channels ]

    /// <summary>
    /// Logs channel message sent.
    /// </summary>
    [LoggerMessage(
        EventId = ChannelMessageSentEventId,
        Level = LogLevel.Debug,
        Message = "Message sent to channel '{ChannelName}'. Type: {MessageType}")]
    public static partial void ChannelMessageSent(ILogger logger, string channelName, string messageType);

    /// <summary>
    /// Logs channel message received.
    /// </summary>
    [LoggerMessage(
        EventId = ChannelMessageReceivedEventId,
        Level = LogLevel.Debug,
        Message = "Message received from channel '{ChannelName}'. Type: {MessageType}")]
    public static partial void ChannelMessageReceived(ILogger logger, string channelName, string messageType);

    /// <summary>
    /// Logs channel backpressure.
    /// </summary>
    [LoggerMessage(
        EventId = ChannelMessageSentEventId,
        Level = LogLevel.Warning,
        Message = "Channel '{ChannelName}' experiencing backpressure. Pending: {PendingCount}")]
    public static partial void ChannelBackpressure(ILogger logger, string channelName, int pendingCount);

    #endregion

    #region [ Timers ]

    /// <summary>
    /// Logs timer tick.
    /// </summary>
    [LoggerMessage(
        EventId = TimerTickEventId,
        Level = LogLevel.Trace,
        Message = "Timer '{TimerName}' tick at {TickTime}")]
    public static partial void TimerTick(ILogger logger, string timerName, DateTimeOffset tickTime);

    /// <summary>
    /// Logs timer started.
    /// </summary>
    [LoggerMessage(
        EventId = TimerTickEventId,
        Level = LogLevel.Information,
        Message = "Timer '{TimerName}' started with interval {IntervalMs}ms")]
    public static partial void TimerStarted(ILogger logger, string timerName, long intervalMs);

    /// <summary>
    /// Logs timer stopped.
    /// </summary>
    [LoggerMessage(
        EventId = TimerTickEventId,
        Level = LogLevel.Information,
        Message = "Timer '{TimerName}' stopped")]
    public static partial void TimerStopped(ILogger logger, string timerName);

    #endregion
}

