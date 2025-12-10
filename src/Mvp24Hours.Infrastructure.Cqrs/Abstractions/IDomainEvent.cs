//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that other parts of the system might be interested in.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Domain Events vs Notifications:</strong>
/// Domain events extend <see cref="IMediatorNotification"/> because they are published
/// through the Mediator to multiple handlers. The difference is semantic:
/// <list type="bullet">
/// <item><c>IDomainEvent</c> - Events that originate from the domain layer (entities/aggregates)</item>
/// <item><c>IMediatorNotification</c> - General in-process notifications</item>
/// </list>
/// </para>
/// <para>
/// <strong>Domain Events vs Integration Events:</strong>
/// <list type="bullet">
/// <item><c>IDomainEvent</c> - In-process events within a bounded context</item>
/// <item><c>IBusinessEvent</c> (existing) - Events that cross bounded contexts via message broker</item>
/// </list>
/// </para>
/// <para>
/// <strong>Common Use Cases:</strong>
/// <list type="bullet">
/// <item>OrderPlaced, OrderShipped, OrderCancelled</item>
/// <item>UserRegistered, UserEmailVerified</item>
/// <item>PaymentReceived, PaymentFailed</item>
/// <item>InventoryUpdated, StockLevelCritical</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a domain event
/// public record OrderPlacedEvent(
///     int OrderId,
///     string CustomerEmail,
///     decimal TotalAmount,
///     DateTime OccurredAt) : IDomainEvent;
/// 
/// // Raise the event from an entity
/// public class Order : EntityBase&lt;int&gt;, IHasDomainEvents
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///     
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public void Place()
///     {
///         Status = OrderStatus.Placed;
///         _domainEvents.Add(new OrderPlacedEvent(Id, CustomerEmail, TotalAmount, DateTime.UtcNow));
///     }
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
/// }
/// </code>
/// </example>
public interface IDomainEvent : IMediatorNotification
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base record for domain events with common properties.
/// Provides a convenient base class with automatic timestamp.
/// </summary>
/// <example>
/// <code>
/// public record OrderPlacedEvent(int OrderId, decimal Amount) : DomainEventBase;
/// 
/// public record CustomerCreatedEvent : DomainEventBase
/// {
///     public int CustomerId { get; init; }
///     public string Email { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a unique identifier for this event instance.
    /// Useful for idempotency and event tracking.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Defines a handler for a domain event of type <typeparamref name="TEvent"/>.
/// This is an alias for <see cref="IMediatorNotificationHandler{TNotification}"/> 
/// that provides semantic clarity when working with domain events.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
/// <remarks>
/// Multiple handlers can process the same domain event, enabling
/// different parts of the system to react to domain changes.
/// </remarks>
/// <example>
/// <code>
/// public class SendOrderConfirmationHandler : IDomainEventHandler&lt;OrderPlacedEvent&gt;
/// {
///     private readonly IEmailService _emailService;
///     
///     public SendOrderConfirmationHandler(IEmailService emailService)
///     {
///         _emailService = emailService;
///     }
///     
///     public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
///     {
///         await _emailService.SendOrderConfirmationAsync(
///             notification.CustomerEmail,
///             notification.OrderId,
///             cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IDomainEventHandler<in TEvent> : IMediatorNotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
}

