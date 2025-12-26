//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Management
{
    /// <summary>
    /// Interface for collecting and retrieving job execution metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides methods for collecting and querying metrics about job execution,
    /// including throughput, latency, failure rates, and queue statistics. Metrics are useful
    /// for monitoring, alerting, and performance optimization.
    /// </para>
    /// </remarks>
    public interface IJobMetrics
    {
        /// <summary>
        /// Records a job execution metric.
        /// </summary>
        /// <param name="metric">The metric to record.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordMetricAsync(
            JobMetric metric,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated metrics for a specific job type.
        /// </summary>
        /// <param name="jobType">The job type name (fully qualified type name).</param>
        /// <param name="startDate">The start date for the metrics period.</param>
        /// <param name="endDate">The end date for the metrics period.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Aggregated metrics for the job type.</returns>
        Task<JobMetricsAggregate> GetMetricsAsync(
            string jobType,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated metrics for all job types.
        /// </summary>
        /// <param name="startDate">The start date for the metrics period.</param>
        /// <param name="endDate">The end date for the metrics period.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary mapping job type names to their aggregated metrics.</returns>
        Task<IReadOnlyDictionary<string, JobMetricsAggregate>> GetAllMetricsAsync(
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets queue statistics for a specific queue.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Queue statistics.</returns>
        Task<QueueStatistics> GetQueueStatisticsAsync(
            string queueName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics for all queues.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary mapping queue names to their statistics.</returns>
        Task<IReadOnlyDictionary<string, QueueStatistics>> GetAllQueueStatisticsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets all metrics (useful for testing or periodic resets).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ResetMetricsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a single job execution metric.
    /// </summary>
    public class JobMetric
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job type name (fully qualified type name).
        /// </summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets when the metric was recorded.
        /// </summary>
        public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the execution duration.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets whether the execution succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the execution status.
        /// </summary>
        public Contract.JobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the attempt number.
        /// </summary>
        public int AttemptNumber { get; set; }
    }

    /// <summary>
    /// Represents aggregated metrics for a job type.
    /// </summary>
    public class JobMetricsAggregate
    {
        /// <summary>
        /// Gets or sets the job type name.
        /// </summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public long TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public long SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public long FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of cancelled executions.
        /// </summary>
        public long CancelledExecutions { get; set; }

        /// <summary>
        /// Gets or sets the average execution duration.
        /// </summary>
        public TimeSpan? AverageDuration { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution duration.
        /// </summary>
        public TimeSpan? MinDuration { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution duration.
        /// </summary>
        public TimeSpan? MaxDuration { get; set; }

        /// <summary>
        /// Gets or sets the 50th percentile (median) execution duration.
        /// </summary>
        public TimeSpan? P50Duration { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile execution duration.
        /// </summary>
        public TimeSpan? P95Duration { get; set; }

        /// <summary>
        /// Gets or sets the 99th percentile execution duration.
        /// </summary>
        public TimeSpan? P99Duration { get; set; }

        /// <summary>
        /// Gets or sets the throughput (executions per second).
        /// </summary>
        public double Throughput { get; set; }

        /// <summary>
        /// Gets or sets the success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets or sets the failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets or sets the start date of the metrics period.
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the metrics period.
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }
    }

    /// <summary>
    /// Represents statistics for a job queue.
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of pending jobs.
        /// </summary>
        public int PendingJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of running jobs.
        /// </summary>
        public int RunningJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of completed jobs (in the current period).
        /// </summary>
        public long CompletedJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of failed jobs (in the current period).
        /// </summary>
        public long FailedJobs { get; set; }

        /// <summary>
        /// Gets or sets the average wait time for jobs in the queue.
        /// </summary>
        public TimeSpan? AverageWaitTime { get; set; }

        /// <summary>
        /// Gets or sets the average execution time for jobs in the queue.
        /// </summary>
        public TimeSpan? AverageExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the throughput (jobs per second).
        /// </summary>
        public double Throughput { get; set; }

        /// <summary>
        /// Gets or sets when the statistics were last updated.
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    }
}

