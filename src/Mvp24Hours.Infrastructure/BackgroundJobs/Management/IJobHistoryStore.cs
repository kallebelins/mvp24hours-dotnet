//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Management
{
    /// <summary>
    /// Interface for storing and retrieving job execution history and audit information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides methods for storing and querying job execution history,
    /// which is useful for auditing, debugging, and monitoring purposes. Job history
    /// includes information about each execution attempt, including:
    /// - Job ID and type
    /// - Execution start and end times
    /// - Duration
    /// - Status (success, failure, cancellation)
    /// - Error messages and exceptions
    /// - Retry attempts
    /// - Metadata
    /// </para>
    /// </remarks>
    public interface IJobHistoryStore
    {
        /// <summary>
        /// Records a job execution in the history store.
        /// </summary>
        /// <param name="record">The job execution record to store.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordExecutionAsync(
            JobExecutionRecord record,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the execution history for a specific job.
        /// </summary>
        /// <param name="jobId">The job ID to query.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A list of execution records for the job, ordered by execution time (most recent first).</returns>
        Task<IReadOnlyList<JobExecutionRecord>> GetJobHistoryAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets execution history for multiple jobs matching the filter criteria.
        /// </summary>
        /// <param name="filter">The filter criteria for querying history.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A list of execution records matching the filter, ordered by execution time (most recent first).</returns>
        Task<IReadOnlyList<JobExecutionRecord>> QueryHistoryAsync(
            JobHistoryFilter filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets execution statistics for a specific job type.
        /// </summary>
        /// <param name="jobType">The job type name (fully qualified type name).</param>
        /// <param name="startDate">The start date for the statistics period.</param>
        /// <param name="endDate">The end date for the statistics period.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Execution statistics for the job type.</returns>
        Task<JobExecutionStatistics> GetStatisticsAsync(
            string jobType,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes old execution records beyond the retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain records.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> CleanupOldRecordsAsync(
            int retentionDays,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a job execution record for history and auditing.
    /// </summary>
    public class JobExecutionRecord
    {
        /// <summary>
        /// Gets or sets the unique identifier of the execution record.
        /// </summary>
        public string RecordId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job type name (fully qualified type name).
        /// </summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution attempt number.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Gets or sets when the execution started.
        /// </summary>
        public DateTimeOffset StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the execution completed (or failed/cancelled).
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the execution duration.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the execution status.
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message (if failed).
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception type name (if failed).
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception stack trace (if failed).
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the queue name where the job was processed.
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets the job priority.
        /// </summary>
        public Options.JobPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the job execution.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the serialized job arguments (for auditing).
        /// </summary>
        public string? SerializedArgs { get; set; }
    }

    /// <summary>
    /// Filter criteria for querying job execution history.
    /// </summary>
    public class JobHistoryFilter
    {
        /// <summary>
        /// Gets or sets the job ID to filter by.
        /// </summary>
        public string? JobId { get; set; }

        /// <summary>
        /// Gets or sets the job type name to filter by.
        /// </summary>
        public string? JobType { get; set; }

        /// <summary>
        /// Gets or sets the status to filter by.
        /// </summary>
        public JobStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the queue name to filter by.
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets the start date for the filter period.
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date for the filter period.
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records to return.
        /// </summary>
        public int? MaxRecords { get; set; }

        /// <summary>
        /// Gets or sets the number of records to skip (for pagination).
        /// </summary>
        public int? Skip { get; set; }
    }

    /// <summary>
    /// Represents execution statistics for a job type.
    /// </summary>
    public class JobExecutionStatistics
    {
        /// <summary>
        /// Gets or sets the job type name.
        /// </summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of cancelled executions.
        /// </summary>
        public int CancelledExecutions { get; set; }

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
        /// Gets or sets the success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets or sets the failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0.0;
    }
}

