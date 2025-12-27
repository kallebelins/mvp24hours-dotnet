//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for <see cref="Activity"/> providing convenient enrichment and error handling.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify common operations on activities such as setting success/failure status,
/// adding events, and enriching with context information.
/// </para>
/// </remarks>
public static class ActivityExtensions
{
    #region Status Methods

    /// <summary>
    /// Marks the activity as successful with OK status.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? SetSuccess(this Activity? activity)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.OperationSuccess, true);
        activity.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }

    /// <summary>
    /// Marks the activity as successful with additional tags.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <param name="tags">Additional tags to add.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? SetSuccess(this Activity? activity, params (string Key, object? Value)[] tags)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.OperationSuccess, true);
        activity.SetStatus(ActivityStatusCode.Ok);

        foreach (var (key, value) in tags)
        {
            activity.SetTag(key, value);
        }

        return activity;
    }

    /// <summary>
    /// Marks the activity as failed with an exception.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="escaped">Whether the exception escaped the span (default: true).</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? SetError(this Activity? activity, Exception exception, bool escaped = true)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.OperationSuccess, false);
        activity.SetTag(SemanticTags.ErrorType, exception.GetType().FullName);
        activity.SetTag(SemanticTags.ErrorMessage, exception.Message);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception event following OpenTelemetry conventions
        activity.RecordException(exception, escaped);

        return activity;
    }

    /// <summary>
    /// Marks the activity as failed with an error message.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? SetError(this Activity? activity, string errorMessage, string? errorCode = null)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.OperationSuccess, false);
        activity.SetTag(SemanticTags.ErrorMessage, errorMessage);
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);

        if (!string.IsNullOrEmpty(errorCode))
        {
            activity.SetTag(SemanticTags.ErrorCode, errorCode);
        }

        return activity;
    }

    #endregion

    #region Exception Recording

    /// <summary>
    /// Records an exception as an event on the activity following OpenTelemetry semantic conventions.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="exception">The exception to record.</param>
    /// <param name="escaped">Whether the exception escaped the span.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordException(this Activity? activity, Exception exception, bool escaped = true)
    {
        if (activity == null) return null;

        var tags = new ActivityTagsCollection
        {
            { SemanticTags.ExceptionType, exception.GetType().FullName },
            { SemanticTags.ExceptionMessage, exception.Message },
            { SemanticTags.ExceptionEscaped, escaped }
        };

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            tags.Add(SemanticTags.ExceptionStacktrace, exception.StackTrace);
        }

        activity.AddEvent(new ActivityEvent(SemanticEvents.Exception, tags: tags));
        return activity;
    }

    #endregion

    #region Event Recording

    /// <summary>
    /// Records a custom event with optional tags.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="tags">Optional tags for the event.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordEvent(this Activity? activity, string eventName, params (string Key, object? Value)[] tags)
    {
        if (activity == null) return null;

        var activityTags = new ActivityTagsCollection();
        foreach (var (key, value) in tags)
        {
            activityTags.Add(key, value);
        }

        activity.AddEvent(new ActivityEvent(eventName, tags: activityTags));
        return activity;
    }

    /// <summary>
    /// Records a retry attempt event.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <param name="delay">Optional delay before retry.</param>
    /// <param name="reason">Optional reason for retry.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordRetryAttempt(
        this Activity? activity,
        int attemptNumber,
        TimeSpan? delay = null,
        string? reason = null)
    {
        if (activity == null) return null;

        var tags = new ActivityTagsCollection
        {
            { "retry.attempt_number", attemptNumber }
        };

        if (delay.HasValue)
        {
            tags.Add("retry.delay_ms", delay.Value.TotalMilliseconds);
        }

        if (!string.IsNullOrEmpty(reason))
        {
            tags.Add("retry.reason", reason);
        }

        activity.AddEvent(new ActivityEvent(SemanticEvents.RetryAttempt, tags: tags));
        return activity;
    }

    /// <summary>
    /// Records a cache hit event.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordCacheHit(this Activity? activity, string cacheKey)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.CacheHit, true);
        activity.AddEvent(new ActivityEvent(SemanticEvents.CacheHit, tags: new ActivityTagsCollection
        {
            { SemanticTags.CacheKey, cacheKey }
        }));
        return activity;
    }

    /// <summary>
    /// Records a cache miss event.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordCacheMiss(this Activity? activity, string cacheKey)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.CacheHit, false);
        activity.AddEvent(new ActivityEvent(SemanticEvents.CacheMiss, tags: new ActivityTagsCollection
        {
            { SemanticTags.CacheKey, cacheKey }
        }));
        return activity;
    }

    /// <summary>
    /// Records a slow query event.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    /// <param name="thresholdMs">The slow query threshold.</param>
    /// <param name="statement">Optional SQL statement (sanitized).</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordSlowQuery(
        this Activity? activity,
        double durationMs,
        double thresholdMs,
        string? statement = null)
    {
        if (activity == null) return null;

        var tags = new ActivityTagsCollection
        {
            { SemanticTags.OperationDurationMs, durationMs },
            { "slow_query.threshold_ms", thresholdMs }
        };

        if (!string.IsNullOrEmpty(statement))
        {
            tags.Add(SemanticTags.DbStatement, statement);
        }

        activity.AddEvent(new ActivityEvent(SemanticEvents.SlowQueryDetected, tags: tags));
        return activity;
    }

    /// <summary>
    /// Records a validation failure event.
    /// </summary>
    /// <param name="activity">The activity to record on.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? RecordValidationFailure(
        this Activity? activity,
        IEnumerable<string> errors)
    {
        if (activity == null) return null;

        var errorList = errors.ToList();
        var tags = new ActivityTagsCollection
        {
            { "validation.error_count", errorList.Count },
            { "validation.errors", string.Join("; ", errorList.Take(10)) } // Limit to first 10 errors
        };

        activity.AddEvent(new ActivityEvent(SemanticEvents.ValidationFailure, tags: tags));
        return activity;
    }

    #endregion

    #region Context Enrichment

    /// <summary>
    /// Adds correlation ID to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithCorrelationId(this Activity? activity, string? correlationId)
    {
        if (activity == null || string.IsNullOrEmpty(correlationId)) return activity;

        activity.SetTag(SemanticTags.CorrelationId, correlationId);
        activity.SetBaggage("correlation.id", correlationId);
        return activity;
    }

    /// <summary>
    /// Adds causation ID to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="causationId">The causation ID.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithCausationId(this Activity? activity, string? causationId)
    {
        if (activity == null || string.IsNullOrEmpty(causationId)) return activity;

        activity.SetTag(SemanticTags.CausationId, causationId);
        return activity;
    }

    /// <summary>
    /// Adds user context to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="userName">Optional user name.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithUser(this Activity? activity, string? userId, string? userName = null)
    {
        if (activity == null) return null;

        if (!string.IsNullOrEmpty(userId))
        {
            activity.SetTag(SemanticTags.EnduserId, userId);
        }

        if (!string.IsNullOrEmpty(userName))
        {
            activity.SetTag(SemanticTags.EnduserName, userName);
        }

        return activity;
    }

    /// <summary>
    /// Adds tenant context to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="tenantName">Optional tenant name.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithTenant(this Activity? activity, string? tenantId, string? tenantName = null)
    {
        if (activity == null) return null;

        if (!string.IsNullOrEmpty(tenantId))
        {
            activity.SetTag(SemanticTags.TenantId, tenantId);
            activity.SetBaggage("tenant.id", tenantId);
        }

        if (!string.IsNullOrEmpty(tenantName))
        {
            activity.SetTag(SemanticTags.TenantName, tenantName);
        }

        return activity;
    }

    /// <summary>
    /// Adds operation timing information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithDuration(this Activity? activity, double durationMs)
    {
        if (activity == null) return null;

        activity.SetTag(SemanticTags.OperationDurationMs, durationMs);
        return activity;
    }

    /// <summary>
    /// Adds database context to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="dbSystem">The database system (e.g., sqlserver, postgresql).</param>
    /// <param name="dbName">The database name.</param>
    /// <param name="operation">The operation type (SELECT, INSERT, etc.).</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithDatabase(
        this Activity? activity,
        string? dbSystem = null,
        string? dbName = null,
        string? operation = null)
    {
        if (activity == null) return null;

        if (!string.IsNullOrEmpty(dbSystem))
            activity.SetTag(SemanticTags.DbSystem, dbSystem);

        if (!string.IsNullOrEmpty(dbName))
            activity.SetTag(SemanticTags.DbName, dbName);

        if (!string.IsNullOrEmpty(operation))
            activity.SetTag(SemanticTags.DbOperation, operation);

        return activity;
    }

    /// <summary>
    /// Adds messaging context to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="system">The messaging system (e.g., rabbitmq).</param>
    /// <param name="destination">The destination name.</param>
    /// <param name="messageId">Optional message ID.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity? WithMessaging(
        this Activity? activity,
        string? system = null,
        string? destination = null,
        string? messageId = null)
    {
        if (activity == null) return null;

        if (!string.IsNullOrEmpty(system))
            activity.SetTag(SemanticTags.MessagingSystem, system);

        if (!string.IsNullOrEmpty(destination))
            activity.SetTag(SemanticTags.MessagingDestinationName, destination);

        if (!string.IsNullOrEmpty(messageId))
            activity.SetTag(SemanticTags.MessagingMessageId, messageId);

        return activity;
    }

    #endregion

    #region Scoped Activity

    /// <summary>
    /// Creates a scoped activity that automatically sets success/error on dispose.
    /// </summary>
    /// <param name="source">The ActivitySource.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>A scoped activity wrapper.</returns>
    public static ScopedActivity StartScopedActivity(
        this ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = source.StartActivity(name, kind);
        return new ScopedActivity(activity);
    }

    #endregion
}

/// <summary>
/// A wrapper for Activity that automatically handles success/error status on dispose.
/// </summary>
/// <remarks>
/// <para>
/// Use this to ensure activities are properly completed even in exceptional cases:
/// </para>
/// <code>
/// using (var scope = source.StartScopedActivity("MyOperation"))
/// {
///     try
///     {
///         // ... operation logic
///     }
///     catch (Exception ex)
///     {
///         scope.SetException(ex);
///         throw;
///     }
/// }
/// </code>
/// </remarks>
public sealed class ScopedActivity : IDisposable
{
    private readonly Activity? _activity;
    private Exception? _exception;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying activity.
    /// </summary>
    public Activity? Activity => _activity;

    /// <summary>
    /// Initializes a new instance of <see cref="ScopedActivity"/>.
    /// </summary>
    /// <param name="activity">The activity to wrap.</param>
    public ScopedActivity(Activity? activity)
    {
        _activity = activity;
    }

    /// <summary>
    /// Sets an exception to be recorded when the scope is disposed.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void SetException(Exception exception)
    {
        _exception = exception;
    }

    /// <summary>
    /// Adds a tag to the underlying activity.
    /// </summary>
    public ScopedActivity SetTag(string key, object? value)
    {
        _activity?.SetTag(key, value);
        return this;
    }

    /// <summary>
    /// Records an event on the underlying activity.
    /// </summary>
    public ScopedActivity RecordEvent(string name, params (string Key, object? Value)[] tags)
    {
        _activity?.RecordEvent(name, tags);
        return this;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_activity == null) return;

        if (_exception != null)
        {
            _activity.SetError(_exception);
        }
        else
        {
            _activity.SetSuccess();
        }

        _activity.Dispose();
    }
}

