//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Helper class for creating and managing OpenTelemetry activities with semantic conventions.
/// </summary>
/// <remarks>
/// <para>
/// This class provides simplified methods for starting activities following
/// OpenTelemetry semantic conventions. It handles null-safety and enrichment
/// automatically.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// using var activity = ActivityHelper.StartActivity(
///     Mvp24HoursActivitySources.Core.Source,
///     "ProcessOrder",
///     ActivityKind.Internal,
///     tags: new Dictionary&lt;string, object?&gt;
///     {
///         [SemanticTags.OperationName] = "ProcessOrder",
///         [SemanticTags.OperationType] = "Command"
///     });
/// 
/// try
/// {
///     // ... operation logic
///     activity?.SetSuccess();
/// }
/// catch (Exception ex)
/// {
///     activity?.SetError(ex);
///     throw;
/// }
/// </code>
/// </remarks>
public static class ActivityHelper
{
    #region StartActivity Methods

    /// <summary>
    /// Starts a new activity with the specified parameters.
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="name">The name of the activity.</param>
    /// <param name="kind">The kind of activity (default: Internal).</param>
    /// <param name="tags">Optional tags to add to the activity.</param>
    /// <param name="links">Optional links to related activities.</param>
    /// <param name="parentContext">Optional parent activity context.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartActivity(
        ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Internal,
        IEnumerable<KeyValuePair<string, object?>>? tags = null,
        IEnumerable<ActivityLink>? links = null,
        ActivityContext parentContext = default)
    {
        if (source == null) return null;

        var activity = source.StartActivity(
            name,
            kind,
            parentContext,
            tags,
            links);

        return activity;
    }

    /// <summary>
    /// Starts a new internal activity for an operation.
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operationType">The type of operation (e.g., Command, Query).</param>
    /// <param name="additionalTags">Additional tags to add.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartOperation(
        ActivitySource source,
        string operationName,
        string? operationType = null,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.OperationName, operationName)
        };

        if (!string.IsNullOrEmpty(operationType))
        {
            tags.Add(new(SemanticTags.OperationType, operationType));
        }

        if (additionalTags != null)
        {
            tags.AddRange(additionalTags);
        }

        return StartActivity(source, operationName, ActivityKind.Internal, tags);
    }

    /// <summary>
    /// Starts a new client activity for external calls (e.g., HTTP, database).
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="additionalTags">Additional tags to add.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartClientActivity(
        ActivitySource source,
        string operationName,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        return StartActivity(source, operationName, ActivityKind.Client, additionalTags);
    }

    /// <summary>
    /// Starts a new server activity for handling incoming requests.
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="additionalTags">Additional tags to add.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartServerActivity(
        ActivitySource source,
        string operationName,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        return StartActivity(source, operationName, ActivityKind.Server, additionalTags);
    }

    /// <summary>
    /// Starts a new producer activity for publishing messages.
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="destinationName">The destination (queue/topic) name.</param>
    /// <param name="additionalTags">Additional tags to add.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartProducerActivity(
        ActivitySource source,
        string operationName,
        string? destinationName = null,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        
        if (!string.IsNullOrEmpty(destinationName))
        {
            tags.Add(new(SemanticTags.MessagingDestinationName, destinationName));
        }

        if (additionalTags != null)
        {
            tags.AddRange(additionalTags);
        }

        return StartActivity(source, operationName, ActivityKind.Producer, tags);
    }

    /// <summary>
    /// Starts a new consumer activity for processing messages.
    /// </summary>
    /// <param name="source">The ActivitySource to use.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="destinationName">The source (queue/topic) name.</param>
    /// <param name="additionalTags">Additional tags to add.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartConsumerActivity(
        ActivitySource source,
        string operationName,
        string? destinationName = null,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        
        if (!string.IsNullOrEmpty(destinationName))
        {
            tags.Add(new(SemanticTags.MessagingDestinationName, destinationName));
        }

        if (additionalTags != null)
        {
            tags.AddRange(additionalTags);
        }

        return StartActivity(source, operationName, ActivityKind.Consumer, tags);
    }

    #endregion

    #region Module-Specific Helpers

    /// <summary>
    /// Starts a Core module activity.
    /// </summary>
    public static Activity? StartCoreActivity(
        string operationName,
        [CallerMemberName] string? callerName = null)
    {
        var name = string.IsNullOrEmpty(callerName) 
            ? operationName 
            : $"{operationName}.{callerName}";
        
        return StartOperation(Mvp24HoursActivitySources.Core.Source, name);
    }

    /// <summary>
    /// Starts a Pipe module activity for pipeline execution.
    /// </summary>
    public static Activity? StartPipelineActivity(string pipelineName, int totalOperations = 0)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.PipelineName, pipelineName),
            new(SemanticTags.OperationType, "Pipeline")
        };

        if (totalOperations > 0)
        {
            tags.Add(new(SemanticTags.PipelineTotalOperations, totalOperations));
        }

        return StartActivity(
            Mvp24HoursActivitySources.Pipe.Source,
            Mvp24HoursActivitySources.Pipe.Activities.Pipeline,
            ActivityKind.Internal,
            tags);
    }

    /// <summary>
    /// Starts a Pipe module activity for operation execution.
    /// </summary>
    public static Activity? StartPipelineOperationActivity(string operationName, int index = 0)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.PipelineOperationName, operationName),
            new(SemanticTags.PipelineOperationIndex, index)
        };

        return StartActivity(
            Mvp24HoursActivitySources.Pipe.Source,
            Mvp24HoursActivitySources.Pipe.Activities.Operation,
            ActivityKind.Internal,
            tags);
    }

    /// <summary>
    /// Starts a CQRS module activity for command handling.
    /// </summary>
    public static Activity? StartCommandActivity(string commandName)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.MediatorRequestName, commandName),
            new(SemanticTags.MediatorRequestType, "Command")
        };

        return StartActivity(
            Mvp24HoursActivitySources.Cqrs.Source,
            Mvp24HoursActivitySources.Cqrs.Activities.Command,
            ActivityKind.Internal,
            tags);
    }

    /// <summary>
    /// Starts a CQRS module activity for query handling.
    /// </summary>
    public static Activity? StartQueryActivity(string queryName)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.MediatorRequestName, queryName),
            new(SemanticTags.MediatorRequestType, "Query")
        };

        return StartActivity(
            Mvp24HoursActivitySources.Cqrs.Source,
            Mvp24HoursActivitySources.Cqrs.Activities.Query,
            ActivityKind.Internal,
            tags);
    }

    /// <summary>
    /// Starts a Data module activity for database operations.
    /// </summary>
    public static Activity? StartDatabaseActivity(
        string operationName,
        string dbOperation,
        string? dbSystem = null,
        string? dbName = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.DbOperation, dbOperation)
        };

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(new(SemanticTags.DbSystem, dbSystem));
        }

        if (!string.IsNullOrEmpty(dbName))
        {
            tags.Add(new(SemanticTags.DbName, dbName));
        }

        return StartActivity(
            Mvp24HoursActivitySources.Data.Source,
            operationName,
            ActivityKind.Client,
            tags);
    }

    /// <summary>
    /// Starts a RabbitMQ module activity for message publishing.
    /// </summary>
    public static Activity? StartMessagePublishActivity(
        string destinationName,
        string? routingKey = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.MessagingSystem, "rabbitmq"),
            new(SemanticTags.MessagingDestinationName, destinationName)
        };

        if (!string.IsNullOrEmpty(routingKey))
        {
            tags.Add(new(SemanticTags.MessagingRabbitmqRoutingKey, routingKey));
        }

        return StartActivity(
            Mvp24HoursActivitySources.RabbitMQ.Source,
            Mvp24HoursActivitySources.RabbitMQ.Activities.Publish,
            ActivityKind.Producer,
            tags);
    }

    /// <summary>
    /// Starts a RabbitMQ module activity for message consuming.
    /// </summary>
    public static Activity? StartMessageConsumeActivity(
        string queueName,
        string? messageId = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.MessagingSystem, "rabbitmq"),
            new(SemanticTags.MessagingDestinationName, queueName),
            new(SemanticTags.MessagingDestinationKind, "queue")
        };

        if (!string.IsNullOrEmpty(messageId))
        {
            tags.Add(new(SemanticTags.MessagingMessageId, messageId));
        }

        return StartActivity(
            Mvp24HoursActivitySources.RabbitMQ.Source,
            Mvp24HoursActivitySources.RabbitMQ.Activities.Consume,
            ActivityKind.Consumer,
            tags);
    }

    /// <summary>
    /// Starts a Caching module activity for cache operations.
    /// </summary>
    public static Activity? StartCacheActivity(
        string operation,
        string cacheKey,
        string? cacheSystem = null)
    {
        var activityName = operation.ToUpperInvariant() switch
        {
            "GET" => Mvp24HoursActivitySources.Caching.Activities.Get,
            "SET" => Mvp24HoursActivitySources.Caching.Activities.Set,
            "INVALIDATE" or "DELETE" or "REMOVE" => Mvp24HoursActivitySources.Caching.Activities.Invalidate,
            "REFRESH" => Mvp24HoursActivitySources.Caching.Activities.Refresh,
            _ => $"Mvp24Hours.Caching.{operation}"
        };

        var tags = new List<KeyValuePair<string, object?>>
        {
            new(SemanticTags.CacheOperation, operation),
            new(SemanticTags.CacheKey, cacheKey)
        };

        if (!string.IsNullOrEmpty(cacheSystem))
        {
            tags.Add(new(SemanticTags.CacheSystem, cacheSystem));
        }

        return StartActivity(
            Mvp24HoursActivitySources.Caching.Source,
            activityName,
            ActivityKind.Client,
            tags);
    }

    #endregion

    #region Context Propagation

    /// <summary>
    /// Gets the current trace context for propagation.
    /// </summary>
    /// <returns>The current activity context, or default if no activity is active.</returns>
    public static ActivityContext GetCurrentContext()
    {
        return Activity.Current?.Context ?? default;
    }

    /// <summary>
    /// Extracts trace context headers for HTTP propagation (W3C format).
    /// </summary>
    /// <returns>Dictionary with traceparent and tracestate headers.</returns>
    public static Dictionary<string, string> GetTracePropagationHeaders()
    {
        var headers = new Dictionary<string, string>();
        var activity = Activity.Current;

        if (activity != null)
        {
            // W3C Trace Context
            headers["traceparent"] = $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
            
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                headers["tracestate"] = activity.TraceStateString;
            }

            // Baggage (W3C Baggage)
            var baggage = activity.Baggage.ToList();
            if (baggage.Count > 0)
            {
                headers["baggage"] = string.Join(",", baggage.Select(b => $"{b.Key}={Uri.EscapeDataString(b.Value ?? "")}"));
            }
        }

        return headers;
    }

    /// <summary>
    /// Parses trace context from HTTP headers (W3C format).
    /// </summary>
    /// <param name="traceparent">The traceparent header value.</param>
    /// <param name="tracestate">Optional tracestate header value.</param>
    /// <returns>The parsed ActivityContext, or default if parsing fails.</returns>
    public static ActivityContext ParseTraceContext(string? traceparent, string? tracestate = null)
    {
        if (string.IsNullOrEmpty(traceparent))
            return default;

        try
        {
            // Format: 00-{traceId}-{spanId}-{flags}
            var parts = traceparent.Split('-');
            if (parts.Length < 4)
                return default;

            // Validate format: version should be 00, traceId 32 chars, spanId 16 chars
            if (parts[1].Length != 32 || parts[2].Length != 16)
                return default;

            var traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
            var spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
            var flags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;

            return new ActivityContext(traceId, spanId, flags, tracestate);
        }
        catch
        {
            return default;
        }
    }

    #endregion
}

