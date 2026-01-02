//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Interface for event-sourced repository.
/// Provides methods for loading and saving aggregates using event sourcing.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate.</typeparam>
public interface IEventStoreRepository<TAggregate>
    where TAggregate : IEventSourcedAggregate
{
    /// <summary>
    /// Gets an aggregate by its identifier.
    /// Reconstructs the aggregate by replaying its events.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate, or null if not found.</returns>
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an aggregate at a specific version.
    /// Useful for time-travel queries.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="version">The version to reconstruct to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate at the specified version.</returns>
    Task<TAggregate?> GetByIdAtVersionAsync(Guid id, long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate by appending its uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate exists.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the aggregate exists.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event-sourced repository implementation.
/// Uses an event store to persist and reconstruct aggregates.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate.</typeparam>
/// <remarks>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Loads aggregates by replaying events</item>
/// <item>Saves aggregates by appending uncommitted events</item>
/// <item>Supports snapshot optimization</item>
/// <item>Provides time-travel queries</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create repository
/// var repository = new EventStoreRepository&lt;Order&gt;(eventStore, factory);
/// 
/// // Create and save aggregate
/// var order = Order.Create("customer@example.com");
/// order.AddItem(productId, 2, 29.99m);
/// await repository.SaveAsync(order);
/// 
/// // Load aggregate
/// var loadedOrder = await repository.GetByIdAsync(order.Id);
/// </code>
/// </example>
public class EventStoreRepository<TAggregate> : IEventStoreRepository<TAggregate>
    where TAggregate : IEventSourcedAggregate, new()
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore? _snapshotStore;
    private readonly ISnapshotStrategy? _snapshotStrategy;
    private readonly IEventSerializer? _eventSerializer;

    /// <summary>
    /// Initializes a new instance without snapshot support.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    public EventStoreRepository(IEventStore eventStore)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    /// <summary>
    /// Initializes a new instance with snapshot support.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="snapshotStore">The snapshot store.</param>
    /// <param name="snapshotStrategy">The snapshot strategy.</param>
    /// <param name="eventSerializer">The event serializer for snapshots.</param>
    public EventStoreRepository(
        IEventStore eventStore,
        ISnapshotStore snapshotStore,
        ISnapshotStrategy snapshotStrategy,
        IEventSerializer? eventSerializer = null)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _snapshotStrategy = snapshotStrategy ?? throw new ArgumentNullException(nameof(snapshotStrategy));
        _eventSerializer = eventSerializer;
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Try to load from snapshot first
        long fromVersion = 0;
        TAggregate? aggregate = default;

        if (_snapshotStore != null && typeof(TAggregate).GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISnapshotAggregate<>)))
        {
            var snapshot = await _snapshotStore.GetLatestSnapshotAsync(id, cancellationToken);
            if (snapshot != null)
            {
                aggregate = RestoreFromSnapshot(snapshot);
                if (aggregate != null)
                {
                    fromVersion = snapshot.Version;
                }
            }
        }

        // Load events (all or from snapshot version)
        var events = await _eventStore.GetEventsAsync(id, fromVersion, cancellationToken);

        if (!events.Any() && aggregate == null)
        {
            return default;
        }

        // Create aggregate if not restored from snapshot
        aggregate ??= new TAggregate();

        // Apply remaining events
        if (events.Any())
        {
            aggregate.LoadFromHistory(events);
        }

        return aggregate;
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAtVersionAsync(Guid id, long version, CancellationToken cancellationToken = default)
    {
        // Try snapshot at or before version
        long fromVersion = 0;
        TAggregate? aggregate = default;

        if (_snapshotStore != null)
        {
            var snapshot = await _snapshotStore.GetSnapshotAtVersionAsync(id, version, cancellationToken);
            if (snapshot != null)
            {
                aggregate = RestoreFromSnapshot(snapshot);
                if (aggregate != null)
                {
                    fromVersion = snapshot.Version;
                }
            }
        }

        // Load events up to version
        var events = await _eventStore.GetEventsAsync(id, fromVersion, cancellationToken);
        var eventsToApply = events.Take((int)(version - fromVersion)).ToList();

        if (!eventsToApply.Any() && aggregate == null)
        {
            return default;
        }

        aggregate ??= new TAggregate();

        if (eventsToApply.Any())
        {
            aggregate.LoadFromHistory(eventsToApply);
        }

        return aggregate;
    }

    /// <inheritdoc />
    public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var uncommittedEvents = aggregate.UncommittedEvents;

        if (!uncommittedEvents.Any())
        {
            return;
        }

        // Calculate expected version (current version minus uncommitted events)
        var expectedVersion = aggregate.Version - uncommittedEvents.Count;

        // Append events
        await _eventStore.AppendEventsAsync(
            aggregate.Id,
            uncommittedEvents,
            expectedVersion,
            cancellationToken);

        // Check if we should take a snapshot
        if (_snapshotStore != null && _snapshotStrategy != null)
        {
            var lastSnapshotVersion = await GetLastSnapshotVersionAsync(aggregate.Id, cancellationToken);

            if (_snapshotStrategy.ShouldTakeSnapshot(aggregate, lastSnapshotVersion))
            {
                await SaveSnapshotAsync(aggregate, cancellationToken);
            }
        }

        // Clear uncommitted events
        aggregate.ClearUncommittedEvents();
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _eventStore.ExistsAsync(id, cancellationToken);
    }

    private async Task<long> GetLastSnapshotVersionAsync(Guid aggregateId, CancellationToken cancellationToken)
    {
        if (_snapshotStore == null)
        {
            return 0;
        }

        var snapshot = await _snapshotStore.GetLatestSnapshotAsync(aggregateId, cancellationToken);
        return snapshot?.Version ?? 0;
    }

    private async Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        if (_snapshotStore == null)
        {
            return;
        }

        // Check if aggregate supports snapshots
        var snapshotInterface = typeof(TAggregate).GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISnapshotAggregate<>));

        if (snapshotInterface == null)
        {
            return;
        }

        var createSnapshotMethod = typeof(TAggregate).GetMethod("CreateSnapshot");
        if (createSnapshotMethod == null)
        {
            return;
        }

        var snapshotData = createSnapshotMethod.Invoke(aggregate, null);
        if (snapshotData == null)
        {
            return;
        }

        var serializedData = _eventSerializer?.Serialize(snapshotData) 
            ?? System.Text.Json.JsonSerializer.Serialize(snapshotData);

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregate.Id,
            AggregateType = typeof(TAggregate).AssemblyQualifiedName ?? typeof(TAggregate).FullName ?? typeof(TAggregate).Name,
            Version = aggregate.Version,
            Data = serializedData,
            SnapshotType = snapshotData.GetType().AssemblyQualifiedName ?? snapshotData.GetType().FullName ?? snapshotData.GetType().Name,
            Timestamp = DateTime.UtcNow
        };

        await _snapshotStore.SaveSnapshotAsync(snapshot, cancellationToken);
    }

    private TAggregate? RestoreFromSnapshot(Snapshot snapshot)
    {
        try
        {
            var snapshotInterface = typeof(TAggregate).GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISnapshotAggregate<>));

            if (snapshotInterface == null)
            {
                return default;
            }

            var snapshotType = Type.GetType(snapshot.SnapshotType);
            if (snapshotType == null)
            {
                return default;
            }

            var snapshotData = _eventSerializer?.Deserialize(snapshotType, snapshot.Data)
                ?? System.Text.Json.JsonSerializer.Deserialize(snapshot.Data, snapshotType);

            if (snapshotData == null)
            {
                return default;
            }

            var aggregate = new TAggregate();
            var restoreMethod = typeof(TAggregate).GetMethod("RestoreFromSnapshot");
            restoreMethod?.Invoke(aggregate, new[] { snapshotData, snapshot.Version });

            return aggregate;
        }
        catch
        {
            // If snapshot restoration fails, we'll reconstruct from events
            return default;
        }
    }
}

