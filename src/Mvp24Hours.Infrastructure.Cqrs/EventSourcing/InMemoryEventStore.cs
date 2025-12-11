//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// In-memory implementation of <see cref="IEventStore"/>.
/// Useful for testing and development environments.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores events in memory
/// and will lose all data when the application restarts.
/// For production, use a persistent implementation (SQL, EventStoreDB, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For testing
/// var eventStore = new InMemoryEventStore();
/// var repository = new EventStoreRepository&lt;Order&gt;(eventStore);
/// 
/// // Create and save
/// var order = Order.Create("test@example.com");
/// await repository.SaveAsync(order);
/// 
/// // Verify events
/// var events = await eventStore.GetEventsAsync(order.Id);
/// Assert.Single(events);
/// </code>
/// </example>
public class InMemoryEventStore : IEventStoreWithSubscription
{
    private readonly ConcurrentDictionary<Guid, List<StoredEvent>> _eventStreams = new();
    private readonly List<StoredEvent> _allEvents = new();
    private readonly IEventSerializer _serializer;
    private readonly object _lock = new();
    private long _globalPosition;

    /// <summary>
    /// Initializes a new instance with default JSON serializer.
    /// </summary>
    public InMemoryEventStore() : this(new JsonEventSerializer()) { }

    /// <summary>
    /// Initializes a new instance with custom serializer.
    /// </summary>
    /// <param name="serializer">The event serializer.</param>
    public InMemoryEventStore(IEventSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            var currentVersion = GetCurrentVersionInternal(aggregateId);

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(
                    $"Aggregate {aggregateId}: expected version {expectedVersion}, but found {currentVersion}");
            }

            var stream = _eventStreams.GetOrAdd(aggregateId, _ => new List<StoredEvent>());
            var version = currentVersion;

            foreach (var @event in eventList)
            {
                version++;
                _globalPosition++;

                var storedEvent = new StoredEvent
                {
                    Id = Guid.NewGuid(),
                    GlobalPosition = _globalPosition,
                    AggregateId = aggregateId,
                    AggregateType = @event.GetType().DeclaringType?.Name ?? "Unknown",
                    Version = version,
                    EventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName ?? @event.GetType().Name,
                    EventData = _serializer.Serialize(@event),
                    Timestamp = DateTime.UtcNow
                };

                stream.Add(storedEvent);
                _allEvents.Add(storedEvent);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_eventStreams.TryGetValue(aggregateId, out var stream))
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
            }

            var events = stream
                .Where(e => e.Version > fromVersion)
                .OrderBy(e => e.Version)
                .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
                .ToList();

            return Task.FromResult<IReadOnlyList<IDomainEvent>>(events);
        }
    }

    /// <inheritdoc />
    public Task<long> GetCurrentVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(GetCurrentVersionInternal(aggregateId));
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _eventStreams.TryGetValue(aggregateId, out var stream) && stream.Count > 0);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StoredEvent> SubscribeFromPositionAsync(
        long fromPosition,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var position = fromPosition;

        while (!cancellationToken.IsCancellationRequested)
        {
            StoredEvent[] newEvents;

            lock (_lock)
            {
                newEvents = _allEvents
                    .Where(e => e.GlobalPosition > position)
                    .OrderBy(e => e.GlobalPosition)
                    .ToArray();
            }

            foreach (var @event in newEvents)
            {
                yield return @event;
                position = @event.GlobalPosition;
            }

            if (!newEvents.Any())
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StoredEvent> SubscribeByAggregateTypeAsync(
        string aggregateType,
        long fromPosition,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var @event in SubscribeFromPositionAsync(fromPosition, cancellationToken))
        {
            if (@event.AggregateType == aggregateType)
            {
                yield return @event;
            }
        }
    }

    /// <summary>
    /// Gets all stored events for testing/debugging.
    /// </summary>
    public IReadOnlyList<StoredEvent> GetAllStoredEvents()
    {
        lock (_lock)
        {
            return _allEvents.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all events. Useful for testing.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _eventStreams.Clear();
            _allEvents.Clear();
            _globalPosition = 0;
        }
    }

    /// <summary>
    /// Gets the total event count.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _allEvents.Count;
            }
        }
    }

    /// <summary>
    /// Gets the current global position.
    /// </summary>
    public long GlobalPosition => _globalPosition;

    private long GetCurrentVersionInternal(Guid aggregateId)
    {
        if (!_eventStreams.TryGetValue(aggregateId, out var stream) || stream.Count == 0)
        {
            return 0;
        }

        return stream.Max(e => e.Version);
    }
}

