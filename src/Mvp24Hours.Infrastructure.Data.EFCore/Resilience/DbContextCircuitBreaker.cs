//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Resilience;

/// <summary>
/// Circuit breaker implementation for database connections.
/// Prevents cascade failures by temporarily stopping requests when too many failures occur.
/// </summary>
/// <remarks>
/// <para>
/// Circuit breaker states:
/// <list type="bullet">
/// <item><strong>Closed</strong> - Normal operation, requests flow through</item>
/// <item><strong>Open</strong> - Too many failures, requests fail immediately without attempting database connection</item>
/// <item><strong>HalfOpen</strong> - Testing if database has recovered, limited requests allowed</item>
/// </list>
/// </para>
/// <para>
/// The circuit breaker helps:
/// <list type="bullet">
/// <item>Prevent cascade failures in distributed systems</item>
/// <item>Give failing services time to recover</item>
/// <item>Fail fast when database is unavailable</item>
/// <item>Preserve system resources during outages</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var circuitBreaker = new DbContextCircuitBreaker(resilienceOptions, logger);
/// 
/// // Before each database operation
/// circuitBreaker.EnsureCircuitClosed();
/// 
/// try
/// {
///     // Perform database operation
///     await dbContext.SaveChangesAsync();
///     circuitBreaker.RecordSuccess();
/// }
/// catch (Exception ex)
/// {
///     circuitBreaker.RecordFailure();
///     throw;
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of <see cref="DbContextCircuitBreaker"/>.
/// </remarks>
/// <param name="options">The resilience configuration options.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
public class DbContextCircuitBreaker(EFCoreResilienceOptions options, ILogger? logger = null)
{
    private readonly EFCoreResilienceOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger? _logger = logger;
    private readonly object _syncLock = new();

    private CircuitState _state = CircuitState.Closed;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _circuitOpenedTime = DateTime.MinValue;
    private int _successCount;
    private int _failureCount;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_syncLock)
            {
                UpdateCircuitState();
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// Gets the total success count since last reset.
    /// </summary>
    public int TotalSuccessCount => _successCount;

    /// <summary>
    /// Gets the total failure count since last reset.
    /// </summary>
    public int TotalFailureCount => _failureCount;

    /// <summary>
    /// Gets whether the circuit is currently allowing requests.
    /// </summary>
    public bool IsAllowingRequests => State != CircuitState.Open;

    /// <summary>
    /// Ensures the circuit is not open. Throws if the circuit is open.
    /// </summary>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
    public void EnsureCircuitClosed()
    {
        if (!_options.EnableCircuitBreaker)
        {
            return;
        }

        CircuitState state = State;
        if (state == CircuitState.Open)
        {
            TimeSpan remainingTime = GetRemainingOpenTime();
            throw new CircuitBreakerOpenException(
                $"Circuit breaker is open. Database operations are temporarily blocked. " +
                $"Circuit will test again in {remainingTime.TotalSeconds:F1} seconds.",
                remainingTime);
        }
    }

    /// <summary>
    /// Records a successful database operation.
    /// </summary>
    public void RecordSuccess()
    {
        if (!_options.EnableCircuitBreaker)
        {
            return;
        }

        lock (_syncLock)
        {
            Interlocked.Increment(ref _successCount);
            ConsecutiveFailures = 0;

            if (_state == CircuitState.HalfOpen)
            {
                // Success in half-open state closes the circuit
                TransitionToState(CircuitState.Closed, "Database operation succeeded in half-open state");
            }
        }
    }

    /// <summary>
    /// Records a failed database operation.
    /// </summary>
    public void RecordFailure()
    {
        if (!_options.EnableCircuitBreaker)
        {
            return;
        }

        lock (_syncLock)
        {
            Interlocked.Increment(ref _failureCount);
            ConsecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                // Failure in half-open state reopens the circuit
                TransitionToState(CircuitState.Open, "Database operation failed in half-open state");
                _circuitOpenedTime = DateTime.UtcNow;
            }
            else if (_state == CircuitState.Closed &&
                     ConsecutiveFailures >= _options.CircuitBreakerFailureThreshold)
            {
                // Too many consecutive failures, open the circuit
                TransitionToState(CircuitState.Open,
                    $"Consecutive failures ({ConsecutiveFailures}) exceeded threshold ({_options.CircuitBreakerFailureThreshold})");
                _circuitOpenedTime = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_syncLock)
        {
            ConsecutiveFailures = 0;
            _successCount = 0;
            _failureCount = 0;
            TransitionToState(CircuitState.Closed, "Manual reset");
        }
    }

    /// <summary>
    /// Gets the remaining time the circuit will stay open.
    /// </summary>
    public TimeSpan GetRemainingOpenTime()
    {
        if (_state != CircuitState.Open)
        {
            return TimeSpan.Zero;
        }

        TimeSpan elapsed = DateTime.UtcNow - _circuitOpenedTime;
        var duration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds);
        TimeSpan remaining = duration - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    #region Private Methods

    private void UpdateCircuitState()
    {
        if (_state == CircuitState.Open)
        {
            TimeSpan elapsed = DateTime.UtcNow - _circuitOpenedTime;
            if (elapsed >= TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds))
            {
                // Time to test if database has recovered
                TransitionToState(CircuitState.HalfOpen, "Circuit duration expired, testing recovery");
            }
        }
    }

    private void TransitionToState(CircuitState newState, string reason)
    {
        if (_state == newState)
        {
            return;
        }

        CircuitState oldState = _state;
        _state = newState;

        _logger?.Log(
            newState == CircuitState.Open ? LogLevel.Warning : LogLevel.Information,
            "Database circuit breaker: {OldState} -> {NewState}. Reason: {Reason}",
            oldState, newState, reason);
    }

    #endregion
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed, requests flow normally.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, requests fail immediately.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is testing if the service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when the circuit breaker is open.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="CircuitBreakerOpenException"/>.
/// </remarks>
/// <param name="message">The exception message.</param>
/// <param name="retryAfter">The remaining time until recovery test.</param>
public class CircuitBreakerOpenException(string message, TimeSpan retryAfter) : Exception(message)
{
    /// <summary>
    /// Gets the remaining time until the circuit breaker will test recovery.
    /// </summary>
    public TimeSpan RetryAfter { get; } = retryAfter;
}

