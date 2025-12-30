# HybridCache - .NET 9 Native Multi-Level Caching

## Overview

**HybridCache** is the native .NET 9 solution for multi-level caching that combines the best of both worlds:

- **L1 (In-Memory):** Fast, local cache per application instance
- **L2 (Distributed):** Shared cache via IDistributedCache (Redis, SQL Server, etc.)
- **Stampede Protection:** Built-in prevention of cache stampedes

## Why Use HybridCache?

| Feature | MultiLevelCache (Custom) | HybridCache (.NET 9) |
|---------|--------------------------|----------------------|
| L1 + L2 Support | ✅ Manual implementation | ✅ Native |
| Stampede Protection | ⚠️ Custom SemaphoreSlim | ✅ Built-in |
| Tag-based Invalidation | ⚠️ Custom implementation | ✅ Native RemoveByTagAsync |
| Serialization | ⚠️ Custom setup | ✅ Optimized native |
| Performance | Good | Better (native optimization) |
| Maintenance | Framework code | .NET Runtime |

## Getting Started

### Basic Setup (In-Memory Only)

```csharp
// Program.cs
services.AddMvpHybridCache();
```

### With Redis as L2 (Distributed)

```csharp
// Program.cs
services.AddMvpHybridCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
    options.RedisInstanceName = "myapp:";
});
```

### Using the Convenience Method

```csharp
// Shorthand for Redis configuration
services.AddMvpHybridCacheWithRedis("localhost:6379", options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
});
```

## Usage Patterns

### GetOrCreateAsync (Recommended)

The `GetOrCreateAsync` pattern is the **recommended** way to use HybridCache. It provides:

- Automatic cache lookup before factory execution
- Native stampede protection
- Single factory call even with concurrent requests

```csharp
public class ProductService
{
    private readonly ICacheProvider _cache;
    private readonly IProductRepository _repository;

    public ProductService(ICacheProvider cache, IProductRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<Product> GetProductAsync(int id, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
            $"product:{id}",
            async token => await _repository.GetByIdAsync(id, token),
            new CacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) 
            },
            tags: new[] { "products", $"product:{id}" },
            ct);
    }
}
```

### Simple Get/Set Pattern

```csharp
// Get from cache
var product = await _cache.GetAsync<Product>($"product:{id}");

if (product == null)
{
    // Load from database
    product = await _repository.GetByIdAsync(id);
    
    // Store in cache with tags
    await _cache.SetWithTagsAsync(
        $"product:{id}",
        product,
        tags: new[] { "products", $"category:{product.CategoryId}" },
        expirationMinutes: 30);
}

return product;
```

### Tag-Based Invalidation

Tags allow you to invalidate groups of related cache entries:

```csharp
// Invalidate all products when catalog changes
await _cache.InvalidateByTagAsync("products");

// Invalidate all entries for a specific category
await _cache.InvalidateByTagAsync($"category:{categoryId}");

// Invalidate multiple tags at once
await _cache.InvalidateByTagsAsync(new[] { "products", "categories", "inventory" });
```

## Configuration Options

### MvpHybridCacheOptions

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultExpiration` | TimeSpan | 5 minutes | Default expiration for cache entries |
| `DefaultLocalCacheExpiration` | TimeSpan? | null | L1 cache expiration (defaults to DefaultExpiration) |
| `MaximumPayloadBytes` | long | 1MB | Max size for L1 cache entries |
| `MaximumKeyLength` | int | 1024 | Max key length before hashing |
| `UseRedisAsL2` | bool | false | Enable Redis as L2 cache |
| `RedisConnectionString` | string? | null | Redis connection string |
| `RedisInstanceName` | string? | "mvp24h:" | Redis key prefix |
| `EnableStampedeProtection` | bool | true | Enable stampede protection |
| `DefaultTags` | IList<string> | [] | Tags applied to all entries |
| `EnableCompression` | bool | false | Compress large values |
| `CompressionThresholdBytes` | int | 1024 | Min size for compression |
| `EnableDetailedLogging` | bool | false | Enable debug logging |
| `KeyPrefix` | string? | null | Prefix for all keys |
| `SerializerType` | enum | SystemTextJson | Serializer to use |

### Full Configuration Example

```csharp
services.AddMvpHybridCache(options =>
{
    // Expiration
    options.DefaultExpiration = TimeSpan.FromMinutes(15);
    options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(5);
    
    // Size limits
    options.MaximumPayloadBytes = 2 * 1024 * 1024; // 2MB
    options.MaximumKeyLength = 512;
    
    // Redis L2
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379,abortConnect=false";
    options.RedisInstanceName = "myapp:cache:";
    
    // Features
    options.EnableStampedeProtection = true;
    options.EnableCompression = true;
    options.CompressionThresholdBytes = 4096; // 4KB
    
    // Tags
    options.DefaultTags = new List<string> { "v1" };
    
    // Multi-tenancy
    options.KeyPrefix = "tenant-123:";
    
    // Development
    options.EnableDetailedLogging = true;
});
```

## Tag Management

### In-Memory Tag Manager (Default)

For single-instance applications:

```csharp
// Already registered by default
services.AddMvpHybridCache();
```

### Redis Tag Manager (Distributed)

For multi-instance applications sharing cache:

```csharp
services.AddMvpHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});

// Replace default tag manager with Redis-based one
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
services.AddHybridCacheTagManager<RedisHybridCacheTagManager>();

// Configure Redis tag manager options
services.Configure<RedisHybridCacheTagManagerOptions>(options =>
{
    options.DatabaseId = 1; // Use different DB for tags
    options.KeyPrefix = "myapp:tags:";
    options.TagExpiration = TimeSpan.FromHours(24);
});
```

### Tag Statistics

Monitor tag usage for optimization:

```csharp
public class CacheMonitorController : ControllerBase
{
    private readonly IHybridCacheTagManager _tagManager;

    [HttpGet("stats")]
    public IActionResult GetTagStatistics()
    {
        var stats = _tagManager.GetStatistics();
        return Ok(new
        {
            TotalTags = stats.TotalTags,
            TotalAssociations = stats.TotalAssociations,
            TagInvalidations = stats.TagInvalidations,
            KeysPerTag = stats.KeysPerTag
        });
    }
}
```

## Migration from MultiLevelCache

### Before (MultiLevelCache - Deprecated)

```csharp
// Registration
services.AddSingleton<ICacheProvider>(sp =>
{
    var memory = sp.GetRequiredService<IMemoryCache>();
    var distributed = sp.GetRequiredService<IDistributedCache>();
    return new MultiLevelCache(
        new MemoryCacheProvider(memory),
        new DistributedCacheProvider(distributed));
});

// Usage
var value = await _cache.GetOrSetAsync(
    "key",
    async ct => await LoadDataAsync(),
    new CacheEntryOptions { ... });
```

### After (HybridCache - Recommended)

```csharp
// Registration (much simpler!)
services.AddMvpHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});

// Usage (same interface, better performance)
var value = await _cache.GetOrCreateAsync(
    "key",
    async ct => await LoadDataAsync(),
    new CacheEntryOptions { ... },
    tags: new[] { "data" });
```

### Replace Existing Provider

```csharp
// Remove existing ICacheProvider and replace with HybridCache
services.ReplaceCacheProviderWithHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});
```

## Best Practices

### 1. Use Tags for Related Data

```csharp
// Products tagged by entity type and category
await _cache.SetWithTagsAsync(
    $"product:{product.Id}",
    product,
    new[] { "products", $"category:{product.CategoryId}", $"brand:{product.BrandId}" });

// When category changes, invalidate all related products
await _cache.InvalidateByTagAsync($"category:{categoryId}");
```

### 2. Use GetOrCreateAsync Instead of Get/Set

```csharp
// ❌ Avoid: Race condition, no stampede protection
var data = await _cache.GetAsync<Data>(key);
if (data == null)
{
    data = await LoadDataAsync();
    await _cache.SetAsync(key, data);
}

// ✅ Prefer: Atomic, stampede-protected
var data = await _cache.GetOrCreateAsync(key, ct => LoadDataAsync());
```

### 3. Configure Appropriate Expirations

```csharp
// Static data: longer expiration
options.DefaultExpiration = TimeSpan.FromHours(1);

// Frequently changing data: shorter L1 expiration
options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(1);
```

### 4. Monitor Cache Performance

```csharp
// Use built-in metrics with OpenTelemetry
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMvp24HoursMetrics();
    });
```

## Serialization

### System.Text.Json (Default)

Best compatibility, good performance:

```csharp
options.SerializerType = HybridCacheSerializerType.SystemTextJson;
options.SerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
```

### MessagePack (Faster)

Smaller payloads, better performance:

```csharp
options.SerializerType = HybridCacheSerializerType.MessagePack;
```

## Troubleshooting

### Cache Miss When Expected Hit

1. Check key consistency (use key generation helper)
2. Verify expiration settings
3. Check L2 connection (Redis)
4. Enable detailed logging

```csharp
options.EnableDetailedLogging = true;
```

### High Memory Usage

1. Reduce `MaximumPayloadBytes`
2. Enable compression
3. Use shorter L1 expiration

```csharp
options.MaximumPayloadBytes = 512 * 1024; // 512KB
options.EnableCompression = true;
options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(2);
```

### Tags Not Working

1. Ensure HybridCacheProvider is registered
2. For distributed apps, use RedisHybridCacheTagManager
3. Check tag format consistency

## API Reference

### ICacheProvider Extensions

| Method | Description |
|--------|-------------|
| `GetOrCreateAsync<T>` | Get or create with factory and tags |
| `GetOrDefaultAsync<T>` | Get with default value fallback |
| `SetWithTagsAsync<T>` | Set with tags |
| `InvalidateByTagAsync` | Invalidate all entries with tag |
| `InvalidateByTagsAsync` | Invalidate multiple tags |
| `SetWithSlidingExpirationAsync<T>` | Set with sliding expiration |
| `ContainsKeyAsync` | Check if key exists |
| `RemoveByPrefixAsync` | Remove keys by prefix |

### IHybridCacheTagManager

| Method | Description |
|--------|-------------|
| `TrackKeyWithTagsAsync` | Associate key with tags |
| `RemoveKeyFromTagsAsync` | Remove key from tag tracking |
| `GetKeysByTagAsync` | Get all keys for a tag |
| `GetTagsByKeyAsync` | Get all tags for a key |
| `InvalidateTagAsync` | Invalidate a tag |
| `GetStatistics` | Get tag usage statistics |
| `ClearAsync` | Clear all tag tracking |

## See Also

- [Rate Limiting](rate-limiting.md) - Native rate limiting with System.Threading.RateLimiting
- [Time Provider](time-provider.md) - Time abstraction for testability
- [Observability](../observability/home.md) - Logging, tracing, and metrics

