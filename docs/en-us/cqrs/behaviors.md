# CQRS Pipeline Behaviors

Behaviors wrap `IMediatorRequestHandler<TRequest,TResponse>` execution. Register them through `MediatorOptions`; the container preserves the order shown below.

## Registration

```csharp
builder.Services.AddDistributedMemoryCache();

builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<CreateOrderCommand>();
    options.WithDefaultBehaviors();
    options.WithObservabilityBehaviors();
    options.WithAuditBehavior(auditAllCommands: false);
    options.WithAdvancedResiliency(defaultTimeoutMs: 30_000);
    options.RegisterValidationBehavior = true;
    options.RegisterCachingBehavior = true;
    options.RegisterTransactionBehavior = true;
});
```

`WithAllBehaviors()` also enables behaviors that need external services. Ensure FluentValidation, `IDistributedCache`, `IUnitOfWorkAsync`, and `IUserContext` are registered before using the corresponding behavior.

## Execution order

The built-in registration order is:

1. `UnhandledExceptionBehavior`
2. `RequestContextBehavior`
3. `TracingBehavior`
4. `TelemetryBehavior`
5. `LoggingBehavior`
6. `PerformanceBehavior`
7. `AuditBehavior`
8. `AuthorizationBehavior`
9. `ValidationBehavior`
10. `IdempotencyBehavior`
11. `CachingBehavior`
12. `RetryBehavior`
13. `TransactionBehavior`
14. `TenantBehavior`
15. `CurrentUserBehavior`
16. `TimeoutBehavior`
17. `CircuitBreakerBehavior`
18. extensibility behaviors
19. handler

Outer behaviors observe failures from inner behaviors. A behavior registered later is closer to the handler.

## Unhandled exceptions

```csharp
options.RegisterUnhandledExceptionBehavior = true;
```

`UnhandledExceptionBehavior` logs the request type, exception type, and message, then rethrows the original exception. It does not convert, swallow, or retry errors. `WithDefaultBehaviors()` enables it with logging and performance tracking.

For typed recovery, enable `WithExceptionHandlers()` and register `IExceptionHandler<TRequest,TResponse,TException>` or a global exception handler. These are distinct from the final unhandled-exception logger.

## Timeout

```csharp
options.RegisterTimeoutBehavior = true;
options.DefaultTimeoutMilliseconds = 30_000;
```

Set zero to disable the global timeout. A request can override it:

```csharp
public sealed record GenerateReport : IMediatorCommand<Report>, IHasTimeout
{
    public int? TimeoutMilliseconds => 120_000;
}
```

The behavior races handler completion against the timeout and throws `TimeoutException` when the timeout wins. It links caller cancellation with its timeout token, but `RequestHandlerDelegate<TResponse>` has no token parameter; handlers must continue honoring the cancellation token originally supplied by the mediator.

`WithAdvancedResiliency()` enables timeout, circuit breaker, retry, and idempotency. See the focused [retry](resilience/retry.md), [circuit breaker](resilience/circuit-breaker.md), and [idempotency](resilience/idempotency.md) guides.

## Validation

```csharp
options.RegisterValidationBehavior = true;
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
```

`ValidationBehavior` runs registered FluentValidation validators and fails before the handler when validation errors exist.

## Caching and invalidation

```csharp
options.RegisterCachingBehavior = true;
```

Queries implement `ICacheable`; invalidating commands implement `ICacheInvalidator`. Register an `IDistributedCache` provider first. See [CQRS caching](integration-caching.md) for the exact interfaces and Redis registration.

## Transactions

```csharp
options.RegisterTransactionBehavior = true;

public sealed record CreateOrder(...) : IMediatorCommand<Guid>, ITransactional;
```

`TransactionBehavior` uses `IUnitOfWorkAsync`, saves on success, and rolls back on failure. `WithPipelineCompatibility()` enables transaction behavior and sets `IsBreakOnFail` and `ForceRollbackOnFailure`.

## Authorization

```csharp
options.RegisterAuthorizationBehavior = true;

public sealed record DeleteOrder(Guid Id)
    : IMediatorCommand, IAuthorized
{
    public IEnumerable<string> RequiredRoles => ["Admin"];
    public IEnumerable<string> RequiredPermissions => ["orders:delete"];
    public IEnumerable<string> RequiredPolicies => [];
}
```

Register an application `IUserContext`.

## Retry and idempotency

```csharp
options.RegisterRetryBehavior = true;
options.RegisterIdempotencyBehavior = true;
options.MaxRetryAttempts = 3;
options.RetryBaseDelayMilliseconds = 100;
options.IdempotencyDurationHours = 24;
```

`IRetryable` exposes `MaxRetryAttempts`, `RetryDelay`, `UseExponentialBackoff`, and `IsTransientException`. `IIdempotentCommand` exposes optional `IdempotencyKey` and `IdempotencyDuration`. Strict concurrent deduplication additionally requires a distributed lock.

See [idempotency](resilience/idempotency.md) and [inbox/outbox](resilience/inbox-outbox.md).

## Observability and audit helpers

```csharp
options.WithObservabilityBehaviors();
options.WithAuditBehavior(auditAllCommands: false);
```

`WithObservabilityBehaviors()` enables request context, Activity tracing, and telemetry. `WithAuditBehavior()` enables `AuditBehavior`; DI supplies `InMemoryAuditStore` unless a custom `IAuditStore` is registered. The in-memory store is for development/testing, not durable audit retention.

Implement `IAuditable` to control request/response payload capture and metadata. Payload capture defaults to false to avoid leaking sensitive data.

## Scoping behaviors by request type

Several behaviors only apply to requests that opt in via a marker interface. If a request doesn't implement the marker, the behavior calls `next()` immediately and does nothing else — equivalent to the behavior not being registered at all for that request. This lets you register a behavior once globally (via `MediatorOptions`) while controlling per-request-type whether it actually runs.

| Behavior | Marker interface | Effect when the marker is absent |
| --- | --- | --- |
| `CachingBehavior` | `ICacheable` | Handler executes on every call; no cache read/write. |
| `TransactionBehavior` / `TransactionWithEventsBehavior` | `ITransactional` | Handler executes without a wrapping transaction (also requires `IUnitOfWorkAsync` to be available). |
| `IdempotencyBehavior` | `IIdempotentCommand` | Handler executes on every call; no idempotency key lookup/cache. |
| `RetryBehavior` | `IRetryable` | Transient failures propagate immediately; no retry attempts. |
| `CircuitBreakerBehavior` | `ICircuitBreakerProtected` | Requests bypass the circuit breaker entirely, even if other requests tripped it. |

All five marker interfaces live in the `Mvp24Hours.Infrastructure.Cqrs.Behaviors` namespace (declared in `Abstractions/` for discoverability, but the namespace matches the behavior that consumes them). Apply a marker directly on the request:

```csharp
public sealed record GetProductById(int ProductId)
    : IMediatorQuery<Product>, ICacheable
{
    public string? CacheKey => $"product:{ProductId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed record CreateOrder(string CustomerName)
    : IMediatorCommand<Order>, ITransactional, IRetryable;
```

A single request can combine multiple markers (e.g. `ITransactional` and `IRetryable`) — each behavior checks only its own marker, independently of the others.

**Default behavior:** this scoping is unconditional and always active once a behavior is registered — there is no `MediatorOptions` flag to disable it. Enabling `RegisterTransactionBehavior`, `RegisterIdempotencyBehavior`, `RegisterRetryBehavior`, or `RegisterCircuitBreakerBehavior` does **not** make the behavior run for every request; it only makes the behavior available in the pipeline for requests that carry the matching marker. This is by design: registering `TransactionBehavior` globally and having it apply to every command regardless of marker would silently wrap unrelated commands in transactions.

`ValidationBehavior`, `AuthorizationBehavior`, `LoggingBehavior`, `TracingBehavior`, `TelemetryBehavior`, `PerformanceBehavior`, and `UnhandledExceptionBehavior` are cross-cutting by design and do not use this opt-in marker pattern — once registered, they apply to every request (`AuthorizationBehavior` and `AuditBehavior` have their own opt-in markers, `IAuthorized` and `IAuditable`, following the same idiom).

## Custom behavior

```csharp
public sealed class CorrelationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return await next();
    }
}

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(CorrelationBehavior<,>));
```

## Related

- [API reference](api-reference.md)
- [CQRS caching](integration-caching.md)
- [CQRS and RabbitMQ](integration-rabbitmq.md)
- [Retry resilience](resilience/retry.md)
- [Idempotency](resilience/idempotency.md)
- [Inbox/outbox](resilience/inbox-outbox.md)
