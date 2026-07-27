# HybridCache on .NET 10

`AddMvpHybridCache` integrates the native `Microsoft.Extensions.Caching.Hybrid.HybridCache` with Mvp24Hours `ICacheProvider`. It combines local L1 caching, optional `IDistributedCache` L2 storage, stampede protection, and tag invalidation.

## Registration

```csharp
using Mvp24Hours.Infrastructure.Caching.HybridCache;

builder.Services.AddMvpHybridCache();
```

Redis L2:

```csharp
string redis = builder.Configuration
    .GetConnectionString("RedisDbContext")
    ?? throw new InvalidOperationException("RedisDbContext is required.");

builder.Services.AddMvpHybridCacheWithRedis(redis, options =>
{
    options.RedisInstanceName = "orders:";
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(1);
    options.DefaultTags = ["orders-v1"];
});
```

```json
{
  "ConnectionStrings": {
    "RedisDbContext": "localhost:6379,abortConnect=false"
  }
}
```

`AddMvpHybridCacheWithRedis` sets `UseRedisAsL2` and the connection string, then invokes the supplied callback. `ReplaceCacheProviderWithHybridCache` removes existing `ICacheProvider` registrations before adding HybridCache.

## `MvpHybridCacheOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultExpiration` | `TimeSpan` | `5 minutes` | Default L2/entry expiration. |
| `DefaultLocalCacheExpiration` | `TimeSpan?` | `null` | L1 expiration; falls back to `DefaultExpiration`. |
| `MaximumPayloadBytes` | `long` | `1048576` | Maximum payload accepted by native HybridCache. |
| `MaximumKeyLength` | `int` | `1024` | Maximum native key length. |
| `UseRedisAsL2` | `bool` | `false` | Registers Redis as `IDistributedCache`. |
| `RedisConnectionString` | `string?` | `null` | Required when Redis L2 is enabled. |
| `RedisInstanceName` | `string?` | `mvp24h:` | Redis key prefix. |
| `EnableStampedeProtection` | `bool` | `true` | Mvp24Hours feature flag for stampede-protected use. |
| `ReportTagStatistics` | `bool` | `true` | Enables tag-manager statistics. |
| `DefaultTags` | `IList<string>` | empty | Tags applied by the provider to entries. |
| `EnableCompression` | `bool` | `false` | Enables provider compression. |
| `CompressionThresholdBytes` | `int` | `1024` | Compression threshold. |
| `EnableDetailedLogging` | `bool` | `false` | Enables detailed provider logs. |
| `KeyPrefix` | `string?` | `null` | Application/tenant key prefix. |
| `SerializerType` | `HybridCacheSerializerType` | `SystemTextJson` | `SystemTextJson`, `MessagePack`, or `Custom`. |
| `SerializerOptions` | `object?` | `null` | Serializer-specific settings. |

The native registration currently maps expiration, local expiration, payload size, and key length into `HybridCacheOptions`. Redis settings register `StackExchangeRedisCache`. `HybridCacheProvider` consumes key prefix, default tags, and detailed logging; the current implementation does not wire the compression, serializer-selection, stampede flag, or tag-statistics flags into additional runtime behavior. Native `GetOrCreateAsync` still provides stampede protection.

## Cache-aside usage

```csharp
public sealed class ProductReader(ICacheProvider cache, IProductStore store)
{
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return cache.GetOrCreateAsync(
            $"product:{id}",
            token => store.GetAsync(id, token),
            new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            tags: ["products", $"product:{id}"],
            cancellationToken);
    }
}
```

Use `GetOrCreateAsync` to benefit from native stampede protection. The provider also supports normal `ICacheProvider` get/set/remove APIs.

## Tag invalidation

```csharp
await cache.InvalidateByTagAsync("products", cancellationToken);
await cache.InvalidateByTagsAsync(
    ["products", "inventory"],
    cancellationToken);
```

These extensions require `HybridCacheProvider` and throw `InvalidOperationException` with a registration hint when used with another provider.

The default `InMemoryHybridCacheTagManager` is process-local. Multi-instance deployments can register the Redis implementation:

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redis));
builder.Services.Configure<RedisHybridCacheTagManagerOptions>(options =>
{
    options.DatabaseId = 0;
    options.KeyPrefix = "orders:tags:";
    options.TagExpiration = TimeSpan.FromDays(1);
    options.KeyTagsMappingExpiration = TimeSpan.FromDays(1);
});
builder.Services
    .AddHybridCacheTagManager<RedisHybridCacheTagManager>();
```

### `RedisHybridCacheTagManagerOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DatabaseId` | `int` | `0` | Redis logical database. |
| `KeyPrefix` | `string` | `mvp24h:tags:` | Tag metadata prefix. |
| `TagExpiration` | `TimeSpan?` | `null` | Tag-set expiration. |
| `KeyTagsMappingExpiration` | `TimeSpan?` | `null` | Key-to-tag mapping expiration. |

## Serialization

```csharp
builder.Services.AddMvpHybridCache(options =>
{
    options.SerializerType = HybridCacheSerializerType.SystemTextJson;
    options.SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
});
```

`SerializerType` and `SerializerOptions` are retained in the registered Mvp24Hours options, but the current extension does not translate them into native HybridCache serializer registrations. Register the required native serializer/factory separately; changing only these two properties does not switch serialization.

## Migration from custom multi-level cache

```csharp
builder.Services.ReplaceCacheProviderWithHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = redis;
    options.RedisInstanceName = "orders:";
});
```

Review key prefixes, serialized payload compatibility, expiration behavior, and distributed tag storage before cutover. HybridCache does not make Redis mandatory: with `UseRedisAsL2=false`, entries are local to each process.

## Resilience

HybridCache provides stampede protection, not remote-store retry/circuit breaking. For explicit cache failure handling, wrap the registered `ICacheProvider` with `CacheResilienceOptions`; see [Caching advanced](../caching-advanced.md#resilience).

## Testing

Use `AddMvpHybridCache()` for fast process-local tests. Use a Redis container when asserting cross-instance L2 behavior or Redis tag invalidation. Tests should verify:

- one factory invocation under concurrent same-key requests;
- L1 and L2 expiration differences;
- tag invalidation;
- Redis outage fallback/resilience;
- serialization compatibility.

## Related

- [Caching advanced](../caching-advanced.md)
- [CQRS caching](../cqrs/integration-caching.md)
- [.NET platform features](dotnet9-features.md)
