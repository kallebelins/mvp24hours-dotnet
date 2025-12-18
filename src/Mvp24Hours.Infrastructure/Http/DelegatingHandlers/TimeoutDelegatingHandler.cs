//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that implements per-request timeout configuration.
    /// Provides more granular timeout control than HttpClient's default timeout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler allows setting different timeouts per request, overriding
    /// the HttpClient's default timeout. It uses CancellationToken to enforce
    /// the timeout and provides detailed logging.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Per-request timeout configuration via request properties</item>
    /// <item>Default timeout when not specified per request</item>
    /// <item>Detailed timeout exception with request information</item>
    /// <item>Automatic cancellation on timeout</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register handler
    /// services.AddTransient(sp => new TimeoutDelegatingHandler(
    ///     sp.GetRequiredService&lt;ILogger&lt;TimeoutDelegatingHandler&gt;&gt;(),
    ///     TimeSpan.FromSeconds(30)));
    /// 
    /// // Use default timeout
    /// var response = await httpClient.GetAsync("/api/data");
    /// 
    /// // Override timeout for specific request
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/long-running");
    /// request.SetTimeout(TimeSpan.FromMinutes(5));
    /// var response = await httpClient.SendAsync(request);
    /// </code>
    /// </example>
    public class TimeoutDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<TimeoutDelegatingHandler> _logger;
        private readonly TimeSpan _defaultTimeout;
        private readonly bool _enabled;

        /// <summary>
        /// The key used to store timeout value in request options.
        /// </summary>
        public const string TimeoutPropertyKey = "Mvp24Hours.HttpTimeout";

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TimeoutDelegatingHandler(ILogger<TimeoutDelegatingHandler> logger)
            : this(logger, TimeSpan.FromSeconds(30), true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="defaultTimeout">The default timeout for requests.</param>
        /// <param name="enabled">Whether the timeout handler is enabled.</param>
        public TimeoutDelegatingHandler(
            ILogger<TimeoutDelegatingHandler> logger,
            TimeSpan defaultTimeout,
            bool enabled = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultTimeout = defaultTimeout;
            _enabled = enabled;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_enabled)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Get timeout from request options or use default
            var timeout = GetRequestTimeout(request);

            if (timeout == Timeout.InfiniteTimeSpan || timeout == TimeSpan.Zero)
            {
                // No timeout, just pass through
                return await base.SendAsync(request, cancellationToken);
            }

            _logger.LogDebug(
                "Applying timeout of {TimeoutMs}ms to request {Method} {Uri}",
                timeout.TotalMilliseconds, request.Method, request.RequestUri);

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                return await base.SendAsync(request, linkedCts.Token);
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Request to {Method} {Uri} timed out after {TimeoutMs}ms",
                    request.Method, request.RequestUri, timeout.TotalMilliseconds);

                throw new HttpRequestTimeoutException(
                    $"The HTTP request to {request.RequestUri} timed out after {timeout.TotalMilliseconds}ms",
                    request.Method,
                    request.RequestUri,
                    timeout,
                    ex);
            }
        }

        private TimeSpan GetRequestTimeout(HttpRequestMessage request)
        {
#if NET5_0_OR_GREATER
            if (request.Options.TryGetValue(
                new HttpRequestOptionsKey<TimeSpan>(TimeoutPropertyKey), out var timeout))
            {
                return timeout;
            }
#else
            if (request.Properties.TryGetValue(TimeoutPropertyKey, out var timeoutObj) &&
                timeoutObj is TimeSpan timeout)
            {
                return timeout;
            }
#endif
            return _defaultTimeout;
        }
    }

    /// <summary>
    /// Exception thrown when an HTTP request times out.
    /// </summary>
    public class HttpRequestTimeoutException : HttpRequestException
    {
        /// <summary>
        /// Gets the HTTP method of the timed out request.
        /// </summary>
        public HttpMethod Method { get; }

        /// <summary>
        /// Gets the URI of the timed out request.
        /// </summary>
        public Uri? RequestUri { get; }

        /// <summary>
        /// Gets the timeout duration that was exceeded.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestTimeoutException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="innerException">The inner exception.</param>
        public HttpRequestTimeoutException(
            string message,
            HttpMethod method,
            Uri? requestUri,
            TimeSpan timeout,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Method = method;
            RequestUri = requestUri;
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Extension methods for setting per-request timeout.
    /// </summary>
    public static class HttpRequestMessageTimeoutExtensions
    {
        /// <summary>
        /// Sets the timeout for this specific HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The request message for chaining.</returns>
        public static HttpRequestMessage SetTimeout(
            this HttpRequestMessage request,
            TimeSpan timeout)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

#if NET5_0_OR_GREATER
            request.Options.Set(
                new HttpRequestOptionsKey<TimeSpan>(TimeoutDelegatingHandler.TimeoutPropertyKey),
                timeout);
#else
            request.Properties[TimeoutDelegatingHandler.TimeoutPropertyKey] = timeout;
#endif
            return request;
        }

        /// <summary>
        /// Removes the per-request timeout, using the default timeout.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>The request message for chaining.</returns>
        public static HttpRequestMessage ClearTimeout(
            this HttpRequestMessage request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

#if NET5_0_OR_GREATER
            // Set to infinite timespan to effectively clear the timeout
            request.Options.Set(
                new HttpRequestOptionsKey<TimeSpan>(TimeoutDelegatingHandler.TimeoutPropertyKey),
                Timeout.InfiniteTimeSpan);
#else
            request.Properties.Remove(TimeoutDelegatingHandler.TimeoutPropertyKey);
#endif
            return request;
        }

        /// <summary>
        /// Sets the timeout to infinite (no timeout) for this request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>The request message for chaining.</returns>
        public static HttpRequestMessage NoTimeout(
            this HttpRequestMessage request)
        {
            return request.SetTimeout(Timeout.InfiniteTimeSpan);
        }
    }
}

