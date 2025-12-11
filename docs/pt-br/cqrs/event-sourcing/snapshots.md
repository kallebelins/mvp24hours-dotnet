# Snapshots para Performance

## Visão Geral

Snapshots são fotos do estado de um agregado em um ponto específico no tempo. Eles otimizam a reconstrução de agregados com muitos eventos.

## O Problema

```
┌─────────────────────────────────────────────────────────────────┐
│            Sem Snapshots: Reconstruir 10.000 eventos            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Event 1 ──▶ Event 2 ──▶ ... ──▶ Event 10000 ──▶ Estado       │
│                                                                 │
│   ⏱️ Tempo: ~5 segundos                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│            Com Snapshots: Carregar snapshot + 100 eventos       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Snapshot (v9900) ──▶ Event 9901 ──▶ ... ──▶ Event 10000     │
│                                                                 │
│   ⏱️ Tempo: ~50ms                                               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Estrutura do Snapshot

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

## Interface ISnapshotStore

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

## Implementação SQL Server

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

## Repositório com Snapshots

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
        // Tentar carregar snapshot
        var aggregate = await _snapshotStore.GetSnapshotAsync<TAggregate>(
            id, 
            cancellationToken);
        
        var fromVersion = aggregate?.Version ?? 0;

        // Carregar eventos após o snapshot
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

        // Criar snapshot se necessário
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

## Estratégias de Snapshot

### 1. Intervalo Fixo

```csharp
// Criar snapshot a cada 100 eventos
private bool ShouldCreateSnapshot(TAggregate aggregate)
{
    return aggregate.Version % 100 == 0;
}
```

### 2. Baseado em Tamanho

```csharp
// Criar snapshot se o número de eventos desde o último for grande
private bool ShouldCreateSnapshot(TAggregate aggregate, long lastSnapshotVersion)
{
    return aggregate.Version - lastSnapshotVersion >= 50;
}
```

### 3. Baseado em Tempo

```csharp
// Criar snapshot se o último for muito antigo
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

### 4. Híbrido

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

## Configuração

```csharp
public class SnapshotOptions
{
    public int SnapshotInterval { get; set; } = 100;
    public bool EnableSnapshots { get; set; } = true;
}

// Registro
services.Configure<SnapshotOptions>(configuration.GetSection("Snapshots"));
services.AddScoped<ISnapshotStore, SqlSnapshotStore>();
services.AddScoped(typeof(IEventSourcedRepository<>), 
    typeof(SnapshotEventSourcedRepository<>));
```

## Limpeza de Snapshots

```csharp
public class SnapshotCleanupService : BackgroundService
{
    private readonly SnapshotDbContext _context;
    private readonly int _keepCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Manter apenas os N snapshots mais recentes por agregado
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

## Boas Práticas

1. **Intervalo Adequado**: Balance entre frequência e storage
2. **Serialização Eficiente**: Use formato compacto
3. **Limpeza**: Remova snapshots antigos periodicamente
4. **Teste de Reconstituição**: Verifique se snapshot + eventos = estado correto
5. **Monitoring**: Monitore tempos de carregamento
6. **Fallback**: Funcione sem snapshots (apenas eventos)

