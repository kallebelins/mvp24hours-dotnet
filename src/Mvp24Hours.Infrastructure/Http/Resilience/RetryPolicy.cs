//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Retry policy implementation with exponential backoff and configurable retry conditions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This policy automatically retries failed HTTP requests based on configurable conditions.
    /// Supports multiple backoff strategies: Constant, Linear, Exponential, and Decorrelated Jitter.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Configurable number of retry attempts</item>
    /// <item>Multiple backoff strategies with jitter support</item>
    /// <item>Automatic handling of transient HTTP errors (5xx, 408, 429)</item>
    /// <item>Configurable retry conditions based on HTTP status codes</item>
    /// <item>Respects Retry-After header when present</item>
    /// <item>Detailed logging of retry attempts</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RetryPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<RetryPolicy>? _logger;
        private readonly RetryPolicyOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
        private readonly Random _jitterer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
        /// </summary>
        /// <param name="options">The retry policy options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public RetryPolicy(RetryPolicyOptions options, ILogger<RetryPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _jitterer = new Random();
            _policy = CreatePolicy();
        }

        /// <inheritdoc/>
        public string PolicyName => "RetryPolicy";

        /// <inheritdoc/>
        public Task<HttpResponseMessage> ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken = default)
        {
            if (requestFactory == null)
            {
                throw new ArgumentNullException(nameof(requestFactory));
            }

            if (sendAsync == null)
            {
                throw new ArgumentNullException(nameof(sendAsync));
            }

            if (!_options.Enabled)
            {
                var request = requestFactory();
                return sendAsync(request, cancellationToken);
            }

            var context = new Context
            {
                ["RequestFactory"] = requestFactory,
                ["SendAsync"] = sendAsync
            };

            return _policy.ExecuteAsync(
                async (ctx, ct) =>
                {
                    var reqFactory = (Func<HttpRequestMessage>)ctx["RequestFactory"];
                    var send = (Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>)ctx["SendAsync"];
                    var request = reqFactory();
                    return await send(request, ct);
                },
                context,
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _policy;

        private AsyncRetryPolicy<HttpResponseMessage> CreatePolicy()
        {
            var policyBuilder = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>();

            // Add custom status codes to retry on
            if (_options.RetryStatusCodes.Count > 0)
            {
                foreach (var statusCode in _options.RetryStatusCodes)
                {
                    policyBuilder = policyBuilder.OrResult(response => (int)response.StatusCode == statusCode);
                }
            }

            return policyBuilder
                .WaitAndRetryAsync(
                    retryCount: _options.MaxRetries,
                    sleepDurationProvider: (retryAttempt, outcome, context) => CalculateDelay(retryAttempt, outcome),
                    onRetryAsync: (outcome, timeSpan, retryCount, context) =>
                    {
                        OnRetry(outcome, timeSpan, retryCount, context);
                        return Task.CompletedTask;
                    });
        }

        private TimeSpan CalculateDelay(int retryAttempt, DelegateResult<HttpResponseMessage>? outcome)
        {
            // Check for Retry-After header
            if (outcome?.Result?.Headers.RetryAfter?.Delta.HasValue == true)
            {
                var retryAfter = outcome.Result.Headers.RetryAfter.Delta.Value;
                if (retryAfter <= _options.MaxDelay)
                {
                    _logger?.LogDebug(
                        "Using Retry-After header value of {RetryAfter}ms for retry attempt {Attempt}",
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
            var uri = context.ContainsKey("RequestUri") ? context["RequestUri"]?.ToString() : "unknown";
            var method = context.ContainsKey("Method") ? context["Method"]?.ToString() : "unknown";

            if (outcome.Exception != null)
            {
                _logger?.LogWarning(
                    outcome.Exception,
                    "Retry attempt {RetryCount}/{MaxRetries} for {Method} {Uri} " +
                    "after exception. Waiting {DelayMs}ms before next attempt",
                    retryCount, _options.MaxRetries, method, uri, timeSpan.TotalMilliseconds);
            }
            else
            {
                var statusCode = (int?)outcome.Result?.StatusCode ?? 0;
                var reason = outcome.Result?.ReasonPhrase ?? "Unknown";

                _logger?.LogWarning(
                    "Retry attempt {RetryCount}/{MaxRetries} for {Method} {Uri} " +
                    "after response {StatusCode} {Reason}. Waiting {DelayMs}ms before next attempt",
                    retryCount, _options.MaxRetries, method, uri, statusCode, reason, timeSpan.TotalMilliseconds);
            }
        }
    }
}

