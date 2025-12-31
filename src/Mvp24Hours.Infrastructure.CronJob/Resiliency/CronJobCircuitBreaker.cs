//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mvp24Hours.Infrastructure.CronJob.Resiliency
{
    /// <summary>
    /// Manages circuit breaker state for CronJob executions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The circuit breaker pattern prevents repeated execution of a failing job,
    /// allowing the system to recover before attempting execution again.
    /// </para>
    /// <para>
    /// <strong>State Transitions:</strong>
    /// <list type="bullet">
    /// <item><b>Closed → Open:</b> After consecutive failures reach the threshold</item>
    /// <item><b>Open → Half-Open:</b> After the duration period expires</item>
    /// <item><b>Half-Open → Closed:</b> After successful test executions reach the success threshold</item>
    /// <item><b>Half-Open → Open:</b> If a test execution fails</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CronJobCircuitBreaker
    {
        private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="CronJobCircuitBreaker"/>.
        /// </summary>
        /// <param name="timeProvider">
        /// Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.
        /// </param>
        public CronJobCircuitBreaker(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        /// Gets the current state of the circuit breaker for a job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>The current <see cref="CircuitBreakerState"/>.</returns>
        public CircuitBreakerState GetState(string jobName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

            if (!_circuits.TryGetValue(jobName, out var circuit))
            {
                return CircuitBreakerState.Closed;
            }

            return GetEffectiveState(circuit);
        }

        /// <summary>
        /// Checks if the circuit allows execution for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="duration">Duration the circuit stays open.</param>
        /// <param name="samplingDuration">Time window for tracking failures.</param>
        /// <returns><c>true</c> if execution is allowed; otherwise, <c>false</c>.</returns>
        public bool AllowExecution(
            string jobName,
            int failureThreshold,
            TimeSpan duration,
            TimeSpan samplingDuration)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

            var circuit = _circuits.GetOrAdd(jobName, _ => new CircuitState(_timeProvider));
            var effectiveState = GetEffectiveState(circuit);

            switch (effectiveState)
            {
                case CircuitBreakerState.Closed:
                    // Clean old failures outside sampling window
                    circuit.CleanOldFailures(samplingDuration);
                    return true;

                case CircuitBreakerState.Open:
                    // Check if duration has passed to transition to half-open
                    if (ShouldTransitionToHalfOpen(circuit, duration))
                    {
                        TransitionToHalfOpen(circuit);
                        return true; // Allow test execution
                    }
                    return false;

                case CircuitBreakerState.HalfOpen:
                    // In half-open state, allow one test execution at a time
                    return circuit.TryAcquireTestExecution();

                default:
                    return true;
            }
        }

        /// <summary>
        /// Records a successful execution for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="successThreshold">Number of successes needed to close the circuit from half-open.</param>
        /// <param name="onStateChange">Callback when state changes.</param>
        public void RecordSuccess(
            string jobName,
            int successThreshold,
            Action<CircuitBreakerState, CircuitBreakerState>? onStateChange = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

            if (!_circuits.TryGetValue(jobName, out var circuit))
            {
                return;
            }

            var previousState = GetEffectiveState(circuit);

            switch (previousState)
            {
                case CircuitBreakerState.Closed:
                    // Reset failure count on success in closed state
                    circuit.ResetFailures();
                    break;

                case CircuitBreakerState.HalfOpen:
                    circuit.ReleaseTestExecution();
                    circuit.IncrementSuccessCount();

                    if (circuit.HalfOpenSuccessCount >= successThreshold)
                    {
                        TransitionToClosed(circuit);
                        var newState = CircuitBreakerState.Closed;
                        onStateChange?.Invoke(previousState, newState);
                    }
                    break;
            }
        }

        /// <summary>
        /// Records a failed execution for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="duration">Duration the circuit stays open.</param>
        /// <param name="onStateChange">Callback when state changes.</param>
        public void RecordFailure(
            string jobName,
            int failureThreshold,
            TimeSpan duration,
            Action<CircuitBreakerState, CircuitBreakerState>? onStateChange = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

            var circuit = _circuits.GetOrAdd(jobName, _ => new CircuitState(_timeProvider));
            var previousState = GetEffectiveState(circuit);

            switch (previousState)
            {
                case CircuitBreakerState.Closed:
                    circuit.RecordFailure();

                    if (circuit.FailureCount >= failureThreshold)
                    {
                        TransitionToOpen(circuit, duration);
                        var newState = CircuitBreakerState.Open;
                        onStateChange?.Invoke(previousState, newState);
                    }
                    break;

                case CircuitBreakerState.HalfOpen:
                    circuit.ReleaseTestExecution();
                    // Any failure in half-open immediately opens the circuit
                    TransitionToOpen(circuit, duration);
                    var openState = CircuitBreakerState.Open;
                    onStateChange?.Invoke(previousState, openState);
                    break;
            }
        }

        /// <summary>
        /// Resets the circuit breaker for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        public void Reset(string jobName)
        {
            _circuits.TryRemove(jobName, out _);
        }

        /// <summary>
        /// Gets metrics for the circuit breaker.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>The circuit breaker metrics, or null if no circuit exists.</returns>
        public CircuitBreakerMetrics? GetMetrics(string jobName)
        {
            if (!_circuits.TryGetValue(jobName, out var circuit))
            {
                return null;
            }

            return new CircuitBreakerMetrics
            {
                State = GetEffectiveState(circuit),
                FailureCount = circuit.FailureCount,
                HalfOpenSuccessCount = circuit.HalfOpenSuccessCount,
                LastFailureTime = circuit.LastFailureTime,
                OpenedAt = circuit.OpenedAt
            };
        }

        #region Private Methods

        private CircuitBreakerState GetEffectiveState(CircuitState circuit)
        {
            return circuit.State;
        }

        private bool ShouldTransitionToHalfOpen(CircuitState circuit, TimeSpan duration)
        {
            if (!circuit.OpenedAt.HasValue)
            {
                return false;
            }

            var elapsed = _timeProvider.GetUtcNow() - circuit.OpenedAt.Value;
            return elapsed >= duration;
        }

        private void TransitionToOpen(CircuitState circuit, TimeSpan duration)
        {
            circuit.State = CircuitBreakerState.Open;
            circuit.OpenedAt = _timeProvider.GetUtcNow();
            circuit.HalfOpenSuccessCount = 0;
        }

        private void TransitionToHalfOpen(CircuitState circuit)
        {
            circuit.State = CircuitBreakerState.HalfOpen;
            circuit.HalfOpenSuccessCount = 0;
        }

        private void TransitionToClosed(CircuitState circuit)
        {
            circuit.State = CircuitBreakerState.Closed;
            circuit.ResetFailures();
            circuit.OpenedAt = null;
            circuit.HalfOpenSuccessCount = 0;
        }

        #endregion

        #region Nested Types

        private sealed class CircuitState
        {
            private readonly TimeProvider _timeProvider;
            private readonly ConcurrentQueue<DateTimeOffset> _failures = new();
            private int _testExecutionInProgress;
            private int _halfOpenSuccessCount;

            public CircuitState(TimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
            }

            public CircuitBreakerState State { get; set; } = CircuitBreakerState.Closed;
            public int FailureCount => _failures.Count;
            public int HalfOpenSuccessCount 
            { 
                get => _halfOpenSuccessCount;
                set => _halfOpenSuccessCount = value;
            }
            public DateTimeOffset? LastFailureTime { get; private set; }
            public DateTimeOffset? OpenedAt { get; set; }

            public void RecordFailure()
            {
                var now = _timeProvider.GetUtcNow();
                _failures.Enqueue(now);
                LastFailureTime = now;
            }

            public void ResetFailures()
            {
                while (_failures.TryDequeue(out _)) { }
                LastFailureTime = null;
            }

            public void CleanOldFailures(TimeSpan samplingDuration)
            {
                var cutoff = _timeProvider.GetUtcNow() - samplingDuration;
                while (_failures.TryPeek(out var oldest) && oldest < cutoff)
                {
                    _failures.TryDequeue(out _);
                }
            }

            public void IncrementSuccessCount()
            {
                Interlocked.Increment(ref _halfOpenSuccessCount);
            }

            public bool TryAcquireTestExecution()
            {
                return Interlocked.CompareExchange(ref _testExecutionInProgress, 1, 0) == 0;
            }

            public void ReleaseTestExecution()
            {
                Interlocked.Exchange(ref _testExecutionInProgress, 0);
            }
        }

        #endregion
    }

    /// <summary>
    /// Metrics for a circuit breaker.
    /// </summary>
    public class CircuitBreakerMetrics
    {
        /// <summary>
        /// Gets or sets the current state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State { get; set; }

        /// <summary>
        /// Gets or sets the number of failures recorded.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the number of successful test executions in half-open state.
        /// </summary>
        public int HalfOpenSuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the time of the last failure.
        /// </summary>
        public DateTimeOffset? LastFailureTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the circuit was opened.
        /// </summary>
        public DateTimeOffset? OpenedAt { get; set; }
    }
}

