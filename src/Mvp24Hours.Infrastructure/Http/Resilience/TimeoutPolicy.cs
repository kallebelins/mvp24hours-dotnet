//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.Timeout;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Timeout policy implementation that enforces request timeouts.
    /// Supports both optimistic (cooperative cancellation) and pessimistic (hard timeout) modes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Timeout Modes:</strong>
    /// <list type="bullet">
    /// <item><strong>Optimistic:</strong> Uses CancellationToken for cooperative cancellation (recommended)</item>
    /// <item><strong>Pessimistic:</strong> Hard timeout that aborts the operation (use with caution)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Configurable timeout duration</item>
    /// <item>Optimistic timeout (cooperative cancellation)</item>
    /// <item>Pessimistic timeout (hard abort)</item>
    /// <item>Detailed logging of timeout events</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class TimeoutPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<TimeoutPolicy>? _logger;
        private readonly TimeoutPolicyOptions _options;
        private readonly AsyncTimeoutPolicy<HttpResponseMessage> _policy;
        private readonly TimeoutStrategy _strategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutPolicy"/> class.
        /// </summary>
        /// <param name="options">The timeout policy options.</param>
        /// <param name="strategy">The timeout strategy (Optimistic or Pessimistic). Default is Optimistic.</param>
        /// <param name="logger">Optional logger instance.</param>
        public TimeoutPolicy(
            TimeoutPolicyOptions options,
            TimeoutStrategy strategy = TimeoutStrategy.Optimistic,
            ILogger<TimeoutPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _strategy = strategy;
            _logger = logger;
            _policy = CreatePolicy();
        }

        /// <inheritdoc/>
        public string PolicyName => "TimeoutPolicy";

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

            return _policy.ExecuteAsync(
                async (ct) =>
                {
                    var request = requestFactory();
                    return await sendAsync(request, ct);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _policy;

        private AsyncTimeoutPolicy<HttpResponseMessage> CreatePolicy()
        {
            if (_strategy == TimeoutStrategy.Pessimistic)
            {
                return Policy.TimeoutAsync<HttpResponseMessage>(
                    _options.Timeout,
                    TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        OnTimeout(timespan, "pessimistic");
                        return Task.CompletedTask;
                    });
            }
            else
            {
                return Policy.TimeoutAsync<HttpResponseMessage>(
                    _options.Timeout,
                    TimeoutStrategy.Optimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        OnTimeout(timespan, "optimistic");
                        return Task.CompletedTask;
                    });
            }
        }

        private void OnTimeout(TimeSpan timeout, string strategy)
        {
            _logger?.LogWarning(
                "Request timed out after {TimeoutSeconds}s using {Strategy} timeout strategy",
                timeout.TotalSeconds, strategy);
        }
    }
}

