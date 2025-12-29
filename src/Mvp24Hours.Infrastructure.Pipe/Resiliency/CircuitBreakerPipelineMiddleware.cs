//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Tracks the state of a circuit breaker.
    /// </summary>
    internal class CircuitBreakerState
    {
        private readonly object _lock = new();
        private readonly CircuitBreakerOptions _options;
        private readonly List<DateTimeOffset> _failures = new();
        private int _halfOpenSuccesses;
        private PipelineCircuitState _state = PipelineCircuitState.Closed;
        private DateTimeOffset? _openedAt;

        public CircuitBreakerState(CircuitBreakerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public PipelineCircuitState State
        {
            get
            {
                lock (_lock)
                {
                    UpdateState();
                    return _state;
                }
            }
        }

        public DateTimeOffset? RetryAfter
        {
            get
            {
                lock (_lock)
                {
                    if (_state == PipelineCircuitState.Open && _openedAt.HasValue)
                    {
                        return _openedAt.Value.Add(_options.OpenDuration);
                    }
                    return null;
                }
            }
        }

        public bool AllowExecution()
        {
            lock (_lock)
            {
                UpdateState();

                switch (_state)
                {
                    case PipelineCircuitState.Closed:
                        return true;
                    case PipelineCircuitState.HalfOpen:
                        return true;
                    case PipelineCircuitState.Open:
                        return false;
                    default:
                        return true;
                }
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                if (_state == PipelineCircuitState.HalfOpen)
                {
                    _halfOpenSuccesses++;
                    if (_halfOpenSuccesses >= _options.SuccessThreshold)
                    {
                        TransitionTo(PipelineCircuitState.Closed);
                        _failures.Clear();
                        _halfOpenSuccesses = 0;
                    }
                }
                else if (_state == PipelineCircuitState.Closed)
                {
                    // Success in closed state - no action needed
                }
            }
        }

        public void RecordFailure(Exception exception)
        {
            lock (_lock)
            {
                if (!_options.ShouldCountAsFailure(exception))
                    return;

                if (_state == PipelineCircuitState.HalfOpen)
                {
                    // Failure during half-open - immediately open
                    TransitionTo(PipelineCircuitState.Open);
                    _openedAt = DateTimeOffset.UtcNow;
                    _halfOpenSuccesses = 0;
                }
                else if (_state == PipelineCircuitState.Closed)
                {
                    // Record failure for closed state
                    CleanOldFailures();
                    _failures.Add(DateTimeOffset.UtcNow);

                    if (_failures.Count >= _options.FailureThreshold)
                    {
                        TransitionTo(PipelineCircuitState.Open);
                        _openedAt = DateTimeOffset.UtcNow;
                    }
                }
            }
        }

        private void UpdateState()
        {
            if (_state == PipelineCircuitState.Open && _openedAt.HasValue)
            {
                if (DateTimeOffset.UtcNow >= _openedAt.Value.Add(_options.OpenDuration))
                {
                    TransitionTo(PipelineCircuitState.HalfOpen);
                    _halfOpenSuccesses = 0;
                }
            }
            else if (_state == PipelineCircuitState.Closed)
            {
                CleanOldFailures();
            }
        }

        private void CleanOldFailures()
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(_options.SamplingDuration);
            _failures.RemoveAll(f => f < cutoff);
        }

        private void TransitionTo(PipelineCircuitState newState)
        {
            var oldState = _state;
            if (oldState != newState)
            {
                _state = newState;
                _options.OnStateChange?.Invoke(oldState, newState);
            }
        }
    }

    /// <summary>
    /// Middleware that implements circuit breaker logic for operations.
    /// Applies to operations implementing <see cref="ICircuitBreakerOperation"/> or uses default options.
    /// </summary>
    [Obsolete("Deprecated: Use NativePipelineResilienceExtensions with Microsoft.Extensions.Resilience instead. " +
              "This class will be removed in a future version. " +
              "See docs/en-us/modernization/generic-resilience.md for migration guide.", false)]
    public class CircuitBreakerPipelineMiddleware : IPipelineMiddleware
    {
        private readonly ILogger<CircuitBreakerPipelineMiddleware>? _logger;
        private readonly CircuitBreakerOptions _defaultOptions;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuits = new();

        /// <summary>
        /// Creates a new instance of CircuitBreakerPipelineMiddleware with default options.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public CircuitBreakerPipelineMiddleware(ILogger<CircuitBreakerPipelineMiddleware>? logger = null)
            : this(CircuitBreakerOptions.Default, logger)
        {
        }

        /// <summary>
        /// Creates a new instance of CircuitBreakerPipelineMiddleware with custom options.
        /// </summary>
        /// <param name="defaultOptions">Default circuit breaker options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public CircuitBreakerPipelineMiddleware(CircuitBreakerOptions defaultOptions, ILogger<CircuitBreakerPipelineMiddleware>? logger = null)
        {
            _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -300; // Run after retry middleware

        /// <summary>
        /// Gets the current state of a circuit breaker.
        /// </summary>
        /// <param name="key">The circuit breaker key.</param>
        /// <returns>The current state, or null if the circuit doesn't exist.</returns>
        public PipelineCircuitState? GetCircuitState(string key)
        {
            if (_circuits.TryGetValue(key, out var state))
            {
                return state.State;
            }
            return null;
        }

        /// <summary>
        /// Gets all circuit breaker states.
        /// </summary>
        /// <returns>A dictionary of circuit breaker keys and their states.</returns>
        public IReadOnlyDictionary<string, PipelineCircuitState> GetAllCircuitStates()
        {
            return _circuits.ToDictionary(x => x.Key, x => x.Value.State);
        }

        /// <summary>
        /// Manually resets a circuit breaker to closed state.
        /// </summary>
        /// <param name="key">The circuit breaker key.</param>
        /// <returns>True if the circuit was found and reset.</returns>
        public bool ResetCircuit(string key)
        {
            if (_circuits.TryRemove(key, out _))
            {
                _logger?.LogInformation("Circuit breaker '{Key}' manually reset", key);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            // Check if the current operation has circuit breaker protection
            var cbOperation = GetCircuitBreakerOperation(message);
            var key = cbOperation?.CircuitBreakerKey ?? _defaultOptions.Key;

            var options = GetEffectiveOptions(cbOperation);
            var circuitState = _circuits.GetOrAdd(key, _ => new CircuitBreakerState(options));

            // Check if circuit allows execution
            if (!circuitState.AllowExecution())
            {
                var retryAfter = circuitState.RetryAfter ?? DateTimeOffset.UtcNow.Add(options.OpenDuration);

                _logger?.LogWarning(
                    "Circuit breaker '{Key}' is open, rejecting operation. Retry after: {RetryAfter}",
                    key,
                    retryAfter);

                _logger?.LogDebug(
                    "CircuitBreaker: Rejected. Key: {Key}, RetryAfter: {RetryAfter:O}",
                    key,
                    retryAfter);

                options.OnRejected?.Invoke();

                throw new PipelineCircuitBreakerOpenException(key, retryAfter);
            }

            _logger?.LogDebug(
                "Circuit breaker '{Key}' state: {State}, allowing execution",
                key,
                circuitState.State);

            try
            {
                await next();

                // Check if message became faulty
                if (message.IsFaulty)
                {
                    // Count as failure if faulty
                    var faultyException = new InvalidOperationException("Operation completed with faults");
                    circuitState.RecordFailure(faultyException);

                    _logger?.LogDebug(
                        "Circuit breaker '{Key}' recorded faulty completion as failure. State: {State}",
                        key,
                        circuitState.State);
                }
                else
                {
                    circuitState.RecordSuccess();

                    _logger?.LogDebug(
                        "Circuit breaker '{Key}' recorded success. State: {State}",
                        key,
                        circuitState.State);
                }
            }
            catch (Exception ex)
            {
                // Record failure
                bool shouldCount = cbOperation?.ShouldCountAsFailure(ex) ?? options.ShouldCountAsFailure(ex);

                if (shouldCount)
                {
                    circuitState.RecordFailure(ex);

                    _logger?.LogDebug(
                        ex,
                        "Circuit breaker '{Key}' recorded failure. State: {State}",
                        key,
                        circuitState.State);

                    _logger?.LogDebug(
                        "CircuitBreaker: Failure recorded. Key: {Key}, State: {State}, Error: {ErrorMessage}",
                        key,
                        circuitState.State,
                        ex.Message);
                }

                throw;
            }
        }

        private static ICircuitBreakerOperation? GetCircuitBreakerOperation(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation is ICircuitBreakerOperation cbOperation)
                {
                    return cbOperation;
                }
            }
            return null;
        }

        private CircuitBreakerOptions GetEffectiveOptions(ICircuitBreakerOperation? cbOperation)
        {
            if (cbOperation == null)
                return _defaultOptions;

            return new CircuitBreakerOptions
            {
                Key = cbOperation.CircuitBreakerKey,
                FailureThreshold = cbOperation.FailureThreshold,
                OpenDuration = cbOperation.OpenDuration,
                SuccessThreshold = cbOperation.SuccessThreshold,
                BreakOnExceptions = cbOperation.BreakOnExceptions,
                SamplingDuration = _defaultOptions.SamplingDuration,
                OnStateChange = (old, @new) =>
                {
                    cbOperation.OnStateChange(old, @new);
                    _defaultOptions.OnStateChange?.Invoke(old, @new);
                }
            };
        }
    }
}

