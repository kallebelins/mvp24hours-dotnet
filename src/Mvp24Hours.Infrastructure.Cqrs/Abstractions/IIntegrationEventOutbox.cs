//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Interface for the Integration Event Outbox.
/// Implements the Outbox Pattern for reliable event publishing.
/// </summary>
/// <remarks>
/// <para>
/// The Outbox Pattern ensures that domain changes and events are published
/// atomically within the same database transaction.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>When a domain operation completes, events are stored in an outbox table</item>
/// <item>A background process reads pending events from the outbox</item>
/// <item>Events are published to the message broker</item>
/// <item>Successfully published events are marked as processed</item>
/// </list>
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Guaranteed delivery of events</item>
/// <item>Atomic consistency between data and events</item>
/// <item>Resilience to message broker failures</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your command handler
/// public async Task Handle(CreateOrderCommand command, CancellationToken ct)
/// {
///     var order = new Order(...);
///     await _repository.AddAsync(order);
///     
///     // Add event to outbox (will be published later)
///     await _outbox.AddAsync(new OrderCreatedIntegrationEvent
///     {
///         OrderId = order.Id,
///         CustomerEmail = order.Customer.Email
///     });
///     
///     await _unitOfWork.SaveChangesAsync(ct);
/// }
/// </code>
/// </example>
public interface IIntegrationEventOutbox
{
    /// <summary>
    /// Adds an integration event to the outbox for later publishing.
    /// </summary>
    /// <typeparam name="TEvent">The type of integration event.</typeparam>
    /// <param name="event">The event to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Gets pending (unpublished) events from the outbox.
    /// </summary>
    /// <param name="batchSize">Maximum number of events to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event as successfully published.
    /// </summary>
    /// <param name="messageId">The ID of the outbox message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event as failed.
    /// </summary>
    /// <param name="messageId">The ID of the outbox message.</param>
    /// <param name="error">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed messages.
    /// </summary>
    /// <param name="olderThan">Delete messages processed before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages deleted.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message in the outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of integration event.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Serialized event payload.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the message was processed (if any).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Current status of the message.
    /// </summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Status of an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Message is pending publication.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message has been successfully published.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Message failed to publish (will be retried).
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Message has exceeded retry limit.
    /// </summary>
    DeadLetter = 3
}

