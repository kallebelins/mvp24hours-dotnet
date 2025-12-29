//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// ActivitySource for CronJob operations in OpenTelemetry-compatible tracing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API which is automatically
/// exported by OpenTelemetry when configured. Activities created here will appear
/// as spans in your tracing backend (Jaeger, Zipkin, Application Insights, etc.).
/// </para>
/// <para>
/// <strong>Activity Names:</strong>
/// <list type="bullet">
/// <item>Mvp24Hours.CronJob.Execute - For job execution operations</item>
/// <item>Mvp24Hours.CronJob.Schedule - For job scheduling operations</item>
/// <item>Mvp24Hours.CronJob.Start - For job startup operations</item>
/// <item>Mvp24Hours.CronJob.Stop - For job stop operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours CronJob activities
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(CronJobActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     });
/// </code>
/// </example>
public static class CronJobActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours CronJob operations.
    /// Use this constant when configuring OpenTelemetry.
    /// </summary>
    public const string SourceName = "Mvp24Hours.CronJob";

    /// <summary>
    /// The version of the ActivitySource.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// Activity names for different CronJob operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for job execution operations.</summary>
        public const string Execute = "Mvp24Hours.CronJob.Execute";

        /// <summary>Activity name for job scheduling operations.</summary>
        public const string Schedule = "Mvp24Hours.CronJob.Schedule";

        /// <summary>Activity name for job startup operations.</summary>
        public const string Start = "Mvp24Hours.CronJob.Start";

        /// <summary>Activity name for job stop operations.</summary>
        public const string Stop = "Mvp24Hours.CronJob.Stop";

        /// <summary>Activity name for job failure operations.</summary>
        public const string Failure = "Mvp24Hours.CronJob.Failure";
    }

    /// <summary>
    /// Semantic tags for CronJob activities following OpenTelemetry conventions.
    /// </summary>
    public static class Tags
    {
        /// <summary>The name of the CronJob.</summary>
        public const string JobName = "cronjob.name";

        /// <summary>The CRON expression for the job.</summary>
        public const string CronExpression = "cronjob.expression";

        /// <summary>The timezone of the job.</summary>
        public const string TimeZone = "cronjob.timezone";

        /// <summary>The next scheduled execution time.</summary>
        public const string NextExecution = "cronjob.next_execution";

        /// <summary>The duration of the job execution in milliseconds.</summary>
        public const string DurationMs = "cronjob.duration_ms";

        /// <summary>Whether the job was successful.</summary>
        public const string Success = "cronjob.success";

        /// <summary>The error message if the job failed.</summary>
        public const string ErrorMessage = "cronjob.error_message";

        /// <summary>The execution count for the job.</summary>
        public const string ExecutionCount = "cronjob.execution_count";
    }

    #region Activity Factory Methods

    /// <summary>
    /// Starts an activity for job execution.
    /// </summary>
    /// <param name="jobName">The name of the job being executed.</param>
    /// <param name="cronExpression">Optional CRON expression.</param>
    /// <param name="timeZone">Optional timezone.</param>
    /// <returns>The started activity, or null if not sampled.</returns>
    public static Activity? StartExecuteActivity(
        string jobName,
        string? cronExpression = null,
        string? timeZone = null)
    {
        var activity = Source.StartActivity(ActivityNames.Execute, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(Tags.JobName, jobName);

        if (!string.IsNullOrEmpty(cronExpression))
        {
            activity.SetTag(Tags.CronExpression, cronExpression);
        }

        if (!string.IsNullOrEmpty(timeZone))
        {
            activity.SetTag(Tags.TimeZone, timeZone);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for job scheduling.
    /// </summary>
    /// <param name="jobName">The name of the job being scheduled.</param>
    /// <param name="cronExpression">The CRON expression.</param>
    /// <param name="nextExecution">The next scheduled execution time.</param>
    /// <returns>The started activity, or null if not sampled.</returns>
    public static Activity? StartScheduleActivity(
        string jobName,
        string cronExpression,
        DateTimeOffset? nextExecution = null)
    {
        var activity = Source.StartActivity(ActivityNames.Schedule, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(Tags.JobName, jobName);
        activity.SetTag(Tags.CronExpression, cronExpression);

        if (nextExecution.HasValue)
        {
            activity.SetTag(Tags.NextExecution, nextExecution.Value.ToString("O"));
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for job startup.
    /// </summary>
    /// <param name="jobName">The name of the job starting.</param>
    /// <param name="cronExpression">Optional CRON expression.</param>
    /// <returns>The started activity, or null if not sampled.</returns>
    public static Activity? StartStartActivity(
        string jobName,
        string? cronExpression = null)
    {
        var activity = Source.StartActivity(ActivityNames.Start, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(Tags.JobName, jobName);

        if (!string.IsNullOrEmpty(cronExpression))
        {
            activity.SetTag(Tags.CronExpression, cronExpression);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for job stop.
    /// </summary>
    /// <param name="jobName">The name of the job stopping.</param>
    /// <returns>The started activity, or null if not sampled.</returns>
    public static Activity? StartStopActivity(string jobName)
    {
        var activity = Source.StartActivity(ActivityNames.Stop, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(Tags.JobName, jobName);

        return activity;
    }

    #endregion

    #region Activity Extensions

    /// <summary>
    /// Sets the execution result on an activity.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="errorMessage">Optional error message if failed.</param>
    public static void SetExecutionResult(
        this Activity? activity,
        bool success,
        double durationMs,
        string? errorMessage = null)
    {
        if (activity == null)
            return;

        activity.SetTag(Tags.Success, success);
        activity.SetTag(Tags.DurationMs, durationMs);

        if (success)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, errorMessage ?? "Job execution failed");
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                activity.SetTag(Tags.ErrorMessage, errorMessage);
            }
        }
    }

    /// <summary>
    /// Records an error on an activity.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void RecordError(this Activity? activity, Exception exception)
    {
        if (activity == null || exception == null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(Tags.Success, false);
        activity.SetTag(Tags.ErrorMessage, exception.Message);

        // Add exception event
        activity.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName ?? exception.GetType().Name },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace ?? string.Empty }
        }));
    }

    #endregion
}

