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
/// Provides metrics instrumentation for HTTP/WebAPI operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// HTTP request/response metrics, rate limiting, and API health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>requests_total</c> - Counter for HTTP requests</item>
/// <item><c>request_duration_ms</c> - Histogram for request duration</item>
/// <item><c>request_size_bytes</c> - Histogram for request body size</item>
/// <item><c>response_size_bytes</c> - Histogram for response body size</item>
/// <item><c>active_requests</c> - Gauge for active requests</item>
/// <item><c>rate_limit_hits_total</c> - Counter for rate limit hits</item>
/// <item><c>idempotent_duplicates_total</c> - Counter for idempotent duplicates</item>
/// </list>
/// </para>
/// </remarks>
public sealed class HttpMetrics
{
    private readonly Counter<long> _requestsTotal;
    private readonly Counter<long> _requestsFailedTotal;
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<int> _requestSizeBytes;
    private readonly Histogram<int> _responseSizeBytes;
    private readonly UpDownCounter<int> _activeRequests;
    private readonly Counter<long> _rateLimitHitsTotal;
    private readonly Counter<long> _idempotentDuplicatesTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMetrics"/> class.
    /// </summary>
    public HttpMetrics()
    {
        var meter = Mvp24HoursMeters.WebAPI.Meter;

        _requestsTotal = meter.CreateCounter<long>(
            MetricNames.HttpRequestsTotal,
            unit: "{requests}",
            description: "Total number of HTTP requests");

        _requestsFailedTotal = meter.CreateCounter<long>(
            MetricNames.HttpRequestsFailedTotal,
            unit: "{requests}",
            description: "Total number of failed HTTP requests");

        _requestDuration = meter.CreateHistogram<double>(
            MetricNames.HttpRequestDuration,
            unit: "ms",
            description: "Duration of HTTP requests in milliseconds");

        _requestSizeBytes = meter.CreateHistogram<int>(
            MetricNames.HttpRequestSizeBytes,
            unit: "By",
            description: "Size of HTTP request body in bytes");

        _responseSizeBytes = meter.CreateHistogram<int>(
            MetricNames.HttpResponseSizeBytes,
            unit: "By",
            description: "Size of HTTP response body in bytes");

        _activeRequests = meter.CreateUpDownCounter<int>(
            MetricNames.HttpActiveRequests,
            unit: "{requests}",
            description: "Number of active HTTP requests");

        _rateLimitHitsTotal = meter.CreateCounter<long>(
            MetricNames.HttpRateLimitHitsTotal,
            unit: "{hits}",
            description: "Total number of rate limit hits");

        _idempotentDuplicatesTotal = meter.CreateCounter<long>(
            MetricNames.HttpIdempotentDuplicatesTotal,
            unit: "{duplicates}",
            description: "Total number of idempotent request duplicates");
    }

    #region Request Tracking

    /// <summary>
    /// Begins tracking an HTTP request.
    /// </summary>
    /// <param name="method">HTTP method (GET, POST, etc.).</param>
    /// <param name="route">Route template.</param>
    /// <returns>A scope that should be disposed when request completes.</returns>
    public HttpRequestScope BeginRequest(string method, string route)
    {
        return new HttpRequestScope(this, method, route);
    }

    /// <summary>
    /// Records an HTTP request.
    /// </summary>
    /// <param name="method">HTTP method (GET, POST, etc.).</param>
    /// <param name="route">Route template.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="requestSize">Request body size in bytes (optional).</param>
    /// <param name="responseSize">Response body size in bytes (optional).</param>
    public void RecordRequest(
        string method,
        string route,
        int statusCode,
        double durationMs,
        int requestSize = 0,
        int responseSize = 0)
    {
        var success = statusCode < 400;
        var tags = new TagList
        {
            { MetricTags.HttpMethod, method },
            { MetricTags.HttpRoute, route },
            { MetricTags.HttpStatusCode, statusCode },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _requestsTotal.Add(1, tags);

        if (!success)
        {
            _requestsFailedTotal.Add(1, tags);
        }

        _requestDuration.Record(durationMs, tags);

        if (requestSize > 0)
        {
            _requestSizeBytes.Record(requestSize, tags);
        }

        if (responseSize > 0)
        {
            _responseSizeBytes.Record(responseSize, tags);
        }
    }

    /// <summary>
    /// Increments the active request count.
    /// </summary>
    public void IncrementActiveRequests()
    {
        _activeRequests.Add(1);
    }

    /// <summary>
    /// Decrements the active request count.
    /// </summary>
    public void DecrementActiveRequests()
    {
        _activeRequests.Add(-1);
    }

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Records a rate limit hit.
    /// </summary>
    /// <param name="route">Route that was rate limited.</param>
    /// <param name="policyName">Name of the rate limit policy.</param>
    public void RecordRateLimitHit(string route, string? policyName = null)
    {
        var tags = new TagList { { MetricTags.HttpRoute, route } };

        if (!string.IsNullOrEmpty(policyName))
        {
            tags.Add("policy", policyName);
        }

        _rateLimitHitsTotal.Add(1, tags);
    }

    #endregion

    #region Idempotency

    /// <summary>
    /// Records an idempotent duplicate request.
    /// </summary>
    /// <param name="route">Route of the duplicate request.</param>
    public void RecordIdempotentDuplicate(string route)
    {
        var tags = new TagList { { MetricTags.HttpRoute, route } };
        _idempotentDuplicatesTotal.Add(1, tags);
    }

    #endregion

    #region Scope Struct

    /// <summary>
    /// Represents a scope for tracking HTTP request duration.
    /// </summary>
    public struct HttpRequestScope : IDisposable
    {
        private readonly HttpMetrics _metrics;
        private readonly string _method;
        private readonly string _route;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; private set; }

        /// <summary>
        /// Gets or sets the request body size in bytes.
        /// </summary>
        public int RequestSize { get; private set; }

        /// <summary>
        /// Gets or sets the response body size in bytes.
        /// </summary>
        public int ResponseSize { get; private set; }

        internal HttpRequestScope(HttpMetrics metrics, string method, string route)
        {
            _metrics = metrics;
            _method = method;
            _route = route;
            _startTimestamp = Stopwatch.GetTimestamp();
            StatusCode = 200;
            RequestSize = 0;
            ResponseSize = 0;

            _metrics.IncrementActiveRequests();
        }

        /// <summary>
        /// Sets the status code for the request.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        public void SetStatusCode(int statusCode) => StatusCode = statusCode;

        /// <summary>
        /// Sets the request and response sizes.
        /// </summary>
        /// <param name="requestSize">Request body size in bytes.</param>
        /// <param name="responseSize">Response body size in bytes.</param>
        public void SetSizes(int requestSize, int responseSize)
        {
            RequestSize = requestSize;
            ResponseSize = responseSize;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _metrics.DecrementActiveRequests();
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordRequest(_method, _route, StatusCode, elapsed.TotalMilliseconds, RequestSize, ResponseSize);
        }
    }

    #endregion
}

