# Complex Event-Driven RabbitMQ Customer API

Demonstrates a **production-grade Outbox → Broker → Inbox** pattern using:

- `Mvp24Hours.Infrastructure.Cqrs` — Mediator, `IIntegrationEventOutbox`, `IInboxStore`, `IInboxProcessor`, `OutboxProcessor`, `InboxCleanupService`
- `Mvp24Hours.Infrastructure.RabbitMQ` — `IMvpRabbitMQClient`, `IMvpRabbitMQConsumerAsync`, DLX support
- EF Core (SQL Server) — durable outbox + inbox tables, domain tables

Target: **net10.0** | Language: English

---

## Architecture

- **WebAPI → Application → Domain**
- **Infrastructure → Domain** (and **Infrastructure → Application** for AutoMapper profiles and RabbitMQ consumers); composed at WebAPI
- **Application must not reference Infrastructure or WebAPI**

---

## Flow

```
POST /api/customers
  ┌─ CreateCustomerCommandHandler ─────────────────────────────────────┐
  │  1. Add Customer entity to EF change tracker                       │
  │  2. unitOfWork.SaveChangesAsync() ← commits Customer row           │
  │  3. IIntegrationEventOutbox.AddAsync(CustomerCreatedIntegrationEvent)
  │     └─ stages OutboxEntry in EF change tracker                     │
  │  4. unitOfWork.SaveChangesAsync() ← commits OutboxEntry row        │
  └────────────────────────────────────────────────────────────────────┘
  (background) OutboxProcessor polls OutboxEntries every 5 seconds
  ┌─ OutboxProcessor ──────────────────────────────────────────────────┐
  │  5. GetPendingAsync() reads rows WHERE Status = 'Pending'          │
  │  6. IIntegrationEventPublisher.PublishFromOutboxAsync(message)     │
  │     └─ RabbitMqIntegrationEventPublisher                           │
  │        └─ IMvpRabbitMQClient.Publish(wrapper, correlationId)       │
  │  7. MarkAsPublishedAsync() → Status = 'Published'                  │
  └────────────────────────────────────────────────────────────────────┘
  (consumer) CustomerCreatedConsumer receives from RabbitMQ
  ┌─ CustomerCreatedConsumer ──────────────────────────────────────────┐
  │  8. Deserialize IntegrationEventEnvelope + CustomerCreatedEvent    │
  │  9. IInboxProcessor.ProcessAsync(event, handler)                   │
  │     ├─ ExistsAsync(messageId) → short-circuit if duplicate         │
  │     ├─ handler(event): write NotificationLog to DB                 │
  │     └─ MarkAsProcessedAsync(messageId) → InboxEntry row            │
  └────────────────────────────────────────────────────────────────────┘
```

### CorrelationId / CausationId propagation

| Field | Source | Carrier |
|-------|--------|---------|
| `CorrelationId` | `X-Correlation-Id` HTTP header (or auto-generated Guid) | `CreateCustomerCommand` → `CustomerCreatedIntegrationEvent` → RabbitMQ message |
| `CausationId` | Command type name (`CreateCustomerCommand`) | `CustomerCreatedIntegrationEvent.CausationId` |

Both IDs flow through the `OutboxEntry.CorrelationId` column and `IntegrationEventEnvelope` wrapper.

---

## Project Layout

```
CustomerAPI.Domain/
  Entities/
    Customer.cs                 — Domain entity
    NotificationLog.cs          — Consumer-side audit log

CustomerAPI.Application/
  Events/
    CustomerCreatedIntegrationEvent.cs  — Integration event (extends IntegrationEventBase)
  Customers/Commands/CreateCustomer/   — CQRS command + handler + validator
  Customers/Queries/GetCustomers/      — CQRS query + handler
  DTOs/Customers/                      — Request/response DTOs

CustomerAPI.Infrastructure/
  Data/
    EFDBContext.cs              — Includes Customers, NotificationLogs, OutboxEntries, InboxEntries
    Entities/
      OutboxEntry.cs            — EF entity for durable outbox table
      InboxEntry.cs             — EF entity for inbox deduplication table
    Stores/
      EfCoreIntegrationEventOutbox.cs  — IIntegrationEventOutbox → SQL
      EfCoreInboxStore.cs              — IInboxStore → SQL
    Configurations/             — EF fluent configs for all four tables
  Messaging/Consumers/
    CustomerCreatedConsumer.cs  — IMvpRabbitMQConsumerAsync with inbox idempotency
  Mappings/
    CustomerMappingProfile.cs   — AutoMapper profile

CustomerAPI.WebAPI/
  Program.cs                    — Host startup
  Controllers/CustomerController.cs
  Extensions/ServiceBuilderExtensions.cs  — DI wiring
  Configuration/ConnectionStringsOptions.cs
  appsettings.json / appsettings.Development.json
```

---

## Quick Start

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

### 1. Prerequisites

| Dependency | Default port | Notes |
|-----------|-------------|-------|
| SQL Server | 1433 | Replace `CHANGE_ME` password in appsettings.Development.json |
| RabbitMQ | 5672 (AMQP) / 15672 (Management UI) | `guest:guest` by default |

### 2. Docker Compose

From `samples/src/complex-event-driven-rabbitmq-customer-api`:

```bash
docker compose up -d
```

- SQL Server: localhost **1433**
- RabbitMQ AMQP: **5672**; Management UI: **15672** (default `guest` / `guest`)

Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "EFDBContext": "Data Source=.,1433;Initial Catalog=MyEventDrivenTestDb;...",
    "RabbitMQContext": "amqp://guest:guest@localhost:5672"
  }
}
```

### 3. Run

```bash
cd samples/src/complex-event-driven-rabbitmq-customer-api
docker compose up -d
dotnet run --project CustomerAPI.WebAPI
```

### 4. Test the flow

```bash
# Create a customer (triggers the full Outbox → RabbitMQ → Inbox flow)
curl -X POST https://localhost:5001/api/customers \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: my-trace-12345" \
  -d '{"name": "Alice", "email": "alice@example.com"}'

# List customers
curl https://localhost:5001/api/customers

# Health checks
curl https://localhost:5001/hc
```

### 5. Verify end-to-end

After a successful POST you should observe (within ~5 seconds):

| Table | Expected row |
|-------|-------------|
| `Customers` | New customer row |
| `OutboxEntries` | Status = `Published`, CorrelationId set |
| `InboxEntries` | MessageId = EventId, MessageType = `CustomerCreatedIntegrationEvent` |
| `NotificationLogs` | EventType, CorrelationId, Notes with customer info |

---

## Outbox / Inbox Design Decisions

### Why NOT `AddMvpInboxOutbox()` directly?

The library's `AddMvpInboxOutbox()` registers `IIntegrationEventOutbox` and `IInboxStore` as **Singleton** via `TryAddSingleton`. This conflicts with EF Core's **Scoped** `DbContext` — injecting a Scoped service into a Singleton causes a **captive dependency** runtime error.

**This sample registers stores as Scoped** and wires up the CQRS components manually:

```csharp
// Scoped → shares DbContext with command handler within a request scope
services.AddScoped<IIntegrationEventOutbox, EfCoreIntegrationEventOutbox>();
services.AddScoped<IInboxStore, EfCoreInboxStore>();
services.AddScoped<IInboxProcessor, InboxProcessor>();
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

// Singleton in-memory (replace with EF-backed for production dead-letter tracking)
services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

// Background hosted services
services.AddHostedService<OutboxProcessor>();       // polls OutboxEntries every 5s
services.AddHostedService<OutboxCleanupService>(); // deletes old Published rows
services.AddHostedService<InboxCleanupService>();  // deletes old InboxEntries
```

If the library adds a `UseOutboxStore<T>(ServiceLifetime.Scoped)` overload in the future, this manual wiring can be replaced with:

```csharp
// Future API (not yet available)
services.AddMvpInboxOutbox()
    .UseOutboxStore<EfCoreIntegrationEventOutbox>(ServiceLifetime.Scoped)
    .UseInboxStore<EfCoreInboxStore>(ServiceLifetime.Scoped)
    .UseIntegrationEventPublisher<RabbitMqIntegrationEventPublisher>();
```

### Atomicity trade-off

True atomicity (Customer + OutboxEntry in a single transaction) requires the outbox `AddAsync` to be called **before** `SaveChangesAsync`. Because Customer uses an auto-increment int PK, we need the Customer.Id for the event, which is only assigned after the first `SaveChangesAsync`.

**Current approach (two saves):**
1. `SaveChangesAsync` → commits Customer, sets `entity.Id`
2. `outbox.AddAsync(event)` → stages OutboxEntry (same DbContext)
3. `SaveChangesAsync` → commits OutboxEntry

Risk: if the process crashes between steps 2 and 3, the OutboxEntry is lost. The Customer is persisted but no event is published. For full atomicity, consider a GUID primary key or a pre-generated sequence.

### At-Least-Once delivery

The outbox guarantees **at-least-once** delivery (not exactly-once) to RabbitMQ. The **inbox** (`IInboxProcessor`) deduplicates on the consumer side, achieving **effectively-once** processing.

### Dead-Letter Queue (DLQ)

The RabbitMQ client is configured with:

```csharp
clientOptions.QueueArguments = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dlx.event-driven-customer" }
};
clientOptions.MaxRedeliveredCount = 3;
```

After `MaxRedeliveredCount` redeliveries, unacknowledged messages are routed to `dlx.event-driven-customer`. Monitor this queue via the RabbitMQ Management UI (`http://localhost:15672`).

The `OutboxProcessor` also implements exponential-backoff retry (`RetryBaseDelayMilliseconds = 1000`) and moves messages to `IDeadLetterStore` after `MaxRetries = 5` failures.

---

## When to use this vs `simple-rabbitmq-customer-api`

| Concern | `simple-rabbitmq` | This sample |
|---------|------------------|-------------|
| Durability | Messages lost if broker unreachable at publish time | Outbox table survives broker outages |
| Idempotency | No duplicate protection | Inbox deduplication table |
| Complexity | Low — direct publish in controller | Higher — two extra tables, background services |
| Use case | Internal tooling, low-stakes notifications | Financial events, order creation, compliance audit |

**Choose simple-rabbitmq** when occasional message loss is acceptable and you control both producer and consumer.

**Choose this sample** when you need guaranteed delivery, audit trails, and protection against duplicate processing.

---

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/customers` | List customers (filter by name, active) |
| `POST` | `/api/customers` | Create customer → triggers outbox flow |
| `GET` | `/hc` | Health check (SQL + RabbitMQ) |
| `GET` | `/swagger` | OpenAPI UI (non-production only) |

Set `X-Correlation-Id` header on POST to propagate your trace ID end-to-end.
