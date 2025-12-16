//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Interface for message deduplication storage.
    /// Used to track processed message IDs and prevent duplicate processing.
    /// </summary>
    public interface IMessageDeduplicationStore
    {
        /// <summary>
        /// Checks if a message has already been processed.
        /// </summary>
        /// <param name="messageId">The unique message identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was already processed, false otherwise.</returns>
        Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as processed.
        /// </summary>
        /// <param name="messageId">The unique message identifier.</param>
        /// <param name="expiresAt">Optional expiration time for the entry.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task MarkAsProcessedAsync(string messageId, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a message ID from the store (e.g., for cleanup or retry scenarios).
        /// </summary>
        /// <param name="messageId">The unique message identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up expired entries from the store.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
    }
}

