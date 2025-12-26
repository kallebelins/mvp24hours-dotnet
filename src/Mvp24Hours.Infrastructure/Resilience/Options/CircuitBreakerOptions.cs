//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Resilience.Options
{
    /// <summary>
    /// Configuration options for circuit breaker policies (generic, non-HTTP specific).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options are used for generic circuit breaker operations across different types
    /// of operations (database, messaging, file operations, etc.), not just HTTP.
    /// </para>
    /// </remarks>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Gets or sets the number of consecutive failures before opening the circuit. Default is 5.
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the sampling duration for failure counting. Default is 30 seconds.
        /// </summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the minimum throughput before the circuit breaker can activate. Default is 10.
        /// </summary>
        /// <remarks>
        /// The circuit breaker only activates if there have been at least this many
        /// operations within the sampling duration.
        /// </remarks>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Gets or sets the duration the circuit remains open. Default is 30 seconds.
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the failure ratio threshold (0.0 to 1.0). Default is 0.5.
        /// </summary>
        /// <remarks>
        /// The circuit opens when the ratio of failures to total operations exceeds this value.
        /// For example, 0.5 means 50% failure rate triggers the circuit breaker.
        /// </remarks>
        public double FailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should be counted as a failure.
        /// If null, all exceptions are counted as failures.
        /// </summary>
        public Func<Exception, bool>? ShouldCountAsFailure { get; set; }

        /// <summary>
        /// Callback invoked when the circuit breaker opens.
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnBreak { get; set; }

        /// <summary>
        /// Callback invoked when the circuit breaker closes (resets).
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnReset { get; set; }

        /// <summary>
        /// Callback invoked when the circuit breaker enters half-open state.
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnHalfOpen { get; set; }
    }

    /// <summary>
    /// Information about a circuit breaker state change.
    /// </summary>
    public class CircuitBreakerStateChangeInfo
    {
        /// <summary>
        /// Gets or sets the operation name or identifier.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new circuit state.
        /// </summary>
        public CircuitBreakerState NewState { get; set; }

        /// <summary>
        /// Gets or sets the break duration (only applicable when opening).
        /// </summary>
        public TimeSpan? BreakDuration { get; set; }

        /// <summary>
        /// Gets or sets the reason for the state change.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the state change.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is closed - normal operation, requests pass through.
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open - requests fail immediately without attempting execution.
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open - allowing limited requests to test if service recovered.
        /// </summary>
        HalfOpen
    }
}

