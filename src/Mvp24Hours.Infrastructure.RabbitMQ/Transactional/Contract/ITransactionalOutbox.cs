//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract
{
    /// <summary>
    /// Interface for transactional outbox storage specifically for RabbitMQ messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends the generic outbox pattern to handle RabbitMQ-specific
    /// message properties like routing keys, exchange names, and message headers.
    /// </para>
    /// <para>
    /// <strong>Implementation Notes:</strong>
    /// <list type="bullet">
    /// <item>Implementations should store messages in the same database transaction as business data</item>
    /// <item>Messages should be stored with all necessary metadata for RabbitMQ publishing</item>
    /// <item>Support for message deduplication via message ID is recommended</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ITransactionalOutbox
    {
        /// <summary>
        /// Adds a message to the outbox.
        /// </summary>
        /// <param name="message">The outbox message to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple messages to the outbox in a batch.
        /// </summary>
        /// <param name="messages">The outbox messages to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddRangeAsync(IEnumerable<TransactionalOutboxMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets pending messages from the outbox that are ready for publishing.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of pending outbox messages.</returns>
        Task<IReadOnlyList<TransactionalOutboxMessage>> GetPendingAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as successfully published.
        /// </summary>
        /// <param name="messageId">The ID of the message to mark as published.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as failed with an error description.
        /// </summary>
        /// <param name="messageId">The ID of the message that failed.</param>
        /// <param name="error">The error message describing the failure.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of pending messages in the outbox.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of pending messages.</returns>
        Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old processed messages from the outbox.
        /// </summary>
        /// <param name="olderThan">Delete messages processed before this date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of messages deleted.</returns>
        Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets messages that have exceeded the retry limit (dead letters).
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of dead letter messages.</returns>
        Task<IReadOnlyList<TransactionalOutboxMessage>> GetDeadLettersAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a message stored in the transactional outbox for RabbitMQ.
    /// </summary>
    public sealed class TransactionalOutboxMessage
    {
        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The fully qualified type name of the message.
        /// </summary>
        public string MessageType { get; init; } = string.Empty;

        /// <summary>
        /// The serialized message payload (typically JSON).
        /// </summary>
        public string Payload { get; init; } = string.Empty;

        /// <summary>
        /// The routing key for RabbitMQ message routing.
        /// </summary>
        public string? RoutingKey { get; init; }

        /// <summary>
        /// The exchange name to publish to.
        /// </summary>
        public string? Exchange { get; init; }

        /// <summary>
        /// Serialized message headers (JSON dictionary).
        /// </summary>
        public string? Headers { get; init; }

        /// <summary>
        /// Correlation ID for tracing related messages.
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Causation ID for tracking event causality.
        /// </summary>
        public string? CausationId { get; init; }

        /// <summary>
        /// When the message was created/staged.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// When the message was successfully published.
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Current status of the message.
        /// </summary>
        public TransactionalOutboxStatus Status { get; set; } = TransactionalOutboxStatus.Pending;

        /// <summary>
        /// Number of publish retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Error message from the last failed attempt.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Next retry time (for exponential backoff).
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Optional scheduled publish time.
        /// </summary>
        public DateTime? ScheduledAt { get; init; }

        /// <summary>
        /// Message priority (0-255, higher is more important).
        /// </summary>
        public byte Priority { get; init; }

        /// <summary>
        /// Tenant ID for multi-tenant scenarios.
        /// </summary>
        public string? TenantId { get; init; }
    }

    /// <summary>
    /// Status of a transactional outbox message.
    /// </summary>
    public enum TransactionalOutboxStatus
    {
        /// <summary>
        /// Message is pending publication.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Message is currently being processed.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Message has been successfully published.
        /// </summary>
        Published = 2,

        /// <summary>
        /// Message failed to publish (will be retried).
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Message has exceeded retry limit.
        /// </summary>
        DeadLetter = 4,

        /// <summary>
        /// Message is scheduled for future publication.
        /// </summary>
        Scheduled = 5
    }
}

