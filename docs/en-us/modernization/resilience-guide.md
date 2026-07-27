# Resilience Selection Guide

Mvp24Hours has several resilience integrations because retries, timeouts, and
circuit breakers must run at the boundary that owns the operation. Choose one
primary policy layer for each outbound call; stacking independent retries can
multiply attempts and exceed the intended timeout.

This page is a decision guide only. Option tables live on the linked module
pages.

## Quick selection

| Operation boundary | Start with | Canonical documentation |
|---|---|---|
| `HttpClient` request | `Microsoft.Extensions.Http.Resilience`, `AddMvpStandardResilience`, or `IHttpClientBuilder.AddMvpResilience` | [HTTP resilience](http-resilience.md) |
| Arbitrary non-HTTP operation | `AddNativeResilience` and `NativeResilienceOptions` from `Mvp24Hours.Infrastructure.Resilience.Native` | [Generic resilience](generic-resilience.md) |
| EF Core execution | EF execution strategy through `AddMvp24HoursDbContextWithResilience`, or EF Core Polly `AddNativeDbResilience` | [EF Core advanced](../database/efcore-advanced.md#resilience) |
| MongoDB operation | Driver retry settings plus `AddMongoDbResiliency`, or Polly v8 `AddNativeMongoDbResilience` | [MongoDB advanced](../database/mongodb-advanced.md#resiliency) |
| Cache call | `AddResilientCacheProvider` / `CacheResilienceOptions` | [Caching advanced](../caching-advanced.md#resilience) |
| Scheduled job | `AddResilientCronJob` / `CronJobResilienceConfig<T>` | [CronJob resilience](../cronjob-resilience.md) |
| Pipeline operation | `AddNativePipelineResilience` / `NativePipelineResilienceOptions` | [Pipeline](../pipeline.md) |
| CQRS request | Mediator retry/timeout/circuit-breaker behaviors, or `AddNativeCqrsResilience` | [CQRS behaviors](../cqrs/behaviors.md) |

## Naming traps

These collisions are the most common source of incorrect registration:

| Name | Meanings |
|---|---|
| `NativeResilienceOptions` | HTTP options in `Mvp24Hours.Infrastructure.Http.Resilience` versus generic options in `Mvp24Hours.Infrastructure.Resilience.Native` |
| `AddMvpResilience` | `IHttpClientBuilder` HTTP resilience versus Application `AddMvpResilience` exception-to-result mapping |
| `AddNativeDbResilience` | Infrastructure generic keyed `INativeResiliencePipeline` versus EF Core named Polly `ResiliencePipeline` |

Application `AddMvpResilience` is not operational resilience. It registers
exception mapping helpers such as `SafeExecutor` and does not retry, open a
circuit, or enforce timeouts. See
[Application Services](../application-services.md).

Qualify namespaces when both HTTP and generic resilience types are imported.

## HTTP versus generic pipelines

HTTP resilience understands HTTP outcomes, status codes, request timeouts, and
handler lifetimes. Use it inside `IHttpClientFactory`:

```csharp
services.AddHttpClient("Catalog")
    .AddMvpStandardResilience();
```

Generic resilience handles functions that are not HTTP requests:

```csharp
services.AddNativeResilience("inventory", options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableTimeout = true;
    options.TimeoutDuration = TimeSpan.FromSeconds(10);
});
```

Aspire `EnableResilience` and nested `AspireResilienceOptions` are configuration
contracts. They do not replace the HTTP registration above. See
[.NET Aspire](aspire.md).

## Database and messaging choices

EF Core has two distinct integration shapes:

1. `AddMvp24HoursDbContextWithResilience<TContext>` configures the provider
   execution strategy, command timeouts, pooling, and related infrastructure.
2. EF Core `AddNativeDbResilience` registers a named Polly v8
   `ResiliencePipeline` for explicitly wrapped database operations.

Do not assume that registering the named pipeline automatically wraps every EF
Core command. Resolve and execute through that pipeline where it is required.
Avoid adding an outer retry around an EF execution strategy unless the resulting
attempt count and transaction semantics are deliberate.

MongoDB already has driver-level `RetryReads` and `RetryWrites`. The module
path `AddMongoDbResiliency` and the Polly path `AddNativeMongoDbResilience` are
additional layers. Calculate the combined attempt and timeout budget before
enabling more than one.

## Module wrappers

Cache, CronJob, pipeline, and CQRS integrations carry module semantics that a
generic pipeline cannot infer:

- cache resilience supports graceful degradation and stale/fallback behavior
  and maps into the generic native pipeline internally;
- CronJob resilience is custom in-process job state, overlap prevention, and
  scheduling logic — not Polly / `Microsoft.Extensions.Resilience`;
- pipeline resilience wraps pipeline operations and rollback/error flow;
- CQRS has both legacy mediator behaviors and `AddNativeCqrsResilience`. Do not
  enable both stacks for the same request path unless the combined attempt
  budget is intentional.

Prefer the module wrapper when those semantics matter. Use a generic named
pipeline when the operation has no module-specific contract.

## Avoid policy multiplication

For one logical call, list every possible retry layer:

```text
CQRS behavior
  -> application service
    -> HttpClient resilience handler
      -> downstream service
```

If the CQRS behavior makes three attempts and the HTTP handler also makes
three, the downstream service can receive up to nine requests. The same issue
can occur with EF execution strategies, MongoDB driver retries, cache wrappers,
and job-level retries.

Set a single end-to-end timeout budget, then allocate time to the chosen retry
owner. Retries are normally appropriate only for transient failures and
idempotent operations. Commands with side effects need an idempotency key,
deduplication, inbox/outbox handling, or another explicit replay strategy.

## What is not operational resilience

- Application `AddMvpResilience` exception mapping
- Aspire resilience flags that only store configuration contracts
- CQRS idempotency and inbox/outbox patterns, which prevent duplicate side
  effects rather than retrying transient faults
- Health checks, which diagnose availability and do not replace request-time
  failure handling

## Production checklist

- Choose one primary retry owner per outbound boundary.
- Include all nested attempts when calculating the maximum request count.
- Make timeout ordering and cancellation propagation explicit.
- Test the actual transient exceptions or HTTP outcomes handled by the policy.
- Verify transaction and idempotency behavior before retrying writes.
- Export retry, timeout, circuit-state, and fallback telemetry.
- Prefer the registration truth in module pages over illustrative Polly samples
  on older CQRS deep-dive pages when they disagree.

## Related

- [Migration to native APIs](migration-guide.md)
- [HTTP clients and resilience reference](../infrastructure/http-resilience.md)
- [Observability](../observability/home.md)
- [CQRS inbox/outbox](../cqrs/resilience/inbox-outbox.md)
- [Configuration reference](../configuration-reference.md)
- [Microsoft resilience guidance](https://learn.microsoft.com/dotnet/core/resilience/)
