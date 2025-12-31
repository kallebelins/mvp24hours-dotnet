//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Resiliency
{
    /// <summary>
    /// Configuration interface for CronJob resilience policies.
    /// Defines retry, circuit breaker, overlapping execution, and graceful shutdown settings.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides comprehensive resilience configuration for CronJob services,
    /// including retry policies, circuit breaker patterns, and execution control.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddResilientCronJob&lt;MyJobService&gt;(config =>
    /// {
    ///     config.CronExpression = "*/5 * * * *";
    ///     config.Resilience.EnableRetry = true;
    ///     config.Resilience.MaxRetryAttempts = 3;
    ///     config.Resilience.EnableCircuitBreaker = true;
    ///     config.Resilience.PreventOverlapping = true;
    /// });
    /// </code>
    /// </example>
    public interface ICronJobResilienceConfig<T>
    {
        #region Retry Configuration

        /// <summary>
        /// Gets or sets whether retry policy is enabled for DoWork execution.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        bool EnableRetry { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        /// <value>Default is <c>3</c>.</value>
        int MaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the initial delay between retry attempts.
        /// </summary>
        /// <value>Default is <c>1 second</c>.</value>
        TimeSpan RetryDelay { get; set; }

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retry delays.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        bool UseExponentialBackoff { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay between retry attempts when using exponential backoff.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        TimeSpan MaxRetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the jitter factor for retry delays (0.0 to 1.0).
        /// Adds randomness to prevent thundering herd problems.
        /// </summary>
        /// <value>Default is <c>0.2</c> (20% jitter).</value>
        double RetryJitterFactor { get; set; }

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should trigger a retry.
        /// </summary>
        /// <value>Default is <c>null</c> (all exceptions trigger retry).</value>
        Func<Exception, bool>? ShouldRetryOnException { get; set; }

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Gets or sets whether circuit breaker pattern is enabled.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        bool EnableCircuitBreaker { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive failures before the circuit opens.
        /// </summary>
        /// <value>Default is <c>5</c>.</value>
        int CircuitBreakerFailureThreshold { get; set; }

        /// <summary>
        /// Gets or sets the duration the circuit stays open before attempting recovery.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        TimeSpan CircuitBreakerDuration { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions required to close the circuit from half-open state.
        /// </summary>
        /// <value>Default is <c>1</c>.</value>
        int CircuitBreakerSuccessThreshold { get; set; }

        /// <summary>
        /// Gets or sets the sampling window for tracking failures.
        /// </summary>
        /// <value>Default is <c>60 seconds</c>.</value>
        TimeSpan CircuitBreakerSamplingDuration { get; set; }

        #endregion

        #region Overlapping Execution Control

        /// <summary>
        /// Gets or sets whether to prevent overlapping executions.
        /// When enabled, a new execution will be skipped if the previous one is still running.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        bool PreventOverlapping { get; set; }

        /// <summary>
        /// Gets or sets whether to log a warning when an execution is skipped due to overlapping.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        bool LogOverlappingSkipped { get; set; }

        /// <summary>
        /// Gets or sets the maximum time to wait for acquiring the execution lock.
        /// </summary>
        /// <value>Default is <c>TimeSpan.Zero</c> (no wait, skip immediately).</value>
        TimeSpan OverlappingWaitTimeout { get; set; }

        #endregion

        #region Graceful Shutdown Configuration

        /// <summary>
        /// Gets or sets the timeout for graceful shutdown.
        /// The job will be forcefully cancelled after this timeout.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        TimeSpan GracefulShutdownTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to wait for the current execution to complete during shutdown.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        bool WaitForExecutionOnShutdown { get; set; }

        #endregion

        #region Cancellation Token Configuration

        /// <summary>
        /// Gets or sets whether to propagate cancellation token correctly to all nested operations.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        bool PropagateCancellation { get; set; }

        /// <summary>
        /// Gets or sets the timeout for individual job execution.
        /// If the job takes longer than this, it will be cancelled.
        /// </summary>
        /// <value>Default is <c>null</c> (no timeout).</value>
        TimeSpan? ExecutionTimeout { get; set; }

        #endregion

        #region Callback Hooks

        /// <summary>
        /// Gets or sets a callback to be invoked when a retry is attempted.
        /// </summary>
        /// <value>Parameters: exception, attempt number, delay.</value>
        Action<Exception, int, TimeSpan>? OnRetry { get; set; }

        /// <summary>
        /// Gets or sets a callback to be invoked when the circuit breaker state changes.
        /// </summary>
        /// <value>Parameters: old state, new state.</value>
        Action<CircuitBreakerState, CircuitBreakerState>? OnCircuitBreakerStateChange { get; set; }

        /// <summary>
        /// Gets or sets a callback to be invoked when an execution is skipped due to overlapping.
        /// </summary>
        Action? OnOverlappingSkipped { get; set; }

        /// <summary>
        /// Gets or sets a callback to be invoked when a job fails after all retries are exhausted.
        /// </summary>
        Action<Exception>? OnJobFailed { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// The circuit is closed and executions are allowed.
        /// </summary>
        Closed,

        /// <summary>
        /// The circuit is open and executions are blocked.
        /// </summary>
        Open,

        /// <summary>
        /// The circuit is partially open, allowing limited test executions.
        /// </summary>
        HalfOpen
    }
}

