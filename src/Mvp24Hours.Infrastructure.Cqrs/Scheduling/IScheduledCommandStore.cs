//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Interface for storing scheduled commands.
    /// </summary>
    public interface IScheduledCommandStore
    {
        /// <summary>
        /// Saves a scheduled command entry.
        /// </summary>
        /// <param name="entry">The entry to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveAsync(ScheduledCommandEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a scheduled command entry by ID.
        /// </summary>
        /// <param name="id">The entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entry or null if not found</returns>
        Task<ScheduledCommandEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets scheduled commands that are ready for execution.
        /// </summary>
        /// <param name="batchSize">Maximum number of commands to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ready commands ordered by priority and scheduled time</returns>
        Task<IReadOnlyList<ScheduledCommandEntry>> GetReadyForExecutionAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets scheduled commands that are ready for retry.
        /// </summary>
        /// <param name="batchSize">Maximum number of commands to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of commands ready for retry</returns>
        Task<IReadOnlyList<ScheduledCommandEntry>> GetReadyForRetryAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a scheduled command entry.
        /// </summary>
        /// <param name="entry">The entry to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateAsync(ScheduledCommandEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a scheduled command entry.
        /// </summary>
        /// <param name="id">The entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes completed commands older than a specified date.
        /// </summary>
        /// <param name="olderThan">Delete commands completed before this date (UTC)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted entries</returns>
        Task<int> PurgeCompletedAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets commands by status.
        /// </summary>
        /// <param name="status">The status to filter by</param>
        /// <param name="skip">Number of entries to skip</param>
        /// <param name="take">Maximum number of entries to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<IReadOnlyList<ScheduledCommandEntry>> GetByStatusAsync(
            ScheduledCommandStatus status,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets commands by correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<IReadOnlyList<ScheduledCommandEntry>> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks expired commands as expired.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of commands marked as expired</returns>
        Task<int> MarkExpiredAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about scheduled commands.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<ScheduledCommandStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about scheduled commands.
    /// </summary>
    public class ScheduledCommandStatistics
    {
        /// <summary>
        /// Number of pending commands.
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Number of commands currently being processed.
        /// </summary>
        public int ProcessingCount { get; set; }

        /// <summary>
        /// Number of completed commands.
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Number of failed commands.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of cancelled commands.
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// Number of expired commands.
        /// </summary>
        public int ExpiredCount { get; set; }

        /// <summary>
        /// Total number of commands.
        /// </summary>
        public int TotalCount => PendingCount + ProcessingCount + CompletedCount + FailedCount + CancelledCount + ExpiredCount;

        /// <summary>
        /// Oldest pending command date.
        /// </summary>
        public DateTime? OldestPendingAt { get; set; }

        /// <summary>
        /// Number of commands waiting to be retried.
        /// </summary>
        public int RetryPendingCount { get; set; }
    }
}

