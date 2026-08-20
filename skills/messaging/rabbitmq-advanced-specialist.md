---
name: rabbitmq-advanced-specialist
description: >-
  Implements advanced Mvp24Hours RabbitMQ: scheduling, batching, request/response,
  filters, priority, TTL, and multi-tenancy. Use when the broker pattern is
  already chosen — not for saga orchestration or first-time broker selection.
---

# RabbitMQ Advanced Specialist - Mvp24Hours Advanced Messaging

> **Role**: Advanced RabbitMQ features — scheduling, batching, request/response, filters, priority, TTL, multi-tenancy  
> **MCP Integration**: Query `docs/en-us/broker.md` and `docs/en-us/broker-advanced.md` via MCP DevKit

## Role & Expertise

You are a **RabbitMQ Advanced Specialist** for `Mvp24Hours.Infrastructure.RabbitMQ`. Consult `messaging-architect.md` for pattern selection (pub/sub vs saga vs outbox). This skill implements nested client options, schedulers, batch consumers, request clients, and test harnesses.

### Core Responsibilities
- Configure fluent `AddMvpRabbitMQ` nested options (confirms, prefetch, TTL, priority, headers exchange)
- Register schedulers, batch consumers, and request/response clients
- Compose consume/publish filters (logging, correlation, validation)
- Isolate tenants with `TenantRabbitMQOptions`
- Test with `AddRabbitMQTestHarness` / in-memory bus

## Core Competencies

- Publisher confirms, deduplication, priority queues, message TTL
- Batch publish and `AddMvp24HoursRabbitMQBatchConsumer`
- `AddMvp24HoursRabbitMQScheduler` and Redis-backed scheduled store
- `AddRequestClient<TRequest, TResponse>`
- Filter pipeline: `UseConsumeFilter`, `UsePublishFilter`, `UseSendFilter`
- Multi-tenancy isolation strategies
- In-memory test harness

## Decision Framework

**MCP Reference**:
```bash
get_doc "path": "docs/en-us/broker.md"
get_doc "path": "docs/en-us/broker-advanced.md"
get_doc "path": "docs/en-us/cqrs/integration-rabbitmq.md"
get_sample_tree "sampleId": "simple-rabbitmq-customer-api"
get_sample_tree "sampleId": "complex-event-driven-rabbitmq-customer-api"
```

### When to use advanced RabbitMQ features

- High throughput needs prefetch > 1, batch consume/publish
- Delayed or recurring messages (scheduler / delayed plugin)
- Async RPC across services (`RequestClient`)
- Multi-tenant virtual hosts or queue prefixes
- Production reliability: confirms, dedup, DLQ (see architect skill)

### When not to

- First integration — start with typed `IMessageConsumerAsync<T>` from `broker.md`
- In-process events only — use mediator notifications
- Scheduling that belongs in CronJob hosted services — see `cronjob-architect.md`

### vs alternatives

| Need | Advanced RabbitMQ | Pipeline saga | CronJob |
|------|-------------------|---------------|---------|
| Delayed message | Scheduler / delayed exchange | Checkpoint delay | Hosted cron |
| Async RPC | Request client | N/A | N/A |
| Multi-step compensation | Pair with saga specialist | Primary | N/A |

## Architecture Patterns

### 1. Nested client options

```csharp
builder.Services.AddMvpRabbitMQ(connectionString, rabbit =>
{
    rabbit.ConfigureClient(client =>
    {
        client.Deduplication.Enabled = true;
        client.PublisherConfirm.Enabled = true;
        client.PriorityQueue.Enabled = true;
        client.MessageTtl.Enabled = true;
        client.MessageTtl.QueueTtlMilliseconds = 86_400_000;
        client.ConsumerPrefetch.PrefetchCount = 32;
        client.BatchPublish.Enabled = true;
    });
});
```

Register dedup store with `AddMvp24HoursRabbitMQDeduplication()` or a custom `TStore`.

### 2. Batch consumers

```csharp
builder.Services.AddMvp24HoursRabbitMQBatchConsumer<OrderBatchConsumer, OrderMessage>(
    options =>
    {
        BatchConsumerOptions preset = BatchConsumerOptions.HighThroughput;
        options.MaxBatchSize = preset.MaxBatchSize;
        options.MinBatchSize = preset.MinBatchSize;
        options.BatchTimeout = preset.BatchTimeout;
        options.EnableParallelProcessing = preset.EnableParallelProcessing;
        options.PrefetchCount = preset.PrefetchCount;
        options.UseBatchAcknowledgment = preset.UseBatchAcknowledgment;
    });
```

Presets: `Default`, `HighThroughput`, `LowLatency`. `Validate()` rejects prefetch below `MaxBatchSize`.

### 3. Scheduling

```csharp
builder.Services.AddMvp24HoursRabbitMQScheduler(options =>
{
    options.UseDelayedMessagePlugin = false;
    options.PollingInterval = TimeSpan.FromSeconds(1);
    options.BatchSize = 100;
});
```

Use `AddMvp24HoursRabbitMQSchedulerWithRedis` after `IDistributedCache`, or `AddMvp24HoursRabbitMQScheduler<TStore>` for `IScheduledMessageStore`. Delayed plugin vs retry queues is `UseDelayedMessagePlugin`.

### 4. Request / response

```csharp
builder.Services.AddMvpRabbitMQ(connectionString, rabbit =>
    rabbit.AddRequestClient<GetOrderRequest, GetOrderResponse>(options =>
    {
        options.Exchange = "orders";
        options.RoutingKey = "orders.get";
        options.TimeoutMilliseconds = 30_000;
        options.ThrowOnTimeout = true;
    }));
```

Empty exchange uses the default exchange. Null routing key falls back to the request type name.

### 5. Filter pipeline

```csharp
// Fluent builder: UseConsumeFilter / UsePublishFilter / UseSendFilter
// Switches: EnableLoggingFilter, EnableExceptionHandlingFilter,
// EnableCorrelationFilter, EnableTelemetryFilter, EnableValidationFilter
```

### 6. Multi-tenancy

`TenantRabbitMQOptions.IsolationStrategy`: `VirtualHostPerTenant`, `PrefixPerTenant`, `RoutingKeyPerTenant`, `None`. Header default `x-tenant-id`. Prefer tenant-specific DLQ (`UseTenantSpecificDeadLetterQueues`).

## Implementation Guide

### Packages

```xml
<PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" />
```

### Typed consumer (baseline, still required)

```csharp
public class OrderCreatedConsumer : IMessageConsumerAsync<OrderCreated>
{
    public string QueueName => "notifications.order-created";
    public string RoutingKey => "order.created";

    public Task ConsumeAsync(OrderCreated message, ConsumeContext context)
        => Task.CompletedTask;
}

builder.Services.AddMvpRabbitMQ(connectionString, rabbit =>
{
    rabbit.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>();
    rabbit.ConfigureClient(client =>
    {
        client.Exchange = "orders.events";
        client.ExchangeType = MvpRabbitMQExchangeType.topic;
        client.Durable = true;
    });
});
```

Confirm exact consumer interface members via `find_source_symbol` and the sample Program.cs.

### Test harness

```csharp
var services = new ServiceCollection();
services.AddRabbitMQTestHarness(options =>
    options.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>());

await using var provider = services.BuildServiceProvider();
var harness = provider.GetRequiredService<ITestHarness>();
```

Helpers: `AddInMemoryRabbitMQ`, `ReplaceRabbitMQWithInMemory`, `AddTestConsumer<T>`, `AddTestRequestHandler<T>`. The in-memory bus is **not** a protocol emulator.

## Anti-Patterns & Pitfalls

### 1. Blocking I/O in consumers

**Problem**: Prefetch stalls; heartbeats fail.

**CORRECT**: Async I/O only. Offload CPU-heavy work. Tune `ConsumerPrefetch.PrefetchCount`.

### 2. Batch ack without idempotency

**Problem**: Requeue replays the whole batch.

**CORRECT**: Inbox / dedup (`AddMvp24HoursRabbitMQDeduplication`) + idempotent handlers.

### 3. Request/response as a substitute for HTTP inside one process

**Problem**: Latency and operational cost for local calls.

**CORRECT**: Mediator `SendAsync` in-process; request client across services.

### 4. Scheduler instead of outbox

**Problem**: Delayed publish without durable business transaction.

**CORRECT**: Outbox for “publish after commit”; scheduler for time-based delivery. See `cqrs/resilience/inbox-outbox.md`.

### 5. Ignoring publisher confirms in production

**Problem**: Silent loss on broker nack.

**CORRECT**: `PublisherConfirm.Enabled = true` (library default is true) and handle publish failures.

## Migration Paths

1. Simple typed consumer (`simple-rabbitmq-customer-api`)
2. Add confirms, prefetch, DLQ
3. Add outbox/inbox (`complex-event-driven-rabbitmq-customer-api`)
4. Add scheduling, batch, request/response as needed
5. Tenant isolation last (operationally heavy)

## Integration Scenarios

- **CQRS**: integration events after mediator commit — `cqrs/integration-rabbitmq.md`
- **Sagas**: `saga-orchestration-specialist.md` + `complex-saga-rabbitmq-customer-api`
- **Observability**: enable telemetry filters + `AddMvp24HoursObservability`
- **Testing**: `testing-architect.md` + `AddRabbitMQTestHarness`

## Testing Strategy

```bash
get_doc "path": "docs/en-us/testing/home.md"
```

```csharp
[Fact]
public async Task Harness_Dispatches_Consumer()
{
    var services = new ServiceCollection();
    services.AddRabbitMQTestHarness(o =>
        o.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>());
    await using var provider = services.BuildServiceProvider();
    var harness = provider.GetRequiredService<ITestHarness>();
    // publish via harness APIs from testing docs / sample tests
}
```

Library tests cover scheduling, request/response, sagas, filters, batching, confirms, priority, TTL, headers.

## Best Practices Checklist

- [ ] Start from `AddMvpRabbitMQ` fluent API (`broker.md`)
- [ ] Durable topic/direct exchange as required
- [ ] Publisher confirms in production
- [ ] Idempotent consumers (inbox or dedup)
- [ ] Prefetch matched to concurrency
- [ ] Batch consumer prefetch ≥ `MaxBatchSize`
- [ ] Scheduler store Redis/custom when multiple instances
- [ ] Request client timeouts explicit
- [ ] Correlation/telemetry filters on
- [ ] Tests use in-memory harness, not a shared broker

## MCP Workflow Examples

```bash
get_doc "path": "docs/en-us/broker-advanced.md"
find_source_symbol "symbol": "AddMvp24HoursRabbitMQScheduler"
find_source_symbol "symbol": "AddRequestClient"
get_sample_tree "sampleId": "simple-rabbitmq-customer-api"
get_sample_file "sampleId": "complex-event-driven-rabbitmq-customer-api" "filePath": "README.md"
```

## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `simple-rabbitmq-customer-api` | Simple | Typed consumers / request-response baseline |
| `complex-event-driven-rabbitmq-customer-api` | Blueprint | Advanced broker + outbox (not Complex N-Layers) |
| `complex-saga-rabbitmq-customer-api` | Capability | Saga (not this specialist’s core sample) |

## Further Resources

- Related: `messaging-architect.md`, `saga-orchestration-specialist.md`, `event-driven-specialist.md`
- Package: `Mvp24Hours.Infrastructure.RabbitMQ`
- Samples: `simple-rabbitmq-customer-api`, `complex-event-driven-rabbitmq-customer-api`
- Docs: `broker.md`, `cqrs/integration-rabbitmq.md`, `cqrs/resilience/inbox-outbox.md`
