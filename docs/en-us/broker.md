# Message Broker with RabbitMQ

`Mvp24Hours.Infrastructure.RabbitMQ` provides the legacy `IMvpRabbitMQClient` API and the recommended fluent `AddMvpRabbitMQ` configuration API. The examples on this page target .NET 10.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
```

## Recommended registration

```csharp
using Mvp24Hours.Extensions;

builder.Services.AddMvpRabbitMQ(
    builder.Configuration.GetConnectionString("RabbitMQContext")!,
    rabbit =>
    {
        rabbit.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>();
        rabbit.AddRequestClient<GetOrderRequest, GetOrderResponse>(request =>
        {
            request.Exchange = "orders";
            request.RoutingKey = "orders.get";
            request.TimeoutMilliseconds = 10_000;
        });
        rabbit.ConfigureClient(client =>
        {
            client.Exchange = "app.events";
            client.ExchangeType = MvpRabbitMQExchangeType.topic;
            client.Durable = true;
            client.PublisherConfirm.Enabled = true;
            client.ConsumerPrefetch.PrefetchCount = 16;
        });
    });
```

The overloads also accept `host` and `port`, or only an `Action<RabbitMQConfigurationBuilder>`. The older `AddMvp24HoursRabbitMQ(Assembly, ...)` and explicit consumer-list overloads remain available for `IMvpRabbitMQConsumer` implementations.

## Connection options

### `RabbitMQConnectionOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `amqp://guest:guest@localhost:5672` | AMQP connection string. |
| `Configuration` | `RabbitMQConnection?` | `null` | Structured connection settings instead of the connection string. |
| `RetryCount` | `int` | `3` | Connection retry count. |
| `DispatchConsumersAsync` | `bool` | `true` | Enables asynchronous consumer dispatch. |

### `RabbitMQOptions`

`RabbitMQClientOptions` inherits these topology and publishing properties.

| Name | Type | Default | Description |
|---|---|---|---|
| `Exchange` | `string` | `amq.direct` | Exchange name. |
| `ExchangeType` | `MvpRabbitMQExchangeType` | `direct` | Exchange type. |
| `RoutingKey` | `string` | empty | Default routing key. |
| `QueueName` | `string` | empty | Queue name. |
| `Durable` | `bool` | `true` | Declares durable topology. |
| `Exclusive` | `bool` | `false` | Declares an exclusive queue. |
| `AutoDelete` | `bool` | `false` | Deletes topology when no longer used. |
| `ExchangeArguments` | `Dictionary<string, object>?` | `null` | Native exchange arguments. |
| `QueueArguments` | `Dictionary<string, object>?` | `null` | Native queue arguments, such as `x-queue-type`. |
| `BasicProperties` | `IBasicProperties?` | `null` | Default RabbitMQ message properties. |

### `RabbitMQClientOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `MaxRedeliveredCount` | `int` | `3` | Maximum redeliveries before rejection/dead-letter handling. |
| `DeadLetter` | `RabbitMQOptions?` | `null` | Dead-letter exchange/queue configuration. |
| `Deduplication` | `MessageDeduplicationOptions` | defaults below | Message-ID deduplication configuration. |
| `PriorityQueue` | `PriorityQueueOptions` | defaults below | Queue/message priority configuration. |
| `MessageTtl` | `MessageTtlOptions` | defaults below | Message and queue lifetime configuration. |
| `HeadersExchange` | `HeadersExchangeOptions` | defaults below | Header-based binding and publishing defaults. |
| `ConsumerPrefetch` | `ConsumerPrefetchOptions` | defaults below | Consumer QoS and concurrency. |
| `PublisherConfirm` | `PublisherConfirmOptions` | defaults below | Publisher acknowledgement behavior. |
| `BatchPublish` | `BatchPublishOptions` | defaults below | Buffered batch publishing. |
| `EnableStructuredLogging` | `bool` | `false` | Enables structured message logging. |
| `EnableMetrics` | `bool` | `false` | Enables client metrics collection. |

## Consumer and producer

The legacy consumer contract is still supported:

```csharp
public sealed class CustomerConsumer : IMvpRabbitMQConsumerAsync
{
    public string RoutingKey => nameof(CustomerChanged);
    public string QueueName => "customers.changed";

    public Task ReceivedAsync(object message, string token)
    {
        // Process the deserialized message.
        return Task.CompletedTask;
    }
}
```

Publish through the registered abstraction:

```csharp
var client = serviceProvider.GetRequiredService<IMvpRabbitMQClient>();
client.Publish(new CustomerChanged(customerId), nameof(CustomerChanged));
```

For typed consumers, request/response, scheduling, batching, filters, multi-tenancy, TTL, headers, and testing, continue with [RabbitMQ advanced features](broker-advanced.md).

## Background consumption

The legacy client can be hosted without a manual consume loop:

```csharp
builder.Services.AddMvp24HoursHostedService(options =>
{
    options.Callback = _ => { };
    options.DueTime = TimeSpan.Zero;
    options.Period = TimeSpan.FromSeconds(3);
});
```

### `RabbitMQHostedOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Callback` | `TimerCallback` | required | Callback used by the hosted consumer. |
| `State` | `object?` | `null` | Callback state. |
| `DueTime` | `TimeSpan` | `TimeSpan.Zero` | Delay before the first callback. |
| `Period` | `TimeSpan` | `3 seconds` | Callback period. |

## Local RabbitMQ

```bash
docker run --rm --name mvp-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

The AMQP endpoint is `amqp://guest:guest@localhost:5672`; the management UI is `http://localhost:15672`.

## Related

- [RabbitMQ advanced features](broker-advanced.md)
- [CQRS and RabbitMQ](cqrs/integration-rabbitmq.md)
- [Inbox/outbox](cqrs/resilience/inbox-outbox.md)
- [Observability](observability/home.md)

> **Samples:** [`simple-rabbitmq-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/simple-rabbitmq-customer-api/CustomerAPI.WebAPI/README.md) (direct publish/consume) · [`complex-event-driven-rabbitmq-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-event-driven-rabbitmq-customer-api/README.md) (durable outbox and inbox)
