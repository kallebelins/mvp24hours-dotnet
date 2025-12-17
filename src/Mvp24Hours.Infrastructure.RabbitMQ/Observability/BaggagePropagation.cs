//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Provides baggage propagation for correlation, tenant, and user information across RabbitMQ messages.
/// </summary>
/// <remarks>
/// <para>
/// Baggage is key-value pairs that propagate along with trace context. This class helps propagate
/// important contextual information like correlation IDs, tenant IDs, and user information
/// between services via RabbitMQ messages.
/// </para>
/// <para>
/// <strong>Standard Baggage Keys:</strong>
/// <list type="bullet">
/// <item>correlation-id - The correlation ID for request tracing</item>
/// <item>causation-id - The ID of the event that caused this message</item>
/// <item>tenant-id - The tenant identifier for multi-tenant scenarios</item>
/// <item>user-id - The user identifier</item>
/// <item>conversation-id - The conversation ID for multi-message flows</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Inject baggage into message headers before publishing
/// var headers = new Dictionary&lt;string, object&gt;();
/// BaggagePropagation.InjectBaggage(headers, new BaggageContext
/// {
///     CorrelationId = "123",
///     TenantId = "tenant-abc",
///     UserId = "user-xyz"
/// });
///
/// // Extract baggage from message headers when consuming
/// var context = BaggagePropagation.ExtractBaggage(headers);
/// Console.WriteLine($"Correlation: {context.CorrelationId}");
/// </code>
/// </example>
public static class BaggagePropagation
{
    /// <summary>
    /// Standard header/baggage keys used for propagation.
    /// </summary>
    public static class Keys
    {
        /// <summary>Header key for W3C traceparent.</summary>
        public const string TraceParent = "traceparent";

        /// <summary>Header key for W3C tracestate.</summary>
        public const string TraceState = "tracestate";

        /// <summary>Header key for baggage (W3C Baggage).</summary>
        public const string Baggage = "baggage";

        /// <summary>Header key for correlation ID.</summary>
        public const string CorrelationId = "x-correlation-id";

        /// <summary>Header key for causation ID.</summary>
        public const string CausationId = "x-causation-id";

        /// <summary>Header key for tenant ID.</summary>
        public const string TenantId = "x-tenant-id";

        /// <summary>Header key for user ID.</summary>
        public const string UserId = "x-user-id";

        /// <summary>Header key for conversation ID.</summary>
        public const string ConversationId = "x-conversation-id";

        /// <summary>Header key for message type.</summary>
        public const string MessageType = "x-message-type";

        /// <summary>Header key for source service name.</summary>
        public const string SourceService = "x-source-service";

        /// <summary>Header key for timestamp.</summary>
        public const string Timestamp = "x-timestamp";
    }

    /// <summary>
    /// Injects the current Activity trace context and baggage into message headers.
    /// </summary>
    /// <param name="headers">The message headers dictionary to inject into.</param>
    /// <param name="activity">Optional activity to extract context from. Uses Activity.Current if not provided.</param>
    public static void InjectTraceContext(IDictionary<string, object> headers, Activity? activity = null)
    {
        ArgumentNullException.ThrowIfNull(headers);

        activity ??= Activity.Current;
        if (activity == null) return;

        // Inject W3C Trace Context
        headers[Keys.TraceParent] = activity.Id ?? $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers[Keys.TraceState] = activity.TraceStateString;
        }

        // Inject baggage as W3C Baggage format
        var baggageItems = new List<string>();
        foreach (var kvp in activity.Baggage)
        {
            if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
            {
                baggageItems.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
        }

        if (baggageItems.Count > 0)
        {
            headers[Keys.Baggage] = string.Join(",", baggageItems);
        }
    }

    /// <summary>
    /// Injects a baggage context into message headers.
    /// </summary>
    /// <param name="headers">The message headers dictionary to inject into.</param>
    /// <param name="context">The baggage context to inject.</param>
    public static void InjectBaggage(IDictionary<string, object> headers, BaggageContext context)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(context);

        if (!string.IsNullOrEmpty(context.CorrelationId))
            headers[Keys.CorrelationId] = context.CorrelationId;

        if (!string.IsNullOrEmpty(context.CausationId))
            headers[Keys.CausationId] = context.CausationId;

        if (!string.IsNullOrEmpty(context.TenantId))
            headers[Keys.TenantId] = context.TenantId;

        if (!string.IsNullOrEmpty(context.UserId))
            headers[Keys.UserId] = context.UserId;

        if (!string.IsNullOrEmpty(context.ConversationId))
            headers[Keys.ConversationId] = context.ConversationId;

        if (!string.IsNullOrEmpty(context.MessageType))
            headers[Keys.MessageType] = context.MessageType;

        if (!string.IsNullOrEmpty(context.SourceService))
            headers[Keys.SourceService] = context.SourceService;

        if (context.Timestamp.HasValue)
            headers[Keys.Timestamp] = context.Timestamp.Value.ToString("O");

        // Also inject into current activity baggage if available
        var activity = Activity.Current;
        if (activity != null)
        {
            if (!string.IsNullOrEmpty(context.CorrelationId))
                activity.SetBaggage("correlation-id", context.CorrelationId);

            if (!string.IsNullOrEmpty(context.TenantId))
                activity.SetBaggage("tenant-id", context.TenantId);

            if (!string.IsNullOrEmpty(context.UserId))
                activity.SetBaggage("user-id", context.UserId);
        }
    }

    /// <summary>
    /// Extracts parent activity context from message headers.
    /// </summary>
    /// <param name="headers">The message headers to extract from.</param>
    /// <returns>The extracted activity context, or default if not found.</returns>
    public static ActivityContext ExtractTraceContext(IDictionary<string, object>? headers)
    {
        if (headers == null) return default;

        if (headers.TryGetValue(Keys.TraceParent, out var traceparentObj) && traceparentObj != null)
        {
            var traceparent = GetHeaderStringValue(traceparentObj);
            if (!string.IsNullOrEmpty(traceparent) && 
                ActivityContext.TryParse(traceparent, null, out var activityContext))
            {
                return activityContext;
            }
        }

        return default;
    }

    /// <summary>
    /// Extracts baggage from message headers.
    /// </summary>
    /// <param name="headers">The message headers to extract from.</param>
    /// <returns>The extracted baggage context.</returns>
    public static BaggageContext ExtractBaggage(IDictionary<string, object>? headers)
    {
        var context = new BaggageContext();

        if (headers == null) return context;

        if (headers.TryGetValue(Keys.CorrelationId, out var correlationId))
            context.CorrelationId = GetHeaderStringValue(correlationId);

        if (headers.TryGetValue(Keys.CausationId, out var causationId))
            context.CausationId = GetHeaderStringValue(causationId);

        if (headers.TryGetValue(Keys.TenantId, out var tenantId))
            context.TenantId = GetHeaderStringValue(tenantId);

        if (headers.TryGetValue(Keys.UserId, out var userId))
            context.UserId = GetHeaderStringValue(userId);

        if (headers.TryGetValue(Keys.ConversationId, out var conversationId))
            context.ConversationId = GetHeaderStringValue(conversationId);

        if (headers.TryGetValue(Keys.MessageType, out var messageType))
            context.MessageType = GetHeaderStringValue(messageType);

        if (headers.TryGetValue(Keys.SourceService, out var sourceService))
            context.SourceService = GetHeaderStringValue(sourceService);

        if (headers.TryGetValue(Keys.Timestamp, out var timestamp))
        {
            var timestampStr = GetHeaderStringValue(timestamp);
            if (DateTimeOffset.TryParse(timestampStr, out var dto))
                context.Timestamp = dto;
        }

        // Also extract from W3C Baggage header
        if (headers.TryGetValue(Keys.Baggage, out var baggageObj))
        {
            var baggage = GetHeaderStringValue(baggageObj);
            if (!string.IsNullOrEmpty(baggage))
            {
                ParseW3CBaggage(baggage, context);
            }
        }

        return context;
    }

    /// <summary>
    /// Restores baggage from a context into the current Activity.
    /// </summary>
    /// <param name="context">The baggage context to restore.</param>
    public static void RestoreBaggageToActivity(BaggageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activity = Activity.Current;
        if (activity == null) return;

        if (!string.IsNullOrEmpty(context.CorrelationId))
            activity.SetBaggage("correlation-id", context.CorrelationId);

        if (!string.IsNullOrEmpty(context.CausationId))
            activity.SetBaggage("causation-id", context.CausationId);

        if (!string.IsNullOrEmpty(context.TenantId))
            activity.SetBaggage("tenant-id", context.TenantId);

        if (!string.IsNullOrEmpty(context.UserId))
            activity.SetBaggage("user-id", context.UserId);

        if (!string.IsNullOrEmpty(context.ConversationId))
            activity.SetBaggage("conversation-id", context.ConversationId);
    }

    /// <summary>
    /// Creates a baggage context from the current Activity.
    /// </summary>
    /// <returns>A baggage context populated from the current Activity's baggage.</returns>
    public static BaggageContext CreateFromCurrentActivity()
    {
        var context = new BaggageContext();
        var activity = Activity.Current;

        if (activity == null) return context;

        foreach (var kvp in activity.Baggage)
        {
            switch (kvp.Key)
            {
                case "correlation-id":
                    context.CorrelationId = kvp.Value;
                    break;
                case "causation-id":
                    context.CausationId = kvp.Value;
                    break;
                case "tenant-id":
                    context.TenantId = kvp.Value;
                    break;
                case "user-id":
                    context.UserId = kvp.Value;
                    break;
                case "conversation-id":
                    context.ConversationId = kvp.Value;
                    break;
            }
        }

        return context;
    }

    /// <summary>
    /// Gets the trace ID from the current activity or generates a new one.
    /// </summary>
    /// <returns>The trace ID as a string.</returns>
    public static string GetOrCreateTraceId()
    {
        return Activity.Current?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString();
    }

    /// <summary>
    /// Gets the span ID from the current activity or generates a new one.
    /// </summary>
    /// <returns>The span ID as a string.</returns>
    public static string GetOrCreateSpanId()
    {
        return Activity.Current?.SpanId.ToString() ?? ActivitySpanId.CreateRandom().ToString();
    }

    private static string? GetHeaderStringValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString()
        };
    }

    private static void ParseW3CBaggage(string baggage, BaggageContext context)
    {
        var pairs = baggage.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var kvp = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kvp.Length != 2) continue;

            var key = Uri.UnescapeDataString(kvp[0].Trim());
            var value = Uri.UnescapeDataString(kvp[1].Trim());

            switch (key)
            {
                case "correlation-id":
                    context.CorrelationId ??= value;
                    break;
                case "causation-id":
                    context.CausationId ??= value;
                    break;
                case "tenant-id":
                    context.TenantId ??= value;
                    break;
                case "user-id":
                    context.UserId ??= value;
                    break;
                case "conversation-id":
                    context.ConversationId ??= value;
                    break;
            }
        }
    }
}

/// <summary>
/// Represents contextual information (baggage) that propagates with messages.
/// </summary>
public sealed class BaggageContext
{
    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID (the ID of the event that caused this message).
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID for multi-message flows.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the source service name.
    /// </summary>
    public string? SourceService { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Creates a new BaggageContext with default values.
    /// </summary>
    public BaggageContext()
    {
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new BaggageContext with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>A new BaggageContext instance.</returns>
    public static BaggageContext WithCorrelation(string correlationId)
    {
        return new BaggageContext { CorrelationId = correlationId };
    }

    /// <summary>
    /// Creates a new BaggageContext for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>A new BaggageContext instance.</returns>
    public static BaggageContext ForTenant(string tenantId)
    {
        return new BaggageContext { TenantId = tenantId };
    }

    /// <summary>
    /// Creates a copy of this context with a new causation ID.
    /// </summary>
    /// <param name="causationId">The new causation ID (usually the current message ID).</param>
    /// <returns>A new BaggageContext with updated causation.</returns>
    public BaggageContext WithCausation(string causationId)
    {
        return new BaggageContext
        {
            CorrelationId = CorrelationId,
            CausationId = causationId,
            TenantId = TenantId,
            UserId = UserId,
            ConversationId = ConversationId,
            MessageType = MessageType,
            SourceService = SourceService,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

