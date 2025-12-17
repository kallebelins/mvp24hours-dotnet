//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Interface for storing and managing scheduled messages.
    /// </summary>
    public interface IScheduledMessageStore
    {
        /// <summary>
        /// Adds a new scheduled message to the store.
        /// </summary>
        /// <param name="message">The scheduled message to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(ScheduledMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing scheduled message.
        /// </summary>
        /// <param name="message">The scheduled message to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(ScheduledMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a scheduled message by ID.
        /// </summary>
        /// <param name="id">The ID of the scheduled message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled message or null if not found.</returns>
        Task<ScheduledMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all messages due for delivery (scheduled time has passed).
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of due messages.</returns>
        Task<IEnumerable<ScheduledMessage>> GetDueMessagesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending messages (not yet delivered).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of pending messages.</returns>
        Task<IEnumerable<ScheduledMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active recurring messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of active recurring messages.</returns>
        Task<IEnumerable<ScheduledMessage>> GetActiveRecurringMessagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets messages by status.
        /// </summary>
        /// <param name="status">The status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of messages with the specified status.</returns>
        Task<IEnumerable<ScheduledMessage>> GetByStatusAsync(
            ScheduledMessageStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a scheduled message from the store.
        /// </summary>
        /// <param name="id">The ID of the message to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was removed; false if not found.</returns>
        Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as processing.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was marked; false if not found or already processing.</returns>
        Task<bool> MarkAsProcessingAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as completed.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkAsCompletedAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as failed.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="error">The error message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old completed and failed messages.
        /// </summary>
        /// <param name="olderThan">Remove messages older than this time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of messages removed.</returns>
        Task<int> CleanupOldMessagesAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of messages by status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary with status counts.</returns>
        Task<Dictionary<ScheduledMessageStatus, int>> GetStatusCountsAsync(
            CancellationToken cancellationToken = default);
    }
}

