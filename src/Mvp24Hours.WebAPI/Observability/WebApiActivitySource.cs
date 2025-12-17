//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.WebAPI.Observability;

/// <summary>
/// ActivitySource for WebAPI operations in OpenTelemetry-compatible tracing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API and Metrics API
/// which are automatically exported by OpenTelemetry when configured.
/// </para>
/// <para>
/// <strong>Metric Names:</strong>
/// <list type="bullet">
/// <item>mvp24hours_http_requests_total - Counter of total HTTP requests</item>
/// <item>mvp24hours_http_request_duration_ms - Histogram of request durations</item>
/// <item>mvp24hours_http_requests_in_progress - Gauge of in-flight requests</item>
/// <item>mvp24hours_http_request_size_bytes - Histogram of request sizes</item>
/// <item>mvp24hours_http_response_size_bytes - Histogram of response sizes</item>
/// <item>mvp24hours_http_errors_total - Counter of HTTP errors</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours WebAPI activities and metrics
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(WebApiActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     })
///     .WithMetrics(builder =>
///     {
///         builder
///             .AddMeter(WebApiActivitySource.MeterName)
///             .AddAspNetCoreInstrumentation()
///             .AddPrometheusExporter();
///     });
/// </code>
/// </example>
public static class WebApiActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours WebAPI operations.
    /// </summary>
    public const string SourceName = "Mvp24Hours.WebAPI";

    /// <summary>
    /// The name of the Meter for Mvp24Hours WebAPI metrics.
    /// </summary>
    public const string MeterName = "Mvp24Hours.WebAPI";

    /// <summary>
    /// The version of the instrumentation.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// The Meter instance used for creating metrics.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, Version);

    #region Metrics

    /// <summary>
    /// Counter for total HTTP requests.
    /// </summary>
    public static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>(
        "mvp24hours_http_requests_total",
        unit: "{request}",
        description: "Total number of HTTP requests processed");

    /// <summary>
    /// Histogram for request duration in milliseconds.
    /// </summary>
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "mvp24hours_http_request_duration_ms",
        unit: "ms",
        description: "HTTP request duration in milliseconds");

    /// <summary>
    /// Gauge for in-progress requests.
    /// </summary>
    private static long _requestsInProgress;

    /// <summary>
    /// Observable gauge for in-progress requests.
    /// </summary>
    public static readonly ObservableGauge<long> RequestsInProgress = Meter.CreateObservableGauge(
        "mvp24hours_http_requests_in_progress",
        () => _requestsInProgress,
        unit: "{request}",
        description: "Number of HTTP requests currently in progress");

    /// <summary>
    /// Histogram for request body size in bytes.
    /// </summary>
    public static readonly Histogram<long> RequestSize = Meter.CreateHistogram<long>(
        "mvp24hours_http_request_size_bytes",
        unit: "By",
        description: "HTTP request body size in bytes");

    /// <summary>
    /// Histogram for response body size in bytes.
    /// </summary>
    public static readonly Histogram<long> ResponseSize = Meter.CreateHistogram<long>(
        "mvp24hours_http_response_size_bytes",
        unit: "By",
        description: "HTTP response body size in bytes");

    /// <summary>
    /// Counter for HTTP errors.
    /// </summary>
    public static readonly Counter<long> ErrorsTotal = Meter.CreateCounter<long>(
        "mvp24hours_http_errors_total",
        unit: "{error}",
        description: "Total number of HTTP errors");

    /// <summary>
    /// Counter for slow requests.
    /// </summary>
    public static readonly Counter<long> SlowRequestsTotal = Meter.CreateCounter<long>(
        "mvp24hours_http_slow_requests_total",
        unit: "{request}",
        description: "Total number of slow HTTP requests");

    #endregion

    #region Activity Names

    /// <summary>
    /// Activity names for different operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for HTTP request processing.</summary>
        public const string HttpRequest = "Mvp24Hours.WebAPI.HttpRequest";

        /// <summary>Activity name for request logging.</summary>
        public const string RequestLogging = "Mvp24Hours.WebAPI.RequestLogging";

        /// <summary>Activity name for exception handling.</summary>
        public const string ExceptionHandling = "Mvp24Hours.WebAPI.ExceptionHandling";

        /// <summary>Activity name for model validation.</summary>
        public const string ModelValidation = "Mvp24Hours.WebAPI.ModelValidation";
    }

    #endregion

    #region Tag Names

    /// <summary>
    /// Tag names for activity attributes following OpenTelemetry semantic conventions.
    /// </summary>
    public static class TagNames
    {
        /// <summary>HTTP request method.</summary>
        public const string HttpMethod = "http.method";

        /// <summary>HTTP request path.</summary>
        public const string HttpPath = "http.route";

        /// <summary>HTTP response status code.</summary>
        public const string HttpStatusCode = "http.status_code";

        /// <summary>HTTP request URL.</summary>
        public const string HttpUrl = "http.url";

        /// <summary>HTTP request scheme (http/https).</summary>
        public const string HttpScheme = "http.scheme";

        /// <summary>HTTP request host.</summary>
        public const string HttpHost = "http.host";

        /// <summary>HTTP request user agent.</summary>
        public const string HttpUserAgent = "http.user_agent";

        /// <summary>HTTP request content length.</summary>
        public const string HttpRequestContentLength = "http.request_content_length";

        /// <summary>HTTP response content length.</summary>
        public const string HttpResponseContentLength = "http.response_content_length";

        /// <summary>Correlation ID for request tracing.</summary>
        public const string CorrelationId = "correlation.id";

        /// <summary>Causation ID for request tracing.</summary>
        public const string CausationId = "causation.id";

        /// <summary>User ID for the request.</summary>
        public const string UserId = "enduser.id";

        /// <summary>Tenant ID for multi-tenant scenarios.</summary>
        public const string TenantId = "tenant.id";

        /// <summary>Client IP address.</summary>
        public const string ClientIp = "http.client_ip";

        /// <summary>Request duration in milliseconds.</summary>
        public const string DurationMs = "request.duration_ms";

        /// <summary>Whether the request was successful.</summary>
        public const string IsSuccess = "request.is_success";

        /// <summary>Error type name.</summary>
        public const string ErrorType = "error.type";

        /// <summary>Error message.</summary>
        public const string ErrorMessage = "error.message";

        /// <summary>Whether the request was slow.</summary>
        public const string IsSlow = "request.is_slow";

        /// <summary>Action name for MVC/WebAPI endpoints.</summary>
        public const string ActionName = "http.action";

        /// <summary>Controller name for MVC/WebAPI endpoints.</summary>
        public const string ControllerName = "http.controller";
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Increments the in-progress request counter.
    /// </summary>
    public static void IncrementInProgress()
    {
        System.Threading.Interlocked.Increment(ref _requestsInProgress);
    }

    /// <summary>
    /// Decrements the in-progress request counter.
    /// </summary>
    public static void DecrementInProgress()
    {
        System.Threading.Interlocked.Decrement(ref _requestsInProgress);
    }

    /// <summary>
    /// Records request completion with all relevant metrics.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Request path.</param>
    /// <param name="statusCode">Response status code.</param>
    /// <param name="durationMs">Request duration in milliseconds.</param>
    /// <param name="requestSizeBytes">Request body size.</param>
    /// <param name="responseSizeBytes">Response body size.</param>
    /// <param name="isError">Whether the request resulted in an error.</param>
    /// <param name="isSlow">Whether the request was slow.</param>
    public static void RecordRequest(
        string method,
        string path,
        int statusCode,
        double durationMs,
        long requestSizeBytes = 0,
        long responseSizeBytes = 0,
        bool isError = false,
        bool isSlow = false)
    {
        var tags = new TagList
        {
            { TagNames.HttpMethod, method },
            { TagNames.HttpPath, path },
            { TagNames.HttpStatusCode, statusCode }
        };

        RequestsTotal.Add(1, tags);
        RequestDuration.Record(durationMs, tags);

        if (requestSizeBytes > 0)
        {
            RequestSize.Record(requestSizeBytes, tags);
        }

        if (responseSizeBytes > 0)
        {
            ResponseSize.Record(responseSizeBytes, tags);
        }

        if (isError)
        {
            ErrorsTotal.Add(1, tags);
        }

        if (isSlow)
        {
            SlowRequestsTotal.Add(1, tags);
        }
    }

    /// <summary>
    /// Starts an activity for an HTTP request.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Request path.</param>
    /// <param name="correlationId">Correlation ID.</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartHttpRequestActivity(string method, string path, string? correlationId = null)
    {
        var activity = Source.StartActivity(ActivityNames.HttpRequest, ActivityKind.Server);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.HttpMethod, method);
        activity.SetTag(TagNames.HttpPath, path);

        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag(TagNames.CorrelationId, correlationId);
        }

        return activity;
    }

    /// <summary>
    /// Sets success status on an activity.
    /// </summary>
    public static void SetSuccess(Activity? activity, int statusCode)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.HttpStatusCode, statusCode);
        activity.SetTag(TagNames.IsSuccess, true);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Sets error status on an activity.
    /// </summary>
    public static void SetError(Activity? activity, Exception exception, int statusCode)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.HttpStatusCode, statusCode);
        activity.SetTag(TagNames.IsSuccess, false);
        activity.SetTag(TagNames.ErrorType, exception.GetType().FullName);
        activity.SetTag(TagNames.ErrorMessage, exception.Message);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception event
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    /// <summary>
    /// Adds request context information to an activity.
    /// </summary>
    public static void EnrichActivity(
        Activity? activity,
        string? userId = null,
        string? tenantId = null,
        string? causationId = null,
        string? clientIp = null,
        string? userAgent = null)
    {
        if (activity == null)
            return;

        if (!string.IsNullOrEmpty(userId))
            activity.SetTag(TagNames.UserId, userId);

        if (!string.IsNullOrEmpty(tenantId))
            activity.SetTag(TagNames.TenantId, tenantId);

        if (!string.IsNullOrEmpty(causationId))
            activity.SetTag(TagNames.CausationId, causationId);

        if (!string.IsNullOrEmpty(clientIp))
            activity.SetTag(TagNames.ClientIp, clientIp);

        if (!string.IsNullOrEmpty(userAgent))
            activity.SetTag(TagNames.HttpUserAgent, userAgent);
    }

    #endregion
}

