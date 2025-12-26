//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Queue
{
    /// <summary>
    /// In-memory implementation of email queue for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores queued emails in memory. It's suitable for testing,
    /// development, or single-instance applications. For production distributed scenarios,
    /// consider using a persistent queue (Redis, RabbitMQ, etc.).
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// - Not persistent (lost on application restart)
    /// - Not distributed (only works within a single application instance)
    /// - Limited scalability for high-volume scenarios
    /// </para>
    /// </remarks>
    public class InMemoryEmailQueue : IEmailQueue
    {
        private readonly ConcurrentDictionary<string, EmailQueueItem> _items = new();
        private int _counter = 0;

        /// <inheritdoc />
        public Task<string> EnqueueAsync(
            EmailMessage message,
            EmailPriority priority = EmailPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var queueItemId = GenerateQueueItemId();
            var item = new EmailQueueItem
            {
                QueueItemId = queueItemId,
                Message = message,
                Priority = priority,
                Status = EmailQueueStatus.Queued,
                QueuedAt = DateTimeOffset.UtcNow
            };

            _items.TryAdd(queueItemId, item);
            return Task.FromResult(queueItemId);
        }

        /// <inheritdoc />
        public Task<string> EnqueueScheduledAsync(
            EmailMessage message,
            DateTimeOffset scheduledSendTime,
            EmailPriority priority = EmailPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var queueItemId = GenerateQueueItemId();
            var item = new EmailQueueItem
            {
                QueueItemId = queueItemId,
                Message = message,
                Priority = priority,
                Status = EmailQueueStatus.Scheduled,
                QueuedAt = DateTimeOffset.UtcNow,
                ScheduledSendTime = scheduledSendTime
            };

            _items.TryAdd(queueItemId, item);
            return Task.FromResult(queueItemId);
        }

        /// <inheritdoc />
        public Task<EmailQueueItemStatus> GetStatusAsync(
            string queueItemId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueItemId))
            {
                throw new ArgumentException("Queue item ID cannot be null or empty.", nameof(queueItemId));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_items.TryGetValue(queueItemId, out var item))
            {
                throw new InvalidOperationException($"Queue item '{queueItemId}' not found.");
            }

            var status = new EmailQueueItemStatus
            {
                QueueItemId = item.QueueItemId,
                Status = item.Status,
                QueuedAt = item.QueuedAt,
                ScheduledSendTime = item.ScheduledSendTime,
                SentAt = item.SentAt,
                AttemptCount = item.AttemptCount,
                LastError = item.LastError,
                MessageId = item.MessageId
            };

            return Task.FromResult(status);
        }

        /// <inheritdoc />
        public Task CancelAsync(
            string queueItemId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueItemId))
            {
                throw new ArgumentException("Queue item ID cannot be null or empty.", nameof(queueItemId));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_items.TryGetValue(queueItemId, out var item))
            {
                throw new InvalidOperationException($"Queue item '{queueItemId}' not found.");
            }

            if (item.Status == EmailQueueStatus.Sent)
            {
                throw new InvalidOperationException($"Cannot cancel queue item '{queueItemId}' - already sent.");
            }

            item.Status = EmailQueueStatus.Cancelled;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the next queued item to process (for background worker).
        /// </summary>
        internal IEmailQueueItem? GetNextItem()
        {
            var now = DateTimeOffset.UtcNow;
            var availableItems = _items.Values
                .Where(item => item.Status == EmailQueueStatus.Queued ||
                              (item.Status == EmailQueueStatus.Scheduled && item.ScheduledSendTime <= now))
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.QueuedAt)
                .FirstOrDefault();

            return availableItems;
        }

        /// <summary>
        /// Marks an item as sending (for background worker).
        /// </summary>
        internal void MarkAsSending(string queueItemId)
        {
            if (_items.TryGetValue(queueItemId, out var item))
            {
                item.Status = EmailQueueStatus.Sending;
                item.AttemptCount++;
            }
        }

        /// <summary>
        /// Marks an item as sent (for background worker).
        /// </summary>
        internal void MarkAsSent(string queueItemId, string? messageId = null)
        {
            if (_items.TryGetValue(queueItemId, out var item))
            {
                item.Status = EmailQueueStatus.Sent;
                item.SentAt = DateTimeOffset.UtcNow;
                item.MessageId = messageId;
            }
        }

        /// <summary>
        /// Marks an item as failed (for background worker).
        /// </summary>
        internal void MarkAsFailed(string queueItemId, string error)
        {
            if (_items.TryGetValue(queueItemId, out var item))
            {
                item.Status = EmailQueueStatus.Failed;
                item.LastError = error;
            }
        }

        private string GenerateQueueItemId()
        {
            var id = Interlocked.Increment(ref _counter);
            return $"email-queue-{id}-{Guid.NewGuid():N}";
        }

        private class EmailQueueItem : IEmailQueueItem
        {
            public string QueueItemId { get; set; } = string.Empty;
            public EmailMessage Message { get; set; } = null!;
            public EmailPriority Priority { get; set; }
            public EmailQueueStatus Status { get; set; }
            public DateTimeOffset QueuedAt { get; set; }
            public DateTimeOffset? ScheduledSendTime { get; set; }
            public DateTimeOffset? SentAt { get; set; }
            public int AttemptCount { get; set; }
            public string? LastError { get; set; }
            public string? MessageId { get; set; }
        }
    }

    /// <summary>
    /// Internal interface for email queue items (used by queue processor).
    /// </summary>
    internal interface IEmailQueueItem
    {
        string QueueItemId { get; }
        EmailMessage Message { get; }
        EmailPriority Priority { get; }
        EmailQueueStatus Status { get; }
        DateTimeOffset QueuedAt { get; }
        DateTimeOffset? ScheduledSendTime { get; }
        DateTimeOffset? SentAt { get; }
        int AttemptCount { get; }
        string? LastError { get; }
        string? MessageId { get; }
    }
}

