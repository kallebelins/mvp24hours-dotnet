# CQRS Caching Integration

The mediator caching behaviors use `Microsoft.Extensions.Caching.Distributed.IDistributedCache`. They do not use Mvp24Hours `ICacheProvider` directly. Configure one distributed cache implementation, enable the query behavior, and opt individual requests into caching.

## Registration

Process-local development:

```csharp
builder.Services.AddMediatorMemoryCache();
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
});
```

Shared Redis cache:

```csharp
string redis = builder.Configuration
    .GetConnectionString("RedisDbContext")
    ?? throw new InvalidOperationException("RedisDbContext is required.");

builder.Services.AddMediatorRedisCache(redis, instanceName: "orders:");
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
    options.RegisterIdempotencyBehavior = true;
});
```

Advanced Redis configuration uses the real `RedisCacheOptions` overload:

```csharp
builder.Services.AddMediatorRedisCache(options =>
{
    options.Configuration = redis;
    options.InstanceName = "orders:";
    options.ConfigurationOptions = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectTimeout = 5_000,
        SyncTimeout = 5_000
    };
});
```

The `Mvp24Hours.Infrastructure.Caching.Redis` package also registers `IDistributedCache`, so `AddMvp24HoursCachingRedis` can supply the mediator dependency. Do not register two competing `IDistributedCache` providers.

## Cacheable queries

```csharp
public sealed record GetProductQuery(Guid ProductId)
    : IMediatorQuery<ProductDto?>, ICacheable
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
}

public sealed class GetProductHandler(IProductRepository repository)
    : IMediatorQueryHandler<GetProductQuery, ProductDto?>
{
    public Task<ProductDto?> Handle(
        GetProductQuery request,
        CancellationToken cancellationToken)
    {
        return repository.GetAsync(request.ProductId, cancellationToken);
    }
}
```

`CachingBehavior<TRequest,TResponse>`:

1. ignores requests that do not implement `ICacheable`;
2. reads `mediator:{CacheKey}` from `IDistributedCache`;
3. deserializes a hit with `System.Text.Json`;
4. invokes the handler on a miss;
5. serializes non-null results with absolute expiration.

### `ICacheable`

| Member | Type | Default | Description |
|---|---|---|---|
| `CacheKey` | `string?` | `null` | Logical key; the behavior prepends `mediator:`. |
| `CacheDuration` | `TimeSpan?` | `null` | Absolute TTL; defaults to five minutes. |

When `CacheKey` is null, the behavior derives a key from the request type and `string.GetHashCode()` of its JSON representation. Because that hash is not stable across every process/runtime, provide an explicit deterministic key for Redis and multi-instance deployments.

## Invalidation after commands

`CacheInvalidationBehavior<TRequest,TResponse>` is available, but `MediatorOptions.RegisterCachingBehavior` does not register it. Register it explicitly:

```csharp
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
});
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(CacheInvalidationBehavior<,>));
```

```csharp
public sealed record UpdateProductCommand(Guid ProductId, string Name)
    : IMediatorCommand<ProductDto>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate =>
        [$"product:{ProductId}", "products:all"];
}
```

Invalidation runs after a successful handler and removes `mediator:{key}` for every value in `CacheKeysToInvalidate`. Removal failures are logged and do not replace the successful command response.

| `ICacheInvalidator` member | Type | Description |
|---|---|---|
| `CacheKeysToInvalidate` | `IEnumerable<string>` | Logical keys removed after successful execution. |

## Idempotent commands

The idempotency behavior uses the same `IDistributedCache` infrastructure:

```csharp
public sealed record CapturePaymentCommand(
    Guid PaymentId,
    decimal Amount)
    : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public string IdempotencyKey => $"payment:{PaymentId}";
    public TimeSpan IdempotencyDuration => TimeSpan.FromHours(24);
}
```

Enable `RegisterIdempotencyBehavior`. For horizontally scaled applications, Redis is required for shared results. Use a business identifier as the key; the generated fallback also relies on serialized request data.

| `IIdempotentCommand` member | Type | Default | Description |
|---|---|---|---|
| `IdempotencyKey` | `string?` | `null` | Business idempotency key. |
| `IdempotencyDuration` | `TimeSpan?` | `null` | Result TTL; behavior default is 24 hours. |

## `MediatorCacheOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultQueryCacheDuration` | `TimeSpan` | `5 minutes` | Intended query cache default. |
| `DefaultIdempotencyDuration` | `TimeSpan` | `24 hours` | Intended idempotency default. |
| `KeyPrefix` | `string` | `mvp24mediator:` | Intended common key prefix. |
| `UseSlidingExpiration` | `bool` | `false` | Intended sliding-expiration switch. |

`MediatorCacheOptions` exists in the public API, but the current caching and idempotency behavior constructors do not consume it. Do not document `services.Configure<MediatorCacheOptions>` as changing runtime behavior until it is wired into those behaviors. Current behavior constants are `mediator:` plus five minutes for query caching and `idempotency:` plus 24 hours for idempotency.

## HybridCache boundary

`AddMvpHybridCacheWithRedis` registers Redis through `IDistributedCache`, so it can provide the mediator's distributed dependency, but mediator responses still use `IDistributedCache` directly and do not receive HybridCache L1/stampede/tag features. To use those features inside CQRS, implement cache-aside in a handler through `ICacheProvider`, or provide a custom `IPipelineBehavior<,>`.

See [HybridCache](../modernization/hybrid-cache.md) and [Caching advanced](../caching-advanced.md).

## Failure behavior

The query behavior treats cache read, serialization, and write errors as cache misses: it logs and returns the handler result. This is graceful degradation, not retry or circuit breaking. Apply Redis client settings and application resilience independently; avoid extending handler latency excessively during a cache outage.

## Testing

```csharp
services.AddMediatorMemoryCache();
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<GetProductHandler>();
    options.RegisterCachingBehavior = true;
});
```

Verify handler invocation count, TTL, deterministic keys, null results, invalidation after success, no invalidation after handler failure, and cross-instance behavior against Redis. Because these are standard request behaviors, they do not run for mediator streaming requests.

## Related

- [Behaviors](behaviors.md)
- [CQRS API reference](api-reference.md)
- [Caching advanced](../caching-advanced.md)
- [HybridCache](../modernization/hybrid-cache.md)
