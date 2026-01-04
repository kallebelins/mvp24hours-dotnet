# Event Sourcing - Overview

## What is Event Sourcing?

Event Sourcing is an architectural pattern where application state is determined by a sequence of events, instead of storing only the current state.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Traditional vs Event Sourcing                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Traditional (Current State):                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Order { Id: 1, Status: "Shipped", Total: 100.00 }       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Event Sourcing (Event History):                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 1. OrderCreated { Id: 1, Total: 100.00 }                │   │
│  │ 2. OrderPaid { Id: 1, PaymentId: "PAY-123" }            │   │
│  │ 3. OrderShipped { Id: 1, TrackingNumber: "TRACK-456" }  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Benefits

| Benefit | Description |
|---------|-------------|
| **Complete Audit** | Full history of all changes |
| **Debugging** | Reproduce issues by reconstructing state |
| **Flexible Projections** | Create multiple views of same data |
| **Time Travel** | Query state at any point in time |
| **Event Replay** | Rebuild projections from events |

## Core Concepts

### Event

Represents something that happened in the past. Immutable.

```csharp
public record OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Aggregate

Entity that maintains state and emits events.

```csharp
public class Order : EventSourcedAggregate
{
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Reconstruct from events
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;
            case OrderPaidEvent:
                Status = OrderStatus.Paid;
                break;
            case OrderShippedEvent:
                Status = OrderStatus.Shipped;
                break;
        }
    }
}
```

### Event Store

Persists events durably.

```csharp
public interface IEventStore
{
    Task AppendEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, long expectedVersion);
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, long fromVersion = 0);
    Task<long> GetCurrentVersionAsync(Guid aggregateId);
}
```

### Projection

Materialized view of data for queries.

```csharp
public class OrderSummaryProjection : IProjection
{
    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                // Insert new record in query table
                break;
            case OrderShippedEvent e:
                // Update status in query table
                break;
        }
    }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Event Sourcing Architecture                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Command ──▶ Aggregate ──▶ Events ──▶ Event Store             │
│                                             │                   │
│                                             ▼                   │
│                                      ┌──────────────┐          │
│                                      │  Projections │          │
│                                      └──────────────┘          │
│                                             │                   │
│                                             ▼                   │
│   Query ◀────────────────────────── Read Models               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Operation Flow

### Write (Command)

```
1. Load aggregate events
2. Reconstruct state by applying events
3. Execute business logic
4. Emit new events
5. Persist events to Event Store
6. Publish events to projections
```

### Read (Query)

```
1. Query Read Model (projection)
2. Return data optimized for query
```

## When to Use

### ✅ Use Event Sourcing when:

- Complete audit is a requirement
- Complex domain with sophisticated business rules
- Need for multiple data views
- Debugging and troubleshooting are critical
- System integration via events

### ❌ Avoid Event Sourcing when:

- Simple CRUD without complex rules
- Basic audit requirements
- Very tight time-to-market
- Team without experience in the pattern

## Next Steps

- [Aggregates](aggregate.md) - Implementing aggregates
- [Event Store](event-store.md) - Event storage
- [Projections](projections.md) - Read Models
- [Snapshots](snapshots.md) - Performance optimization

