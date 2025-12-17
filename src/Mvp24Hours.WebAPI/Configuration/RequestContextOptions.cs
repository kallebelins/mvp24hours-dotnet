//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for request context middleware that handles
/// Correlation ID, Causation ID, and Request ID propagation.
/// </summary>
/// <remarks>
/// <para>
/// These options control how request context is established and propagated
/// for distributed tracing across microservices.
/// </para>
/// <para>
/// <strong>Header Names:</strong>
/// <list type="bullet">
/// <item>Correlation ID - Groups all operations for a single user request across services</item>
/// <item>Causation ID - Identifies the direct cause of an event or command</item>
/// <item>Request ID - Unique identifier for the current request</item>
/// </list>
/// </para>
/// </remarks>
public class RequestContextOptions
{
    /// <summary>
    /// Gets or sets the header name for Correlation ID.
    /// Default is "X-Correlation-ID".
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Gets or sets the header name for Causation ID.
    /// Default is "X-Causation-ID".
    /// </summary>
    public string CausationIdHeader { get; set; } = "X-Causation-ID";

    /// <summary>
    /// Gets or sets the header name for Request ID.
    /// Default is "X-Request-ID".
    /// </summary>
    public string RequestIdHeader { get; set; } = "X-Request-ID";

    /// <summary>
    /// Gets or sets the header name for Tenant ID.
    /// Default is "X-Tenant-ID".
    /// </summary>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets alternative header names to check for Correlation ID.
    /// This allows compatibility with different naming conventions.
    /// </summary>
    public HashSet<string> AlternativeCorrelationHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Request-Id",
        "X-Trace-Id",
        "Request-Id",
        "TraceId"
    };

    /// <summary>
    /// Gets or sets whether to include context IDs in response headers.
    /// Default is true.
    /// </summary>
    public bool IncludeInResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom ID generator function.
    /// If null, Guid.NewGuid().ToString("N") is used.
    /// </summary>
    public Func<string>? IdGenerator { get; set; }

    /// <summary>
    /// Gets or sets whether to propagate context headers to outgoing HTTP requests.
    /// Requires using the CorrelationIdHandler with HttpClient.
    /// Default is true.
    /// </summary>
    public bool PropagateToOutgoingRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use W3C Trace Context headers (traceparent, tracestate).
    /// When enabled, also extracts/propagates W3C standard headers.
    /// Default is false.
    /// </summary>
    public bool UseW3CTraceContext { get; set; } = false;

    /// <summary>
    /// Gets or sets the W3C traceparent header name.
    /// </summary>
    public string TraceParentHeader { get; set; } = "traceparent";

    /// <summary>
    /// Gets or sets the W3C tracestate header name.
    /// </summary>
    public string TraceStateHeader { get; set; } = "tracestate";
}


