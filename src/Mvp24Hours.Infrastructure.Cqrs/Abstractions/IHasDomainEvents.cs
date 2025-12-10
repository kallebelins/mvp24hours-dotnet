//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Interface for entities or aggregates that can raise domain events.
/// Entities implementing this interface can accumulate domain events that will be
/// dispatched after the entity is persisted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pattern: Domain Event Publishing</strong>
/// </para>
/// <para>
/// Domain events are raised by entities/aggregates during state changes but are not
/// immediately published. Instead, they are accumulated and dispatched after the 
/// changes are successfully persisted to the database. This ensures:
/// <list type="bullet">
/// <item>Events are only published for successful transactions</item>
/// <item>Events contain the final state of the entity</item>
/// <item>Handlers can rely on data being committed</item>
/// </list>
/// </para>
/// <para>
/// <strong>Best Practice:</strong> Clear domain events after successful dispatch
/// to prevent duplicate processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : EntityBase&lt;int&gt;, IHasDomainEvents
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///     
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
///     
///     public void Place()
///     {
///         if (Status != OrderStatus.Draft)
///             throw new DomainException("Only draft orders can be placed.");
///             
///         Status = OrderStatus.Placed;
///         PlacedAt = DateTime.UtcNow;
///         
///         // Raise domain event
///         _domainEvents.Add(new OrderPlacedEvent(Id, CustomerEmail, TotalAmount, PlacedAt.Value));
///     }
///     
///     public void Ship(string trackingNumber)
///     {
///         if (Status != OrderStatus.Placed)
///             throw new DomainException("Only placed orders can be shipped.");
///             
///         Status = OrderStatus.Shipped;
///         TrackingNumber = trackingNumber;
///         ShippedAt = DateTime.UtcNow;
///         
///         _domainEvents.Add(new OrderShippedEvent(Id, TrackingNumber, ShippedAt.Value));
///     }
/// }
/// </code>
/// </example>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    /// <remarks>
    /// Returns a read-only view of the pending domain events.
    /// Events should be dispatched via <see cref="IDomainEventDispatcher"/> after
    /// the entity is persisted.
    /// </remarks>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events.
    /// </summary>
    /// <remarks>
    /// This method should be called after domain events have been successfully dispatched
    /// to prevent duplicate processing. The <see cref="IDomainEventDispatcher"/> typically
    /// calls this method automatically after dispatching.
    /// </remarks>
    void ClearDomainEvents();
}

/// <summary>
/// Interface for dispatching domain events from entities to their handlers.
/// </summary>
/// <remarks>
/// <para>
/// The domain event dispatcher is responsible for publishing domain events
/// accumulated in entities through the Mediator to all registered handlers.
/// </para>
/// <para>
/// <strong>Typical usage flow:</strong>
/// <list type="number">
/// <item>Entity raises domain events during state changes</item>
/// <item>Changes are persisted via UnitOfWork.SaveChanges()</item>
/// <item>Dispatcher collects events from modified entities</item>
/// <item>Dispatcher publishes events via IPublisher</item>
/// <item>Dispatcher clears events from entities</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using the dispatcher manually
/// var order = new Order();
/// order.Place();
/// 
/// await unitOfWork.SaveChangesAsync();
/// await domainEventDispatcher.DispatchEventsAsync(order, cancellationToken);
/// 
/// // Or using the extension method
/// await unitOfWork.SaveChangesWithEventsAsync(new[] { order }, cancellationToken);
/// </code>
/// </example>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all pending domain events from the specified entity.
    /// </summary>
    /// <param name="entity">The entity containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches all pending domain events from multiple entities.
    /// </summary>
    /// <param name="entities">The entities containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default);
}

