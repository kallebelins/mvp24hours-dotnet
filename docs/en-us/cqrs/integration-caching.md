# CachingBehavior with Redis

## Overview

The `CachingBehavior` enables automatic caching of query results using `IDistributedCache`, with support for Redis or memory.

## Configuration

### In-Memory Cache

```csharp
services.AddMediatorMemoryCache();

services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
});
```

### Redis Cache

```csharp
services.AddMediatorRedisCache(
    connectionString: "localhost:6379",
    instanceName: "myapp");

services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
});
```

## Cache Interfaces

### ICacheableRequest

Marks queries that can be cached:

```csharp
public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpiration { get; }
    TimeSpan? SlidingExpiration { get; }
}
```

### ICacheInvalidator

Marks commands that invalidate cache:

```csharp
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
```

## Cacheable Queries

### Basic Query with Cache

```csharp
public record GetProductByIdQuery : IMediatorQuery<ProductDto>, ICacheableRequest
{
    public Guid ProductId { get; init; }
    
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(30);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}
```

### Query with Dynamic Cache

```csharp
public record GetProductsQuery : IMediatorQuery<IReadOnlyList<ProductDto>>, ICacheableRequest
{
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    
    public string CacheKey => $"products:{Category ?? "all"}:{MinPrice}:{MaxPrice}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(15);
    public TimeSpan? SlidingExpiration => null;
}
```

### Paginated Query with Cache

```csharp
public record GetOrdersQuery : IMediatorQuery<PagedResult<OrderDto>>, ICacheableRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
    
    public string CacheKey => $"orders:page{Page}:size{PageSize}:status{Status ?? "all"}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(5);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(2);
}
```

## Cache Invalidation

### Command that Invalidates Cache

```csharp
public record UpdateProductCommand : IMediatorCommand<ProductDto>, ICacheInvalidator
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    
    public IEnumerable<string> CacheKeysToInvalidate => new[]
    {
        $"product:{ProductId}",
        "products:*" // Pattern to invalidate multiple keys
    };
}

public record DeleteProductCommand : IMediatorCommand, ICacheInvalidator
{
    public Guid ProductId { get; init; }
    
    public IEnumerable<string> CacheKeysToInvalidate => new[]
    {
        $"product:{ProductId}",
        "products:*"
    };
}
```

## How It Works

### CachingBehavior

```csharp
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        
        // Try to get from cache
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);
        
        // Execute handler
        var result = await next();

        // Store in cache
        var options = new DistributedCacheEntryOptions();
        
        if (request.AbsoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = request.AbsoluteExpiration;
        
        if (request.SlidingExpiration.HasValue)
            options.SlidingExpiration = request.SlidingExpiration;

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            options,
            cancellationToken);

        return result;
    }
}
```

### CacheInvalidationBehavior

```csharp
public sealed class CacheInvalidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
{
    private readonly IDistributedCache _cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var result = await next();

        foreach (var key in request.CacheKeysToInvalidate)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }

        return result;
    }
}
```

## Advanced Configuration

### MediatorCacheOptions

```csharp
services.AddMediatorRedisCache(
    "localhost:6379",
    configure: options =>
    {
        options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(30);
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(10);
        options.EnableCompression = true;
        options.KeyPrefix = "myapp:cache:";
    });
```

## Best Practices

1. **Unique Keys**: Use keys that uniquely identify the query
2. **Appropriate Expiration**: Configure TTL based on update frequency
3. **Consistent Invalidation**: Always invalidate when modifying data
4. **Efficient Serialization**: Use JSON or MessagePack
5. **Monitoring**: Monitor hit/miss ratio
6. **Fallback**: Handle cache errors gracefully

