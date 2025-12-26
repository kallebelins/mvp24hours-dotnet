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
    /// Interface for managing dead-letter queue (DLQ) for failed jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dead-letter queue stores jobs that have failed after exhausting all retry attempts.
    /// This allows administrators to:
    /// - Inspect failed jobs
    /// - Retry failed jobs manually
    /// - Analyze failure patterns
    /// - Delete or archive failed jobs
    /// </para>
    /// <para>
    /// Jobs are automatically moved to the DLQ when they fail after reaching the maximum
    /// retry attempts configured in their <see cref="Options.JobOptions"/>.
    /// </para>
    /// </remarks>
    public interface IDeadLetterQueue
    {
        /// <summary>
        /// Adds a failed job to the dead-letter queue.
        /// </summary>
        /// <param name="failedJob">The failed job information.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddFailedJobAsync(
            FailedJob failedJob,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all failed jobs in the dead-letter queue.
        /// </summary>
        /// <param name="filter">Optional filter criteria.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A list of failed jobs matching the filter.</returns>
        Task<IReadOnlyList<FailedJob>> GetFailedJobsAsync(
            DeadLetterQueueFilter? filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific failed job by ID.
        /// </summary>
        /// <param name="jobId">The job ID to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The failed job, or <c>null</c> if not found.</returns>
        Task<FailedJob?> GetFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a failed job from the dead-letter queue.
        /// </summary>
        /// <param name="jobId">The job ID to remove.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the job was removed; <c>false</c> if not found.</returns>
        Task<bool> RemoveFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retries a failed job from the dead-letter queue.
        /// </summary>
        /// <param name="jobId">The job ID to retry.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The new job ID if the job was rescheduled; <c>null</c> if the job was not found.</returns>
        /// <remarks>
        /// This method reschedules the job for execution. The job will be removed from the DLQ
        /// and scheduled again with fresh retry attempts.
        /// </remarks>
        Task<string?> RetryFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of failed jobs in the dead-letter queue.
        /// </summary>
        /// <param name="filter">Optional filter criteria.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The count of failed jobs.</returns>
        Task<int> GetFailedJobCountAsync(
            DeadLetterQueueFilter? filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears old failed jobs from the dead-letter queue.
        /// </summary>
        /// <param name="olderThan">Remove jobs older than this date.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of jobs removed.</returns>
        Task<int> ClearOldFailedJobsAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a failed job in the dead-letter queue.
    /// </summary>
    public class FailedJob
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
        /// Gets or sets the serialized job arguments.
        /// </summary>
        public string SerializedArgs { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of retry attempts that were made.
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the maximum retry attempts that were configured.
        /// </summary>
        public int MaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets when the job first failed.
        /// </summary>
        public DateTimeOffset FirstFailedAt { get; set; }

        /// <summary>
        /// Gets or sets when the job was added to the dead-letter queue.
        /// </summary>
        public DateTimeOffset AddedToDlqAt { get; set; }

        /// <summary>
        /// Gets or sets the error message from the last failure.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception type name from the last failure.
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception stack trace from the last failure.
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
        /// Gets or sets the metadata associated with the job.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the job options that were configured.
        /// </summary>
        public Options.JobOptions? JobOptions { get; set; }
    }

    /// <summary>
    /// Filter criteria for querying the dead-letter queue.
    /// </summary>
    public class DeadLetterQueueFilter
    {
        /// <summary>
        /// Gets or sets the job type name to filter by.
        /// </summary>
        public string? JobType { get; set; }

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
}

