//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Mediator-enabled domain event interface.
/// Extends the Core <see cref="CoreDomainEvent"/> with Mediator notification capabilities.
/// Domain events represent something that happened in the domain that other parts of the system might be interested in.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Domain Events vs Notifications:</strong>
/// Domain events extend <see cref="IMediatorNotification"/> because they are published
/// through the Mediator to multiple handlers. The difference is semantic:
/// <list type="bullet">
/// <item><c>IMediatorDomainEvent</c> - Events that originate from the domain layer and are published via Mediator</item>
/// <item><c>IMediatorNotification</c> - General in-process notifications</item>
/// <item><c>Core.IDomainEvent</c> - Base domain event interface without Mediator dependency</item>
/// </list>
/// </para>
/// <para>
/// <strong>Domain Events vs Integration Events:</strong>
/// <list type="bullet">
/// <item><c>IMediatorDomainEvent</c> - In-process events within a bounded context</item>
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
///     decimal TotalAmount) : MediatorDomainEventBase;
/// 
/// // Raise the event from an entity
/// public class Order : EntityBase&lt;int&gt;, IHasDomainEvents
/// {
///     private readonly List&lt;Core.IDomainEvent&gt; _domainEvents = new();
///     
///     public IReadOnlyCollection&lt;Core.IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public void Place()
///     {
///         Status = OrderStatus.Placed;
///         _domainEvents.Add(new OrderPlacedEvent(Id, CustomerEmail, TotalAmount));
///     }
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
/// }
/// </code>
/// </example>
public interface IMediatorDomainEvent : CoreDomainEvent, IMediatorNotification
{
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="IMediatorDomainEvent"/> for new code.
/// </summary>
[Obsolete("Use IMediatorDomainEvent instead. This alias will be removed in a future version.")]
public interface IDomainEvent : IMediatorDomainEvent
{
}

/// <summary>
/// Base record for Mediator-enabled domain events with common properties.
/// Provides a convenient base class with automatic timestamp.
/// </summary>
/// <example>
/// <code>
/// public record OrderPlacedEvent(int OrderId, decimal Amount) : MediatorDomainEventBase;
/// 
/// public record CustomerCreatedEvent : MediatorDomainEventBase
/// {
///     public int CustomerId { get; init; }
///     public string Email { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
public abstract record MediatorDomainEventBase : IMediatorDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="MediatorDomainEventBase"/> for new code.
/// </summary>
[Obsolete("Use MediatorDomainEventBase instead. This alias will be removed in a future version.")]
public abstract record DomainEventBase : MediatorDomainEventBase
{
}

/// <summary>
/// Defines a handler for a Mediator domain event of type <typeparamref name="TEvent"/>.
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
/// public class SendOrderConfirmationHandler : IMediatorDomainEventHandler&lt;OrderPlacedEvent&gt;
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
public interface IMediatorDomainEventHandler<in TEvent> : IMediatorNotificationHandler<TEvent>
    where TEvent : IMediatorDomainEvent
{
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="IMediatorDomainEventHandler{TEvent}"/> for new code.
/// </summary>
[Obsolete("Use IMediatorDomainEventHandler<TEvent> instead. This alias will be removed in a future version.")]
public interface IDomainEventHandler<in TEvent> : IMediatorDomainEventHandler<TEvent>
    where TEvent : IMediatorDomainEvent
{
}

