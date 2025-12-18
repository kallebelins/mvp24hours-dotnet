//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Use the CircuitBreakerStateChangeInfo from Options namespace
using CircuitBreakerStateChangeInfo = Mvp24Hours.Infrastructure.Http.Options.CircuitBreakerStateChangeInfo;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that implements the Circuit Breaker pattern using Polly.
    /// Prevents cascading failures by temporarily stopping requests to failing services.
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
    /// <example>
    /// <code>
    /// services.AddTransient(sp => new CircuitBreakerDelegatingHandler(
    ///     sp.GetRequiredService&lt;ILogger&lt;CircuitBreakerDelegatingHandler&gt;&gt;(),
    ///     new CircuitBreakerPolicyOptions
    ///     {
    ///         FailureRatio = 0.5,
    ///         SamplingDuration = TimeSpan.FromSeconds(30),
    ///         MinimumThroughput = 10,
    ///         BreakDuration = TimeSpan.FromSeconds(30)
    ///     }));
    /// 
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler&lt;CircuitBreakerDelegatingHandler&gt;();
    /// </code>
    /// </example>
    public class CircuitBreakerDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<CircuitBreakerDelegatingHandler> _logger;
        private readonly CircuitBreakerPolicyOptions _options;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private readonly string _serviceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CircuitBreakerDelegatingHandler(ILogger<CircuitBreakerDelegatingHandler> logger)
            : this(logger, new CircuitBreakerPolicyOptions(), "HttpClient")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The circuit breaker options.</param>
        /// <param name="serviceName">The name of the service (for logging purposes).</param>
        public CircuitBreakerDelegatingHandler(
            ILogger<CircuitBreakerDelegatingHandler> logger,
            CircuitBreakerPolicyOptions options,
            string serviceName = "HttpClient")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new CircuitBreakerPolicyOptions();
            _serviceName = serviceName ?? "HttpClient";
            _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        }

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        public CircuitState CircuitState => _circuitBreakerPolicy.CircuitState;

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

            try
            {
                return await _circuitBreakerPolicy.ExecuteAsync(
                    async (ct) => await base.SendAsync(request, ct),
                    cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(
                    ex,
                    "Circuit breaker is open for {ServiceName}. Request to {Method} {Uri} was blocked",
                    _serviceName, request.Method, request.RequestUri);
                throw;
            }
        }

        private AsyncCircuitBreakerPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>(ex => ex.CancellationToken == default)
                .OrResult(response => IsFailure(response))
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

            _logger.LogWarning(
                "Circuit breaker OPENED for {ServiceName}. " +
                "Reason: {Reason}. " +
                "Break duration: {BreakDurationSeconds}s. " +
                "Requests will fail immediately until circuit closes",
                _serviceName, reason, breakDuration.TotalSeconds);

            _options.OnBreak?.Invoke(new CircuitBreakerStateChangeInfo
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
            _logger.LogInformation(
                "Circuit breaker CLOSED for {ServiceName}. " +
                "Service appears to have recovered. Normal operation resumed",
                _serviceName);

            _options.OnReset?.Invoke(new CircuitBreakerStateChangeInfo
            {
                ServiceName = _serviceName,
                NewState = CircuitState.Closed,
                Timestamp = DateTime.UtcNow
            });
        }

        private void OnHalfOpen()
        {
            _logger.LogInformation(
                "Circuit breaker HALF-OPEN for {ServiceName}. " +
                "Testing if service has recovered by allowing limited requests",
                _serviceName);

            _options.OnHalfOpen?.Invoke(new CircuitBreakerStateChangeInfo
            {
                ServiceName = _serviceName,
                NewState = CircuitState.HalfOpen,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Manually isolates the circuit breaker, opening it immediately.
        /// </summary>
        public void Isolate()
        {
            _circuitBreakerPolicy.Isolate();
            _logger.LogWarning("Circuit breaker MANUALLY ISOLATED for {ServiceName}", _serviceName);
        }

        /// <summary>
        /// Manually resets the circuit breaker to the closed state.
        /// </summary>
        public void Reset()
        {
            _circuitBreakerPolicy.Reset();
            _logger.LogInformation("Circuit breaker MANUALLY RESET for {ServiceName}", _serviceName);
        }
    }

    /// <summary>
    /// Extended circuit breaker policy options with callbacks.
    /// </summary>
    public static class CircuitBreakerOptionsExtensions
    {
        /// <summary>
        /// Extends CircuitBreakerPolicyOptions with callback support.
        /// </summary>
        public static void SetCallbacks(
            this CircuitBreakerPolicyOptions options,
            Action<CircuitBreakerStateChangeInfo>? onBreak = null,
            Action<CircuitBreakerStateChangeInfo>? onReset = null,
            Action<CircuitBreakerStateChangeInfo>? onHalfOpen = null)
        {
            options.OnBreak = onBreak;
            options.OnReset = onReset;
            options.OnHalfOpen = onHalfOpen;
        }
    }
}

