//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for CQRS/Mediator operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// commands, queries, notifications, domain events, and behaviors.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>commands_total</c> - Counter for commands processed</item>
/// <item><c>command_duration_ms</c> - Histogram for command duration</item>
/// <item><c>queries_total</c> - Counter for queries processed</item>
/// <item><c>query_duration_ms</c> - Histogram for query duration</item>
/// <item><c>notifications_total</c> - Counter for notifications published</item>
/// <item><c>domain_events_total</c> - Counter for domain events dispatched</item>
/// <item><c>integration_events_total</c> - Counter for integration events</item>
/// <item><c>behaviors_total</c> - Counter for behavior executions</item>
/// <item><c>validation_failures_total</c> - Counter for validation failures</item>
/// <item><c>cache_hits_total</c> / <c>cache_misses_total</c> - Cache metrics</item>
/// <item><c>retries_total</c> - Counter for retry attempts</item>
/// <item><c>circuit_breaker_trips_total</c> - Counter for circuit breaker trips</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class LoggingBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
/// {
///     private readonly CqrsMetrics _metrics;
///     
///     public async Task&lt;TResponse&gt; Handle(TRequest request, ...)
///     {
///         using var scope = _metrics.BeginCommand(typeof(TRequest).Name);
///         try
///         {
///             var result = await next();
///             scope.Complete();
///             return result;
///         }
///         catch
///         {
///             scope.Fail();
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
public sealed class CqrsMetrics
{
    #region Counters and Histograms

    private readonly Counter<long> _commandsTotal;
    private readonly Counter<long> _commandsFailedTotal;
    private readonly Histogram<double> _commandDuration;
    private readonly Counter<long> _queriesTotal;
    private readonly Counter<long> _queriesFailedTotal;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _notificationsTotal;
    private readonly Counter<long> _notificationsFailedTotal;
    private readonly Histogram<double> _notificationDuration;
    private readonly Counter<long> _domainEventsTotal;
    private readonly Counter<long> _integrationEventsTotal;
    private readonly Counter<long> _behaviorsTotal;
    private readonly Histogram<double> _behaviorDuration;
    private readonly Counter<long> _sagasTotal;
    private readonly Counter<long> _sagasCompletedTotal;
    private readonly Counter<long> _sagasFailedTotal;
    private readonly Counter<long> _validationFailuresTotal;
    private readonly Counter<long> _cacheHitsTotal;
    private readonly Counter<long> _cacheMissesTotal;
    private readonly Counter<long> _idempotentDuplicatesTotal;
    private readonly Counter<long> _retriesTotal;
    private readonly Counter<long> _circuitBreakerTripsTotal;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsMetrics"/> class.
    /// </summary>
    public CqrsMetrics()
    {
        var meter = Mvp24HoursMeters.Cqrs.Meter;

        // Commands
        _commandsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsCommandsTotal,
            unit: "{commands}",
            description: "Total number of commands processed");

        _commandsFailedTotal = meter.CreateCounter<long>(
            MetricNames.CqrsCommandsFailedTotal,
            unit: "{commands}",
            description: "Total number of failed commands");

        _commandDuration = meter.CreateHistogram<double>(
            MetricNames.CqrsCommandDuration,
            unit: "ms",
            description: "Duration of command processing in milliseconds");

        // Queries
        _queriesTotal = meter.CreateCounter<long>(
            MetricNames.CqrsQueriesTotal,
            unit: "{queries}",
            description: "Total number of queries processed");

        _queriesFailedTotal = meter.CreateCounter<long>(
            MetricNames.CqrsQueriesFailedTotal,
            unit: "{queries}",
            description: "Total number of failed queries");

        _queryDuration = meter.CreateHistogram<double>(
            MetricNames.CqrsQueryDuration,
            unit: "ms",
            description: "Duration of query processing in milliseconds");

        // Notifications
        _notificationsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsNotificationsTotal,
            unit: "{notifications}",
            description: "Total number of notifications published");

        _notificationsFailedTotal = meter.CreateCounter<long>(
            MetricNames.CqrsNotificationsFailedTotal,
            unit: "{notifications}",
            description: "Total number of failed notification handlers");

        _notificationDuration = meter.CreateHistogram<double>(
            MetricNames.CqrsNotificationDuration,
            unit: "ms",
            description: "Duration of notification handling in milliseconds");

        // Events
        _domainEventsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsDomainEventsTotal,
            unit: "{events}",
            description: "Total number of domain events dispatched");

        _integrationEventsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsIntegrationEventsTotal,
            unit: "{events}",
            description: "Total number of integration events published");

        // Behaviors
        _behaviorsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsBehaviorsTotal,
            unit: "{behaviors}",
            description: "Total number of behavior executions");

        _behaviorDuration = meter.CreateHistogram<double>(
            MetricNames.CqrsBehaviorDuration,
            unit: "ms",
            description: "Duration of behavior execution in milliseconds");

        // Sagas
        _sagasTotal = meter.CreateCounter<long>(
            MetricNames.CqrsSagasTotal,
            unit: "{sagas}",
            description: "Total number of saga instances");

        _sagasCompletedTotal = meter.CreateCounter<long>(
            MetricNames.CqrsSagasCompletedTotal,
            unit: "{sagas}",
            description: "Total number of completed sagas");

        _sagasFailedTotal = meter.CreateCounter<long>(
            MetricNames.CqrsSagasFailedTotal,
            unit: "{sagas}",
            description: "Total number of failed sagas");

        // Validation & Caching
        _validationFailuresTotal = meter.CreateCounter<long>(
            MetricNames.CqrsValidationFailuresTotal,
            unit: "{failures}",
            description: "Total number of validation failures");

        _cacheHitsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsCacheHitsTotal,
            unit: "{hits}",
            description: "Total number of cache hits in caching behavior");

        _cacheMissesTotal = meter.CreateCounter<long>(
            MetricNames.CqrsCacheMissesTotal,
            unit: "{misses}",
            description: "Total number of cache misses in caching behavior");

        // Resiliency
        _idempotentDuplicatesTotal = meter.CreateCounter<long>(
            MetricNames.CqrsIdempotentDuplicatesTotal,
            unit: "{duplicates}",
            description: "Total number of idempotent request deduplication");

        _retriesTotal = meter.CreateCounter<long>(
            MetricNames.CqrsRetriesTotal,
            unit: "{retries}",
            description: "Total number of retry attempts");

        _circuitBreakerTripsTotal = meter.CreateCounter<long>(
            MetricNames.CqrsCircuitBreakerTripsTotal,
            unit: "{trips}",
            description: "Total number of circuit breaker trips");
    }

    #region Command Methods

    /// <summary>
    /// Begins tracking a command execution.
    /// </summary>
    /// <param name="commandType">Type name of the command.</param>
    /// <returns>A scope that should be disposed when command completes.</returns>
    public RequestScope BeginCommand(string commandType)
    {
        return new RequestScope(this, commandType, RequestKind.Command);
    }

    /// <summary>
    /// Records a command execution.
    /// </summary>
    /// <param name="commandType">Type name of the command.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the command was successful.</param>
    public void RecordCommand(string commandType, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.CommandType, commandType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _commandsTotal.Add(1, tags);

        if (!success)
        {
            _commandsFailedTotal.Add(1, tags);
        }

        _commandDuration.Record(durationMs, tags);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Begins tracking a query execution.
    /// </summary>
    /// <param name="queryType">Type name of the query.</param>
    /// <returns>A scope that should be disposed when query completes.</returns>
    public RequestScope BeginQuery(string queryType)
    {
        return new RequestScope(this, queryType, RequestKind.Query);
    }

    /// <summary>
    /// Records a query execution.
    /// </summary>
    /// <param name="queryType">Type name of the query.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the query was successful.</param>
    public void RecordQuery(string queryType, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.QueryType, queryType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _queriesTotal.Add(1, tags);

        if (!success)
        {
            _queriesFailedTotal.Add(1, tags);
        }

        _queryDuration.Record(durationMs, tags);
    }

    #endregion

    #region Notification Methods

    /// <summary>
    /// Begins tracking a notification handling.
    /// </summary>
    /// <param name="notificationType">Type name of the notification.</param>
    /// <returns>A scope that should be disposed when handling completes.</returns>
    public RequestScope BeginNotification(string notificationType)
    {
        return new RequestScope(this, notificationType, RequestKind.Notification);
    }

    /// <summary>
    /// Records a notification handling.
    /// </summary>
    /// <param name="notificationType">Type name of the notification.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the handling was successful.</param>
    public void RecordNotification(string notificationType, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.MessageType, notificationType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _notificationsTotal.Add(1, tags);

        if (!success)
        {
            _notificationsFailedTotal.Add(1, tags);
        }

        _notificationDuration.Record(durationMs, tags);
    }

    #endregion

    #region Event Methods

    /// <summary>
    /// Records a domain event dispatch.
    /// </summary>
    /// <param name="eventType">Type name of the domain event.</param>
    public void RecordDomainEvent(string eventType)
    {
        var tags = new TagList { { MetricTags.MessageType, eventType } };
        _domainEventsTotal.Add(1, tags);
    }

    /// <summary>
    /// Records an integration event publication.
    /// </summary>
    /// <param name="eventType">Type name of the integration event.</param>
    public void RecordIntegrationEvent(string eventType)
    {
        var tags = new TagList { { MetricTags.MessageType, eventType } };
        _integrationEventsTotal.Add(1, tags);
    }

    #endregion

    #region Behavior Methods

    /// <summary>
    /// Begins tracking a behavior execution.
    /// </summary>
    /// <param name="behaviorName">Name of the behavior.</param>
    /// <returns>A scope that should be disposed when behavior completes.</returns>
    public BehaviorScope BeginBehavior(string behaviorName)
    {
        return new BehaviorScope(this, behaviorName);
    }

    /// <summary>
    /// Records a behavior execution.
    /// </summary>
    /// <param name="behaviorName">Name of the behavior.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the behavior was successful.</param>
    public void RecordBehavior(string behaviorName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.BehaviorName, behaviorName },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _behaviorsTotal.Add(1, tags);
        _behaviorDuration.Record(durationMs, tags);
    }

    #endregion

    #region Saga Methods

    /// <summary>
    /// Records a saga instance start.
    /// </summary>
    /// <param name="sagaType">Type name of the saga.</param>
    public void RecordSagaStart(string sagaType)
    {
        var tags = new TagList { { MetricTags.OperationName, sagaType } };
        _sagasTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a saga completion.
    /// </summary>
    /// <param name="sagaType">Type name of the saga.</param>
    public void RecordSagaCompleted(string sagaType)
    {
        var tags = new TagList { { MetricTags.OperationName, sagaType } };
        _sagasCompletedTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a saga failure.
    /// </summary>
    /// <param name="sagaType">Type name of the saga.</param>
    public void RecordSagaFailed(string sagaType)
    {
        var tags = new TagList { { MetricTags.OperationName, sagaType } };
        _sagasFailedTotal.Add(1, tags);
    }

    #endregion

    #region Validation & Caching Methods

    /// <summary>
    /// Records a validation failure.
    /// </summary>
    /// <param name="requestType">Type name of the request that failed validation.</param>
    public void RecordValidationFailure(string requestType)
    {
        var tags = new TagList { { MetricTags.CommandType, requestType } };
        _validationFailuresTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    /// <param name="requestType">Type name of the cached request.</param>
    public void RecordCacheHit(string requestType)
    {
        var tags = new TagList { { MetricTags.QueryType, requestType } };
        _cacheHitsTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    /// <param name="requestType">Type name of the request.</param>
    public void RecordCacheMiss(string requestType)
    {
        var tags = new TagList { { MetricTags.QueryType, requestType } };
        _cacheMissesTotal.Add(1, tags);
    }

    #endregion

    #region Resiliency Methods

    /// <summary>
    /// Records an idempotent duplicate detection.
    /// </summary>
    /// <param name="commandType">Type name of the command.</param>
    public void RecordIdempotentDuplicate(string commandType)
    {
        var tags = new TagList { { MetricTags.CommandType, commandType } };
        _idempotentDuplicatesTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    /// <param name="requestType">Type name of the request being retried.</param>
    /// <param name="attemptNumber">The retry attempt number.</param>
    public void RecordRetry(string requestType, int attemptNumber)
    {
        var tags = new TagList
        {
            { MetricTags.CommandType, requestType },
            { "attempt", attemptNumber }
        };
        _retriesTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a circuit breaker trip.
    /// </summary>
    /// <param name="requestType">Type name of the request that caused the trip.</param>
    public void RecordCircuitBreakerTrip(string requestType)
    {
        var tags = new TagList { { MetricTags.CommandType, requestType } };
        _circuitBreakerTripsTotal.Add(1, tags);
    }

    #endregion

    #region Scope Structs

    /// <summary>
    /// Kind of mediator request.
    /// </summary>
    public enum RequestKind
    {
        /// <summary>Command request.</summary>
        Command,
        /// <summary>Query request.</summary>
        Query,
        /// <summary>Notification request.</summary>
        Notification
    }

    /// <summary>
    /// Represents a scope for tracking request execution duration.
    /// </summary>
    public struct RequestScope : IDisposable
    {
        private readonly CqrsMetrics _metrics;
        private readonly string _typeName;
        private readonly RequestKind _kind;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the request succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal RequestScope(CqrsMetrics metrics, string typeName, RequestKind kind)
        {
            _metrics = metrics;
            _typeName = typeName;
            _kind = kind;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the request as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the request as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            var durationMs = elapsed.TotalMilliseconds;

            switch (_kind)
            {
                case RequestKind.Command:
                    _metrics.RecordCommand(_typeName, durationMs, Succeeded);
                    break;
                case RequestKind.Query:
                    _metrics.RecordQuery(_typeName, durationMs, Succeeded);
                    break;
                case RequestKind.Notification:
                    _metrics.RecordNotification(_typeName, durationMs, Succeeded);
                    break;
            }
        }
    }

    /// <summary>
    /// Represents a scope for tracking behavior execution duration.
    /// </summary>
    public struct BehaviorScope : IDisposable
    {
        private readonly CqrsMetrics _metrics;
        private readonly string _behaviorName;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the behavior succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal BehaviorScope(CqrsMetrics metrics, string behaviorName)
        {
            _metrics = metrics;
            _behaviorName = behaviorName;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the behavior as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the behavior as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordBehavior(_behaviorName, elapsed.TotalMilliseconds, Succeeded);
        }
    }

    #endregion
}

