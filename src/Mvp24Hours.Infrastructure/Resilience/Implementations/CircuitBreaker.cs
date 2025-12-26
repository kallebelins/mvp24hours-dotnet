//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Contract;
using Mvp24Hours.Infrastructure.Resilience.Exceptions;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Implementations
{
    /// <summary>
    /// Generic implementation of circuit breaker pattern for protecting operations.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// This implementation provides a generic circuit breaker that can protect any async operation.
    /// It tracks failures and opens the circuit when the failure threshold is exceeded.
    /// </para>
    /// </remarks>
    public class CircuitBreaker<TResult> : ICircuitBreaker<TResult>
    {
        private readonly CircuitBreakerOptions _options;
        private readonly ILogger<CircuitBreaker<TResult>>? _logger;
        private readonly string _operationName;
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<OperationResult> _recentOperations = new();

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private DateTime _lastFailureTime;
        private int _consecutiveFailures;
        private DateTime _circuitOpenedAt;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreaker{TResult}"/> class.
        /// </summary>
        /// <param name="options">Circuit breaker options.</param>
        /// <param name="operationName">Name of the operation (for logging).</param>
        /// <param name="logger">Optional logger.</param>
        public CircuitBreaker(
            CircuitBreakerOptions options,
            string operationName = "Operation",
            ILogger<CircuitBreaker<TResult>>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _operationName = operationName ?? "Operation";
            _logger = logger;
        }

        /// <inheritdoc/>
        public CircuitBreakerState State
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

        /// <inheritdoc/>
        public async Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync((_, ct) => operation(ct), null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            // Check if circuit is open
            if (State == CircuitBreakerState.Open)
            {
                var reason = $"Circuit breaker is open for {_operationName}. " +
                    $"Opened at {_circuitOpenedAt:O}. " +
                    $"Will retry after {_options.BreakDuration.TotalSeconds} seconds.";

                _logger?.LogWarning(reason);

                throw new CircuitBreakerOpenException(reason);
            }

            try
            {
                var result = await operation(context, cancellationToken);
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                if (ShouldCountAsFailure(ex))
                {
                    RecordFailure(ex);
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public void Isolate()
        {
            lock (_lock)
            {
                if (_state != CircuitBreakerState.Open)
                {
                    _state = CircuitBreakerState.Open;
                    _circuitOpenedAt = DateTime.UtcNow;
                    _logger?.LogWarning("Circuit breaker MANUALLY ISOLATED for {OperationName}", _operationName);

                    _options.OnBreak?.Invoke(new CircuitBreakerStateChangeInfo
                    {
                        OperationName = _operationName,
                        NewState = CircuitBreakerState.Open,
                        BreakDuration = _options.BreakDuration,
                        Reason = "Manually isolated",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            lock (_lock)
            {
                if (_state != CircuitBreakerState.Closed)
                {
                    _state = CircuitBreakerState.Closed;
                    _consecutiveFailures = 0;
                    _recentOperations.Clear();
                    _logger?.LogInformation("Circuit breaker MANUALLY RESET for {OperationName}", _operationName);

                    _options.OnReset?.Invoke(new CircuitBreakerStateChangeInfo
                    {
                        OperationName = _operationName,
                        NewState = CircuitBreakerState.Closed,
                        Reason = "Manually reset",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        private void UpdateState()
        {
            var now = DateTime.UtcNow;

            // Clean old operations outside sampling window
            while (_recentOperations.TryPeek(out var op) &&
                now - op.Timestamp > _options.SamplingDuration)
            {
                _recentOperations.TryDequeue(out _);
            }

            // Check if we should transition from Open to HalfOpen
            if (_state == CircuitBreakerState.Open)
            {
                if (now - _circuitOpenedAt >= _options.BreakDuration)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger?.LogInformation(
                        "Circuit breaker HALF-OPEN for {OperationName}. Testing if service recovered",
                        _operationName);

                    _options.OnHalfOpen?.Invoke(new CircuitBreakerStateChangeInfo
                    {
                        OperationName = _operationName,
                        NewState = CircuitBreakerState.HalfOpen,
                        Reason = "Break duration elapsed",
                        Timestamp = now
                    });
                }
            }

            // Check if we should open the circuit
            if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
            {
                var totalOperations = _recentOperations.Count;
                if (totalOperations >= _options.MinimumThroughput)
                {
                    var failures = 0;
                    foreach (var op in _recentOperations)
                    {
                        if (!op.Success)
                        {
                            failures++;
                        }
                    }

                    var failureRatio = (double)failures / totalOperations;
                    if (failureRatio >= _options.FailureRatio)
                    {
                        _state = CircuitBreakerState.Open;
                        _circuitOpenedAt = now;
                        _logger?.LogWarning(
                            "Circuit breaker OPENED for {OperationName}. " +
                            "Failure ratio: {FailureRatio:P2} ({Failures}/{Total}). " +
                            "Break duration: {BreakDuration}s",
                            _operationName,
                            failureRatio,
                            failures,
                            totalOperations,
                            _options.BreakDuration.TotalSeconds);

                        _options.OnBreak?.Invoke(new CircuitBreakerStateChangeInfo
                        {
                            OperationName = _operationName,
                            NewState = CircuitBreakerState.Open,
                            BreakDuration = _options.BreakDuration,
                            Reason = $"Failure ratio {failureRatio:P2} exceeded threshold {_options.FailureRatio:P2}",
                            Timestamp = now
                        });
                    }
                }
            }

            // Check if we should close the circuit (from HalfOpen)
            if (_state == CircuitBreakerState.HalfOpen)
            {
                // If recent operation succeeded, close the circuit
                if (_recentOperations.TryPeek(out var lastOp) && lastOp.Success)
                {
                    _state = CircuitBreakerState.Closed;
                    _consecutiveFailures = 0;
                    _recentOperations.Clear();
                    _logger?.LogInformation(
                        "Circuit breaker CLOSED for {OperationName}. Service appears to have recovered",
                        _operationName);

                    _options.OnReset?.Invoke(new CircuitBreakerStateChangeInfo
                    {
                        OperationName = _operationName,
                        NewState = CircuitBreakerState.Closed,
                        Reason = "Service recovered",
                        Timestamp = now
                    });
                }
            }
        }

        private void RecordSuccess()
        {
            lock (_lock)
            {
                _consecutiveFailures = 0;
                _recentOperations.Enqueue(new OperationResult { Success = true, Timestamp = DateTime.UtcNow });

                // Limit queue size to sampling window
                while (_recentOperations.Count > _options.MinimumThroughput * 2)
                {
                    _recentOperations.TryDequeue(out _);
                }

                UpdateState();
            }
        }

        private void RecordFailure(Exception exception)
        {
            lock (_lock)
            {
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;
                _recentOperations.Enqueue(new OperationResult { Success = false, Timestamp = DateTime.UtcNow });

                // Limit queue size
                while (_recentOperations.Count > _options.MinimumThroughput * 2)
                {
                    _recentOperations.TryDequeue(out _);
                }

                UpdateState();
            }
        }

        private bool ShouldCountAsFailure(Exception exception)
        {
            if (_options.ShouldCountAsFailure != null)
            {
                return _options.ShouldCountAsFailure(exception);
            }

            // Default: count all exceptions as failures
            return true;
        }

        private class OperationResult
        {
            public bool Success { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// Generic implementation of circuit breaker pattern for operations without return values.
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly CircuitBreaker<object?> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
        /// </summary>
        /// <param name="options">Circuit breaker options.</param>
        /// <param name="operationName">Name of the operation (for logging).</param>
        /// <param name="logger">Optional logger.</param>
        public CircuitBreaker(
            CircuitBreakerOptions options,
            string operationName = "Operation",
            ILogger<CircuitBreaker>? logger = null)
        {
            _inner = new CircuitBreaker<object?>(options, operationName, null);
        }

        /// <inheritdoc/>
        public CircuitBreakerState State => _inner.State;

        /// <inheritdoc/>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync((_, ct) => operation(ct), null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            await _inner.ExecuteAsync(
                async (ctx, ct) =>
                {
                    await operation(ctx, ct);
                    return (object?)null;
                },
                context,
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Isolate() => _inner.Isolate();

        /// <inheritdoc/>
        public void Reset() => _inner.Reset();
    }
}

