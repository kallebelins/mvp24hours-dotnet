//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Interface for Dead Letter Queue (DLQ) storage.
/// Stores messages that have exceeded the maximum retry attempts.
/// </summary>
/// <remarks>
/// <para>
/// The Dead Letter Queue is a mechanism for storing messages that cannot be processed
/// successfully after multiple retry attempts. This allows for:
/// <list type="bullet">
/// <item>Manual inspection and debugging of failed messages</item>
/// <item>Retry of failed messages after fixing the underlying issue</item>
/// <item>Audit trail of processing failures</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Move a failed message to DLQ
/// if (message.RetryCount >= maxRetries)
/// {
///     await _deadLetterStore.AddAsync(new DeadLetterMessage
///     {
///         OriginalMessageId = message.Id,
///         EventType = message.EventType,
///         Payload = message.Payload,
///         Error = "Max retries exceeded",
///         FailedAt = DateTime.UtcNow
///     });
/// }
/// </code>
/// </example>
public interface IDeadLetterStore
{
    /// <summary>
    /// Adds a message to the dead letter queue.
    /// </summary>
    /// <param name="message">The dead letter message to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dead letter messages.
    /// </summary>
    /// <param name="limit">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of dead letter messages.</returns>
    Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter messages by event type.
    /// </summary>
    /// <param name="eventType">The type of event to filter by.</param>
    /// <param name="limit">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of dead letter messages.</returns>
    Task<IReadOnlyList<DeadLetterMessage>> GetByEventTypeAsync(
        string eventType, 
        int limit = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dead letter message by ID.
    /// </summary>
    /// <param name="id">The ID of the dead letter message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dead letter message if found; otherwise, null.</returns>
    Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requeues a dead letter message for reprocessing.
    /// </summary>
    /// <param name="id">The ID of the dead letter message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was requeued; otherwise, false.</returns>
    Task<bool> RequeueAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a dead letter message as resolved (manually handled).
    /// </summary>
    /// <param name="id">The ID of the dead letter message.</param>
    /// <param name="resolution">Description of how the message was resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsResolvedAsync(Guid id, string resolution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter message.
    /// </summary>
    /// <param name="id">The ID of the dead letter message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of dead letter messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages in the dead letter queue.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message in the Dead Letter Queue.
/// </summary>
public sealed class DeadLetterMessage
{
    /// <summary>
    /// Unique identifier for this dead letter entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the original message that failed.
    /// </summary>
    public Guid OriginalMessageId { get; init; }

    /// <summary>
    /// The type of event.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// The serialized event payload.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// The error message that caused the failure.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// The full exception details.
    /// </summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>
    /// When the message was moved to the DLQ.
    /// </summary>
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Number of retry attempts before being moved to DLQ.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Current status of the dead letter message.
    /// </summary>
    public DeadLetterStatus Status { get; set; } = DeadLetterStatus.Pending;

    /// <summary>
    /// Resolution description if manually handled.
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// When the message was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The source/origin of the message.
    /// </summary>
    public string? Source { get; init; }
}

/// <summary>
/// Status of a dead letter message.
/// </summary>
public enum DeadLetterStatus
{
    /// <summary>
    /// Message is pending review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message has been requeued for processing.
    /// </summary>
    Requeued = 1,

    /// <summary>
    /// Message has been manually resolved.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Message has been deleted.
    /// </summary>
    Deleted = 3
}


