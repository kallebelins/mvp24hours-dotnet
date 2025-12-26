//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Contract
{
    /// <summary>
    /// Interface for rate limiting SMS messages by destination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rate limiting prevents sending too many messages to the same recipient within a time period.
    /// This helps prevent spam, reduces costs, and ensures compliance with carrier regulations.
    /// </para>
    /// <para>
    /// <strong>Rate Limiting Strategies:</strong>
    /// - Per destination: Limit messages per phone number
    /// - Per sender: Limit messages from a specific sender
    /// - Global: Limit total messages across all destinations
    /// </para>
    /// </remarks>
    public interface ISmsRateLimiter
    {
        /// <summary>
        /// Checks if sending a message to the specified destination is allowed.
        /// </summary>
        /// <param name="destination">The destination phone number.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>True if sending is allowed; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the rate limit for the destination has been exceeded.
        /// If false is returned, the message should not be sent.
        /// </remarks>
        Task<bool> IsAllowedAsync(string destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that a message was sent to the specified destination.
        /// </summary>
        /// <param name="destination">The destination phone number.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method should be called after successfully sending a message to update
        /// the rate limit counters.
        /// </remarks>
        Task RecordSentAsync(string destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the number of messages sent to the destination in the current time window.
        /// </summary>
        /// <param name="destination">The destination phone number.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The count of messages sent in the current time window.</returns>
        Task<int> GetCountAsync(string destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the rate limit for the specified destination.
        /// </summary>
        /// <param name="destination">The destination phone number.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method clears the rate limit counters for the destination, allowing
        /// immediate sending (useful for testing or manual overrides).
        /// </remarks>
        Task ResetAsync(string destination, CancellationToken cancellationToken = default);
    }
}

