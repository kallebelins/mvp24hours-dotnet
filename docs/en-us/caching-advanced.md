# Caching Advanced

`Mvp24Hours.Infrastructure.Caching` supplies `ICacheProvider`, memory/distributed providers, resilience, invalidation, warming, prefetching, observability, EF Core interception, and .NET HybridCache integration.

## Install and DI

```bash
dotnet add package Mvp24Hours.Infrastructure.Caching
dotnet add package Mvp24Hours.Infrastructure.Caching.Redis
```

```csharp
string redis = builder.Configuration
    .GetConnectionString("RedisDbContext")
    ?? throw new InvalidOperationException("RedisDbContext is required.");

builder.Services.AddMvp24HoursCaching(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    options.DefaultKeyPrefix = "orders";
    options.EnableCompression = true;
});
builder.Services.AddMvp24HoursCachingRedis(redis, instanceName: "orders");
```

`AddMvp24HoursCaching` selects an existing `IDistributedCache` when available and otherwise uses memory. Register Redis before the provider is first resolved. The Redis overload accepts either a connection string or `StackExchange.Redis.ConfigurationOptions`.

```json
{
  "ConnectionStrings": {
    "RedisDbContext": "localhost:6379,abortConnect=false"
  }
}
```

## `CacheOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultAbsoluteExpiration` | `TimeSpan?` | `5 minutes` | Default absolute TTL. |
| `DefaultSlidingExpiration` | `TimeSpan?` | `null` | Default sliding TTL. |
| `DefaultKeyPrefix` | `string?` | `null` | Key namespace. |
| `KeySeparator` | `string` | `:` | Key-part separator. |
| `UseHashForLongKeys` | `bool` | `true` | Hashes oversized/complex keys. |
| `MaxKeyLength` | `int` | `250` | Hashing threshold. |
| `EnableCompression` | `bool` | `false` | Compresses large values. |
| `CompressionThresholdBytes` | `int` | `1024` | Compression threshold. |
| `CompressionAlgorithm` | `CompressionAlgorithm` | `Brotli` | `Brotli` or `Gzip`. |
| `BatchSize` | `int` | `100` | Batch operation size. |
| `MaxBatchConcurrency` | `int` | `10` | Batch concurrency limit. |
| `EnablePrefetching` | `bool` | `false` | Enables prefetch behavior by convention. |
| `EnableWarming` | `bool` | `true` | Enables startup warming by convention. |

## Provider usage

```csharp
public sealed class ProductReader(ICacheProvider cache, IProductStore store)
{
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return cache.GetOrSetAsync(
            $"product:{id}",
            token => store.GetAsync(id, token),
            new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            cancellationToken);
    }
}
```

`ICacheProvider` provides typed/string get and set, remove/remove-many, exists, get-many/set-many, and refresh methods. The `GetOrSetAsync` extension implements cache-aside fallback.

## Resilience

Register the base provider first, then the resilient wrapper:

```csharp
builder.Services.AddMvp24HoursCaching();
builder.Services.AddMvp24HoursCachingRedis(redis);
builder.Services.AddResilientCacheProvider(options =>
{
    options.EnableCircuitBreaker = true;
    options.CircuitBreaker.FailureThreshold = 5;
    options.CircuitBreaker.MinimumThroughput = 10;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    options.EnableRetry = true;
    options.MaxRetries = 3;
    options.EnableGracefulDegradation = true;
});
```

`AddResilientCacheProvider` registers `ResilientCacheProvider`; inject that concrete wrapper when the base `ICacheProvider` registration must remain available. To replace it explicitly, register `ICacheProvider` with `baseProvider.WithResilience(...)`.

### `CacheResilienceOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `EnableCircuitBreaker` | `bool` | `true` | Enables the remote-cache circuit breaker. |
| `CircuitBreaker` | `CircuitBreakerOptions` | values below | Circuit breaker policy. |
| `EnableRetry` | `bool` | `true` | Enables transient retries. |
| `MaxRetries` | `int` | `3` | Retry attempts. |
| `RetryDelay` | `TimeSpan` | `100 ms` | Base delay. |
| `UseExponentialBackoff` | `bool` | `true` | Uses exponential delays. |
| `MaxRetryDelay` | `TimeSpan` | `5 seconds` | Delay cap. |
| `EnableGracefulDegradation` | `bool` | `true` | Returns null/default instead of throwing cache failures. |
| `LogFailures` | `bool` | `true` | Logs degraded operations. |
| `ShouldRetry` | `Func<Exception,bool>?` | `null` | Custom transient predicate. |
| `ShouldCountAsFailure` | `Func<Exception,bool>?` | `null` | Custom breaker predicate. |
| `OnFallback` | `Action<string,Exception>?` | `null` | Fallback callback. |
| `OnCircuitBreakerOpen` | `Action<string>?` | `null` | Breaker-open callback. |

The nested `CircuitBreaker` defaults are `FailureThreshold=5`, `SamplingDuration=30 seconds`, `MinimumThroughput=10`, `BreakDuration=30 seconds`, and `FailureRatio=0.5`.

Graceful degradation makes cache loss non-fatal; the source-of-truth call still needs its own timeout and resilience policy.

## Tags, dependencies, and invalidation

```csharp
builder.Services.AddCacheInvalidationFeatures();

var tags = serviceProvider.GetRequiredService<ICacheTagManager>();
await tags.TagKeyAsync("product:42", ["products", "category:7"], cancellationToken);
await tags.InvalidateByTagAsync("products", cancellationToken);
```

The individual registrations are `AddCacheTagManager`, `AddCacheDependencyManager`, `AddCacheStampedePrevention`, and `AddInMemoryCacheInvalidationEvents`. The in-memory event publisher synchronizes only inside one process; use a distributed publisher for multiple application instances.

HybridCache has native tag invalidation through `InvalidateByTagAsync` and `InvalidateByTagsAsync`; see [HybridCache](modernization/hybrid-cache.md).

## Prefetching and warming

```csharp
builder.Services
    .AddCachePrefetching()
    .AddCacheWarming(enableAutoWarmup: true)
    .AddCacheWarmupOperation<ProductCatalogWarmup>();
```

`ICachePrefetcher.PrefetchAsync` checks for an existing entry before calling the value factory. `ICacheWarmer.WarmUpAsync` executes registered `ICacheWarmupOperation` instances; the hosted service logs startup failures without failing application startup.

## EF Core interceptor

```csharp
builder.Services.AddSingleton(sp =>
    new EfCoreCacheInterceptor(
        sp.GetRequiredService<ICacheProvider>(),
        sp.GetService<ILogger<EfCoreCacheInterceptor>>(),
        new EfCoreCacheOptions
        {
            DefaultCacheDurationSeconds = 300,
            EnableCaching = true,
            InvalidateOnModify = true
        }));

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<EfCoreCacheInterceptor>());
});
```

### `EfCoreCacheOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultCacheDurationSeconds` | `int` | `300` | SELECT result TTL. |
| `EnableCaching` | `bool` | `true` | Enables SELECT interception. |
| `InvalidateOnModify` | `bool` | `true` | Invalidates affected table entries after writes. |

The interceptor derives keys from SQL and parameters. Validate behavior against the chosen provider and query shape before enabling it broadly.

## Compression and observability

```csharp
builder.Services
    .AddCacheCompression(
        CompressionAlgorithm.Brotli,
        CompressionLevel.Optimal,
        compressionThresholdBytes: 1024)
    .AddCacheMetrics()
    .AddCacheObservability();
```

`AddObservableCacheProvider` decorates an existing provider with tracing, metrics, and logging. Configure OpenTelemetry with the cache ActivitySource and Meter names exposed by `CacheActivitySource`.

## HybridCache

For native L1/L2 caching, stampede protection, and tags:

```csharp
builder.Services.AddMvpHybridCacheWithRedis(redis, options =>
{
    options.RedisInstanceName = "orders:";
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
});
```

See [HybridCache](modernization/hybrid-cache.md) for its complete options table.

## Testing

Use `AddMvp24HoursCaching()` without Redis for unit tests. Redis integration tests in this repository use `AddMvp24HoursCachingRedis(fixture.ConnectionString)` against a container. Test both cache hits and fallback behavior; resilience tests should assert retry, open-circuit, and graceful-degradation outcomes.

## Related

- [HybridCache](modernization/hybrid-cache.md)
- [CQRS caching](cqrs/integration-caching.md)
- [Repository usage](database/use-repository.md)
