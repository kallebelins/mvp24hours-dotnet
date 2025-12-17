//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for request telemetry and metrics middleware.
/// </summary>
/// <remarks>
/// <para>
/// These options control OpenTelemetry tracing and metrics collection
/// for HTTP requests processed by the API.
/// </para>
/// </remarks>
public class RequestTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether tracing is enabled.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record exception details in traces.
    /// Should be false in production for security.
    /// </summary>
    public bool RecordExceptionDetails { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enrich traces with request/response headers.
    /// </summary>
    public bool EnrichWithHeaders { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enrich traces with user information.
    /// </summary>
    public bool EnrichWithUser { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich traces with tenant information.
    /// </summary>
    public bool EnrichWithTenant { get; set; } = true;

    /// <summary>
    /// Gets or sets the header name for correlation ID.
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Gets or sets the header name for causation ID.
    /// </summary>
    public string CausationIdHeader { get; set; } = "X-Causation-ID";

    /// <summary>
    /// Gets or sets the header name for tenant ID.
    /// </summary>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets paths to exclude from telemetry collection.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/metrics",
        "/swagger",
        "/swagger/index.html",
        "/favicon.ico"
    };

    /// <summary>
    /// Gets or sets custom tags to add to all traces.
    /// </summary>
    public Dictionary<string, string> CustomTags { get; set; } = new();

    /// <summary>
    /// Gets or sets histogram bucket boundaries for request duration metrics (in milliseconds).
    /// </summary>
    public double[] DurationBuckets { get; set; } = { 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000 };

    /// <summary>
    /// Gets or sets histogram bucket boundaries for request size metrics (in bytes).
    /// </summary>
    public double[] SizeBuckets { get; set; } = { 100, 1024, 10240, 102400, 1048576, 10485760 };
}

