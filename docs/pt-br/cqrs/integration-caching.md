# CachingBehavior com Redis

## Visão Geral

O `CachingBehavior` permite cache automático de resultados de queries usando `IDistributedCache`, com suporte para Redis ou memória.

## Configuração

### Cache em Memória

```csharp
services.AddMediatorMemoryCache();

services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
});
```

### Cache com Redis

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

## Interfaces de Cache

### ICacheableRequest

Marca queries que podem ser cacheadas:

```csharp
public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpiration { get; }
    TimeSpan? SlidingExpiration { get; }
}
```

### ICacheInvalidator

Marca comandos que invalidam cache:

```csharp
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
```

## Queries Cacheáveis

### Query Básica com Cache

```csharp
public record GetProductByIdQuery : IMediatorQuery<ProductDto>, ICacheableRequest
{
    public Guid ProductId { get; init; }
    
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(30);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}
```

### Query com Cache Dinâmico

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

### Query Paginada com Cache

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

## Invalidação de Cache

### Comando que Invalida Cache

```csharp
public record UpdateProductCommand : IMediatorCommand<ProductDto>, ICacheInvalidator
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    
    public IEnumerable<string> CacheKeysToInvalidate => new[]
    {
        $"product:{ProductId}",
        "products:*" // Padrão para invalidar múltiplas chaves
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

## Como Funciona

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
        
        // Tenta obter do cache
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);
        
        // Executa handler
        var result = await next();

        // Armazena no cache
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

## Configurações Avançadas

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

## Boas Práticas

1. **Chaves Únicas**: Use chaves que identifiquem unicamente a query
2. **Expiração Adequada**: Configure TTL baseado na frequência de atualização
3. **Invalidação Consistente**: Sempre invalide ao modificar dados
4. **Serialização Eficiente**: Use JSON ou MessagePack
5. **Monitoramento**: Monitore hit/miss ratio
6. **Fallback**: Trate erros de cache graciosamente

