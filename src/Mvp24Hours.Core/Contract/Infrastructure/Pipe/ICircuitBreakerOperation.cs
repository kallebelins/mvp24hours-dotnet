//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines the current state of a circuit breaker.
    /// </summary>
    public enum PipelineCircuitState
    {
        /// <summary>
        /// Circuit is closed - operations execute normally.
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open - operations fail fast without execution.
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open - limited operations are allowed to test recovery.
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Defines circuit breaker behavior for an operation.
    /// Operations implementing this interface will have circuit breaker protection.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyProtectedOperation : OperationBaseAsync, ICircuitBreakerOperation
    /// {
    ///     public string CircuitBreakerKey => "external-api";
    ///     public int FailureThreshold => 5;
    ///     public TimeSpan OpenDuration => TimeSpan.FromMinutes(1);
    /// 
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         // Operation logic - protected by circuit breaker
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ICircuitBreakerOperation
    {
        /// <summary>
        /// Gets the unique key identifying this circuit breaker.
        /// Operations with the same key share the same circuit breaker state.
        /// Default implementation returns the type name.
        /// </summary>
        string CircuitBreakerKey => GetType().FullName ?? GetType().Name;

        /// <summary>
        /// Gets the number of failures that will trip the circuit to open.
        /// Default implementation returns 5.
        /// </summary>
        int FailureThreshold => 5;

        /// <summary>
        /// Gets the duration the circuit stays open before transitioning to half-open.
        /// Default implementation returns 30 seconds.
        /// </summary>
        TimeSpan OpenDuration => TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the number of successful operations required to close a half-open circuit.
        /// Default implementation returns 2.
        /// </summary>
        int SuccessThreshold => 2;

        /// <summary>
        /// Gets the types of exceptions that should count as failures.
        /// Null or empty means all exceptions count as failures.
        /// Default implementation returns null (all exceptions).
        /// </summary>
        Type[]? BreakOnExceptions => null;

        /// <summary>
        /// Determines whether a specific exception should count as a failure.
        /// Override this for custom failure detection.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>True if the exception should count as a failure, false otherwise.</returns>
        bool ShouldCountAsFailure(Exception exception)
        {
            if (BreakOnExceptions == null || BreakOnExceptions.Length == 0)
                return true;

            foreach (var type in BreakOnExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the circuit state changes.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="oldState">The previous state.</param>
        /// <param name="newState">The new state.</param>
        void OnStateChange(PipelineCircuitState oldState, PipelineCircuitState newState) { }
    }

    /// <summary>
    /// Configuration options for circuit breaker behavior.
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Gets or sets the unique key identifying this circuit breaker.
        /// </summary>
        public string Key { get; set; } = "default";

        /// <summary>
        /// Gets or sets the number of failures that will trip the circuit to open.
        /// Default: 5.
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration the circuit stays open before transitioning to half-open.
        /// Default: 30 seconds.
        /// </summary>
        public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the number of successful operations required to close a half-open circuit.
        /// Default: 2.
        /// </summary>
        public int SuccessThreshold { get; set; } = 2;

        /// <summary>
        /// Gets or sets the sliding window duration for counting failures.
        /// Failures older than this are not counted.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the types of exceptions that should count as failures.
        /// Null or empty means all exceptions count as failures.
        /// Default: null (all exceptions).
        /// </summary>
        public Type[]? BreakOnExceptions { get; set; }

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should count as failure.
        /// When set, this takes precedence over BreakOnExceptions.
        /// Default: null.
        /// </summary>
        public Func<Exception, bool>? ShouldCountAsFailurePredicate { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit state changes.
        /// Default: null.
        /// </summary>
        public Action<PipelineCircuitState, PipelineCircuitState>? OnStateChange { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit breaker rejects an operation.
        /// Default: null.
        /// </summary>
        public Action? OnRejected { get; set; }

        /// <summary>
        /// Creates default circuit breaker options.
        /// </summary>
        public static CircuitBreakerOptions Default => new();

        /// <summary>
        /// Creates sensitive circuit breaker options that trip quickly.
        /// </summary>
        public static CircuitBreakerOptions Sensitive => new()
        {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromSeconds(60),
            SuccessThreshold = 1,
            SamplingDuration = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Creates tolerant circuit breaker options that allow more failures.
        /// </summary>
        public static CircuitBreakerOptions Tolerant => new()
        {
            FailureThreshold = 10,
            OpenDuration = TimeSpan.FromSeconds(15),
            SuccessThreshold = 3,
            SamplingDuration = TimeSpan.FromMinutes(2)
        };

        /// <summary>
        /// Determines whether a specific exception should count as a failure.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>True if the exception should count as a failure, false otherwise.</returns>
        public bool ShouldCountAsFailure(Exception exception)
        {
            if (ShouldCountAsFailurePredicate != null)
                return ShouldCountAsFailurePredicate(exception);

            if (BreakOnExceptions == null || BreakOnExceptions.Length == 0)
                return true;

            foreach (var type in BreakOnExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Exception thrown when a circuit breaker is open and rejects an operation.
    /// </summary>
    public class PipelineCircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Gets the circuit breaker key that rejected the operation.
        /// </summary>
        public string CircuitBreakerKey { get; }

        /// <summary>
        /// Gets the time when the circuit will transition to half-open.
        /// </summary>
        public DateTimeOffset RetryAfter { get; }

        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="circuitBreakerKey">The circuit breaker key.</param>
        /// <param name="retryAfter">When the circuit will allow retries.</param>
        public PipelineCircuitBreakerOpenException(string circuitBreakerKey, DateTimeOffset retryAfter)
            : base($"Circuit breaker '{circuitBreakerKey}' is open. Retry after {retryAfter:O}.")
        {
            CircuitBreakerKey = circuitBreakerKey;
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="circuitBreakerKey">The circuit breaker key.</param>
        /// <param name="retryAfter">When the circuit will allow retries.</param>
        /// <param name="innerException">The inner exception.</param>
        public PipelineCircuitBreakerOpenException(string circuitBreakerKey, DateTimeOffset retryAfter, Exception innerException)
            : base($"Circuit breaker '{circuitBreakerKey}' is open. Retry after {retryAfter:O}.", innerException)
        {
            CircuitBreakerKey = circuitBreakerKey;
            RetryAfter = retryAfter;
        }
    }
}

