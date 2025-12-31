//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// Interface for CronJob metrics customization and observation.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows consumers to implement custom metrics collection
/// for CronJob operations, integrating with their preferred monitoring systems.
/// </para>
/// <para>
/// The default implementation uses OpenTelemetry Metrics via <see cref="CronJobMetricsService"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomCronJobMetrics : ICronJobMetrics
/// {
///     public void RecordExecution(string jobName, double durationMs, bool success, int executionCount)
///     {
///         // Send to your custom monitoring system
///     }
/// }
/// </code>
/// </example>
public interface ICronJobMetrics
{
    /// <summary>
    /// Records a job execution with its duration and result.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="executionCount">The execution count for this job.</param>
    void RecordExecution(string jobName, double durationMs, bool success, int executionCount);

    /// <summary>
    /// Records a job execution failure.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="durationMs">Duration in milliseconds before failure.</param>
    /// <param name="executionCount">The execution count for this job.</param>
    void RecordFailure(string jobName, Exception exception, double durationMs, int executionCount);

    /// <summary>
    /// Records a job start event.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="cronExpression">The CRON expression for the job.</param>
    void RecordJobStarted(string jobName, string? cronExpression);

    /// <summary>
    /// Records a job stop event.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="totalExecutions">Total executions before stopping.</param>
    void RecordJobStopped(string jobName, long totalExecutions);

    /// <summary>
    /// Records a skipped execution (due to overlapping or circuit breaker).
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="reason">The reason for skipping (e.g., "overlapping", "circuit_breaker").</param>
    void RecordSkippedExecution(string jobName, string reason);

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <param name="maxAttempts">The maximum retry attempts configured.</param>
    /// <param name="delayMs">The delay before this retry.</param>
    void RecordRetryAttempt(string jobName, int attemptNumber, int maxAttempts, double delayMs);

    /// <summary>
    /// Records a circuit breaker state change.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="previousState">The previous circuit breaker state.</param>
    /// <param name="newState">The new circuit breaker state.</param>
    void RecordCircuitBreakerStateChange(string jobName, string previousState, string newState);

    /// <summary>
    /// Increments the active job count.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    void IncrementActiveJob(string jobName);

    /// <summary>
    /// Decrements the active job count.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    void DecrementActiveJob(string jobName);

    /// <summary>
    /// Records the next scheduled execution time.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="nextExecution">The next scheduled execution time.</param>
    void RecordNextScheduledExecution(string jobName, DateTimeOffset nextExecution);

    /// <summary>
    /// Records the last execution time for tracking execution age.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="lastExecution">The time of the last execution.</param>
    void RecordLastExecution(string jobName, DateTimeOffset lastExecution);
}

