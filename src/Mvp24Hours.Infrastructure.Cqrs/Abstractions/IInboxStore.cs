//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Interface for the Inbox Store.
/// Implements the Inbox Pattern for idempotent message processing.
/// </summary>
/// <remarks>
/// <para>
/// The Inbox Pattern ensures that messages are processed exactly once,
/// even if they are delivered multiple times by the message broker.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>Consumer receives a message from the broker</item>
/// <item>Check if the message ID already exists in the inbox</item>
/// <item>If exists → Message already processed, skip (deduplicate)</item>
/// <item>If not → Process and store the message ID in the inbox</item>
/// </list>
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Exactly-once processing semantics</item>
/// <item>Protection against message redelivery</item>
/// <item>Idempotent message handling</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your integration event handler
/// public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
/// {
///     // Check for duplicate
///     if (await _inbox.ExistsAsync(@event.Id, ct))
///     {
///         return; // Already processed
///     }
///     
///     // Process the event
///     await _inventoryService.ReserveItemsAsync(@event.Items);
///     
///     // Mark as processed
///     await _inbox.MarkAsProcessedAsync(@event.Id, typeof(OrderCreatedEvent).Name, ct);
/// }
/// </code>
/// </example>
public interface IInboxStore
{
    /// <summary>
    /// Checks if a message has already been processed.
    /// </summary>
    /// <param name="messageId">The unique ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was already processed; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The unique ID of the message.</param>
    /// <param name="messageType">The type of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the inbox message by ID.
    /// </summary>
    /// <param name="messageId">The unique ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inbox message if found; otherwise, null.</returns>
    Task<InboxMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inbox messages within a time range.
    /// </summary>
    /// <param name="from">Start of the time range.</param>
    /// <param name="to">End of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of inbox messages.</returns>
    Task<IReadOnlyList<InboxMessage>> GetByTimeRangeAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed messages.
    /// </summary>
    /// <param name="olderThan">Delete messages processed before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages deleted.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message in the inbox.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Unique identifier for the message (same as the original message ID).
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of message.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// When the message was processed.
    /// </summary>
    public DateTime ProcessedAt { get; init; }

    /// <summary>
    /// The consumer/handler that processed this message.
    /// </summary>
    public string? ConsumerName { get; init; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}


