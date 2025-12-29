//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for CronJob/scheduled job operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// scheduled job executions, durations, and health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>executions_total</c> - Counter for job executions</item>
/// <item><c>executions_failed_total</c> - Counter for failed executions</item>
/// <item><c>execution_duration_ms</c> - Histogram for execution duration</item>
/// <item><c>active_count</c> - Gauge for active jobs</item>
/// <item><c>scheduled_count</c> - Gauge for scheduled jobs</item>
/// <item><c>last_execution_age_seconds</c> - Gauge for time since last execution</item>
/// </list>
/// </para>
/// </remarks>
public sealed class CronJobMetrics
{
    private readonly Counter<long> _executionsTotal;
    private readonly Counter<long> _executionsFailedTotal;
    private readonly Histogram<double> _executionDuration;
    private readonly UpDownCounter<int> _activeCount;
    private readonly UpDownCounter<int> _scheduledCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronJobMetrics"/> class.
    /// </summary>
    public CronJobMetrics()
    {
        var meter = Mvp24HoursMeters.CronJob.Meter;

        _executionsTotal = meter.CreateCounter<long>(
            MetricNames.CronJobExecutionsTotal,
            unit: "{executions}",
            description: "Total number of job executions");

        _executionsFailedTotal = meter.CreateCounter<long>(
            MetricNames.CronJobExecutionsFailedTotal,
            unit: "{executions}",
            description: "Total number of failed job executions");

        _executionDuration = meter.CreateHistogram<double>(
            MetricNames.CronJobExecutionDuration,
            unit: "ms",
            description: "Duration of job executions in milliseconds");

        _activeCount = meter.CreateUpDownCounter<int>(
            MetricNames.CronJobActiveCount,
            unit: "{jobs}",
            description: "Number of active/running jobs");

        _scheduledCount = meter.CreateUpDownCounter<int>(
            MetricNames.CronJobScheduledCount,
            unit: "{jobs}",
            description: "Number of scheduled jobs");
    }

    /// <summary>
    /// Begins tracking a job execution.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    /// <returns>A scope that should be disposed when execution completes.</returns>
    public JobExecutionScope BeginExecution(string jobType)
    {
        return new JobExecutionScope(this, jobType);
    }

    /// <summary>
    /// Records a job execution.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the execution was successful.</param>
    public void RecordExecution(string jobType, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.JobType, jobType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _executionsTotal.Add(1, tags);

        if (!success)
        {
            _executionsFailedTotal.Add(1, tags);
        }

        _executionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Increments the active job count.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    public void IncrementActive(string jobType)
    {
        var tags = new TagList { { MetricTags.JobType, jobType } };
        _activeCount.Add(1, tags);
    }

    /// <summary>
    /// Decrements the active job count.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    public void DecrementActive(string jobType)
    {
        var tags = new TagList { { MetricTags.JobType, jobType } };
        _activeCount.Add(-1, tags);
    }

    /// <summary>
    /// Updates the scheduled job count.
    /// </summary>
    /// <param name="delta">Change in count.</param>
    public void UpdateScheduledCount(int delta)
    {
        _scheduledCount.Add(delta);
    }

    /// <summary>
    /// Represents a scope for tracking job execution duration.
    /// </summary>
    public struct JobExecutionScope : IDisposable
    {
        private readonly CronJobMetrics _metrics;
        private readonly string _jobType;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the execution succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal JobExecutionScope(CronJobMetrics metrics, string jobType)
        {
            _metrics = metrics;
            _jobType = jobType;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;

            _metrics.IncrementActive(_jobType);
        }

        /// <summary>
        /// Marks the execution as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the execution as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            _metrics.DecrementActive(_jobType);
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordExecution(_jobType, elapsed.TotalMilliseconds, Succeeded);
        }
    }
}

