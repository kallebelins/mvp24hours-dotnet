# Read Model Projections

## Overview

Projections are materialized views of data, optimized for queries. They consume events from the Event Store and keep read models up to date.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Projection Architecture                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Event Store ──▶ Projection Handler ──▶ Read Model (DB)       │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │ Events:                                                 │  │
│   │   OrderCreated ─┐                                       │  │
│   │   OrderPaid ────┼──▶ OrderSummaryProjection ──▶ Orders  │  │
│   │   OrderShipped ─┘    (Read Model)              (Table)  │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Interfaces

### IProjection

```csharp
public interface IProjection
{
    string Name { get; }
    Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}
```

### IProjectionHandler

```csharp
public interface IProjectionHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

## Read Model

### Entity

```csharp
public class OrderReadModel
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public int ItemCount { get; set; }
    public long Version { get; set; }
}
```

### DbContext

```csharp
public class ReadModelDbContext : DbContext
{
    public DbSet<OrderReadModel> Orders { get; set; }
    public DbSet<ProjectionCheckpoint> Checkpoints { get; set; }
}

public class ProjectionCheckpoint
{
    public string ProjectionName { get; set; } = string.Empty;
    public long LastProcessedPosition { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

## Projection Implementation

### OrderSummaryProjection

```csharp
public class OrderSummaryProjection : IProjection
{
    private readonly ReadModelDbContext _context;
    
    public string Name => "OrderSummary";

    public async Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                await HandleOrderCreatedAsync(e, cancellationToken);
                break;
            case OrderItemAddedEvent e:
                await HandleOrderItemAddedAsync(e, cancellationToken);
                break;
            case OrderPaidEvent e:
                await HandleOrderPaidAsync(e, cancellationToken);
                break;
            case OrderShippedEvent e:
                await HandleOrderShippedAsync(e, cancellationToken);
                break;
            case OrderCancelledEvent e:
                await HandleOrderCancelledAsync(e, cancellationToken);
                break;
        }
    }

    private async Task HandleOrderCreatedAsync(
        OrderCreatedEvent e, 
        CancellationToken cancellationToken)
    {
        var order = new OrderReadModel
        {
            Id = e.OrderId,
            CustomerEmail = e.CustomerEmail,
            TotalAmount = e.TotalAmount,
            Status = "Created",
            CreatedAt = e.OccurredAt,
            Version = 1
        };
        
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleOrderItemAddedAsync(
        OrderItemAddedEvent e, 
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(
            new object[] { e.OrderId }, 
            cancellationToken);
        
        if (order != null)
        {
            order.ItemCount++;
            order.TotalAmount += e.Quantity * e.UnitPrice;
            order.Version++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleOrderPaidAsync(
        OrderPaidEvent e, 
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(
            new object[] { e.OrderId }, 
            cancellationToken);
        
        if (order != null)
        {
            order.Status = "Paid";
            order.PaidAt = e.OccurredAt;
            order.Version++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleOrderShippedAsync(
        OrderShippedEvent e, 
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(
            new object[] { e.OrderId }, 
            cancellationToken);
        
        if (order != null)
        {
            order.Status = "Shipped";
            order.ShippedAt = e.OccurredAt;
            order.TrackingNumber = e.TrackingNumber;
            order.Version++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

## Projection Manager

```csharp
public class ProjectionManager : BackgroundService
{
    private readonly IEventStoreSubscription _subscription;
    private readonly IEnumerable<IProjection> _projections;
    private readonly ReadModelDbContext _context;
    private readonly ILogger<ProjectionManager> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load last processed position
        var checkpoint = await _context.Checkpoints
            .FirstOrDefaultAsync(c => c.ProjectionName == "All", stoppingToken);
        
        var startPosition = checkpoint?.LastProcessedPosition ?? 0;

        await _subscription.SubscribeFromPositionAsync(
            startPosition,
            async (@event, ct) =>
            {
                foreach (var projection in _projections)
                {
                    try
                    {
                        await projection.HandleAsync(@event, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Projection {Projection} failed for event {Event}",
                            projection.Name, @event.GetType().Name);
                    }
                }

                // Update checkpoint
                await UpdateCheckpointAsync(@event, stoppingToken);
            },
            stoppingToken);
    }

    private async Task UpdateCheckpointAsync(IDomainEvent @event, CancellationToken ct)
    {
        var checkpoint = await _context.Checkpoints
            .FirstOrDefaultAsync(c => c.ProjectionName == "All", ct);
        
        if (checkpoint == null)
        {
            checkpoint = new ProjectionCheckpoint { ProjectionName = "All" };
            await _context.Checkpoints.AddAsync(checkpoint, ct);
        }

        checkpoint.LastProcessedPosition++;
        checkpoint.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }
}
```

## Projection Rebuild

```csharp
public class ProjectionRebuilder
{
    private readonly IEventStore _eventStore;
    private readonly IProjection _projection;
    private readonly ReadModelDbContext _context;

    public async Task RebuildAsync(CancellationToken cancellationToken)
    {
        // Clear existing data
        await _context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE Orders", 
            cancellationToken);

        // Reprocess all events
        var allEvents = await _eventStore.GetAllEventsAsync(cancellationToken);
        
        foreach (var @event in allEvents)
        {
            await _projection.HandleAsync(@event, cancellationToken);
        }
    }
}
```

## Query Handler

```csharp
public class GetOrderSummaryQueryHandler 
    : IMediatorQueryHandler<GetOrderSummaryQuery, OrderSummaryDto?>
{
    private readonly ReadModelDbContext _context;

    public async Task<OrderSummaryDto?> Handle(
        GetOrderSummaryQuery request, 
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        
        return order is null ? null : new OrderSummaryDto
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ItemCount = order.ItemCount
        };
    }
}
```

## Best Practices

1. **Idempotency**: Handlers must be idempotent
2. **Checkpoints**: Persist position for recovery
3. **Rebuild**: Allow rebuilding projections from scratch
4. **Separation**: Use different database for read models
5. **Async**: Process events asynchronously
6. **Monitoring**: Monitor projection lag

