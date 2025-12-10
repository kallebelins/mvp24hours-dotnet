//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for integration events.
/// Integration events are used for communication between bounded contexts
/// via message brokers (RabbitMQ, Kafka, etc.).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Integration Events vs Domain Events:</strong>
/// <list type="bullet">
/// <item>Domain Events (<see cref="IDomainEvent"/>) - In-process events within a bounded context</item>
/// <item>Integration Events - Cross-process events between bounded contexts</item>
/// </list>
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item>Keep integration events immutable and serializable</item>
/// <item>Include correlation/causation IDs for tracing</item>
/// <item>Use outbox pattern for reliability</item>
/// <item>Version your events for backward compatibility</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define an integration event
/// public record OrderCreatedIntegrationEvent : IIntegrationEvent
/// {
///     public Guid Id { get; init; } = Guid.NewGuid();
///     public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
///     public string? CorrelationId { get; init; }
///     
///     public Guid OrderId { get; init; }
///     public string CustomerEmail { get; init; } = string.Empty;
///     public decimal TotalAmount { get; init; }
/// }
/// 
/// // Publish via outbox pattern
/// await _outbox.AddAsync(new OrderCreatedIntegrationEvent
/// {
///     OrderId = order.Id,
///     CustomerEmail = order.Customer.Email,
///     TotalAmount = order.TotalAmount
/// });
/// </code>
/// </example>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Handler interface for integration events.
/// </summary>
/// <typeparam name="TEvent">The type of integration event to handle.</typeparam>
/// <remarks>
/// <para>
/// Integration event handlers are typically invoked by message consumers
/// when receiving messages from a message broker.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler&lt;OrderCreatedIntegrationEvent&gt;
/// {
///     private readonly IEmailService _emailService;
///     
///     public OrderCreatedIntegrationEventHandler(IEmailService emailService)
///     {
///         _emailService = emailService;
///     }
///     
///     public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
///     {
///         await _emailService.SendOrderConfirmationAsync(
///             @event.CustomerEmail,
///             @event.OrderId);
///     }
/// }
/// </code>
/// </example>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handles the integration event.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base record for integration events with common properties.
/// </summary>
/// <remarks>
/// <para>
/// This base record provides standard properties that all integration events should have.
/// Inherit from this record to create your own integration events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record CustomerCreatedEvent : IntegrationEventBase
/// {
///     public Guid CustomerId { get; init; }
///     public string Name { get; init; } = string.Empty;
///     public string Email { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional causation ID linking to the event that caused this event.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Event type name for serialization/deserialization.
    /// </summary>
    public string EventType => GetType().Name;
}

