//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Interface for event-sourced aggregates.
/// An aggregate is a cluster of domain objects that can be treated as a single unit
/// for data changes.
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
/// </remarks>
/// <example>
/// <code>
/// public class Order : AggregateRoot&lt;Guid&gt;
/// {
///     public OrderStatus Status { get; private set; }
///     
///     protected override void Apply(IDomainEvent @event)
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
public interface IAggregate
{
    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the current version of the aggregate.
    /// The version is incremented each time an event is applied.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Gets the uncommitted events that have been raised but not yet persisted.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> UncommittedEvents { get; }

    /// <summary>
    /// Clears the uncommitted events after they have been persisted.
    /// </summary>
    void ClearUncommittedEvents();

    /// <summary>
    /// Loads the aggregate from historical events.
    /// </summary>
    /// <param name="events">The historical events to replay.</param>
    void LoadFromHistory(IEnumerable<IDomainEvent> events);
}

/// <summary>
/// Generic interface for event-sourced aggregates with typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IAggregate<TId> : IAggregate
{
    /// <summary>
    /// Gets or sets the typed identifier of the aggregate.
    /// </summary>
    new TId Id { get; }
}

/// <summary>
/// Interface for aggregates that support snapshots.
/// Snapshots allow faster aggregate reconstruction by storing
/// the aggregate state at a specific version.
/// </summary>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
public interface ISnapshotAggregate<TSnapshot> : IAggregate
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

