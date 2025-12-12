//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;
using CoreVersionedAggregate = Mvp24Hours.Core.Contract.Domain.Entity.IVersionedAggregate;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Interface for event-sourced aggregates.
/// An aggregate is a cluster of domain objects that can be treated as a single unit
/// for data changes. Extends the Core <see cref="CoreVersionedAggregate"/> with event sourcing capabilities.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Event Sourcing Pattern:</strong>
/// Instead of storing current state, aggregates store and replay events.
/// The current state is derived by applying all historical events in order.
/// </para>
/// <para>
/// <strong>Aggregate Invariants:</strong>
/// <list type="bullet">
/// <item>An aggregate has a unique identifier</item>
/// <item>An aggregate has a version for optimistic concurrency</item>
/// <item>All state changes produce domain events</item>
/// <item>Events are applied in order to reconstruct state</item>
/// </list>
/// </para>
/// <para>
/// <strong>Core vs CQRS Aggregates:</strong>
/// <list type="bullet">
/// <item><c>Core.IAggregateRoot</c> - Basic DDD aggregate root marker</item>
/// <item><c>Core.IVersionedAggregate</c> - Aggregate with version for concurrency</item>
/// <item><c>IEventSourcedAggregate</c> - Full event sourcing support</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : AggregateRoot&lt;Guid&gt;
/// {
///     public OrderStatus Status { get; private set; }
///     
///     protected override void Apply(CoreDomainEvent @event)
///     {
///         switch (@event)
///         {
///             case OrderCreatedEvent e:
///                 Id = e.OrderId;
///                 Status = OrderStatus.Created;
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IEventSourcedAggregate : CoreVersionedAggregate, CoreHasDomainEvents
{
    /// <summary>
    /// Gets the uncommitted events that have been raised but not yet persisted.
    /// </summary>
    IReadOnlyCollection<CoreDomainEvent> UncommittedEvents { get; }

    /// <summary>
    /// Clears the uncommitted events after they have been persisted.
    /// </summary>
    void ClearUncommittedEvents();

    /// <summary>
    /// Loads the aggregate from historical events.
    /// </summary>
    /// <param name="events">The historical events to replay.</param>
    void LoadFromHistory(IEnumerable<CoreDomainEvent> events);
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="IEventSourcedAggregate"/> for new code.
/// </summary>
[Obsolete("Use IEventSourcedAggregate instead. This alias will be removed in a future version.")]
public interface IAggregate : IEventSourcedAggregate
{
    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    new Guid Id { get; }
}

/// <summary>
/// Generic interface for event-sourced aggregates with typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IEventSourcedAggregate<TId> : IEventSourcedAggregate, IAggregateRoot<TId>
{
}

/// <summary>
/// Alias for backward compatibility. Use <see cref="IEventSourcedAggregate{TId}"/> for new code.
/// </summary>
[Obsolete("Use IEventSourcedAggregate<TId> instead. This alias will be removed in a future version.")]
public interface IAggregate<TId> : IEventSourcedAggregate<TId>
{
}

/// <summary>
/// Interface for aggregates that support snapshots.
/// Snapshots allow faster aggregate reconstruction by storing
/// the aggregate state at a specific version.
/// </summary>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
public interface ISnapshotAggregate<TSnapshot> : IEventSourcedAggregate
    where TSnapshot : class
{
    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>A snapshot object representing the current state.</returns>
    TSnapshot CreateSnapshot();

    /// <summary>
    /// Restores the aggregate state from a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="version">The version at which the snapshot was taken.</param>
    void RestoreFromSnapshot(TSnapshot snapshot, long version);
}

/// <summary>
/// Factory interface for creating aggregates.
/// Used by repositories to instantiate aggregates from events.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate to create.</typeparam>
public interface IAggregateFactory<TAggregate>
    where TAggregate : IAggregate
{
    /// <summary>
    /// Creates a new instance of the aggregate.
    /// </summary>
    /// <returns>A new aggregate instance.</returns>
    TAggregate Create();
}

/// <summary>
/// Default aggregate factory that uses parameterless constructor.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate to create.</typeparam>
public class DefaultAggregateFactory<TAggregate> : IAggregateFactory<TAggregate>
    where TAggregate : IAggregate, new()
{
    /// <inheritdoc />
    public TAggregate Create() => new TAggregate();
}

