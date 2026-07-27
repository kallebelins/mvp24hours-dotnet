# RabbitMQ Advanced Features

This page documents the nested client options and advanced registrations implemented by `Mvp24Hours.Infrastructure.RabbitMQ`. Start with [Message Broker](broker.md).

## Nested client options

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

### `MessageDeduplicationOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables duplicate detection. |
| `ExpirationMinutes` | `int` | `60` | Deduplication entry lifetime. |
| `MaxEntries` | `int` | `100000` | Maximum entries for the in-memory store. |
| `MessageIdHeaderName` | `string` | `x-message-id` | Header used as the deduplication key. |

Register the backing store with `AddMvp24HoursRabbitMQDeduplication()` or `AddMvp24HoursRabbitMQDeduplication<TStore>()`.

### `PublisherConfirmOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enables broker publisher confirms. |
| `TimeoutMilliseconds` | `int` | `5000` | Maximum confirm wait. |
| `UseAsyncCallbacks` | `bool` | `false` | Uses asynchronous ack/nack callbacks. |
| `WaitForConfirmsOrDie` | `bool` | `true` | Fails publishing when a nack is received. |

### `PriorityQueueOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables priority queue declarations. |
| `MaxPriority` | `byte` | `10` | Maximum priority, from 0 to 255. |
| `DefaultPriority` | `byte` | `0` | Priority assigned when a message does not specify one. |

### `MessageTtlOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables TTL configuration. |
| `DefaultTtlMilliseconds` | `int` | `0` | Per-message default TTL; zero disables it. |
| `QueueTtlMilliseconds` | `int` | `0` | Queue-wide message TTL; zero disables it. |
| `QueueExpiresMilliseconds` | `int` | `0` | Deletes an unused queue after this period; zero disables it. |

### `HeadersExchangeOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables headers-exchange support. |
| `MatchType` | `string` | `all` | RabbitMQ `x-match`: `all` or `any`. |
| `BindingHeaders` | `Dictionary<string, object>?` | `null` | Headers used when binding. |
| `DefaultMessageHeaders` | `Dictionary<string, object>?` | `null` | Headers added when publishing. |

### `ConsumerPrefetchOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `PrefetchCount` | `ushort` | `1` | Maximum unacknowledged messages per consumer/channel. |
| `PrefetchSize` | `uint` | `0` | Byte limit; zero means unlimited. |
| `Global` | `bool` | `false` | Applies QoS to the channel instead of each consumer. |
| `ConcurrentConsumers` | `int` | `1` | Consumers created for the queue. |

### `BatchPublishOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables buffered publishing. |
| `MaxBatchSize` | `int` | `100` | Maximum messages per batch. |
| `MaxBatchDelayMilliseconds` | `int` | `100` | Maximum flush delay. |
| `MaxBatchSizeBytes` | `long` | `1048576` | Maximum serialized batch size. |

## Batch consumers

```csharp
builder.Services.AddMvp24HoursRabbitMQBatchConsumer<OrderBatchConsumer, OrderMessage>(
    options =>
    {
        BatchConsumerOptions preset = BatchConsumerOptions.HighThroughput;
        options.MaxBatchSize = preset.MaxBatchSize;
        options.MinBatchSize = preset.MinBatchSize;
        options.BatchTimeout = preset.BatchTimeout;
        options.MessageWaitTimeout = preset.MessageWaitTimeout;
        options.EnableParallelProcessing = preset.EnableParallelProcessing;
        options.MaxDegreeOfParallelism = preset.MaxDegreeOfParallelism;
        options.UseBatchAcknowledgment = preset.UseBatchAcknowledgment;
        options.PrefetchCount = preset.PrefetchCount;
    });
```

`BatchConsumerOptions.Default`, `.HighThroughput`, and `.LowLatency` return configured instances. `Validate()` rejects invalid batch sizes, timeouts, parallelism, retry counts, and a prefetch count below `MaxBatchSize`.

| Name | Type | Default | Description |
|---|---|---|---|
| `MaxBatchSize` | `int` | `10` | Maximum messages in a batch. |
| `MinBatchSize` | `int` | `1` | Minimum preferred batch size. |
| `BatchTimeout` | `TimeSpan` | `1 second` | Maximum time to fill a batch. |
| `MessageWaitTimeout` | `TimeSpan` | `500 ms` | Wait between individual messages. |
| `EnableParallelProcessing` | `bool` | `false` | Processes independent messages concurrently. |
| `MaxDegreeOfParallelism` | `int` | `0` | Parallelism; zero uses processor count. |
| `UseBatchAcknowledgment` | `bool` | `true` | Acknowledges the complete batch together. |
| `RequeueOnFailure` | `bool` | `true` | Requeues messages after batch failure. |
| `MaxRetryAttempts` | `int` | `3` | Batch retry attempts. |
| `RetryDelay` | `TimeSpan` | `1 second` | Base retry delay. |
| `UseExponentialBackoff` | `bool` | `true` | Increases retry delays exponentially. |
| `PrefetchCount` | `ushort` | `20` | Batch-consumer QoS count. |

High-throughput uses batches of 100, minimum 10, a five-second timeout, parallel processing, and prefetch 200. Low-latency uses batches of 5, a 100 ms timeout, sequential individual acknowledgements, and prefetch 10.

## Scheduling

```csharp
builder.Services.AddMvp24HoursRabbitMQScheduler(options =>
{
    options.UseDelayedMessagePlugin = false;
    options.PollingInterval = TimeSpan.FromSeconds(1);
    options.BatchSize = 100;
});
```

Use `AddMvp24HoursRabbitMQSchedulerWithRedis` after registering `IDistributedCache` for distributed storage, or `AddMvp24HoursRabbitMQScheduler<TStore>` for a custom `IScheduledMessageStore`.

### `MessageSchedulerOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enables scheduling. |
| `UseDelayedMessagePlugin` | `bool` | `false` | Uses the RabbitMQ delayed-exchange plugin; otherwise uses retry queues. |
| `DelayedExchangeName` | `string` | `mvp.delayed.exchange` | Delayed exchange name. |
| `ScheduledQueueName` | `string` | `mvp.scheduled.messages` | Scheduler queue name. |
| `PollingInterval` | `TimeSpan` | `1 second` | Store polling interval. |
| `BatchSize` | `int` | `100` | Messages processed per poll. |
| `PersistMessages` | `bool` | `true` | Persists scheduled records. |
| `MaxRetryCount` | `int` | `3` | Failed-delivery retries. |
| `RetryDelayMultiplier` | `double` | `2.0` | Exponential retry multiplier. |
| `BaseRetryDelayMs` | `int` | `1000` | Initial retry delay. |
| `CompletedMessageTtl` | `TimeSpan` | `24 hours` | Retention for completed records. |
| `EnableRecurringMessages` | `bool` | `true` | Enables recurring messages. |
| `MinimumRecurringInterval` | `TimeSpan` | `1 minute` | Smallest recurring interval. |

## Request/response

Request clients are configured on the fluent builder:

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

### `RequestClientOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Exchange` | `string` | empty | Request exchange; empty uses the default exchange. |
| `RoutingKey` | `string?` | `null` | Request routing key; the request type name is the fallback. |
| `TimeoutMilliseconds` | `int` | `30000` | Response timeout. |
| `ThrowOnTimeout` | `bool` | `false` | Throws instead of returning a timeout response. |

## Filter pipeline

`FilterPipelineOptions` exposes the registered filter collections and built-in switches:

| Name | Type | Default | Description |
|---|---|---|---|
| `ConsumeFilters` | `IReadOnlyList<Type>` | empty | Registered consume-filter types; populated through `UseConsumeFilter`. |
| `PublishFilters` | `IReadOnlyList<Type>` | empty | Registered publish-filter types; populated through `UsePublishFilter`. |
| `SendFilters` | `IReadOnlyList<Type>` | empty | Registered send-filter types; populated through `UseSendFilter`. |
| `EnableLoggingFilter` | `bool` | `false` | Enables the built-in logging filter. |
| `EnableExceptionHandlingFilter` | `bool` | `false` | Enables exception handling. |
| `EnableCorrelationFilter` | `bool` | `false` | Enables correlation propagation. |
| `EnableTelemetryFilter` | `bool` | `false` | Enables telemetry. |
| `EnableValidationFilter` | `bool` | `false` | Enables validation. |

Use `UseConsumeFilter<T>()`, `UsePublishFilter<T>()`, `UseSendFilter<T>()`, their typed overloads, or `UseFilter<T>()`. The fluent RabbitMQ builder also exposes `UseConsumeFilter`, `UsePublishFilter`, and `UseSendFilter`.

## Multi-tenancy

`TenantRabbitMQOptions` supports `VirtualHostPerTenant`, `PrefixPerTenant`, `RoutingKeyPerTenant`, and `None`.

| Name | Type | Default | Description |
|---|---|---|---|
| `IsolationStrategy` | `TenantIsolationStrategy` | `VirtualHostPerTenant` | Tenant isolation model. |
| `TenantIdHeader` | `string` | `x-tenant-id` | Tenant ID header. |
| `TenantNameHeader` | `string` | `x-tenant-name` | Tenant name header. |
| `RejectMessagesWithoutTenant` | `bool` | `false` | Rejects unscoped messages. |
| `ValidateTenantExists` | `bool` | `true` | Resolves the tenant before processing. |
| `AutoPropagateTenantHeaders` | `bool` | `true` | Copies tenant headers when publishing. |
| `DefaultVirtualHost` | `string?` | `null` | Fallback virtual host. |
| `VirtualHostTemplate` | `string` | `{tenantId}` | Virtual-host template. |
| `QueuePrefixTemplate` | `string` | `{tenantId}_` | Queue prefix template. |
| `ExchangePrefixTemplate` | `string` | `{tenantId}_` | Exchange prefix template. |
| `DeadLetterQueueTemplate` | `string` | `{tenantId}_dlq` | Per-tenant DLQ template. |
| `DeadLetterExchangeTemplate` | `string` | `{tenantId}_dlx` | Per-tenant DLX template. |
| `ConnectionPoolSizePerTenant` | `int` | `5` | Connections retained per tenant. |
| `MaxTenantConnections` | `int` | `100` | Global tenant-connection cap. |
| `IdleConnectionTimeout` | `TimeSpan` | `30 minutes` | Idle eviction time. |
| `Tenants` | `Dictionary<string, TenantRabbitMQConnectionConfig>` | empty | Static tenant connections. |
| `UseTenantSpecificDeadLetterQueues` | `bool` | `true` | Isolates dead letters per tenant. |

Each value in `Tenants` uses:

| `TenantRabbitMQConnectionConfig` member | Type | Default | Description |
|---|---|---|---|
| `VirtualHost` | `string?` | `null` | Tenant-specific virtual host. |
| `ConnectionString` | `string?` | `null` | Complete tenant-specific connection string. |
| `Username` | `string?` | `null` | Tenant-specific username when not embedded in the connection string. |
| `Password` | `string?` | `null` | Tenant-specific password. |
| `IsEnabled` | `bool` | `true` | Allows the tenant connection. |

## Test harness

Use the in-memory bus for deterministic unit/integration tests; it is not a RabbitMQ protocol emulator.

```csharp
var services = new ServiceCollection();
services.AddRabbitMQTestHarness(options =>
    options.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>());

await using var provider = services.BuildServiceProvider();
var harness = provider.GetRequiredService<ITestHarness>();
```

Available helpers are `AddInMemoryRabbitMQ`, `AddRabbitMQTestHarness`, `ReplaceRabbitMQWithInMemory`, `AddTestConsumer<TConsumer>`, and `AddTestRequestHandler<THandler>`. `TestHarnessOptions` contains `AutoRegisterConsumers` (`false`) and `ConsumerAssemblies` (empty), populated by `AddConsumersFromAssembly` or `AddConsumersFromAssemblyContaining<T>()`.

The application RabbitMQ tests cover scheduling, request/response, sagas, multi-tenancy, filters, batching, publisher confirms, priorities, TTL/headers, and the in-memory harness.

## Related

- [Basic RabbitMQ configuration](broker.md)
- [CQRS and RabbitMQ](cqrs/integration-rabbitmq.md)
- [Inbox/outbox](cqrs/resilience/inbox-outbox.md)
