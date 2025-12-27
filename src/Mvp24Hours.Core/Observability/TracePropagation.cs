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
/// Provides utilities for trace context propagation following W3C Trace Context standard.
/// </summary>
/// <remarks>
/// <para>
/// This class implements W3C Trace Context (https://www.w3.org/TR/trace-context/) for
/// propagating trace context across service boundaries. It supports:
/// </para>
/// <list type="bullet">
/// <item><c>traceparent</c> - Required header containing version, trace-id, parent-id, and trace-flags</item>
/// <item><c>tracestate</c> - Optional header for vendor-specific trace information</item>
/// <item><c>baggage</c> - W3C Baggage for propagating custom key-value pairs</item>
/// </list>
/// <para>
/// <strong>Header Names:</strong>
/// </para>
/// <code>
/// traceparent: 00-{trace-id}-{parent-id}-{trace-flags}
/// tracestate: vendor1=value1,vendor2=value2
/// baggage: key1=value1,key2=value2
/// </code>
/// </remarks>
public static class TracePropagation
{
    #region Header Names

    /// <summary>
    /// W3C Trace Context traceparent header name.
    /// </summary>
    public const string TraceparentHeader = "traceparent";

    /// <summary>
    /// W3C Trace Context tracestate header name.
    /// </summary>
    public const string TracestateHeader = "tracestate";

    /// <summary>
    /// W3C Baggage header name.
    /// </summary>
    public const string BaggageHeader = "baggage";

    /// <summary>
    /// Custom correlation ID header (backwards compatibility).
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Custom request ID header.
    /// </summary>
    public const string RequestIdHeader = "X-Request-Id";

    #endregion

    #region Injection (Outgoing)

    /// <summary>
    /// Injects the current trace context into a dictionary of headers.
    /// </summary>
    /// <param name="headers">The dictionary to inject headers into.</param>
    /// <param name="activity">Optional activity to use. If null, uses Activity.Current.</param>
    /// <remarks>
    /// <para>
    /// This method adds the following headers:
    /// <list type="bullet">
    /// <item><c>traceparent</c> - W3C Trace Context</item>
    /// <item><c>tracestate</c> - If present on the activity</item>
    /// <item><c>baggage</c> - If baggage items are present</item>
    /// <item><c>X-Correlation-Id</c> - From baggage or trace ID</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static void InjectTraceContext(IDictionary<string, string> headers, Activity? activity = null)
    {
        activity ??= Activity.Current;
        if (activity == null) return;

        // W3C Trace Context - traceparent
        var traceparent = FormatTraceparent(activity);
        headers[TraceparentHeader] = traceparent;

        // W3C Trace Context - tracestate (optional)
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers[TracestateHeader] = activity.TraceStateString;
        }

        // W3C Baggage
        var baggageItems = activity.Baggage.ToList();
        if (baggageItems.Count > 0)
        {
            var baggageValue = string.Join(",", 
                baggageItems.Select(b => $"{b.Key}={Uri.EscapeDataString(b.Value ?? "")}"));
            headers[BaggageHeader] = baggageValue;
        }

        // Correlation ID (from baggage or generate from trace ID)
        var correlationId = activity.GetBaggageItem("correlation.id") 
                           ?? activity.TraceId.ToString();
        headers[CorrelationIdHeader] = correlationId;
    }

    /// <summary>
    /// Injects trace context using a custom setter action.
    /// </summary>
    /// <typeparam name="T">The carrier type.</typeparam>
    /// <param name="carrier">The carrier object.</param>
    /// <param name="setter">Action to set a header value.</param>
    /// <param name="activity">Optional activity to use. If null, uses Activity.Current.</param>
    public static void InjectTraceContext<T>(
        T carrier, 
        Action<T, string, string> setter,
        Activity? activity = null)
    {
        activity ??= Activity.Current;
        if (activity == null) return;

        setter(carrier, TraceparentHeader, FormatTraceparent(activity));

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            setter(carrier, TracestateHeader, activity.TraceStateString);
        }

        var baggageItems = activity.Baggage.ToList();
        if (baggageItems.Count > 0)
        {
            var baggageValue = string.Join(",", 
                baggageItems.Select(b => $"{b.Key}={Uri.EscapeDataString(b.Value ?? "")}"));
            setter(carrier, BaggageHeader, baggageValue);
        }

        var correlationId = activity.GetBaggageItem("correlation.id") 
                           ?? activity.TraceId.ToString();
        setter(carrier, CorrelationIdHeader, correlationId);
    }

    #endregion

    #region Extraction (Incoming)

    /// <summary>
    /// Extracts trace context from a dictionary of headers.
    /// </summary>
    /// <param name="headers">The dictionary containing headers.</param>
    /// <returns>The extracted trace context, or null if extraction fails.</returns>
    public static TraceContext? ExtractTraceContext(IDictionary<string, string?> headers)
    {
        var traceparent = GetHeaderValue(headers, TraceparentHeader);
        if (string.IsNullOrEmpty(traceparent))
            return null;

        var context = ParseTraceparent(traceparent);
        if (context == null)
            return null;

        context.TraceState = GetHeaderValue(headers, TracestateHeader);
        
        // Parse baggage
        var baggage = GetHeaderValue(headers, BaggageHeader);
        if (!string.IsNullOrEmpty(baggage))
        {
            context.Baggage = ParseBaggage(baggage);
        }

        // Correlation ID (may be in baggage or separate header)
        context.CorrelationId = context.Baggage?.GetValueOrDefault("correlation.id")
                               ?? GetHeaderValue(headers, CorrelationIdHeader)
                               ?? context.TraceId;

        return context;
    }

    /// <summary>
    /// Extracts trace context using a custom getter function.
    /// </summary>
    /// <typeparam name="T">The carrier type.</typeparam>
    /// <param name="carrier">The carrier object.</param>
    /// <param name="getter">Function to get a header value.</param>
    /// <returns>The extracted trace context, or null if extraction fails.</returns>
    public static TraceContext? ExtractTraceContext<T>(
        T carrier,
        Func<T, string, string?> getter)
    {
        var traceparent = getter(carrier, TraceparentHeader);
        if (string.IsNullOrEmpty(traceparent))
            return null;

        var context = ParseTraceparent(traceparent);
        if (context == null)
            return null;

        context.TraceState = getter(carrier, TracestateHeader);
        
        var baggage = getter(carrier, BaggageHeader);
        if (!string.IsNullOrEmpty(baggage))
        {
            context.Baggage = ParseBaggage(baggage);
        }

        context.CorrelationId = context.Baggage?.GetValueOrDefault("correlation.id")
                               ?? getter(carrier, CorrelationIdHeader)
                               ?? context.TraceId;

        return context;
    }

    /// <summary>
    /// Creates an ActivityContext from extracted trace context.
    /// </summary>
    /// <param name="context">The extracted trace context.</param>
    /// <returns>The ActivityContext for starting child activities.</returns>
    public static ActivityContext ToActivityContext(this TraceContext context)
    {
        try
        {
            // Parse trace ID (32 hex characters)
            if (string.IsNullOrEmpty(context.TraceId) || context.TraceId.Length != 32)
                return default;

            // Parse span ID (16 hex characters)
            if (string.IsNullOrEmpty(context.SpanId) || context.SpanId.Length != 16)
                return default;

            var traceId = ActivityTraceId.CreateFromString(context.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(context.SpanId.AsSpan());

            return new ActivityContext(traceId, spanId, context.TraceFlags, context.TraceState);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Starts a new activity with the extracted trace context as parent.
    /// </summary>
    /// <param name="source">The ActivitySource.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="context">The extracted trace context.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>The new activity, or null if not sampled.</returns>
    public static Activity? StartActivityWithParent(
        this ActivitySource source,
        string name,
        TraceContext context,
        ActivityKind kind = ActivityKind.Server)
    {
        var parentContext = context.ToActivityContext();
        
        var activity = source.StartActivity(
            name,
            kind,
            parentContext);

        if (activity != null)
        {
            // Restore baggage
            if (context.Baggage != null)
            {
                foreach (var item in context.Baggage)
                {
                    activity.SetBaggage(item.Key, item.Value);
                }
            }

            // Set correlation ID tag
            if (!string.IsNullOrEmpty(context.CorrelationId))
            {
                activity.SetTag(SemanticTags.CorrelationId, context.CorrelationId);
            }
        }

        return activity;
    }

    #endregion

    #region Private Helpers

    private static string FormatTraceparent(Activity activity)
    {
        var flags = activity.Recorded ? "01" : "00";
        return $"00-{activity.TraceId}-{activity.SpanId}-{flags}";
    }

    private static TraceContext? ParseTraceparent(string traceparent)
    {
        // Format: version-traceid-parentid-traceflags
        // Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01

        var parts = traceparent.Split('-');
        if (parts.Length < 4)
            return null;

        var version = parts[0];
        if (version != "00") // Only support version 00 for now
            return null;

        var traceId = parts[1];
        var spanId = parts[2];
        var flags = parts[3];

        // Validate trace ID and span ID format
        if (traceId.Length != 32 || spanId.Length != 16)
            return null;

        return new TraceContext
        {
            TraceId = traceId,
            SpanId = spanId,
            TraceFlags = flags == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None
        };
    }

    private static Dictionary<string, string> ParseBaggage(string baggage)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var item in baggage.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = Uri.UnescapeDataString(parts[1].Trim());
                result[key] = value;
            }
        }

        return result;
    }

    private static string? GetHeaderValue(IDictionary<string, string?> headers, string key)
    {
        if (headers.TryGetValue(key, out var value))
            return value;

        // Try case-insensitive search
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }

        return null;
    }

    #endregion
}

/// <summary>
/// Represents extracted trace context information.
/// </summary>
public class TraceContext
{
    /// <summary>
    /// Gets or sets the trace ID (32 hex characters).
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the span/parent ID (16 hex characters).
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trace flags.
    /// </summary>
    public ActivityTraceFlags TraceFlags { get; set; }

    /// <summary>
    /// Gets or sets the trace state string.
    /// </summary>
    public string? TraceState { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the baggage items.
    /// </summary>
    public Dictionary<string, string>? Baggage { get; set; }

    /// <summary>
    /// Gets whether this context was sampled for recording.
    /// </summary>
    public bool IsSampled => TraceFlags.HasFlag(ActivityTraceFlags.Recorded);
}

