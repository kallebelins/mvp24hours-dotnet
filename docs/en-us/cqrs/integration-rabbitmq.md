# CQRS Integration with RabbitMQ

The CQRS and RabbitMQ packages share integration-event and outbox abstractions, but they do not provide an automatic `IIntegrationEventPublisher` implementation. Register an application implementation that publishes through `IMvpRabbitMQClient`, and use the provided `RabbitMQOutboxAdapter` when RabbitMQ transactional messaging must reuse a CQRS outbox.

## Install and register

```bash
dotnet add package Mvp24Hours.Infrastructure.Cqrs
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
```

```csharp
using Mvp24Hours.Extensions;

builder.Services.AddMvpMediator(options =>
    options.RegisterHandlersFromAssemblyContaining<CreateOrderCommand>());

builder.Services.AddMvpRabbitMQ(
    builder.Configuration.GetConnectionString("RabbitMQContext")!,
    rabbit =>
    {
        rabbit.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>();
        rabbit.ConfigureClient(client =>
        {
            client.Exchange = "app.events";
            client.ExchangeType = MvpRabbitMQExchangeType.topic;
            client.PublisherConfirm.Enabled = true;
            client.Deduplication.Enabled = true;
        });
    });

builder.Services.AddMvpInboxOutbox(options =>
{
    options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
    options.MaxRetries = 5;
});

builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<IRabbitMQOutbox, RabbitMQOutboxAdapter>();
```

`AddMvpInbox`, `AddMvpOutbox`, and `AddMvpInboxOutbox` use in-memory stores by default. Replace them for production with `UseInboxStore<TStore>()`, `UseOutboxStore<TStore>()`, and `UseDeadLetterStore<TStore>()`. Register a publisher through `UseIntegrationEventPublisher<TPublisher>()`.

## Integration-event contract

```csharp
public sealed record OrderCreatedIntegrationEvent : IntegrationEventBase
{
    public required Guid OrderId { get; init; }
    public required decimal Total { get; init; }
}
```

`IntegrationEventBase` supplies `Id`, `OccurredOn`, `CorrelationId`, and `CausationId`. Consumers can delegate to an `IIntegrationEventHandler<TEvent>`:

```csharp
public sealed class OrderCreatedHandler
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public Task HandleAsync(
        OrderCreatedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

## Direct publishing

`IIntegrationEventPublisher` has `PublishAsync<TEvent>` and `PublishFromOutboxAsync`. A RabbitMQ implementation is application code because exchange, routing, serialization, and schema-version policy are domain-specific:

```csharp
public sealed class RabbitMqEventPublisher(IMvpRabbitMQClient rabbit)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        rabbit.Publish(@event, typeof(TEvent).Name);
        return Task.CompletedTask;
    }

    public Task PublishFromOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        // Deserialize according to the application's event registry, then publish.
        throw new NotImplementedException();
    }
}
```

The second method must be implemented with a safe event-type registry; do not resolve arbitrary CLR types from untrusted message data.

## Transactional outbox

Add an event to the CQRS outbox inside the same application transaction as the aggregate change:

```csharp
public sealed class CreateOrderHandler(
    IIntegrationEventOutbox outbox,
    IUnitOfWorkAsync unitOfWork)
    : IMediatorCommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

        await outbox.AddAsync(
            new OrderCreatedIntegrationEvent
            {
                OrderId = orderId,
                Total = request.Total
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return orderId;
    }
}
```

The built-in `OutboxProcessor` polls pending events and invokes the registered publisher. `RabbitMQOutboxAdapter` translates `RabbitMQOutboxMessage` records to the CQRS outbox model and supports add, batch add, pending count, published/failed transitions, cleanup, and dead-letter queries.

## `InboxOutboxOptions`

The configuration section constant is `InboxOutbox`.

| Name | Type | Default | Description |
|---|---|---|---|
| `OutboxPollingInterval` | `TimeSpan` | `5 seconds` | Outbox polling frequency. |
| `BatchSize` | `int` | `100` | Maximum records per poll. |
| `MaxRetries` | `int` | `5` | Attempts before dead-letter handling. |
| `RetryBaseDelayMilliseconds` | `int` | `1000` | Initial exponential-backoff delay. |
| `RetryMaxDelayMilliseconds` | `int` | `60000` | Maximum retry delay. |
| `OutboxRetentionDays` | `int` | `7` | Processed outbox retention. |
| `InboxRetentionDays` | `int` | `7` | Inbox deduplication retention. |
| `CleanupInterval` | `TimeSpan` | `1 hour` | Cleanup frequency. |
| `EnableAutomaticCleanup` | `bool` | `true` | Registers cleanup hosted services. |
| `EnableDeadLetterQueue` | `bool` | `true` | Registers dead-letter storage. |
| `DeadLetterRetentionDays` | `int` | `30` | Dead-letter retention. |
| `EnableParallelProcessing` | `bool` | `false` | Processes outbox records concurrently. |
| `MaxDegreeOfParallelism` | `int` | `4` | Parallel-processing limit. |

The in-memory stores are suitable for tests and development only. A production outbox must share the business transaction's durable store.

## Inbox consumption

Use `IInboxProcessor.ProcessAsync` in the RabbitMQ consumer to deduplicate by event ID before invoking the handler:

```csharp
public sealed class OrderCreatedConsumer(IInboxProcessor inbox)
{
    public Task ConsumeAsync(
        OrderCreatedIntegrationEvent @event,
        CancellationToken cancellationToken)
    {
        return inbox.ProcessAsync(
            @event,
            (message, ct) => HandleOrderCreatedAsync(message, ct),
            cancellationToken);
    }
}
```

## Testing

For domain-level tests, register `AddInMemoryRabbitMQ()` or `AddRabbitMQTestHarness()` and the CQRS in-memory inbox/outbox. The source test suite verifies `RabbitMQOutboxAdapter` round trips and published transitions without a broker. Use Testcontainers when AMQP topology, acknowledgements, confirms, TTL, or dead-letter behavior is part of the assertion.

## Related

- [RabbitMQ basics](../broker.md)
- [RabbitMQ advanced features](../broker-advanced.md)
- [CQRS behaviors](behaviors.md)
- [Inbox/outbox](resilience/inbox-outbox.md)
