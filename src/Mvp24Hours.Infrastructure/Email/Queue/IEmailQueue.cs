//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Queue
{
    /// <summary>
    /// Interface for queuing email messages for asynchronous sending.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a way to queue email messages for background processing,
    /// allowing the application to continue without waiting for email sending to complete.
    /// This is useful for high-throughput scenarios or when email sending should not block
    /// the main application flow.
    /// </para>
    /// <para>
    /// <strong>Queue Benefits:</strong>
    /// <list type="bullet">
    /// <item><description>Non-blocking email sending</description></item>
    /// <item><description>Better error handling and retry logic</description></item>
    /// <item><description>Rate limiting and throttling</description></item>
    /// <item><description>Priority-based sending</description></item>
    /// <item><description>Batch processing for efficiency</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IEmailQueue
    {
        /// <summary>
        /// Enqueues an email message for asynchronous sending.
        /// </summary>
        /// <param name="message">The email message to queue.</param>
        /// <param name="priority">The priority of the email (higher priority = sent first).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The queue item ID.</returns>
        /// <remarks>
        /// This method adds the email to the queue and returns immediately. The email will
        /// be processed by a background worker service.
        /// </remarks>
        Task<string> EnqueueAsync(
            EmailMessage message,
            EmailPriority priority = EmailPriority.Normal,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enqueues an email message with a scheduled send time.
        /// </summary>
        /// <param name="message">The email message to queue.</param>
        /// <param name="scheduledSendTime">When to send the email.</param>
        /// <param name="priority">The priority of the email.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The queue item ID.</returns>
        Task<string> EnqueueScheduledAsync(
            EmailMessage message,
            DateTimeOffset scheduledSendTime,
            EmailPriority priority = EmailPriority.Normal,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a queued email.
        /// </summary>
        /// <param name="queueItemId">The queue item ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The queue item status.</returns>
        Task<EmailQueueItemStatus> GetStatusAsync(
            string queueItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a queued email (if not yet sent).
        /// </summary>
        /// <param name="queueItemId">The queue item ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        Task CancelAsync(
            string queueItemId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Status of an email queue item.
    /// </summary>
    public class EmailQueueItemStatus
    {
        /// <summary>
        /// Gets or sets the queue item ID.
        /// </summary>
        public string QueueItemId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status.
        /// </summary>
        public EmailQueueStatus Status { get; set; }

        /// <summary>
        /// Gets or sets when the email was queued.
        /// </summary>
        public DateTimeOffset QueuedAt { get; set; }

        /// <summary>
        /// Gets or sets when the email is scheduled to be sent (if scheduled).
        /// </summary>
        public DateTimeOffset? ScheduledSendTime { get; set; }

        /// <summary>
        /// Gets or sets when the email was sent (if sent).
        /// </summary>
        public DateTimeOffset? SentAt { get; set; }

        /// <summary>
        /// Gets or sets the number of send attempts.
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the last error message (if failed).
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Gets or sets the message ID from the email provider (if sent successfully).
        /// </summary>
        public string? MessageId { get; set; }
    }

    /// <summary>
    /// Email queue status.
    /// </summary>
    public enum EmailQueueStatus
    {
        /// <summary>
        /// Email is queued and waiting to be sent.
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Email is scheduled for future sending.
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// Email is currently being sent.
        /// </summary>
        Sending = 2,

        /// <summary>
        /// Email was sent successfully.
        /// </summary>
        Sent = 3,

        /// <summary>
        /// Email sending failed.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Email was cancelled.
        /// </summary>
        Cancelled = 5
    }
}

