//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Implements a circuit breaker pattern for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The circuit breaker has three states:
    /// <list type="bullet">
    ///   <item><b>Closed</b>: Normal operation, all requests pass through</item>
    ///   <item><b>Open</b>: Circuit tripped, all requests fail immediately</item>
    ///   <item><b>Half-Open</b>: Testing if service has recovered</item>
    /// </list>
    /// </para>
    /// <para>
    /// State transitions:
    /// <list type="bullet">
    ///   <item>Closed → Open: When failure threshold is exceeded</item>
    ///   <item>Open → Half-Open: After break duration expires</item>
    ///   <item>Half-Open → Closed: When a test request succeeds</item>
    ///   <item>Half-Open → Open: When a test request fails</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class MongoDbCircuitBreaker : ICircuitBreakerMetrics
    {
        private readonly MongoDbResiliencyOptions _options;
        private readonly object _stateLock = new();
        private readonly ConcurrentQueue<DateTimeOffset> _recentFailures = new();

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private DateTimeOffset? _openedAt;
        private DateTimeOffset? _lastSuccessTime;
        private DateTimeOffset? _lastFailureTime;

        private long _totalSuccessCount;
        private long _totalFailureCount;
        private long _totalRejectedCount;
        private long _circuitTripCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCircuitBreaker"/> class.
        /// </summary>
        /// <param name="options">The resiliency options.</param>
        public MongoDbCircuitBreaker(MongoDbResiliencyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State
        {
            get
            {
                lock (_stateLock)
                {
                    if (_state == CircuitBreakerState.Open && ShouldTransitionToHalfOpen())
                    {
                        TransitionTo(CircuitBreakerState.HalfOpen);
                    }
                    return _state;
                }
            }
        }

        #region ICircuitBreakerMetrics

        /// <inheritdoc />
        public long TotalSuccessCount => Interlocked.Read(ref _totalSuccessCount);

        /// <inheritdoc />
        public long TotalFailureCount => Interlocked.Read(ref _totalFailureCount);

        /// <inheritdoc />
        public long TotalRejectedCount => Interlocked.Read(ref _totalRejectedCount);

        /// <inheritdoc />
        public long CircuitTripCount => Interlocked.Read(ref _circuitTripCount);

        /// <inheritdoc />
        public double CurrentFailureRate
        {
            get
            {
                var total = TotalSuccessCount + TotalFailureCount;
                if (total == 0) return 0;
                return (double)TotalFailureCount / total;
            }
        }

        /// <inheritdoc />
        public DateTimeOffset? LastSuccessTime => _lastSuccessTime;

        /// <inheritdoc />
        public DateTimeOffset? LastFailureTime => _lastFailureTime;

        /// <inheritdoc />
        public DateTimeOffset? LastOpenTime => _openedAt;

        /// <inheritdoc />
        public void Reset()
        {
            lock (_stateLock)
            {
                Interlocked.Exchange(ref _totalSuccessCount, 0);
                Interlocked.Exchange(ref _totalFailureCount, 0);
                Interlocked.Exchange(ref _totalRejectedCount, 0);
                Interlocked.Exchange(ref _circuitTripCount, 0);
                _lastSuccessTime = null;
                _lastFailureTime = null;
                _openedAt = null;
                while (_recentFailures.TryDequeue(out _)) { }
                _state = CircuitBreakerState.Closed;
            }
        }

        #endregion

        /// <summary>
        /// Checks if the circuit allows an operation to proceed.
        /// </summary>
        /// <returns>True if the operation should proceed; false if it should be rejected.</returns>
        public bool AllowRequest()
        {
            var state = State; // This may trigger state transition

            switch (state)
            {
                case CircuitBreakerState.Closed:
                    return true;

                case CircuitBreakerState.HalfOpen:
                    // In half-open state, allow one test request
                    return true;

                case CircuitBreakerState.Open:
                    Interlocked.Increment(ref _totalRejectedCount);
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets the remaining duration until the circuit transitions to half-open.
        /// </summary>
        /// <returns>The remaining duration, or null if not in open state.</returns>
        public TimeSpan? GetRemainingOpenDuration()
        {
            lock (_stateLock)
            {
                if (_state != CircuitBreakerState.Open || !_openedAt.HasValue)
                    return null;

                var elapsed = DateTimeOffset.UtcNow - _openedAt.Value;
                var breakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds);
                var remaining = breakDuration - elapsed;

                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Records a successful operation.
        /// </summary>
        public void RecordSuccess()
        {
            Interlocked.Increment(ref _totalSuccessCount);
            _lastSuccessTime = DateTimeOffset.UtcNow;

            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Successful test request, close the circuit
                    TransitionTo(CircuitBreakerState.Closed);
                    while (_recentFailures.TryDequeue(out _)) { } // Clear failures
                }
            }
        }

        /// <summary>
        /// Records a failed operation.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        public void RecordFailure(Exception exception)
        {
            Interlocked.Increment(ref _totalFailureCount);
            _lastFailureTime = DateTimeOffset.UtcNow;
            _recentFailures.Enqueue(DateTimeOffset.UtcNow);

            // Clean up old failures outside the sampling window
            CleanupOldFailures();

            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Test request failed, reopen the circuit
                    TransitionTo(CircuitBreakerState.Open);
                    return;
                }

                if (_state == CircuitBreakerState.Closed)
                {
                    if (ShouldTrip())
                    {
                        TransitionTo(CircuitBreakerState.Open);
                    }
                }
            }
        }

        /// <summary>
        /// Manually trips the circuit breaker to open state.
        /// </summary>
        public void Trip()
        {
            lock (_stateLock)
            {
                if (_state != CircuitBreakerState.Open)
                {
                    TransitionTo(CircuitBreakerState.Open);
                }
            }
        }

        /// <summary>
        /// Manually resets the circuit breaker to closed state.
        /// </summary>
        public void ResetState()
        {
            lock (_stateLock)
            {
                while (_recentFailures.TryDequeue(out _)) { }
                TransitionTo(CircuitBreakerState.Closed);
            }
        }

        private bool ShouldTrip()
        {
            // Check minimum throughput
            var recentCount = _recentFailures.Count;
            if (recentCount < _options.CircuitBreakerMinimumThroughput)
                return false;

            // Check failure count threshold
            if (recentCount >= _options.CircuitBreakerFailureThreshold)
                return true;

            // Check failure rate threshold if configured
            if (_options.CircuitBreakerFailureRateThreshold.HasValue)
            {
                var total = TotalSuccessCount + TotalFailureCount;
                if (total > 0)
                {
                    var failureRate = (double)recentCount / total;
                    if (failureRate >= _options.CircuitBreakerFailureRateThreshold.Value)
                        return true;
                }
            }

            return false;
        }

        private bool ShouldTransitionToHalfOpen()
        {
            if (!_openedAt.HasValue)
                return false;

            var elapsed = DateTimeOffset.UtcNow - _openedAt.Value;
            return elapsed >= TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds);
        }

        private void TransitionTo(CircuitBreakerState newState)
        {
            var previousState = _state;
            _state = newState;

            if (newState == CircuitBreakerState.Open)
            {
                _openedAt = DateTimeOffset.UtcNow;
                Interlocked.Increment(ref _circuitTripCount);
            }
            else if (newState == CircuitBreakerState.Closed)
            {
                _openedAt = null;
            }
        }

        private void CleanupOldFailures()
        {
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-_options.CircuitBreakerSamplingDurationSeconds);

            while (_recentFailures.TryPeek(out var oldest) && oldest < cutoff)
            {
                _recentFailures.TryDequeue(out _);
            }
        }
    }
}

