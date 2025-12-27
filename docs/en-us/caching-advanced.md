# Caching - Advanced Features

This documentation covers the advanced caching features of the Mvp24Hours.Infrastructure.Caching module.

## Table of Contents

1. [Overview](#overview)
2. [Cache Providers](#cache-providers)
3. [Cache Patterns](#cache-patterns)
4. [Multi-Level Cache](#multi-level-cache)
5. [Cache Invalidation](#cache-invalidation)
6. [Resilience](#resilience)
7. [Performance](#performance)
8. [Observability](#observability)
9. [Repository Integration](#repository-integration)

---

## Overview

The Caching module provides enterprise-grade caching with:

- **Multiple providers** (Memory, Distributed/Redis)
- **Multi-level cache** (L1 + L2)
- **Advanced patterns** (cache-aside, read-through, write-through, write-behind)
- **Smart invalidation** (tags, dependencies, pub/sub)
- **Resilience** (circuit breaker, fallback, graceful degradation)
- **Observability** (metrics, tracing, health checks)

### Installation

```bash
dotnet add package Mvp24Hours.Infrastructure.Caching
```

### Basic Setup

```csharp
// Program.cs
services.AddMvp24HoursCaching(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.EnableCompression = true;
    options.KeyPrefix = "myapp:";
});
```

---

## Cache Providers

### Memory Cache Provider

```csharp
services.AddMvp24HoursMemoryCache(options =>
{
    options.SizeLimit = 1024; // Max entries
    options.CompactionPercentage = 0.25; // 25% on memory pressure
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
});
```

### Distributed Cache Provider (Redis)

```csharp
services.AddMvp24HoursDistributedCache(options =>
{
    options.ConnectionString = "localhost:6379";
    options.InstanceName = "myapp:";
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
    options.EnableCompression = true;
});
```

### Using the Cache Provider

```csharp
public class ProductService
{
    private readonly ICacheProvider _cache;

    public ProductService(ICacheProvider cache)
    {
        _cache = cache;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var cacheKey = $"products:{id}";
        
        // Try get from cache
        var cached = await _cache.GetAsync<Product>(cacheKey);
        if (cached != null) return cached;

        // Get from database
        var product = await _repository.GetByIdAsync(id);
        
        // Store in cache
        if (product != null)
        {
            await _cache.SetAsync(cacheKey, product, new CacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            });
        }

        return product;
    }
}
```

---

## Cache Patterns

### Cache-Aside Pattern

The most common pattern - check cache first, fetch on miss:

```csharp
// Simple helper
var product = await _cache.GetOrSetAsync(
    $"products:{id}",
    async () => await _repository.GetByIdAsync(id),
    TimeSpan.FromMinutes(30));

// With options
var product = await _cache.GetOrSetAsync(
    $"products:{id}",
    async () => await _repository.GetByIdAsync(id),
    new CacheEntryOptions
    {
        AbsoluteExpiration = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(15),
        Tags = new[] { "products" }
    });
```

### Read-Through Pattern

Cache automatically fetches on miss:

```csharp
services.AddMvp24HoursReadThroughCache<Product>(options =>
{
    options.KeyGenerator = id => $"products:{id}";
    options.Loader = async id => await _repository.GetByIdAsync(int.Parse(id));
    options.Expiration = TimeSpan.FromMinutes(30);
});

// Usage
public class ProductService
{
    private readonly IReadThroughCache<Product> _cache;

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _cache.GetAsync(id.ToString());
    }
}
```

### Write-Through Pattern

Updates written to cache and database synchronously:

```csharp
services.AddMvp24HoursWriteThroughCache<Product>(options =>
{
    options.KeyGenerator = p => $"products:{p.Id}";
    options.Writer = async product => await _repository.UpdateAsync(product);
    options.Expiration = TimeSpan.FromMinutes(30);
});

// Usage
public class ProductService
{
    private readonly IWriteThroughCache<Product> _cache;

    public async Task UpdateAsync(Product product)
    {
        await _cache.SetAsync(product);
        // Both cache and database are updated
    }
}
```

### Write-Behind Pattern

Updates queued and written to database asynchronously:

```csharp
services.AddMvp24HoursWriteBehindCache<Product>(options =>
{
    options.KeyGenerator = p => $"products:{p.Id}";
    options.Writer = async products => await _repository.BulkUpdateAsync(products);
    options.FlushInterval = TimeSpan.FromSeconds(30);
    options.MaxBatchSize = 100;
});

// Usage (cache updated immediately, database updated in background)
await _cache.SetAsync(product);
```

### Refresh-Ahead Pattern

Proactively refreshes cache before expiration:

```csharp
services.AddMvp24HoursRefreshAheadCache<Product>(options =>
{
    options.RefreshThreshold = 0.8; // Refresh at 80% of TTL
    options.Loader = async key => await _repository.GetByIdAsync(int.Parse(key));
    options.Expiration = TimeSpan.FromMinutes(30);
});
```

---

## Multi-Level Cache

Combine fast in-memory L1 with distributed L2:

```csharp
services.AddMvp24HoursMultiLevelCache(options =>
{
    // L1 - Memory (fast, local)
    options.L1.Provider = CacheProviderType.Memory;
    options.L1.Expiration = TimeSpan.FromMinutes(1);
    options.L1.MaxSize = 1000;
    
    // L2 - Redis (shared, persistent)
    options.L2.Provider = CacheProviderType.Distributed;
    options.L2.ConnectionString = "localhost:6379";
    options.L2.Expiration = TimeSpan.FromMinutes(30);
    
    // Synchronization
    options.EnableL1Invalidation = true; // Pub/sub sync between instances
});
```

### Usage

```csharp
public class ProductService
{
    private readonly IMultiLevelCache _cache;

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _cache.GetOrSetAsync(
            $"products:{id}",
            async () => await _repository.GetByIdAsync(id));
        
        // Flow:
        // 1. Check L1 (memory) - microseconds
        // 2. If miss, check L2 (Redis) - milliseconds
        // 3. If miss, call loader
        // 4. Store in L2, then L1
    }
}
```

### Manual Promotion/Demotion

```csharp
// Force promote from L2 to L1
await _cache.PromoteAsync($"products:{id}");

// Force demote from L1 (evict locally)
await _cache.DemoteAsync($"products:{id}");
```

---

## Cache Invalidation

### Tag-Based Invalidation

Group related cache entries by tags:

```csharp
// Store with tags
await _cache.SetAsync($"products:{id}", product, new CacheEntryOptions
{
    Tags = new[] { "products", $"category:{product.CategoryId}" }
});

// Invalidate by tag
await _cache.InvalidateByTagAsync("products"); // All products
await _cache.InvalidateByTagAsync($"category:{categoryId}"); // Products in category
```

### Dependency-Based Invalidation

Automatically invalidate when dependencies change:

```csharp
services.AddMvp24HoursCacheDependencyManager();

// Register dependency
_dependencyManager.RegisterDependency(
    $"products:{id}", 
    $"categories:{product.CategoryId}");

// When category changes, product cache is invalidated
await _dependencyManager.InvalidateAsync($"categories:{categoryId}");
```

### Event-Based Invalidation (Pub/Sub)

Synchronize cache across multiple instances:

```csharp
services.AddMvp24HoursCacheInvalidationEvents(options =>
{
    options.Provider = InvalidationProvider.Redis;
    options.Channel = "cache:invalidation";
});

// Publish invalidation event
await _cacheInvalidator.PublishInvalidationAsync("products:123");

// All instances subscribed to the channel will invalidate
```

### Stampede Prevention

Prevent thundering herd on cache miss:

```csharp
services.AddMvp24HoursCacheStampedePrevention(options =>
{
    options.LockTimeout = TimeSpan.FromSeconds(5);
    options.MaxConcurrentRequests = 1; // Only one request hits DB
});

// Usage - automatic locking
var product = await _cache.GetOrSetWithLockAsync(
    $"products:{id}",
    async () => await _repository.GetByIdAsync(id));
```

---

## Resilience

### Circuit Breaker

Protect against cache failures:

```csharp
services.AddMvp24HoursResilientCache(options =>
{
    options.CircuitBreaker.FailureThreshold = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
    options.CircuitBreaker.MinimumThroughput = 10;
});
```

### Fallback Strategy

Define fallback behavior when cache is unavailable:

```csharp
services.AddMvp24HoursCacheFallback(options =>
{
    options.Strategy = CacheFallbackStrategy.BypassCache;
    // Or: ReturnDefault, ThrowException, UseStaleData
    
    options.OnFallback = async (key, ex) =>
    {
        _logger.LogWarning(ex, "Cache fallback for {Key}", key);
    };
});
```

### Graceful Degradation

```csharp
public class ProductService
{
    private readonly IResilientCacheProvider _cache;

    public async Task<Product?> GetByIdAsync(int id)
    {
        var result = await _cache.GetAsync<Product>($"products:{id}");
        
        if (result.IsSuccess)
            return result.Value;
        
        if (result.IsCircuitOpen)
        {
            _logger.LogWarning("Cache circuit open, fetching from DB");
            return await _repository.GetByIdAsync(id);
        }
        
        // Handle other failure scenarios
        return await _repository.GetByIdAsync(id);
    }
}
```

### Retry Policy

```csharp
services.AddMvp24HoursCacheRetry(options =>
{
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromMilliseconds(100);
    options.UseExponentialBackoff = true;
    options.RetryOnExceptions = new[] { typeof(RedisConnectionException) };
});
```

---

## Performance

### Compression

Reduce network/storage overhead for large values:

```csharp
services.AddMvp24HoursCacheCompression(options =>
{
    options.Algorithm = CompressionAlgorithm.Brotli; // or Gzip
    options.MinimumSizeBytes = 1024; // Only compress > 1KB
    options.CompressionLevel = CompressionLevel.Optimal;
});
```

### Batch Operations

```csharp
// Get multiple keys
var products = await _cache.GetManyAsync<Product>(
    new[] { "products:1", "products:2", "products:3" });

// Set multiple keys
await _cache.SetManyAsync(new Dictionary<string, Product>
{
    ["products:1"] = product1,
    ["products:2"] = product2,
    ["products:3"] = product3
});

// Remove multiple keys
await _cache.RemoveManyAsync(new[] { "products:1", "products:2" });
```

### Prefetching

Load cache entries proactively:

```csharp
services.AddMvp24HoursCachePrefetcher(options =>
{
    options.Strategies.Add<MostAccessedPrefetchStrategy>();
    options.PrefetchInterval = TimeSpan.FromMinutes(5);
    options.MaxPrefetchCount = 100;
});

// Register known keys
_prefetcher.RegisterKey($"products:{popularProductId}", 
    async () => await _repository.GetByIdAsync(popularProductId));
```

### Cache Warming

Pre-populate cache on application startup:

```csharp
services.AddMvp24HoursCacheWarming(options =>
{
    options.WarmOnStartup = true;
    options.ParallelDegree = 4;
});

// Define warmup tasks
public class ProductCacheWarmer : ICacheWarmer
{
    public async Task WarmAsync(ICacheProvider cache, CancellationToken ct)
    {
        var products = await _repository.GetTopProductsAsync(100);
        foreach (var product in products)
        {
            await cache.SetAsync($"products:{product.Id}", product);
        }
    }
}
```

---

## Observability

### Metrics

```csharp
services.AddMvp24HoursCacheMetrics(options =>
{
    options.MeterName = "myapp.cache";
    options.RecordHitMiss = true;
    options.RecordLatency = true;
    options.RecordSize = true;
});

// Exposed metrics:
// - cache_hits_total
// - cache_misses_total
// - cache_hit_ratio
// - cache_operation_duration_seconds
// - cache_entries_count
// - cache_size_bytes
```

### OpenTelemetry Tracing

```csharp
services.AddMvp24HoursCacheTracing(options =>
{
    options.ActivitySourceName = "Mvp24Hours.Caching";
    options.RecordKeyInSpan = true;
    options.RecordValueSize = true;
});

// Traces include:
// - Operation type (Get, Set, Remove)
// - Cache key
// - Duration
// - Hit/Miss status
// - Error details
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddMvp24HoursCacheHealthCheck(options =>
    {
        options.Name = "redis";
        options.Tags = new[] { "ready" };
        options.FailureStatus = HealthStatus.Degraded;
        options.Timeout = TimeSpan.FromSeconds(5);
    });
```

### Structured Logging

```csharp
services.AddMvp24HoursCacheLogging(options =>
{
    options.LogLevel = LogLevel.Debug;
    options.LogHits = true;
    options.LogMisses = true;
    options.LogErrors = true;
    options.MaskKeys = new[] { "*password*", "*secret*" };
});
```

---

## Repository Integration

### Cacheable Repository

```csharp
services.AddMvp24HoursCacheableRepository<Product>(options =>
{
    options.KeyGenerator = p => $"products:{p.Id}";
    options.ListKeyPrefix = "products:list";
    options.Expiration = TimeSpan.FromMinutes(30);
});

public class ProductRepository : CacheableRepository<Product, MyDbContext>
{
    public ProductRepository(
        MyDbContext context, 
        ICacheProvider cache) 
        : base(context, cache)
    {
    }

    // GetById, List, etc. automatically use cache
}
```

### EF Core Interceptor

Automatic caching for EF Core queries:

```csharp
services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(new EfCoreCacheInterceptor(_cache));
});
```

---

## Complete Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Multi-level cache with full features
builder.Services.AddMvp24HoursMultiLevelCache(options =>
{
    // L1 - Memory
    options.L1.Expiration = TimeSpan.FromMinutes(1);
    options.L1.MaxSize = 1000;
    
    // L2 - Redis
    options.L2.ConnectionString = builder.Configuration.GetConnectionString("Redis");
    options.L2.Expiration = TimeSpan.FromMinutes(30);
    
    // Features
    options.EnableCompression = true;
    options.EnableL1Invalidation = true;
});

// Resilience
builder.Services.AddMvp24HoursResilientCache(options =>
{
    options.CircuitBreaker.FailureThreshold = 5;
    options.Retry.MaxRetries = 3;
    options.Fallback.Strategy = CacheFallbackStrategy.BypassCache;
});

// Invalidation
builder.Services.AddMvp24HoursCacheInvalidationEvents(options =>
{
    options.Provider = InvalidationProvider.Redis;
});

// Observability
builder.Services.AddMvp24HoursCacheMetrics();
builder.Services.AddMvp24HoursCacheTracing();
builder.Services.AddHealthChecks().AddMvp24HoursCacheHealthCheck();

// Cache warming
builder.Services.AddMvp24HoursCacheWarming(options =>
{
    options.WarmOnStartup = true;
});
builder.Services.AddTransient<ICacheWarmer, ProductCacheWarmer>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.Run();
```

---

## See Also

- [CQRS Caching Integration](cqrs/integration-caching.md)
- [Application Services Caching](application-services.md#caching)
- [Database Repository](database/use-repository.md)

