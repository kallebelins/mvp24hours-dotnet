//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Interface for publishing integration events to a message broker.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface handle the actual publishing
/// of integration events to external message brokers like RabbitMQ, Kafka, etc.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// For reliable publishing, use <see cref="IIntegrationEventOutbox"/> instead
/// of publishing directly. Direct publishing should only be used for
/// non-critical events where occasional loss is acceptable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Direct publishing (not recommended for critical events)
/// await _publisher.PublishAsync(new OrderCreatedIntegrationEvent
/// {
///     OrderId = order.Id
/// });
/// 
/// // Preferred: Use outbox pattern
/// await _outbox.AddAsync(new OrderCreatedIntegrationEvent { ... });
/// await _unitOfWork.SaveChangesAsync();
/// </code>
/// </example>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to the message broker.
    /// </summary>
    /// <typeparam name="TEvent">The type of integration event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Publishes an integration event from an outbox message.
    /// </summary>
    /// <param name="message">The outbox message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishFromOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for converting Domain Events to Integration Events.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables automatic conversion of domain events to integration events
/// when a domain event should trigger communication across bounded contexts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedDomainToIntegrationEventConverter 
///     : IDomainToIntegrationEventConverter&lt;OrderCreatedDomainEvent, OrderCreatedIntegrationEvent&gt;
/// {
///     public OrderCreatedIntegrationEvent? Convert(OrderCreatedDomainEvent domainEvent)
///     {
///         return new OrderCreatedIntegrationEvent
///         {
///             OrderId = domainEvent.OrderId,
///             CustomerEmail = domainEvent.CustomerEmail,
///             CorrelationId = domainEvent.Id.ToString()
///         };
///     }
/// }
/// </code>
/// </example>
public interface IDomainToIntegrationEventConverter<in TDomainEvent, out TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Converts a domain event to an integration event.
    /// </summary>
    /// <param name="domainEvent">The domain event to convert.</param>
    /// <returns>The integration event, or null if no conversion should occur.</returns>
    TIntegrationEvent? Convert(TDomainEvent domainEvent);
}

