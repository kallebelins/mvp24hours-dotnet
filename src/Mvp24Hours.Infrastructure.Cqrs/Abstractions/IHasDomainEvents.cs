//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Mediator-enabled interface for entities or aggregates that can raise domain events.
/// Extends the Core <see cref="CoreHasDomainEvents"/> with Mediator-specific requirements.
/// Entities implementing this interface can accumulate domain events that will be
/// dispatched via Mediator after the entity is persisted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pattern: Domain Event Publishing via Mediator</strong>
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
/// <para>
/// <strong>Note:</strong> Use <see cref="CoreHasDomainEvents"/> from Core if you don't need Mediator integration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : EntityBase&lt;int&gt;, IMediatorHasDomainEvents
/// {
///     private readonly List&lt;IMediatorDomainEvent&gt; _domainEvents = new();
///     
///     public IReadOnlyCollection&lt;IMediatorDomainEvent&gt; MediatorDomainEvents => _domainEvents.AsReadOnly();
///     IReadOnlyCollection&lt;CoreDomainEvent&gt; CoreHasDomainEvents.DomainEvents => 
///         _domainEvents.Cast&lt;CoreDomainEvent&gt;().ToList().AsReadOnly();
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
///     
///     public void Place()
///     {
///         if (Status != OrderStatus.Draft)
///             throw new DomainException("Only draft orders can be placed.");
///             
///         Status = OrderStatus.Placed;
///         _domainEvents.Add(new OrderPlacedEvent(Id, CustomerEmail, TotalAmount));
///     }
/// }
/// </code>
/// </example>
public interface IMediatorHasDomainEvents : CoreHasDomainEvents
{
    /// <summary>
    /// Gets the collection of Mediator domain events raised by this entity.
    /// </summary>
    /// <remarks>
    /// Returns a read-only view of the pending domain events.
    /// Events should be dispatched via <see cref="IDomainEventDispatcher"/> after
    /// the entity is persisted.
    /// </remarks>
    IReadOnlyCollection<IMediatorDomainEvent> MediatorDomainEvents { get; }
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="IMediatorHasDomainEvents"/> for new code.
/// </summary>
[Obsolete("Use IMediatorHasDomainEvents instead. This alias will be removed in a future version.")]
public interface IHasDomainEvents : CoreHasDomainEvents
{
}

/// <summary>
/// Interface for dispatching domain events from entities to their handlers via Mediator.
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
    Task DispatchEventsAsync(CoreHasDomainEvents entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches all pending domain events from multiple entities.
    /// </summary>
    /// <param name="entities">The entities containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchEventsAsync(IEnumerable<CoreHasDomainEvents> entities, CancellationToken cancellationToken = default);
}

