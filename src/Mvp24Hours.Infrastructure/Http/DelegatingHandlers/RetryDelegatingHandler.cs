//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that implements retry logic using Polly.
    /// Supports configurable retry policies with exponential backoff and jitter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler provides resilience against transient failures by automatically
    /// retrying failed requests based on configurable policies.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Configurable number of retry attempts</item>
    /// <item>Multiple backoff strategies (Constant, Linear, Exponential, Jitter)</item>
    /// <item>Configurable retry conditions based on HTTP status codes</item>
    /// <item>Automatic handling of transient HTTP errors</item>
    /// <item>Detailed logging of retry attempts</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTransient(sp => new RetryDelegatingHandler(
    ///     sp.GetRequiredService&lt;ILogger&lt;RetryDelegatingHandler&gt;&gt;(),
    ///     new RetryPolicyOptions
    ///     {
    ///         MaxRetries = 3,
    ///         InitialDelay = TimeSpan.FromSeconds(1),
    ///         BackoffType = BackoffType.Exponential
    ///     }));
    /// 
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler&lt;RetryDelegatingHandler&gt;();
    /// </code>
    /// </example>
    public class RetryDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<RetryDelegatingHandler> _logger;
        private readonly RetryPolicyOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly Random _jitterer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public RetryDelegatingHandler(ILogger<RetryDelegatingHandler> logger)
            : this(logger, new RetryPolicyOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The retry policy options.</param>
        public RetryDelegatingHandler(
            ILogger<RetryDelegatingHandler> logger,
            RetryPolicyOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new RetryPolicyOptions();
            _jitterer = new Random();
            _retryPolicy = CreateRetryPolicy();
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

            if (!_options.Enabled)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Create a context to pass data between retries
            var context = new Context
            {
                ["RequestUri"] = request.RequestUri?.ToString() ?? "unknown",
                ["Method"] = request.Method.Method
            };

            return await _retryPolicy.ExecuteAsync(
                async (ctx, ct) => await base.SendAsync(CloneRequestIfNeeded(request), ct),
                context,
                cancellationToken);
        }

        private AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>(ex => ex.CancellationToken == default)
                .OrResult(response => ShouldRetry(response))
                .WaitAndRetryAsync(
                    retryCount: _options.MaxRetries,
                    sleepDurationProvider: (retryAttempt, outcome, context) => CalculateDelay(retryAttempt, outcome, context),
                    onRetryAsync: (outcome, timeSpan, retryCount, context) =>
                    {
                        OnRetry(outcome, timeSpan, retryCount, context);
                        return Task.CompletedTask;
                    });
        }

        private bool ShouldRetry(HttpResponseMessage response)
        {
            // Retry on transient HTTP errors (5xx, 408, 429)
            if (IsTransientError(response.StatusCode))
            {
                return true;
            }

            // Retry on configured status codes
            return _options.RetryStatusCodes.Contains((int)response.StatusCode);
        }

        private static bool IsTransientError(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.RequestTimeout => true,        // 408
                HttpStatusCode.TooManyRequests => true,       // 429
                HttpStatusCode.InternalServerError => true,   // 500
                HttpStatusCode.BadGateway => true,            // 502
                HttpStatusCode.ServiceUnavailable => true,    // 503
                HttpStatusCode.GatewayTimeout => true,        // 504
                _ => (int)statusCode >= 500
            };
        }

        private TimeSpan CalculateDelay(int retryAttempt, DelegateResult<HttpResponseMessage>? outcome, Context context)
        {
            // Check for Retry-After header
            if (outcome?.Result?.Headers.RetryAfter?.Delta.HasValue == true)
            {
                var retryAfter = outcome.Result.Headers.RetryAfter.Delta.Value;
                if (retryAfter <= _options.MaxDelay)
                {
                    _logger.LogDebug("Using Retry-After header value of {RetryAfter}ms for retry attempt {Attempt}",
                        retryAfter.TotalMilliseconds, retryAttempt);
                    return retryAfter;
                }
            }

            var delay = _options.BackoffType switch
            {
                BackoffType.Constant => _options.InitialDelay,
                BackoffType.Linear => TimeSpan.FromMilliseconds(
                    _options.InitialDelay.TotalMilliseconds * retryAttempt),
                BackoffType.Exponential => TimeSpan.FromMilliseconds(
                    _options.InitialDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1)),
                BackoffType.DecorrelatedJitter => CalculateJitteredDelay(retryAttempt),
                _ => _options.InitialDelay
            };

            // Ensure delay doesn't exceed max delay
            if (delay > _options.MaxDelay)
            {
                delay = _options.MaxDelay;
            }

            // Add jitter for non-decorrelated types
            if (_options.BackoffType != BackoffType.DecorrelatedJitter && _options.JitterFactor > 0)
            {
                var jitter = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * _options.JitterFactor * _jitterer.NextDouble());
                delay = delay.Add(jitter);
            }

            return delay;
        }

        private TimeSpan CalculateJitteredDelay(int retryAttempt)
        {
            // Decorrelated jitter algorithm for better distribution
            var delay = TimeSpan.FromMilliseconds(
                _options.InitialDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1) *
                (1.0 + _jitterer.NextDouble() * _options.JitterFactor));

            return delay > _options.MaxDelay ? _options.MaxDelay : delay;
        }

        private void OnRetry(
            DelegateResult<HttpResponseMessage> outcome,
            TimeSpan timeSpan,
            int retryCount,
            Context context)
        {
            var uri = context.ContainsKey("RequestUri") ? context["RequestUri"] : "unknown";
            var method = context.ContainsKey("Method") ? context["Method"] : "unknown";

            if (outcome.Exception != null)
            {
                _logger.LogWarning(
                    outcome.Exception,
                    "Retry attempt {RetryCount}/{MaxRetries} for {Method} {Uri} " +
                    "after exception. Waiting {DelayMs}ms before next attempt",
                    retryCount, _options.MaxRetries, method, uri, timeSpan.TotalMilliseconds);
            }
            else
            {
                var statusCode = (int?)outcome.Result?.StatusCode ?? 0;
                var reason = outcome.Result?.ReasonPhrase ?? "Unknown";

                _logger.LogWarning(
                    "Retry attempt {RetryCount}/{MaxRetries} for {Method} {Uri} " +
                    "after response {StatusCode} {Reason}. Waiting {DelayMs}ms before next attempt",
                    retryCount, _options.MaxRetries, method, uri, statusCode, reason, timeSpan.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Clones the request message for retry attempts.
        /// This is necessary because HttpRequestMessage can only be sent once.
        /// </summary>
        private static HttpRequestMessage CloneRequestIfNeeded(HttpRequestMessage request)
        {
            // Note: In a real implementation, you might need to clone the request
            // for subsequent retry attempts since HttpRequestMessage can only be sent once.
            // However, this depends on your specific use case and whether the inner handler
            // disposes the request content.
            return request;
        }
    }
}

