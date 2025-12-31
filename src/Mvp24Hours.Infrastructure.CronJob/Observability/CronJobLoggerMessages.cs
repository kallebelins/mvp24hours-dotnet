//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// High-performance logger messages for CronJob operations using source-generated logging.
/// </summary>
/// <remarks>
/// <para>
/// This class uses the [LoggerMessage] attribute for high-performance logging
/// with zero allocations at runtime.
/// </para>
/// <para>
/// <strong>Event ID Ranges:</strong>
/// <list type="bullet">
/// <item>10000-10099: Job lifecycle events (start, stop, scheduling)</item>
/// <item>10100-10199: Execution events (before, after, error)</item>
/// <item>10200-10299: Resilience events (retry, circuit breaker, skip)</item>
/// <item>10300-10399: Health check events</item>
/// </list>
/// </para>
/// </remarks>
public static partial class CronJobLoggerMessages
{
    #region Job Lifecycle Events (10000-10099)

    /// <summary>
    /// Logs job starting event.
    /// </summary>
    [LoggerMessage(
        EventId = 10000,
        Level = LogLevel.Information,
        Message = "CronJob starting. Name: {JobName}, CronExpression: {CronExpression}, TimeZone: {TimeZone}")]
    public static partial void LogJobStarting(
        ILogger logger,
        string jobName,
        string? cronExpression,
        string? timeZone);

    /// <summary>
    /// Logs job started event.
    /// </summary>
    [LoggerMessage(
        EventId = 10001,
        Level = LogLevel.Information,
        Message = "CronJob started successfully. Name: {JobName}")]
    public static partial void LogJobStarted(ILogger logger, string jobName);

    /// <summary>
    /// Logs job stopping event.
    /// </summary>
    [LoggerMessage(
        EventId = 10002,
        Level = LogLevel.Information,
        Message = "CronJob stopping. Name: {JobName}, TotalExecutions: {TotalExecutions}")]
    public static partial void LogJobStopping(ILogger logger, string jobName, long totalExecutions);

    /// <summary>
    /// Logs job stopped event.
    /// </summary>
    [LoggerMessage(
        EventId = 10003,
        Level = LogLevel.Information,
        Message = "CronJob stopped. Name: {JobName}")]
    public static partial void LogJobStopped(ILogger logger, string jobName);

    /// <summary>
    /// Logs next execution scheduling.
    /// </summary>
    [LoggerMessage(
        EventId = 10010,
        Level = LogLevel.Debug,
        Message = "CronJob next execution scheduled. Name: {JobName}, NextExecution: {NextExecution}, DelayMs: {DelayMs:F0}")]
    public static partial void LogNextExecution(
        ILogger logger,
        string jobName,
        DateTimeOffset nextExecution,
        double delayMs);

    /// <summary>
    /// Logs no next occurrence found.
    /// </summary>
    [LoggerMessage(
        EventId = 10011,
        Level = LogLevel.Warning,
        Message = "CronJob has no next occurrence. Name: {JobName}")]
    public static partial void LogNoNextOccurrence(ILogger logger, string jobName);

    /// <summary>
    /// Logs scheduler started.
    /// </summary>
    [LoggerMessage(
        EventId = 10020,
        Level = LogLevel.Debug,
        Message = "CronJob scheduler started. Name: {JobName}, Expression: {CronExpression}")]
    public static partial void LogSchedulerStarted(ILogger logger, string jobName, string? cronExpression);

    /// <summary>
    /// Logs scheduler stopped.
    /// </summary>
    [LoggerMessage(
        EventId = 10021,
        Level = LogLevel.Debug,
        Message = "CronJob scheduler stopped. Name: {JobName}")]
    public static partial void LogSchedulerStopped(ILogger logger, string jobName);

    /// <summary>
    /// Logs scheduler cancelled.
    /// </summary>
    [LoggerMessage(
        EventId = 10022,
        Level = LogLevel.Debug,
        Message = "CronJob scheduler cancelled gracefully. Name: {JobName}")]
    public static partial void LogSchedulerCancelled(ILogger logger, string jobName);

    #endregion

    #region Execution Events (10100-10199)

    /// <summary>
    /// Logs execution starting.
    /// </summary>
    [LoggerMessage(
        EventId = 10100,
        Level = LogLevel.Debug,
        Message = "CronJob execution starting. Name: {JobName}, ExecutionCount: {ExecutionCount}")]
    public static partial void LogExecutionStarting(ILogger logger, string jobName, long executionCount);

    /// <summary>
    /// Logs execution completed successfully.
    /// </summary>
    [LoggerMessage(
        EventId = 10101,
        Level = LogLevel.Debug,
        Message = "CronJob execution completed. Name: {JobName}, DurationMs: {DurationMs:F2}, ExecutionCount: {ExecutionCount}")]
    public static partial void LogExecutionCompleted(
        ILogger logger,
        string jobName,
        double durationMs,
        long executionCount);

    /// <summary>
    /// Logs execution failed.
    /// </summary>
    [LoggerMessage(
        EventId = 10102,
        Level = LogLevel.Error,
        Message = "CronJob execution failed. Name: {JobName}, DurationMs: {DurationMs:F2}, ExecutionCount: {ExecutionCount}")]
    public static partial void LogExecutionFailed(
        ILogger logger,
        Exception exception,
        string jobName,
        double durationMs,
        long executionCount);

    /// <summary>
    /// Logs execution cancelled.
    /// </summary>
    [LoggerMessage(
        EventId = 10103,
        Level = LogLevel.Debug,
        Message = "CronJob execution cancelled. Name: {JobName}")]
    public static partial void LogExecutionCancelled(ILogger logger, string jobName);

    /// <summary>
    /// Logs execution timed out.
    /// </summary>
    [LoggerMessage(
        EventId = 10104,
        Level = LogLevel.Warning,
        Message = "CronJob execution timed out. Name: {JobName}, TimeoutMs: {TimeoutMs}")]
    public static partial void LogExecutionTimedOut(ILogger logger, string jobName, double timeoutMs);

    /// <summary>
    /// Logs execution once mode.
    /// </summary>
    [LoggerMessage(
        EventId = 10110,
        Level = LogLevel.Debug,
        Message = "CronJob executing once (no CRON expression). Name: {JobName}")]
    public static partial void LogExecuteOnce(ILogger logger, string jobName);

    /// <summary>
    /// Logs execution once completed.
    /// </summary>
    [LoggerMessage(
        EventId = 10111,
        Level = LogLevel.Debug,
        Message = "CronJob execute once completed, stopping application. Name: {JobName}")]
    public static partial void LogExecuteOnceCompleted(ILogger logger, string jobName);

    #endregion

    #region Resilience Events (10200-10299)

    /// <summary>
    /// Logs retry attempt.
    /// </summary>
    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Warning,
        Message = "CronJob execution failed, retrying. Name: {JobName}, Attempt: {Attempt}/{MaxAttempts}, DelayMs: {DelayMs:F0}")]
    public static partial void LogRetryAttempt(
        ILogger logger,
        Exception exception,
        string jobName,
        int attempt,
        int maxAttempts,
        double delayMs);

    /// <summary>
    /// Logs all retries exhausted.
    /// </summary>
    [LoggerMessage(
        EventId = 10201,
        Level = LogLevel.Error,
        Message = "CronJob all retries exhausted. Name: {JobName}, TotalAttempts: {TotalAttempts}")]
    public static partial void LogRetriesExhausted(
        ILogger logger,
        Exception exception,
        string jobName,
        int totalAttempts);

    /// <summary>
    /// Logs execution skipped due to overlapping.
    /// </summary>
    [LoggerMessage(
        EventId = 10210,
        Level = LogLevel.Warning,
        Message = "CronJob execution skipped - previous execution still running. Name: {JobName}")]
    public static partial void LogSkippedOverlapping(ILogger logger, string jobName);

    /// <summary>
    /// Logs execution skipped due to circuit breaker open.
    /// </summary>
    [LoggerMessage(
        EventId = 10211,
        Level = LogLevel.Warning,
        Message = "CronJob execution skipped - circuit breaker open. Name: {JobName}, State: {CircuitBreakerState}")]
    public static partial void LogSkippedCircuitBreakerOpen(
        ILogger logger,
        string jobName,
        string circuitBreakerState);

    /// <summary>
    /// Logs circuit breaker state change.
    /// </summary>
    [LoggerMessage(
        EventId = 10220,
        Level = LogLevel.Warning,
        Message = "CronJob circuit breaker state changed. Name: {JobName}, PreviousState: {PreviousState}, NewState: {NewState}")]
    public static partial void LogCircuitBreakerStateChanged(
        ILogger logger,
        string jobName,
        string previousState,
        string newState);

    /// <summary>
    /// Logs graceful shutdown timeout.
    /// </summary>
    [LoggerMessage(
        EventId = 10230,
        Level = LogLevel.Warning,
        Message = "CronJob graceful shutdown timed out. Name: {JobName}, TimeoutMs: {TimeoutMs}")]
    public static partial void LogGracefulShutdownTimeout(ILogger logger, string jobName, double timeoutMs);

    #endregion

    #region Health Check Events (10300-10399)

    /// <summary>
    /// Logs health check passed.
    /// </summary>
    [LoggerMessage(
        EventId = 10300,
        Level = LogLevel.Debug,
        Message = "CronJob health check passed. JobCount: {JobCount}")]
    public static partial void LogHealthCheckPassed(ILogger logger, int jobCount);

    /// <summary>
    /// Logs health check degraded.
    /// </summary>
    [LoggerMessage(
        EventId = 10301,
        Level = LogLevel.Warning,
        Message = "CronJob health check degraded. DegradedJobs: {DegradedJobs}")]
    public static partial void LogHealthCheckDegraded(ILogger logger, string degradedJobs);

    /// <summary>
    /// Logs health check failed.
    /// </summary>
    [LoggerMessage(
        EventId = 10302,
        Level = LogLevel.Error,
        Message = "CronJob health check failed. UnhealthyJobs: {UnhealthyJobs}")]
    public static partial void LogHealthCheckFailed(ILogger logger, string unhealthyJobs);

    /// <summary>
    /// Logs health check exception.
    /// </summary>
    [LoggerMessage(
        EventId = 10303,
        Level = LogLevel.Error,
        Message = "CronJob health check failed with exception")]
    public static partial void LogHealthCheckException(ILogger logger, Exception exception);

    #endregion
}

