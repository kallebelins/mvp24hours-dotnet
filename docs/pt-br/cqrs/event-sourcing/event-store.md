# Event Store - Armazenamento de Eventos

## Visão Geral

O Event Store é o componente responsável por persistir eventos de forma durável e ordenada. É o coração de uma arquitetura Event Sourcing.

## Interface IEventStore

```csharp
public interface IEventStore
{
    Task AppendEventsAsync(
        Guid aggregateId, 
        IEnumerable<IDomainEvent> events, 
        long expectedVersion,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid aggregateId, 
        long fromVersion = 0,
        CancellationToken cancellationToken = default);
    
    Task<long> GetCurrentVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}
```

## Estrutura de Evento Persistido

```csharp
public class StoredEvent
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public long Version { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

## Implementação SQL Server

### Schema

```sql
CREATE TABLE EventStore (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AggregateId UNIQUEIDENTIFIER NOT NULL,
    AggregateType NVARCHAR(256) NOT NULL,
    Version BIGINT NOT NULL,
    EventType NVARCHAR(256) NOT NULL,
    EventData NVARCHAR(MAX) NOT NULL,
    Metadata NVARCHAR(MAX) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_EventStore_AggregateId_Version 
        UNIQUE (AggregateId, Version)
);

CREATE INDEX IX_EventStore_AggregateId 
    ON EventStore (AggregateId, Version);
```

### Implementação EF Core

```csharp
public class EfCoreEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly IEventSerializer _serializer;

    public async Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion = await GetCurrentVersionAsync(aggregateId, cancellationToken);
        
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(
                $"Expected version {expectedVersion}, but found {currentVersion}");
        }

        var version = expectedVersion;
        var storedEvents = events.Select(e => new StoredEvent
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = e.GetType().DeclaringType?.Name ?? "Unknown",
            Version = ++version,
            EventType = e.GetType().AssemblyQualifiedName!,
            EventData = _serializer.Serialize(e),
            Metadata = JsonSerializer.Serialize(new
            {
                CorrelationId = Activity.Current?.Id,
                Timestamp = DateTime.UtcNow
            }),
            Timestamp = DateTime.UtcNow
        });

        await _context.StoredEvents.AddRangeAsync(storedEvents, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        long fromVersion,
        CancellationToken cancellationToken)
    {
        var storedEvents = await _context.StoredEvents
            .Where(e => e.AggregateId == aggregateId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return storedEvents
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList();
    }

    public async Task<long> GetCurrentVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken)
    {
        return await _context.StoredEvents
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (long?)e.Version, cancellationToken) ?? 0;
    }
}
```

## Serialização de Eventos

```csharp
public interface IEventSerializer
{
    string Serialize(IDomainEvent @event);
    IDomainEvent Deserialize(string eventType, string data);
}

public class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize(IDomainEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType(), _options);
    }

    public IDomainEvent Deserialize(string eventType, string data)
    {
        var type = Type.GetType(eventType) 
            ?? throw new InvalidOperationException($"Type {eventType} not found");
        
        return (IDomainEvent)JsonSerializer.Deserialize(data, type, _options)!;
    }
}
```

## Concurrency Control

### Optimistic Locking

```csharp
public async Task AppendEventsAsync(
    Guid aggregateId,
    IEnumerable<IDomainEvent> events,
    long expectedVersion,
    CancellationToken cancellationToken)
{
    await using var transaction = await _context.Database
        .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    try
    {
        var currentVersion = await GetCurrentVersionAsync(aggregateId, cancellationToken);
        
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(
                $"Aggregate {aggregateId}: expected version {expectedVersion}, " +
                $"but found {currentVersion}");
        }

        // Append events...
        
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### ConcurrencyException

```csharp
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
```

## Stream Subscriptions

### Interface

```csharp
public interface IEventStoreSubscription
{
    Task SubscribeAsync(
        Func<IDomainEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
    
    Task SubscribeFromPositionAsync(
        long position,
        Func<IDomainEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
```

### Polling Implementation

```csharp
public class PollingEventStoreSubscription : IEventStoreSubscription
{
    private readonly EventStoreDbContext _context;
    private readonly IEventSerializer _serializer;
    private long _lastPosition;

    public async Task SubscribeAsync(
        Func<IDomainEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var events = await _context.StoredEvents
                .Where(e => e.Id > _lastPosition)
                .OrderBy(e => e.Id)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var storedEvent in events)
            {
                var @event = _serializer.Deserialize(
                    storedEvent.EventType, 
                    storedEvent.EventData);
                
                await handler(@event, cancellationToken);
                _lastPosition = storedEvent.Id;
            }

            if (!events.Any())
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
```

## Configuração

```csharp
services.AddDbContext<EventStoreDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IEventStore, EfCoreEventStore>();
services.AddScoped<IEventSerializer, JsonEventSerializer>();
services.AddScoped<IEventStoreSubscription, PollingEventStoreSubscription>();
```

## Boas Práticas

1. **Imutabilidade**: Nunca modifique eventos persistidos
2. **Versionamento**: Use versão para controle de concorrência
3. **Índices**: Indexe por AggregateId e Version
4. **Serialização**: Use formato versionável (JSON com schema)
5. **Metadata**: Inclua correlation ID e timestamp
6. **Backup**: Faça backup regular do Event Store
7. **Compactação**: Considere arquivar eventos antigos

