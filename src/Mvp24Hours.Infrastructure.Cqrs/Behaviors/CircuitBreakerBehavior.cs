//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that should use circuit breaker protection.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will have circuit breaker protection applied.
/// When too many failures occur, the circuit will "open" and reject requests immediately.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ExternalApiCommand : IMediatorCommand&lt;string&gt;, ICircuitBreakerProtected
/// {
///     public string CircuitBreakerKey => "external-api";
///     public int FailureThreshold => 5;
///     public int SamplingDurationSeconds => 30;
///     public int MinimumThroughput => 10;
///     public int DurationOfBreakSeconds => 60;
/// }
/// </code>
/// </example>
public interface ICircuitBreakerProtected
{
    /// <summary>
    /// Gets the unique key for this circuit breaker.
    /// Requests with the same key share the same circuit state.
    /// If null, the request type name is used.
    /// </summary>
    string? CircuitBreakerKey => null;

    /// <summary>
    /// Gets the number of failures before the circuit opens.
    /// Default is 5.
    /// </summary>
    int FailureThreshold => 5;

    /// <summary>
    /// Gets the duration in seconds for the sampling window.
    /// Failures are counted within this window.
    /// Default is 30 seconds.
    /// </summary>
    int SamplingDurationSeconds => 30;

    /// <summary>
    /// Gets the minimum number of requests before the circuit can trip.
    /// Default is 10.
    /// </summary>
    int MinimumThroughput => 10;

    /// <summary>
    /// Gets the duration in seconds the circuit stays open before allowing a test request.
    /// Default is 60 seconds.
    /// </summary>
    int DurationOfBreakSeconds => 60;
}

/// <summary>
/// Circuit breaker policy configuration.
/// </summary>
public interface ICircuitBreakerPolicy
{
    /// <summary>
    /// Gets the number of consecutive failures before the circuit opens.
    /// </summary>
    int FailureThreshold { get; }

    /// <summary>
    /// Gets the duration of the sampling window in seconds.
    /// </summary>
    int SamplingDurationSeconds { get; }

    /// <summary>
    /// Gets the minimum number of requests before the circuit can trip.
    /// </summary>
    int MinimumThroughput { get; }

    /// <summary>
    /// Gets the duration the circuit stays open in seconds.
    /// </summary>
    int DurationOfBreakSeconds { get; }

    /// <summary>
    /// Gets the exceptions that should be counted as failures.
    /// If empty, all exceptions are counted.
    /// </summary>
    IEnumerable<Type> FailureExceptions => Enumerable.Empty<Type>();
}

/// <summary>
/// Default implementation of <see cref="ICircuitBreakerPolicy"/>.
/// </summary>
public sealed record CircuitBreakerPolicy : ICircuitBreakerPolicy
{
    /// <summary>
    /// Creates a new circuit breaker policy.
    /// </summary>
    public CircuitBreakerPolicy(
        int failureThreshold = 5,
        int samplingDurationSeconds = 30,
        int minimumThroughput = 10,
        int durationOfBreakSeconds = 60,
        IEnumerable<Type>? failureExceptions = null)
    {
        FailureThreshold = failureThreshold;
        SamplingDurationSeconds = samplingDurationSeconds;
        MinimumThroughput = minimumThroughput;
        DurationOfBreakSeconds = durationOfBreakSeconds;
        FailureExceptions = failureExceptions ?? Enumerable.Empty<Type>();
    }

    /// <inheritdoc />
    public int FailureThreshold { get; }

    /// <inheritdoc />
    public int SamplingDurationSeconds { get; }

    /// <inheritdoc />
    public int MinimumThroughput { get; }

    /// <inheritdoc />
    public int DurationOfBreakSeconds { get; }

    /// <inheritdoc />
    public IEnumerable<Type> FailureExceptions { get; }

    /// <summary>
    /// Creates a relaxed policy (higher thresholds, longer break).
    /// </summary>
    public static CircuitBreakerPolicy Relaxed => new(
        failureThreshold: 10,
        samplingDurationSeconds: 60,
        minimumThroughput: 20,
        durationOfBreakSeconds: 30);

    /// <summary>
    /// Creates an aggressive policy (lower thresholds, shorter break).
    /// </summary>
    public static CircuitBreakerPolicy Aggressive => new(
        failureThreshold: 3,
        samplingDurationSeconds: 15,
        minimumThroughput: 5,
        durationOfBreakSeconds: 120);
}

/// <summary>
/// The current state of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed - requests are allowed.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - requests are rejected.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - one test request is allowed.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Metrics for a circuit breaker.
/// </summary>
public sealed class CircuitBreakerMetrics
{
    /// <summary>
    /// Gets the circuit breaker key.
    /// </summary>
    public string Key { get; init; } = default!;

    /// <summary>
    /// Gets the current state.
    /// </summary>
    public CircuitState State { get; init; }

    /// <summary>
    /// Gets the number of failures in the current window.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the number of successful requests in the current window.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the total number of requests in the current window.
    /// </summary>
    public int TotalRequests => FailureCount + SuccessCount;

    /// <summary>
    /// Gets when the circuit was last opened.
    /// </summary>
    public DateTimeOffset? LastOpenedAt { get; init; }

    /// <summary>
    /// Gets when the circuit will transition to half-open.
    /// </summary>
    public DateTimeOffset? TransitionToHalfOpenAt { get; init; }

    /// <summary>
    /// Gets the failure rate as a percentage.
    /// </summary>
    public double FailureRate => TotalRequests > 0 ? (double)FailureCount / TotalRequests * 100 : 0;
}

/// <summary>
/// Internal circuit breaker state tracker.
/// </summary>
internal sealed class CircuitBreakerState
{
    private readonly object _lock = new();
    private CircuitState _state = CircuitState.Closed;
    private readonly Queue<(DateTimeOffset Timestamp, bool IsSuccess)> _requests = new();
    private DateTimeOffset? _lastOpenedAt;
    private readonly int _failureThreshold;
    private readonly TimeSpan _samplingDuration;
    private readonly int _minimumThroughput;
    private readonly TimeSpan _durationOfBreak;
    private readonly HashSet<Type> _failureExceptions;

    public CircuitBreakerState(ICircuitBreakerPolicy policy)
    {
        _failureThreshold = policy.FailureThreshold;
        _samplingDuration = TimeSpan.FromSeconds(policy.SamplingDurationSeconds);
        _minimumThroughput = policy.MinimumThroughput;
        _durationOfBreak = TimeSpan.FromSeconds(policy.DurationOfBreakSeconds);
        _failureExceptions = new HashSet<Type>(policy.FailureExceptions);
    }

    public CircuitBreakerState(ICircuitBreakerProtected request)
    {
        _failureThreshold = request.FailureThreshold;
        _samplingDuration = TimeSpan.FromSeconds(request.SamplingDurationSeconds);
        _minimumThroughput = request.MinimumThroughput;
        _durationOfBreak = TimeSpan.FromSeconds(request.DurationOfBreakSeconds);
        _failureExceptions = new HashSet<Type>();
    }

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && _lastOpenedAt.HasValue)
                {
                    if (DateTimeOffset.UtcNow >= _lastOpenedAt.Value + _durationOfBreak)
                    {
                        _state = CircuitState.HalfOpen;
                    }
                }
                return _state;
            }
        }
    }

    public bool ShouldAllowRequest()
    {
        lock (_lock)
        {
            var currentState = State;
            return currentState != CircuitState.Open;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            _requests.Enqueue((DateTimeOffset.UtcNow, true));

            if (_state == CircuitState.HalfOpen)
            {
                // Successful test request - close the circuit
                _state = CircuitState.Closed;
                _lastOpenedAt = null;
            }
        }
    }

    public void RecordFailure(Exception exception)
    {
        lock (_lock)
        {
            // Check if this exception type should be counted
            if (_failureExceptions.Count > 0 && !_failureExceptions.Contains(exception.GetType()))
            {
                // Not a failure exception, count as success
                RecordSuccess();
                return;
            }

            CleanupOldRequests();
            _requests.Enqueue((DateTimeOffset.UtcNow, false));

            if (_state == CircuitState.HalfOpen)
            {
                // Failed test request - reopen the circuit
                OpenCircuit();
                return;
            }

            // Check if we should trip the circuit
            var failures = _requests.Count(r => !r.IsSuccess);
            var total = _requests.Count;

            if (total >= _minimumThroughput && failures >= _failureThreshold)
            {
                OpenCircuit();
            }
        }
    }

    public CircuitBreakerMetrics GetMetrics(string key)
    {
        lock (_lock)
        {
            CleanupOldRequests();
            
            return new CircuitBreakerMetrics
            {
                Key = key,
                State = State,
                FailureCount = _requests.Count(r => !r.IsSuccess),
                SuccessCount = _requests.Count(r => r.IsSuccess),
                LastOpenedAt = _lastOpenedAt,
                TransitionToHalfOpenAt = _lastOpenedAt.HasValue 
                    ? _lastOpenedAt.Value + _durationOfBreak 
                    : null
            };
        }
    }

    private void OpenCircuit()
    {
        _state = CircuitState.Open;
        _lastOpenedAt = DateTimeOffset.UtcNow;
    }

    private void CleanupOldRequests()
    {
        var cutoff = DateTimeOffset.UtcNow - _samplingDuration;
        while (_requests.Count > 0 && _requests.Peek().Timestamp < cutoff)
        {
            _requests.Dequeue();
        }
    }
}

/// <summary>
/// Pipeline behavior that provides circuit breaker protection for requests.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior implements the circuit breaker pattern to prevent cascading failures.
/// When a circuit is open, requests are rejected immediately without executing.
/// </para>
/// <para>
/// <strong>Circuit States:</strong>
/// <code>
/// ┌──────────────────────────────────────────────────────────────────────────────┐
/// │                                                                              │
/// │  ┌─────────┐    Failures >= Threshold    ┌─────────┐                       │
/// │  │ CLOSED  │ ─────────────────────────▶  │  OPEN   │                       │
/// │  │(Allow)  │                             │(Reject) │                       │
/// │  └────┬────┘                             └────┬────┘                       │
/// │       │                                       │                            │
/// │       │ Success                    After DurationOfBreak                   │
/// │       ▲                                       ▼                            │
/// │       │                             ┌──────────────┐                       │
/// │       └──────────── Success ─────── │  HALF-OPEN   │                       │
/// │                                     │(Test 1 req)  │                       │
/// │                     Failure ─────▶  └──────────────┘                       │
/// │                                             │                              │
/// │                                             ▼                              │
/// │                                       Back to OPEN                         │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddMvpMediator(options =>
/// {
///     options.RegisterCircuitBreakerBehavior = true;
/// });
/// 
/// // Mark request as circuit breaker protected
/// public class ExternalApiCommand : IMediatorCommand&lt;string&gt;, ICircuitBreakerProtected
/// {
///     public string? CircuitBreakerKey => "external-api-v1";
///     public int FailureThreshold => 3;
///     public int DurationOfBreakSeconds => 30;
/// }
/// </code>
/// </example>
public sealed class CircuitBreakerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private static readonly ConcurrentDictionary<string, CircuitBreakerState> _circuits = new();
    
    private readonly ILogger<CircuitBreakerBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the CircuitBreakerBehavior.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public CircuitBreakerBehavior(
        ILogger<CircuitBreakerBehavior<TRequest, TResponse>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply circuit breaker to protected requests
        if (request is not ICircuitBreakerProtected protectedRequest)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var circuitKey = protectedRequest.CircuitBreakerKey ?? requestName;
        var circuitState = _circuits.GetOrAdd(circuitKey, _ => new CircuitBreakerState(protectedRequest));

        // Check if circuit allows the request
        if (!circuitState.ShouldAllowRequest())
        {
            var metrics = circuitState.GetMetrics(circuitKey);
            
            _logger?.LogWarning(
                "[CircuitBreaker] Circuit '{CircuitKey}' is open. Request {RequestName} rejected. " +
                "Failures: {FailureCount}, Will retry at: {RetryAt}",
                circuitKey,
                requestName,
                metrics.FailureCount,
                metrics.TransitionToHalfOpenAt);

            throw new CircuitBreakerOpenException(circuitKey, requestName, metrics);
        }

        try
        {
            var response = await next();
            circuitState.RecordSuccess();
            
            _logger?.LogDebug(
                "[CircuitBreaker] Request {RequestName} succeeded. Circuit '{CircuitKey}' state: {State}",
                requestName,
                circuitKey,
                circuitState.State);
            
            return response;
        }
        catch (Exception ex)
        {
            circuitState.RecordFailure(ex);
            
            var metrics = circuitState.GetMetrics(circuitKey);
            _logger?.LogWarning(ex,
                "[CircuitBreaker] Request {RequestName} failed. Circuit '{CircuitKey}' state: {State}, " +
                "Failures: {FailureCount}/{FailureThreshold}",
                requestName,
                circuitKey,
                circuitState.State,
                metrics.FailureCount,
                protectedRequest.FailureThreshold);
            
            throw;
        }
    }

    /// <summary>
    /// Gets the current metrics for a circuit breaker.
    /// </summary>
    /// <param name="circuitKey">The circuit breaker key.</param>
    /// <returns>The metrics, or null if no circuit exists for the key.</returns>
    public static CircuitBreakerMetrics? GetMetrics(string circuitKey)
    {
        return _circuits.TryGetValue(circuitKey, out var state) 
            ? state.GetMetrics(circuitKey) 
            : null;
    }

    /// <summary>
    /// Gets all circuit breaker metrics.
    /// </summary>
    /// <returns>All circuit metrics.</returns>
    public static IEnumerable<CircuitBreakerMetrics> GetAllMetrics()
    {
        return _circuits.Select(kvp => kvp.Value.GetMetrics(kvp.Key));
    }

    /// <summary>
    /// Resets a circuit breaker to closed state.
    /// </summary>
    /// <param name="circuitKey">The circuit breaker key.</param>
    /// <returns>True if the circuit was reset, false if not found.</returns>
    public static bool ResetCircuit(string circuitKey)
    {
        return _circuits.TryRemove(circuitKey, out _);
    }

    /// <summary>
    /// Resets all circuit breakers.
    /// </summary>
    public static void ResetAllCircuits()
    {
        _circuits.Clear();
    }
}

/// <summary>
/// Exception thrown when a circuit breaker is open and rejecting requests.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Gets the circuit breaker key.
    /// </summary>
    public string CircuitKey { get; }

    /// <summary>
    /// Gets the name of the request that was rejected.
    /// </summary>
    public string RequestName { get; }

    /// <summary>
    /// Gets the circuit breaker metrics at the time of rejection.
    /// </summary>
    public CircuitBreakerMetrics Metrics { get; }

    /// <summary>
    /// Creates a new instance of CircuitBreakerOpenException.
    /// </summary>
    /// <param name="circuitKey">The circuit key.</param>
    /// <param name="requestName">The request name.</param>
    /// <param name="metrics">The current metrics.</param>
    public CircuitBreakerOpenException(string circuitKey, string requestName, CircuitBreakerMetrics metrics)
        : base($"Circuit breaker '{circuitKey}' is open. Request '{requestName}' was rejected.")
    {
        CircuitKey = circuitKey;
        RequestName = requestName;
        Metrics = metrics;
    }
}

