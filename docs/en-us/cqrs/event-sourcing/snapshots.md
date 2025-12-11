# Snapshots for Performance

## Overview

Snapshots are photos of an aggregate's state at a specific point in time. They optimize the reconstruction of aggregates with many events.

## The Problem

```
┌─────────────────────────────────────────────────────────────────┐
│            Without Snapshots: Reconstruct 10,000 events         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Event 1 ──▶ Event 2 ──▶ ... ──▶ Event 10000 ──▶ State        │
│                                                                 │
│   ⏱️ Time: ~5 seconds                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│            With Snapshots: Load snapshot + 100 events           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Snapshot (v9900) ──▶ Event 9901 ──▶ ... ──▶ Event 10000      │
│                                                                 │
│   ⏱️ Time: ~50ms                                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Snapshot Structure

```csharp
public class Snapshot
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public long Version { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

## ISnapshotStore Interface

```csharp
public interface ISnapshotStore
{
    Task SaveSnapshotAsync<TAggregate>(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
        where TAggregate : EventSourcedAggregate;
    
    Task<TAggregate?> GetSnapshotAsync<TAggregate>(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
        where TAggregate : EventSourcedAggregate, new();
    
    Task<long?> GetLatestVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}
```

## SQL Server Implementation

```csharp
public class SqlSnapshotStore : ISnapshotStore
{
    private readonly SnapshotDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveSnapshotAsync<TAggregate>(
        TAggregate aggregate,
        CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate
    {
        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregate.Id,
            AggregateType = typeof(TAggregate).AssemblyQualifiedName!,
            Version = aggregate.Version,
            Data = JsonSerializer.Serialize(aggregate, _jsonOptions),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Snapshots.AddAsync(snapshot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TAggregate?> GetSnapshotAsync<TAggregate>(
        Guid aggregateId,
        CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate, new()
    {
        var snapshot = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot == null)
            return null;

        return JsonSerializer.Deserialize<TAggregate>(
            snapshot.Data, 
            _jsonOptions);
    }

    public async Task<long?> GetLatestVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken)
    {
        return await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .MaxAsync(s => (long?)s.Version, cancellationToken);
    }
}
```

## Repository with Snapshots

```csharp
public class SnapshotEventSourcedRepository<TAggregate> 
    : IEventSourcedRepository<TAggregate>
    where TAggregate : EventSourcedAggregate, new()
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private readonly int _snapshotInterval;

    public SnapshotEventSourcedRepository(
        IEventStore eventStore,
        ISnapshotStore snapshotStore,
        IOptions<SnapshotOptions> options)
    {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _snapshotInterval = options.Value.SnapshotInterval;
    }

    public async Task<TAggregate?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        // Try to load snapshot
        var aggregate = await _snapshotStore.GetSnapshotAsync<TAggregate>(
            id, 
            cancellationToken);
        
        var fromVersion = aggregate?.Version ?? 0;

        // Load events after snapshot
        var events = await _eventStore.GetEventsAsync(
            id, 
            fromVersion, 
            cancellationToken);

        if (aggregate == null && !events.Any())
            return null;

        aggregate ??= new TAggregate();
        aggregate.LoadFromHistory(events);

        return aggregate;
    }

    public async Task SaveAsync(
        TAggregate aggregate, 
        CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = aggregate.UncommittedEvents;
        
        if (!uncommittedEvents.Any())
            return;

        var expectedVersion = aggregate.Version - uncommittedEvents.Count;
        
        await _eventStore.AppendEventsAsync(
            aggregate.Id,
            uncommittedEvents,
            expectedVersion,
            cancellationToken);

        // Create snapshot if needed
        if (ShouldCreateSnapshot(aggregate))
        {
            await _snapshotStore.SaveSnapshotAsync(aggregate, cancellationToken);
        }

        aggregate.ClearUncommittedEvents();
    }

    private bool ShouldCreateSnapshot(TAggregate aggregate)
    {
        return aggregate.Version % _snapshotInterval == 0;
    }
}
```

## Snapshot Strategies

### 1. Fixed Interval

```csharp
// Create snapshot every 100 events
private bool ShouldCreateSnapshot(TAggregate aggregate)
{
    return aggregate.Version % 100 == 0;
}
```

### 2. Size-Based

```csharp
// Create snapshot if number of events since last is large
private bool ShouldCreateSnapshot(TAggregate aggregate, long lastSnapshotVersion)
{
    return aggregate.Version - lastSnapshotVersion >= 50;
}
```

### 3. Time-Based

```csharp
// Create snapshot if last one is too old
private async Task<bool> ShouldCreateSnapshotAsync(
    TAggregate aggregate, 
    CancellationToken cancellationToken)
{
    var lastSnapshot = await _snapshotStore.GetLatestAsync(
        aggregate.Id, 
        cancellationToken);
    
    if (lastSnapshot == null)
        return aggregate.Version > 0;
    
    return DateTime.UtcNow - lastSnapshot.CreatedAt > TimeSpan.FromHours(1);
}
```

### 4. Hybrid

```csharp
private bool ShouldCreateSnapshot(
    TAggregate aggregate, 
    long lastSnapshotVersion,
    DateTime? lastSnapshotTime)
{
    var eventsSinceSnapshot = aggregate.Version - lastSnapshotVersion;
    var timeSinceSnapshot = lastSnapshotTime.HasValue 
        ? DateTime.UtcNow - lastSnapshotTime.Value 
        : TimeSpan.MaxValue;

    return eventsSinceSnapshot >= 100 
        || timeSinceSnapshot > TimeSpan.FromHours(6);
}
```

## Configuration

```csharp
public class SnapshotOptions
{
    public int SnapshotInterval { get; set; } = 100;
    public bool EnableSnapshots { get; set; } = true;
}

// Registration
services.Configure<SnapshotOptions>(configuration.GetSection("Snapshots"));
services.AddScoped<ISnapshotStore, SqlSnapshotStore>();
services.AddScoped(typeof(IEventSourcedRepository<>), 
    typeof(SnapshotEventSourcedRepository<>));
```

## Snapshot Cleanup

```csharp
public class SnapshotCleanupService : BackgroundService
{
    private readonly SnapshotDbContext _context;
    private readonly int _keepCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Keep only the N most recent snapshots per aggregate
            var oldSnapshots = await _context.Snapshots
                .GroupBy(s => s.AggregateId)
                .SelectMany(g => g.OrderByDescending(s => s.Version)
                    .Skip(_keepCount))
                .ToListAsync(stoppingToken);

            _context.Snapshots.RemoveRange(oldSnapshots);
            await _context.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Best Practices

1. **Appropriate Interval**: Balance between frequency and storage
2. **Efficient Serialization**: Use compact format
3. **Cleanup**: Remove old snapshots periodically
4. **Reconstitution Testing**: Verify snapshot + events = correct state
5. **Monitoring**: Monitor load times
6. **Fallback**: Work without snapshots (events only)

