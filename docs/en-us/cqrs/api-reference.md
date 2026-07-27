# CQRS API Reference

This reference reflects the public APIs in `Mvp24Hours.Infrastructure.Cqrs` for .NET 10.

## Mediator interfaces

```csharp
public interface IMediator : ISender, IPublisher, IStreamSender;

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}

public interface IPublisher
{
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
}

public interface IStreamSender
{
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
```

The public publisher does not take a per-call strategy. Configure `MediatorOptions.DefaultNotificationStrategy` instead.

## Requests and handlers

| API | Purpose |
|---|---|
| `IMediatorRequest<TResponse>` | Base request contract. |
| `IMediatorRequest` | Request returning `Unit`. |
| `IMediatorCommand<TResponse>` / `IMediatorCommand` | Command contracts. |
| `IMediatorQuery<TResponse>` | Query contract. |
| `IMediatorRequestHandler<TRequest,TResponse>` | Base handler with `Handle(request, cancellationToken)`. |
| `IMediatorCommandHandler<TCommand,TResponse>` | Semantic command handler. |
| `IMediatorCommandHandler<TCommand>` | Command handler returning `Unit`. |
| `IMediatorQueryHandler<TQuery,TResponse>` | Semantic query handler. |

```csharp
public sealed record FindOrder(Guid Id) : IMediatorQuery<OrderDto>;

public sealed class FindOrderHandler
    : IMediatorQueryHandler<FindOrder, OrderDto>
{
    public Task<OrderDto> Handle(
        FindOrder request,
        CancellationToken cancellationToken)
    {
        // Query implementation.
        throw new NotImplementedException();
    }
}
```

## Streaming

Streaming uses `CreateStream`. Stream requests do not run through regular `IPipelineBehavior<TRequest,TResponse>` behavior dispatch.

```csharp
public sealed record ReadOrders : IStreamRequest<OrderDto>;

public sealed class ReadOrdersHandler
    : IStreamRequestHandler<ReadOrders, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> Handle(
        ReadOrders request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (OrderDto item in ReadFromStore(cancellationToken))
        {
            yield return item;
        }
    }
}

await foreach (OrderDto item in mediator.CreateStream(
    new ReadOrders(),
    cancellationToken))
{
    // Process incrementally.
}
```

Use `[EnumeratorCancellation]` in async-iterator handlers so caller cancellation reaches enumeration.

## Notifications

`IMediatorNotification` can have multiple `IMediatorNotificationHandler<TNotification>` implementations. Strategies are:

- `Sequential`
- `Parallel`
- `ParallelNoWait`
- `SequentialContinueOnException`

`ParallelNoWait` logs handler failures instead of returning them to the caller.

## Pipeline behaviors

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

Marker/configuration contracts include `ITransactional`, `ICacheable`, `ICacheInvalidator`, `IAuthorized`, `IRetryable`, `IIdempotentCommand`, `IHasTimeout`, and `IAuditable`. See [behaviors](behaviors.md).

## DI

```csharp
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors();
});
```

`AddMvpMediator(Assembly)` and `AddMvpMediator(params Assembly[])` are available. Mediator, sender, publisher, stream sender, and event dispatcher are scoped; discovered handlers and behaviors are transient.

## `MediatorOptions`

All behavior switches default to `false`.

| Name | Type | Default | Description |
|---|---|---|---|
| `RegisterLoggingBehavior` | `bool` | `false` | Registers request logging. |
| `RegisterPerformanceBehavior` | `bool` | `false` | Registers slow-request monitoring. |
| `RegisterUnhandledExceptionBehavior` | `bool` | `false` | Logs and rethrows unhandled errors. |
| `RegisterValidationBehavior` | `bool` | `false` | Runs FluentValidation validators. |
| `RegisterCachingBehavior` | `bool` | `false` | Caches `ICacheable` responses. |
| `RegisterTransactionBehavior` | `bool` | `false` | Uses `IUnitOfWorkAsync`. |
| `RegisterAuthorizationBehavior` | `bool` | `false` | Enforces `IAuthorized`. |
| `RegisterRetryBehavior` | `bool` | `false` | Retries `IRetryable` requests. |
| `RegisterIdempotencyBehavior` | `bool` | `false` | Deduplicates `IIdempotentCommand`. |
| `RegisterRequestContextBehavior` | `bool` | `false` | Establishes request/correlation context. |
| `RegisterTracingBehavior` | `bool` | `false` | Creates Activity traces. |
| `RegisterTelemetryBehavior` | `bool` | `false` | Emits mediator telemetry. |
| `RegisterAuditBehavior` | `bool` | `false` | Writes audit entries. |
| `AuditAllCommands` | `bool` | `false` | Audits commands without `IAuditable`. |
| `RegisterTenantBehavior` | `bool` | `false` | Resolves tenant context. |
| `RegisterCurrentUserBehavior` | `bool` | `false` | Resolves current-user context. |
| `RegisterTimeoutBehavior` | `bool` | `false` | Applies request timeouts. |
| `RegisterCircuitBreakerBehavior` | `bool` | `false` | Applies request circuit breaking. |
| `RegisterPrePostProcessorBehavior` | `bool` | `false` | Runs pre/post processors. |
| `RegisterExceptionHandlerBehavior` | `bool` | `false` | Runs typed exception handlers. |
| `RegisterPipelineHookBehavior` | `bool` | `false` | Runs lifecycle hooks. |
| `IsBreakOnFail` | `bool` | `false` | Pipeline-compatibility stop flag. |
| `ForceRollbackOnFailure` | `bool` | `false` | Forces transactional rollback. |
| `PerformanceThresholdMilliseconds` | `int` | `500` | Slow-request warning threshold. |
| `DefaultTimeoutMilliseconds` | `int` | `0` | Default timeout; zero disables it. |
| `MaxRetryAttempts` | `int` | `3` | Global retry attempts. |
| `RetryBaseDelayMilliseconds` | `int` | `100` | Base exponential retry delay. |
| `IdempotencyDurationHours` | `int` | `24` | Default idempotency retention. |
| `DefaultNotificationStrategy` | `NotificationPublishingStrategy` | `Sequential` | Notification dispatch strategy. |

Assembly registration methods are `RegisterHandlersFromAssembly` and `RegisterHandlersFromAssemblyContaining<T>()`. Presets are `WithDefaultBehaviors`, `WithAllBehaviors`, `WithObservabilityBehaviors`, `WithAuditBehavior`, `WithSecurityBehaviors`, `WithResiliencyBehaviors`, `WithAdvancedResiliency`, `WithPipelineCompatibility`, `WithMultiTenancy`, `WithExtensibility`, `WithPrePostProcessors`, `WithExceptionHandlers`, and `WithPipelineHooks`.

## Inbox/outbox options

Register with `AddMvpInbox`, `AddMvpOutbox`, or `AddMvpInboxOutbox`.

| Name | Type | Default | Description |
|---|---|---|---|
| `OutboxPollingInterval` | `TimeSpan` | `5 seconds` | Publisher polling interval. |
| `BatchSize` | `int` | `100` | Records processed per batch. |
| `MaxRetries` | `int` | `5` | Attempts before dead-lettering. |
| `RetryBaseDelayMilliseconds` | `int` | `1000` | Initial retry delay. |
| `RetryMaxDelayMilliseconds` | `int` | `60000` | Maximum retry delay. |
| `OutboxRetentionDays` | `int` | `7` | Processed outbox retention. |
| `InboxRetentionDays` | `int` | `7` | Inbox deduplication retention. |
| `CleanupInterval` | `TimeSpan` | `1 hour` | Cleanup frequency. |
| `EnableAutomaticCleanup` | `bool` | `true` | Registers cleanup services. |
| `EnableDeadLetterQueue` | `bool` | `true` | Enables dead-letter storage. |
| `DeadLetterRetentionDays` | `int` | `30` | Dead-letter retention. |
| `EnableParallelProcessing` | `bool` | `false` | Enables parallel outbox work. |
| `MaxDegreeOfParallelism` | `int` | `4` | Parallelism limit. |

## Caching registration

```csharp
builder.Services.AddMediatorMemoryCache();

builder.Services.AddMediatorRedisCache(
    builder.Configuration.GetConnectionString("RedisDbContext")!,
    instanceName: "orders:");
```

The advanced Redis overload accepts `Action<RedisCacheOptions>`. These methods register `IDistributedCache`; caching/idempotency behaviors must still be enabled through `MediatorOptions`.

### `MediatorCacheOptions`

This public options type is available for application-level conventions, but the current `AddMediator*Cache` overloads do not take it.

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultQueryCacheDuration` | `TimeSpan` | `5 minutes` | Query result retention. |
| `DefaultIdempotencyDuration` | `TimeSpan` | `24 hours` | Idempotency retention. |
| `KeyPrefix` | `string` | `mvp24mediator:` | Key namespace. |
| `UseSlidingExpiration` | `bool` | `false` | Uses sliding instead of absolute expiration. |

## Domain and integration events

`IDomainEvent` is an `IMediatorNotification`. `IDomainEventDispatcher` dispatches collections of domain events.

`IIntegrationEvent` exposes `Id`, `OccurredOn`, and `CorrelationId`; `IntegrationEventBase` also provides `CausationId`. `IIntegrationEventOutbox` supports add, pending read, published/failed transitions, and cleanup. See [RabbitMQ integration](integration-rabbitmq.md).

## `Unit`

`Unit` represents a successful response with no payload. Use `Unit.Value` or `Unit.Task`.

## Related

- [Behaviors](behaviors.md)
- [Caching integration](integration-caching.md)
- [RabbitMQ integration](integration-rabbitmq.md)
- [Inbox/outbox](resilience/inbox-outbox.md)
