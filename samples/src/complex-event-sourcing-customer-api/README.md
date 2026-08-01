# Complex Event Sourcing Customer API

> **Preview** — The in-memory event store (`InMemoryEventStore`) and snapshot store (`InMemorySnapshotStore`) are fully functional for teaching and development. A durable SQL/EventStoreDB persistence layer is **not yet included** in the library; see the [What is Missing](#what-is-missing) section.

## Status

- Migration status: `migrated (preview capability)`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- `CustomerAggregate : AggregateRoot` with immutable domain events and replay-based rehydration
- In-memory event store, inline projection, and `/rehydrate` endpoint
- Snapshot strategy wired via `EventCountSnapshotStrategy` (teaching depth)
- Native OpenAPI and explicit preview badge for missing durable library store

Target: **net10.0** | Language: English

---

## Architecture

- **WebAPI → Application → Domain**
- No separate Infrastructure project — in-memory event store and projection are registered at WebAPI
- **Application must not reference Infrastructure or WebAPI**
- **Domain** references `Mvp24Hours.Infrastructure.Cqrs` for `AggregateRoot` (library location for event-sourcing primitives; not an EF provider)

---

## What This Sample Demonstrates

- **Event Sourcing fundamentals**: every state change is persisted as an immutable domain event
- **Aggregate reconstruction**: `CustomerAggregate.LoadFromHistory(events)` replays the event stream
- **Domain events** as first-class records: `CustomerCreated`, `CustomerRenamed`, `CustomerDeactivated`
- **In-memory projection**: `CustomerProjection` maintains a denormalized read model updated after each write
- **Rehydration endpoint**: `GET /api/customers/{id}/rehydrate` reconstructs the aggregate directly from the event store
- **Snapshot configuration** wired (via `EventCountSnapshotStrategy`); see [Snapshots](#snapshots)

---

## Flow

```
POST /api/customers
  ┌─ CustomerEventStoreService.CreateAsync ───────────────────────────────────┐
  │  1. CustomerAggregate.Create(name, email)                                  │
  │     └─ Raise(new CustomerCreated(...)) → Apply → sets Name, Email, IsActive│
  │  2. IEventStoreRepository.SaveAsync(aggregate)                             │
  │     └─ IEventStore.AppendEventsAsync(id, [CustomerCreated], expectedVersion=0)
  │  3. CustomerProjection.Apply(aggregate) → update in-memory read model      │
  └────────────────────────────────────────────────────────────────────────────┘

GET /api/customers/{id}               → read from CustomerProjection (fast)
GET /api/customers/{id}/rehydrate     → replay all events from InMemoryEventStore

PUT /api/customers/{id}/name          → appends CustomerRenamed event
POST /api/customers/{id}/deactivate   → appends CustomerDeactivated event
```

---

## Project Layout

```
CustomerAPI.Domain/
  Aggregates/
    CustomerAggregate.cs        — extends AggregateRoot; Raise() + Apply()
  Events/
    CustomerCreated.cs          — record : DomainEventBase
    CustomerRenamed.cs          — record : DomainEventBase
    CustomerDeactivated.cs      — record : DomainEventBase

CustomerAPI.Application/
  Projections/
    CustomerReadModel.cs        — denormalized DTO
    CustomerProjection.cs       — singleton in-memory read model store
  Services/
    CustomerEventStoreService.cs — orchestrates repository + projection

CustomerAPI.WebAPI/
  Controllers/
    CustomersController.cs      — CRUD + /rehydrate endpoint
  Extensions/
    ServiceCollectionExtensions.cs — AddMyEventSourcing, AddMyProjection, AddMyServices
  Program.cs
```

---

## Key APIs Used

| API | Role |
|-----|------|
| `AggregateRoot` | Base class; `protected Raise(event)`, `LoadFromHistory(events)`, `Version` |
| `IDomainEvent` / `DomainEventBase` | Base interface/record for domain events |
| `IEventStoreRepository<T>` | `SaveAsync`, `GetByIdAsync`, `ExistsAsync`, `GetByIdAtVersionAsync` |
| `IEventStore` | `AppendEventsAsync`, `GetEventsAsync`, `GetCurrentVersionAsync` |
| `InMemoryEventStore` | Default in-process event store; not durable across restarts |
| `InMemorySnapshotStore` | Default in-process snapshot store |
| `EventCountSnapshotStrategy` | Takes snapshot every N events (default 100) |
| `AddEventSourcingInMemory()` | Registers all in-memory event sourcing services |
| `AddEventStoreRepository<T>()` | Registers `IEventStoreRepository<T>` with snapshot support |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- No external dependencies — event store and projection are in-memory

## Configuration

No connection strings are required. All state resets when the host stops.

## Run

```bash
cd samples/src/complex-event-sourcing-customer-api/CustomerAPI.WebAPI
dotnet run
```

Open Swagger UI at `https://localhost:{port}/swagger`.

### Try it

```http
# 1. Create customer
POST /api/customers
{ "name": "Alice", "email": "alice@example.com" }
# → 201 Created with customerId

# 2. Read from projection (fast)
GET /api/customers/{id}

# 3. Rename
PUT /api/customers/{id}/name
{ "newName": "Alice Smith" }

# 4. Rehydrate — replay events from store (version increments)
GET /api/customers/{id}/rehydrate

# 5. Deactivate
POST /api/customers/{id}/deactivate

# 6. All customers
GET /api/customers
```

---

## Snapshots

`AddEventSourcingInMemory()` registers `EventCountSnapshotStrategy` (threshold = 100 events)
and `InMemorySnapshotStore`. The `EventStoreRepository` automatically saves a snapshot every
100 events and uses it to skip replay of older events.

`CustomerAggregate` does **not** implement `ISnapshotAggregate<T>` in this sample — the snapshot
strategy is wired but the `SaveSnapshotAsync` guard in `EventStoreRepository` returns early for
aggregates that do not implement the snapshot interface. Adding snapshot support requires:

```csharp
public class CustomerSnapshot { /* flat state */ }

public class CustomerAggregate : SnapshotAggregateRoot<CustomerSnapshot>
{
    public override CustomerSnapshot CreateSnapshot() => new() { ... };
    public override void RestoreFromSnapshot(CustomerSnapshot snapshot, long version)
    {
        Name = snapshot.Name;
        // ... then call SetVersion(version)
    }
}
```

---

## What is Missing

| Feature | Status | Notes |
|---------|--------|-------|
| Durable SQL event store | Not in library | Implement `IEventStore` against SQL Server / PostgreSQL |
| EventStoreDB adapter | Not in library | Implement `IEventStore` against EventStoreDB |
| Event schema versioning / upcasting | Not in library | Implement `IEventSerializer` with type registry |
| Durable snapshot persistence | Not in library | Implement `ISnapshotStore` with EF / SQL |
| Async projection via subscription | Teachable pattern | Use `IEventStoreWithSubscription.SubscribeFromPositionAsync` in a hosted service |

---

## Related documentation

- [Event sourcing home](../../../docs/en-us/cqrs/event-sourcing/home.md), [aggregate](../../../docs/en-us/cqrs/event-sourcing/aggregate.md), [event store](../../../docs/en-us/cqrs/event-sourcing/event-store.md), [projections](../../../docs/en-us/cqrs/event-sourcing/projections.md), [snapshots](../../../docs/en-us/cqrs/event-sourcing/snapshots.md)
- Pattern: [Event Sourcing — Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)

## What this sample intentionally does not cover

- Durable SQL or EventStoreDB event store (library APIs not production-ready)
- Event schema versioning, upcasting, or async projection subscriptions at scale
- Production snapshot persistence beyond in-memory teaching wiring
