//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Circuit Breaker policy implementation that prevents cascading failures by temporarily
    /// stopping requests to failing services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The circuit breaker has three states:
    /// <list type="bullet">
    /// <item><strong>Closed:</strong> Normal operation, requests pass through</item>
    /// <item><strong>Open:</strong> Circuit is open, requests fail immediately</item>
    /// <item><strong>Half-Open:</strong> Allowing limited requests to test if service recovered</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Advanced circuit breaker with failure ratio threshold</item>
    /// <item>Configurable sampling duration and minimum throughput</item>
    /// <item>Automatic recovery testing via half-open state</item>
    /// <item>State change notifications via callbacks</item>
    /// <item>Detailed logging of circuit state changes</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CircuitBreakerPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<CircuitBreakerPolicy>? _logger;
        private readonly CircuitBreakerPolicyOptions _options;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _policy;
        private readonly string _serviceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerPolicy"/> class.
        /// </summary>
        /// <param name="options">The circuit breaker policy options.</param>
        /// <param name="serviceName">The name of the service (for logging purposes).</param>
        /// <param name="logger">Optional logger instance.</param>
        public CircuitBreakerPolicy(
            CircuitBreakerPolicyOptions options,
            string serviceName = "HttpClient",
            ILogger<CircuitBreakerPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceName = serviceName ?? "HttpClient";
            _logger = logger;
            _policy = CreatePolicy();
        }

        /// <inheritdoc/>
        public string PolicyName => "CircuitBreakerPolicy";

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        public CircuitState CircuitState => _policy.CircuitState;

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

            try
            {
                return _policy.ExecuteAsync(
                    async (ct) =>
                    {
                        var request = requestFactory();
                        return await sendAsync(request, ct);
                    },
                    cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogError(
                    ex,
                    "Circuit breaker is open for {ServiceName}. Request was blocked",
                    _serviceName);
                throw;
            }
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _policy;

        /// <summary>
        /// Manually isolates the circuit breaker, opening it immediately.
        /// </summary>
        public void Isolate()
        {
            _policy.Isolate();
            _logger?.LogWarning("Circuit breaker MANUALLY ISOLATED for {ServiceName}", _serviceName);
        }

        /// <summary>
        /// Manually resets the circuit breaker to the closed state.
        /// </summary>
        public void Reset()
        {
            _policy.Reset();
            _logger?.LogInformation("Circuit breaker MANUALLY RESET for {ServiceName}", _serviceName);
        }

        private AsyncCircuitBreakerPolicy<HttpResponseMessage> CreatePolicy()
        {
            var policyBuilder = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .OrResult(response => IsFailure(response));

            return policyBuilder
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: _options.FailureRatio,
                    samplingDuration: _options.SamplingDuration,
                    minimumThroughput: _options.MinimumThroughput,
                    durationOfBreak: _options.BreakDuration,
                    onBreak: OnBreak,
                    onReset: OnReset,
                    onHalfOpen: OnHalfOpen);
        }

        private static bool IsFailure(HttpResponseMessage response)
        {
            // Consider 5xx and some 4xx status codes as failures
            var statusCode = (int)response.StatusCode;
            return statusCode >= 500 || statusCode == 408 || statusCode == 429;
        }

        private void OnBreak(DelegateResult<HttpResponseMessage> outcome, TimeSpan breakDuration)
        {
            var reason = outcome.Exception?.Message ??
                $"HTTP {(int?)outcome.Result?.StatusCode}: {outcome.Result?.ReasonPhrase}";

            _logger?.LogWarning(
                "Circuit breaker OPENED for {ServiceName}. " +
                "Reason: {Reason}. " +
                "Break duration: {BreakDurationSeconds}s. " +
                "Requests will fail immediately until circuit closes",
                _serviceName, reason, breakDuration.TotalSeconds);

            _options.OnBreak?.Invoke(new Options.CircuitBreakerStateChangeInfo
            {
                ServiceName = _serviceName,
                NewState = CircuitState.Open,
                BreakDuration = breakDuration,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });
        }

        private void OnReset()
        {
            _logger?.LogInformation(
                "Circuit breaker CLOSED for {ServiceName}. " +
                "Service appears to have recovered. Normal operation resumed",
                _serviceName);

            _options.OnReset?.Invoke(new Options.CircuitBreakerStateChangeInfo
            {
                ServiceName = _serviceName,
                NewState = CircuitState.Closed,
                Timestamp = DateTime.UtcNow
            });
        }

        private void OnHalfOpen()
        {
            _logger?.LogInformation(
                "Circuit breaker HALF-OPEN for {ServiceName}. " +
                "Testing if service has recovered by allowing limited requests",
                _serviceName);

            _options.OnHalfOpen?.Invoke(new Options.CircuitBreakerStateChangeInfo
            {
                ServiceName = _serviceName,
                NewState = CircuitState.HalfOpen,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

