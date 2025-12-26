//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check for HTTP clients to verify connectivity and response time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies HTTP client connectivity by:
    /// <list type="bullet">
    /// <item>Attempting to send a HEAD or GET request to a health endpoint</item>
    /// <item>Measuring response time</item>
    /// <item>Verifying HTTP status code is successful (2xx)</item>
    /// <item>Checking for timeout or connection errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddHttpClientHealthCheck&lt;MyApiClient&gt;(
    ///         "http-client-api",
    ///         options =>
    ///         {
    ///             options.HealthEndpoint = "/health";
    ///             options.TimeoutSeconds = 5;
    ///             options.ExpectedStatusCode = System.Net.HttpStatusCode.OK;
    ///         });
    /// </code>
    /// </example>
    public class HttpClientHealthCheck<TApi> : IHealthCheck
        where TApi : class
    {
        private readonly ITypedHttpClient<TApi> _httpClient;
        private readonly HttpClientHealthCheckOptions _options;
        private readonly ILogger<HttpClientHealthCheck<TApi>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientHealthCheck{TApi}"/> class.
        /// </summary>
        /// <param name="httpClient">The typed HTTP client to check.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public HttpClientHealthCheck(
            ITypedHttpClient<TApi> httpClient,
            HttpClientHealthCheckOptions? options,
            ILogger<HttpClientHealthCheck<TApi>> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? new HttpClientHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                data["baseAddress"] = _httpClient.BaseAddress?.ToString() ?? "Unknown";
                data["timeout"] = _httpClient.Timeout.TotalSeconds;

                // Build health endpoint URL
                var healthUrl = _options.HealthEndpoint;
                if (!healthUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && _httpClient.BaseAddress != null)
                {
                    healthUrl = new Uri(_httpClient.BaseAddress, healthUrl).ToString();
                }

                data["healthEndpoint"] = healthUrl;

                // Create request with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                HttpResponseMessage? response = null;
                try
                {
                    // Use HEAD request if supported, otherwise GET
                    if (_options.UseHeadRequest)
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Head, healthUrl);
                        response = await _httpClient.SendAsync(request, cts.Token);
                    }
                    else
                    {
                        response = await _httpClient.HttpClient.GetAsync(healthUrl, cts.Token);
                    }
                }
                catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = "Request timeout";

                    _logger.LogWarning("HTTP client health check timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);

                    return HealthCheckResult.Unhealthy(
                        description: $"HTTP client health check timed out after {_options.TimeoutSeconds}s",
                        data: data);
                }

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["statusCode"] = (int)response.StatusCode;
                data["statusCodeName"] = response.StatusCode.ToString();

                // Check response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"HTTP client response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"HTTP client response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                // Check status code
                if (response.StatusCode != _options.ExpectedStatusCode)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"HTTP client returned unexpected status code: {response.StatusCode} (expected: {_options.ExpectedStatusCode})",
                        data: data);
                }

                // Check response content if required
                if (_options.ValidateResponseContent && response.Content != null)
                {
                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    if (!string.IsNullOrWhiteSpace(_options.ExpectedResponseContent) &&
                        !content.Contains(_options.ExpectedResponseContent, StringComparison.OrdinalIgnoreCase))
                    {
                        return HealthCheckResult.Degraded(
                            description: "HTTP client response content validation failed",
                            data: data);
                    }
                }

                return HealthCheckResult.Healthy(
                    description: $"HTTP client is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "HTTP client health check failed with HTTP error");

                return HealthCheckResult.Unhealthy(
                    description: $"HTTP client health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "HTTP client health check failed with unexpected error");

                return HealthCheckResult.Unhealthy(
                    description: $"HTTP client health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }
    }

    /// <summary>
    /// Configuration options for HTTP client health checks.
    /// </summary>
    public sealed class HttpClientHealthCheckOptions
    {
        /// <summary>
        /// Health endpoint URL to check. Can be absolute or relative to BaseAddress.
        /// Default: "/health"
        /// </summary>
        public string HealthEndpoint { get; set; } = "/health";

        /// <summary>
        /// Timeout in seconds for the health check request.
        /// Default is 5 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Expected HTTP status code for healthy response.
        /// Default is OK (200).
        /// </summary>
        public System.Net.HttpStatusCode ExpectedStatusCode { get; set; } = System.Net.HttpStatusCode.OK;

        /// <summary>
        /// Whether to use HEAD request instead of GET.
        /// Default is false (uses GET).
        /// </summary>
        public bool UseHeadRequest { get; set; }

        /// <summary>
        /// Whether to validate response content.
        /// Default is false.
        /// </summary>
        public bool ValidateResponseContent { get; set; }

        /// <summary>
        /// Expected response content substring (case-insensitive).
        /// Only used if ValidateResponseContent is true.
        /// </summary>
        public string? ExpectedResponseContent { get; set; }

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// Default is 500ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 500;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// Default is 2000ms.
        /// </summary>
        public int FailureThresholdMs { get; set; } = 2000;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "http", "httpclient", "ready" };
    }
}

