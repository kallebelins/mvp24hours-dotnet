//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;
using CoreIEntityBase = Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Base class for event-sourced aggregates.
/// Provides the infrastructure for raising and applying domain events.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <list type="number">
/// <item>Define events as records implementing <see cref="CoreDomainEvent"/></item>
/// <item>Override <see cref="Apply"/> to handle each event type</item>
/// <item>Use <see cref="Raise"/> to emit new events</item>
/// <item>Use <see cref="LoadFromHistory"/> to reconstruct from persisted events</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong>
/// <list type="bullet">
/// <item>All state changes MUST happen through events</item>
/// <item>The <see cref="Apply"/> method must be deterministic</item>
/// <item>Never throw exceptions in <see cref="Apply"/> - validate in command methods</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : AggregateRoot&lt;Guid&gt;
/// {
///     public OrderStatus Status { get; private set; }
///     public decimal TotalAmount { get; private set; }
///     
///     // Private constructor for reconstruction
///     private Order() { }
///     
///     // Factory method for creation
///     public static Order Create(string customerEmail)
///     {
///         var order = new Order();
///         order.Raise(new OrderCreatedEvent
///         {
///             OrderId = Guid.NewGuid(),
///             CustomerEmail = customerEmail
///         });
///         return order;
///     }
///     
///     public void Ship(string trackingNumber)
///     {
///         if (Status != OrderStatus.Paid)
///             throw new DomainException("Order must be paid before shipping");
///         
///         Raise(new OrderShippedEvent
///         {
///             OrderId = Id,
///             TrackingNumber = trackingNumber
///         });
///     }
///     
///     protected override void Apply(CoreDomainEvent @event)
///     {
///         switch (@event)
///         {
///             case OrderCreatedEvent e:
///                 Id = e.OrderId;
///                 Status = OrderStatus.Created;
///                 break;
///             case OrderShippedEvent:
///                 Status = OrderStatus.Shipped;
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class AggregateRoot : IEventSourcedAggregate
{
    private readonly List<CoreDomainEvent> _uncommittedEvents = new();
    private long _version;
    private Guid _id;

    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    public Guid Id
    {
        get => _id;
        protected set => _id = value;
    }

    /// <summary>
    /// Gets the entity key (for IEntityBase compatibility).
    /// </summary>
    object CoreIEntityBase.EntityKey => _id;

    /// <summary>
    /// Gets the current version of the aggregate.
    /// The version is incremented each time an event is applied.
    /// </summary>
    public long Version => _version;

    /// <summary>
    /// Gets the uncommitted events that have been raised but not yet persisted.
    /// </summary>
    public IReadOnlyCollection<CoreDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Gets the domain events for IHasDomainEvents compatibility.
    /// </summary>
    IReadOnlyCollection<CoreDomainEvent> CoreHasDomainEvents.DomainEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Gets whether the aggregate has uncommitted events.
    /// </summary>
    public bool HasUncommittedEvents => _uncommittedEvents.Count > 0;

    /// <summary>
    /// Gets whether this is a new aggregate (version 0 with uncommitted events).
    /// </summary>
    public bool IsNew => _version == _uncommittedEvents.Count && _uncommittedEvents.Count > 0;

    /// <summary>
    /// Clears the uncommitted events after they have been persisted.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Clears domain events (IHasDomainEvents implementation).
    /// </summary>
    void CoreHasDomainEvents.ClearDomainEvents() => ClearUncommittedEvents();

    /// <summary>
    /// Loads the aggregate from historical events.
    /// This method is used to reconstruct the aggregate state from persisted events.
    /// </summary>
    /// <param name="events">The historical events to replay.</param>
    /// <exception cref="ArgumentNullException">Thrown when events is null.</exception>
    public void LoadFromHistory(IEnumerable<CoreDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            Apply(@event);
            _version++;
        }
    }

    /// <summary>
    /// Raises a new domain event.
    /// The event is applied to update the aggregate state and added to uncommitted events.
    /// </summary>
    /// <param name="event">The event to raise.</param>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
    protected void Raise(CoreDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        Apply(@event);
        _uncommittedEvents.Add(@event);
        _version++;
    }

    /// <summary>
    /// Applies an event to update the aggregate state.
    /// This method must be overridden to handle each event type.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    /// <remarks>
    /// <para>
    /// <strong>Implementation Guidelines:</strong>
    /// <list type="bullet">
    /// <item>Use pattern matching to handle different event types</item>
    /// <item>Update only the relevant state for each event</item>
    /// <item>This method must be deterministic - same input, same output</item>
    /// <item>Do not throw exceptions - validation should happen before raising events</item>
    /// <item>Do not call external services or perform I/O</item>
    /// </list>
    /// </para>
    /// </remarks>
    protected abstract void Apply(CoreDomainEvent @event);
}

/// <summary>
/// Generic base class for event-sourced aggregates with typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
/// <example>
/// <code>
/// public class Product : AggregateRoot&lt;int&gt;
/// {
///     public string Name { get; private set; } = string.Empty;
///     
///     protected override void Apply(CoreDomainEvent @event)
///     {
///         switch (@event)
///         {
///             case ProductCreatedEvent e:
///                 Id = e.ProductId;
///                 Name = e.Name;
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class AggregateRoot<TId> : IEventSourcedAggregate<TId>
{
    private readonly List<CoreDomainEvent> _uncommittedEvents = new();
    private long _version;

    /// <summary>
    /// Gets or sets the typed identifier of the aggregate.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Explicit implementation of IEventSourcedAggregate.Id.
    /// Converts the typed Id to Guid. If TId is Guid, returns it directly;
    /// otherwise returns a deterministic Guid based on the Id's hash code.
    /// </summary>
    Guid IEventSourcedAggregate.Id => Id switch
    {
        Guid guid => guid,
        _ => Id?.GetHashCode() is int hash 
            ? new Guid(hash, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            : Guid.Empty
    };

    /// <summary>
    /// Gets the entity key (for IEntityBase compatibility).
    /// </summary>
    object CoreIEntityBase.EntityKey => Id!;

    /// <summary>
    /// Gets the current version of the aggregate.
    /// </summary>
    public long Version => _version;

    /// <summary>
    /// Gets the uncommitted events.
    /// </summary>
    public IReadOnlyCollection<CoreDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Gets the domain events for IHasDomainEvents compatibility.
    /// </summary>
    IReadOnlyCollection<CoreDomainEvent> CoreHasDomainEvents.DomainEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Gets whether the aggregate has uncommitted events.
    /// </summary>
    public bool HasUncommittedEvents => _uncommittedEvents.Count > 0;

    /// <summary>
    /// Clears uncommitted events after persistence.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Clears domain events (IHasDomainEvents implementation).
    /// </summary>
    void CoreHasDomainEvents.ClearDomainEvents() => ClearUncommittedEvents();

    /// <summary>
    /// Loads the aggregate from historical events.
    /// </summary>
    /// <param name="events">The historical events to replay.</param>
    public void LoadFromHistory(IEnumerable<CoreDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            Apply(@event);
            _version++;
        }
    }

    /// <summary>
    /// Raises a new domain event.
    /// </summary>
    /// <param name="event">The event to raise.</param>
    protected void Raise(CoreDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        Apply(@event);
        _uncommittedEvents.Add(@event);
        _version++;
    }

    /// <summary>
    /// Applies an event to update aggregate state.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    protected abstract void Apply(CoreDomainEvent @event);
}

/// <summary>
/// Base class for aggregates that support snapshots.
/// Snapshots optimize reconstruction by storing aggregate state at intervals.
/// </summary>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
/// <example>
/// <code>
/// public class OrderSnapshot
/// {
///     public Guid Id { get; set; }
///     public OrderStatus Status { get; set; }
///     public decimal TotalAmount { get; set; }
/// }
/// 
/// public class Order : SnapshotAggregateRoot&lt;OrderSnapshot&gt;
/// {
///     public OrderStatus Status { get; private set; }
///     public decimal TotalAmount { get; private set; }
///     
///     public override OrderSnapshot CreateSnapshot() => new()
///     {
///         Id = Id,
///         Status = Status,
///         TotalAmount = TotalAmount
///     };
///     
///     public override void RestoreFromSnapshot(OrderSnapshot snapshot, long version)
///     {
///         Id = snapshot.Id;
///         Status = snapshot.Status;
///         TotalAmount = snapshot.TotalAmount;
///         SetVersion(version);
///     }
///     
///     protected override void Apply(CoreDomainEvent @event) { ... }
/// }
/// </code>
/// </example>
public abstract class SnapshotAggregateRoot<TSnapshot> : AggregateRoot, ISnapshotAggregate<TSnapshot>
    where TSnapshot : class
{
    private long _snapshotVersion;

    /// <summary>
    /// Gets the version at which the last snapshot was taken.
    /// </summary>
    public long SnapshotVersion => _snapshotVersion;

    /// <summary>
    /// Gets whether the aggregate was restored from a snapshot.
    /// </summary>
    public bool WasRestoredFromSnapshot => _snapshotVersion > 0;

    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>A snapshot object representing the current state.</returns>
    public abstract TSnapshot CreateSnapshot();

    /// <summary>
    /// Restores the aggregate state from a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="version">The version at which the snapshot was taken.</param>
    public abstract void RestoreFromSnapshot(TSnapshot snapshot, long version);

    /// <summary>
    /// Sets the version after restoring from snapshot.
    /// Call this in RestoreFromSnapshot implementation.
    /// </summary>
    /// <param name="version">The version to set.</param>
    protected void SetVersion(long version)
    {
        _snapshotVersion = version;
        // Use reflection to set the private _version field from base class
        var versionField = typeof(AggregateRoot).GetField("_version", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        versionField?.SetValue(this, version);
    }
}

