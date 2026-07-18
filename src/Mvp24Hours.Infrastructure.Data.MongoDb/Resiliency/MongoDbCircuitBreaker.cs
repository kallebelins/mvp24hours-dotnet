//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;

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
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDbCircuitBreaker"/> class.
/// </remarks>
/// <param name="options">The resiliency options.</param>
public sealed class MongoDbCircuitBreaker(MongoDbResiliencyOptions options) : ICircuitBreakerMetrics
{
    private readonly MongoDbResiliencyOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly object _stateLock = new();
    private readonly ConcurrentQueue<DateTimeOffset> _recentFailures = new();

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private long _totalSuccessCount;
    private long _totalFailureCount;
    private long _totalRejectedCount;
    private long _circuitTripCount;

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
            long total = TotalSuccessCount + TotalFailureCount;
            if (total == 0)
            {
                return 0;
            }

            return (double)TotalFailureCount / total;
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? LastSuccessTime { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? LastFailureTime { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? LastOpenTime { get; private set; }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_stateLock)
        {
            Interlocked.Exchange(ref _totalSuccessCount, 0);
            Interlocked.Exchange(ref _totalFailureCount, 0);
            Interlocked.Exchange(ref _totalRejectedCount, 0);
            Interlocked.Exchange(ref _circuitTripCount, 0);
            LastSuccessTime = null;
            LastFailureTime = null;
            LastOpenTime = null;
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
        CircuitBreakerState state = State; // This may trigger state transition

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
            if (_state != CircuitBreakerState.Open || !LastOpenTime.HasValue)
            {
                return null;
            }

            TimeSpan elapsed = DateTimeOffset.UtcNow - LastOpenTime.Value;
            var breakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds);
            TimeSpan remaining = breakDuration - elapsed;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    public void RecordSuccess()
    {
        Interlocked.Increment(ref _totalSuccessCount);
        LastSuccessTime = DateTimeOffset.UtcNow;

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
        LastFailureTime = DateTimeOffset.UtcNow;
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
        int recentCount = _recentFailures.Count;
        if (recentCount < _options.CircuitBreakerMinimumThroughput)
        {
            return false;
        }

        // Check failure count threshold
        if (recentCount >= _options.CircuitBreakerFailureThreshold)
        {
            return true;
        }

        // Check failure rate threshold if configured
        if (_options.CircuitBreakerFailureRateThreshold.HasValue)
        {
            long total = TotalSuccessCount + TotalFailureCount;
            if (total > 0)
            {
                double failureRate = (double)recentCount / total;
                if (failureRate >= _options.CircuitBreakerFailureRateThreshold.Value)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ShouldTransitionToHalfOpen()
    {
        if (!LastOpenTime.HasValue)
        {
            return false;
        }

        TimeSpan elapsed = DateTimeOffset.UtcNow - LastOpenTime.Value;
        return elapsed >= TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds);
    }

    private void TransitionTo(CircuitBreakerState newState)
    {
        _state = newState;

        if (newState == CircuitBreakerState.Open)
        {
            LastOpenTime = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref _circuitTripCount);
        }
        else if (newState == CircuitBreakerState.Closed)
        {
            LastOpenTime = null;
        }
    }

    private void CleanupOldFailures()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddSeconds(-_options.CircuitBreakerSamplingDurationSeconds);

        while (_recentFailures.TryPeek(out DateTimeOffset oldest) && oldest < cutoff)
        {
            _recentFailures.TryDequeue(out _);
        }
    }
}

