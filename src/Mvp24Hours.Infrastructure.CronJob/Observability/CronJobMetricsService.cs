//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Observability;
using Mvp24Hours.Core.Observability.Metrics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// Default implementation of <see cref="ICronJobMetrics"/> using OpenTelemetry Metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class integrates with the Mvp24Hours metrics infrastructure and provides
/// comprehensive metrics for CronJob monitoring.
/// </para>
/// <para>
/// <strong>Metrics Exposed:</strong>
/// <list type="bullet">
/// <item><c>mvp24hours.cronjob.executions_total</c> - Total executions by job</item>
/// <item><c>mvp24hours.cronjob.executions_failed_total</c> - Failed executions by job</item>
/// <item><c>mvp24hours.cronjob.execution_duration_ms</c> - Execution duration histogram</item>
/// <item><c>mvp24hours.cronjob.active_count</c> - Active jobs gauge</item>
/// <item><c>mvp24hours.cronjob.skipped_total</c> - Skipped executions counter</item>
/// <item><c>mvp24hours.cronjob.retries_total</c> - Retry attempts counter</item>
/// <item><c>mvp24hours.cronjob.circuit_breaker_state_changes</c> - Circuit breaker changes</item>
/// <item><c>mvp24hours.cronjob.last_execution_timestamp</c> - Last execution timestamp</item>
/// <item><c>mvp24hours.cronjob.next_execution_delay_seconds</c> - Time until next execution</item>
/// </list>
/// </para>
/// </remarks>
public sealed class CronJobMetricsService : ICronJobMetrics
{
    private readonly ILogger<CronJobMetricsService>? _logger;
    private readonly CronJobMetrics? _coreMetrics;
    
    // Custom metrics for extended functionality
    private readonly Counter<long> _skippedTotal;
    private readonly Counter<long> _retriesTotal;
    private readonly Counter<long> _circuitBreakerChanges;
    private readonly Histogram<double> _retryDelayMs;
    
    // Job state tracking
    private readonly ConcurrentDictionary<string, CronJobState> _jobStates = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CronJobMetricsService"/>.
    /// </summary>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <param name="coreMetrics">Optional core CronJob metrics (if available via DI).</param>
    public CronJobMetricsService(
        ILogger<CronJobMetricsService>? logger = null,
        CronJobMetrics? coreMetrics = null)
    {
        _logger = logger;
        _coreMetrics = coreMetrics;

        var meter = Mvp24HoursMeters.CronJob.Meter;

        _skippedTotal = meter.CreateCounter<long>(
            "mvp24hours.cronjob.skipped_total",
            unit: "{executions}",
            description: "Total number of skipped executions");

        _retriesTotal = meter.CreateCounter<long>(
            "mvp24hours.cronjob.retries_total",
            unit: "{retries}",
            description: "Total number of retry attempts");

        _circuitBreakerChanges = meter.CreateCounter<long>(
            "mvp24hours.cronjob.circuit_breaker_state_changes",
            unit: "{changes}",
            description: "Number of circuit breaker state changes");

        _retryDelayMs = meter.CreateHistogram<double>(
            "mvp24hours.cronjob.retry_delay_ms",
            unit: "ms",
            description: "Retry delay duration in milliseconds");
    }

    /// <inheritdoc />
    public void RecordExecution(string jobName, double durationMs, bool success, int executionCount)
    {
        _coreMetrics?.RecordExecution(jobName, durationMs, success);

        var state = GetOrCreateState(jobName);
        state.TotalExecutions++;
        state.LastExecutionTime = DateTimeOffset.UtcNow;
        state.LastExecutionDurationMs = durationMs;
        state.LastExecutionSuccess = success;
        
        if (!success)
        {
            state.TotalFailures++;
        }

        _logger?.LogDebug(
            "CronJob execution recorded. Job: {JobName}, Duration: {DurationMs:F2}ms, Success: {Success}, TotalExecutions: {TotalExecutions}",
            jobName, durationMs, success, executionCount);
    }

    /// <inheritdoc />
    public void RecordFailure(string jobName, Exception exception, double durationMs, int executionCount)
    {
        RecordExecution(jobName, durationMs, success: false, executionCount);

        var state = GetOrCreateState(jobName);
        state.LastErrorMessage = exception.Message;
        state.LastErrorType = exception.GetType().Name;

        _logger?.LogWarning(
            exception,
            "CronJob execution failed. Job: {JobName}, Duration: {DurationMs:F2}ms, Error: {ErrorType}",
            jobName, durationMs, exception.GetType().Name);
    }

    /// <inheritdoc />
    public void RecordJobStarted(string jobName, string? cronExpression)
    {
        var state = GetOrCreateState(jobName);
        state.IsRunning = true;
        state.StartTime = DateTimeOffset.UtcNow;
        state.CronExpression = cronExpression;

        _coreMetrics?.UpdateScheduledCount(1);

        _logger?.LogInformation(
            "CronJob started. Job: {JobName}, CronExpression: {CronExpression}",
            jobName, cronExpression ?? "(run once)");
    }

    /// <inheritdoc />
    public void RecordJobStopped(string jobName, long totalExecutions)
    {
        var state = GetOrCreateState(jobName);
        state.IsRunning = false;
        state.StopTime = DateTimeOffset.UtcNow;

        _coreMetrics?.UpdateScheduledCount(-1);

        _logger?.LogInformation(
            "CronJob stopped. Job: {JobName}, TotalExecutions: {TotalExecutions}",
            jobName, totalExecutions);
    }

    /// <inheritdoc />
    public void RecordSkippedExecution(string jobName, string reason)
    {
        var tags = new TagList
        {
            { MetricTags.JobType, jobName },
            { "skip_reason", reason }
        };

        _skippedTotal.Add(1, tags);

        var state = GetOrCreateState(jobName);
        state.TotalSkipped++;
        state.LastSkipReason = reason;

        _logger?.LogWarning(
            "CronJob execution skipped. Job: {JobName}, Reason: {SkipReason}",
            jobName, reason);
    }

    /// <inheritdoc />
    public void RecordRetryAttempt(string jobName, int attemptNumber, int maxAttempts, double delayMs)
    {
        var tags = new TagList
        {
            { MetricTags.JobType, jobName },
            { "attempt", attemptNumber },
            { "max_attempts", maxAttempts }
        };

        _retriesTotal.Add(1, tags);
        _retryDelayMs.Record(delayMs, tags);

        var state = GetOrCreateState(jobName);
        state.TotalRetries++;

        _logger?.LogWarning(
            "CronJob retry scheduled. Job: {JobName}, Attempt: {Attempt}/{MaxAttempts}, Delay: {DelayMs:F2}ms",
            jobName, attemptNumber, maxAttempts, delayMs);
    }

    /// <inheritdoc />
    public void RecordCircuitBreakerStateChange(string jobName, string previousState, string newState)
    {
        var tags = new TagList
        {
            { MetricTags.JobType, jobName },
            { "previous_state", previousState },
            { "new_state", newState }
        };

        _circuitBreakerChanges.Add(1, tags);

        var state = GetOrCreateState(jobName);
        state.CircuitBreakerState = newState;

        _logger?.LogWarning(
            "CronJob circuit breaker state changed. Job: {JobName}, PreviousState: {PreviousState}, NewState: {NewState}",
            jobName, previousState, newState);
    }

    /// <inheritdoc />
    public void IncrementActiveJob(string jobName)
    {
        _coreMetrics?.IncrementActive(jobName);

        var state = GetOrCreateState(jobName);
        state.IsActive = true;
    }

    /// <inheritdoc />
    public void DecrementActiveJob(string jobName)
    {
        _coreMetrics?.DecrementActive(jobName);

        var state = GetOrCreateState(jobName);
        state.IsActive = false;
    }

    /// <inheritdoc />
    public void RecordNextScheduledExecution(string jobName, DateTimeOffset nextExecution)
    {
        var state = GetOrCreateState(jobName);
        state.NextScheduledExecution = nextExecution;

        _logger?.LogDebug(
            "CronJob next execution scheduled. Job: {JobName}, NextExecution: {NextExecution}",
            jobName, nextExecution);
    }

    /// <inheritdoc />
    public void RecordLastExecution(string jobName, DateTimeOffset lastExecution)
    {
        var state = GetOrCreateState(jobName);
        state.LastExecutionTime = lastExecution;
    }

    /// <summary>
    /// Gets the current state for a job.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <returns>The job state, or null if not found.</returns>
    public CronJobState? GetJobState(string jobName)
    {
        return _jobStates.TryGetValue(jobName, out var state) ? state : null;
    }

    /// <summary>
    /// Gets all tracked job states.
    /// </summary>
    /// <returns>A dictionary of job names to their states.</returns>
    public ConcurrentDictionary<string, CronJobState> GetAllJobStates()
    {
        return _jobStates;
    }

    private CronJobState GetOrCreateState(string jobName)
    {
        return _jobStates.GetOrAdd(jobName, _ => new CronJobState { JobName = jobName });
    }
}

/// <summary>
/// Represents the current state of a CronJob for health checks and monitoring.
/// </summary>
public sealed class CronJobState
{
    /// <summary>
    /// Gets or sets the job name.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CRON expression.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets whether the job is currently running (registered and scheduled).
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets whether the job is currently executing.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the job start time.
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the job stop time.
    /// </summary>
    public DateTimeOffset? StopTime { get; set; }

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTimeOffset? LastExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution time.
    /// </summary>
    public DateTimeOffset? NextScheduledExecution { get; set; }

    /// <summary>
    /// Gets or sets the last execution duration in milliseconds.
    /// </summary>
    public double LastExecutionDurationMs { get; set; }

    /// <summary>
    /// Gets or sets whether the last execution was successful.
    /// </summary>
    public bool LastExecutionSuccess { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the total number of failures.
    /// </summary>
    public long TotalFailures { get; set; }

    /// <summary>
    /// Gets or sets the total number of skipped executions.
    /// </summary>
    public long TotalSkipped { get; set; }

    /// <summary>
    /// Gets or sets the total number of retries.
    /// </summary>
    public long TotalRetries { get; set; }

    /// <summary>
    /// Gets or sets the last skip reason.
    /// </summary>
    public string? LastSkipReason { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the last error type.
    /// </summary>
    public string? LastErrorType { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker state.
    /// </summary>
    public string? CircuitBreakerState { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalExecutions > 0 
        ? ((TotalExecutions - TotalFailures) / (double)TotalExecutions) * 100 
        : 100;

    /// <summary>
    /// Gets the time since last execution.
    /// </summary>
    public TimeSpan? TimeSinceLastExecution => LastExecutionTime.HasValue 
        ? DateTimeOffset.UtcNow - LastExecutionTime.Value 
        : null;
}

