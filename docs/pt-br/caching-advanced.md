# Caching - Funcionalidades Avançadas

Esta documentação cobre as funcionalidades avançadas de caching do módulo Mvp24Hours.Infrastructure.Caching.

## Sumário

1. [Visão Geral](#visão-geral)
2. [Provedores de Cache](#provedores-de-cache)
3. [Padrões de Cache](#padrões-de-cache)
4. [Cache Multi-Nível](#cache-multi-nível)
5. [Invalidação de Cache](#invalidação-de-cache)
6. [Resiliência](#resiliência)
7. [Performance](#performance)
8. [Observabilidade](#observabilidade)
9. [Integração com Repository](#integração-com-repository)

---

## Visão Geral

O módulo de Caching fornece cache de nível empresarial com:

- **Múltiplos provedores** (Memory, Distributed/Redis)
- **Cache multi-nível** (L1 + L2)
- **Padrões avançados** (cache-aside, read-through, write-through, write-behind)
- **Invalidação inteligente** (tags, dependências, pub/sub)
- **Resiliência** (circuit breaker, fallback, degradação graceful)
- **Observabilidade** (métricas, tracing, health checks)

### Instalação

```bash
dotnet add package Mvp24Hours.Infrastructure.Caching
```

### Configuração Básica

```csharp
// Program.cs
services.AddMvp24HoursCaching(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.EnableCompression = true;
    options.KeyPrefix = "meuapp:";
});
```

---

## Provedores de Cache

### Provedor de Cache em Memória

```csharp
services.AddMvp24HoursMemoryCache(options =>
{
    options.SizeLimit = 1024; // Máx de entradas
    options.CompactionPercentage = 0.25; // 25% em pressão de memória
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
});
```

### Provedor de Cache Distribuído (Redis)

```csharp
services.AddMvp24HoursDistributedCache(options =>
{
    options.ConnectionString = "localhost:6379";
    options.InstanceName = "meuapp:";
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
    options.EnableCompression = true;
});
```

### Usando o Provedor de Cache

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
        
        // Tentar obter do cache
        var cached = await _cache.GetAsync<Product>(cacheKey);
        if (cached != null) return cached;

        // Obter do banco de dados
        var product = await _repository.GetByIdAsync(id);
        
        // Armazenar no cache
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

## Padrões de Cache

### Padrão Cache-Aside

O padrão mais comum - verificar cache primeiro, buscar em caso de miss:

```csharp
// Helper simples
var product = await _cache.GetOrSetAsync(
    $"products:{id}",
    async () => await _repository.GetByIdAsync(id),
    TimeSpan.FromMinutes(30));

// Com opções
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

### Padrão Read-Through

Cache busca automaticamente em caso de miss:

```csharp
services.AddMvp24HoursReadThroughCache<Product>(options =>
{
    options.KeyGenerator = id => $"products:{id}";
    options.Loader = async id => await _repository.GetByIdAsync(int.Parse(id));
    options.Expiration = TimeSpan.FromMinutes(30);
});

// Uso
public class ProductService
{
    private readonly IReadThroughCache<Product> _cache;

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _cache.GetAsync(id.ToString());
    }
}
```

### Padrão Write-Through

Atualizações escritas no cache e banco de dados sincronamente:

```csharp
services.AddMvp24HoursWriteThroughCache<Product>(options =>
{
    options.KeyGenerator = p => $"products:{p.Id}";
    options.Writer = async product => await _repository.UpdateAsync(product);
    options.Expiration = TimeSpan.FromMinutes(30);
});

// Uso
public class ProductService
{
    private readonly IWriteThroughCache<Product> _cache;

    public async Task UpdateAsync(Product product)
    {
        await _cache.SetAsync(product);
        // Cache e banco de dados são atualizados
    }
}
```

### Padrão Write-Behind

Atualizações enfileiradas e escritas no banco de dados assincronamente:

```csharp
services.AddMvp24HoursWriteBehindCache<Product>(options =>
{
    options.KeyGenerator = p => $"products:{p.Id}";
    options.Writer = async products => await _repository.BulkUpdateAsync(products);
    options.FlushInterval = TimeSpan.FromSeconds(30);
    options.MaxBatchSize = 100;
});

// Uso (cache atualizado imediatamente, banco de dados em background)
await _cache.SetAsync(product);
```

### Padrão Refresh-Ahead

Atualiza cache proativamente antes da expiração:

```csharp
services.AddMvp24HoursRefreshAheadCache<Product>(options =>
{
    options.RefreshThreshold = 0.8; // Atualizar em 80% do TTL
    options.Loader = async key => await _repository.GetByIdAsync(int.Parse(key));
    options.Expiration = TimeSpan.FromMinutes(30);
});
```

---

## Cache Multi-Nível

Combine L1 rápido em memória com L2 distribuído:

```csharp
services.AddMvp24HoursMultiLevelCache(options =>
{
    // L1 - Memória (rápido, local)
    options.L1.Provider = CacheProviderType.Memory;
    options.L1.Expiration = TimeSpan.FromMinutes(1);
    options.L1.MaxSize = 1000;
    
    // L2 - Redis (compartilhado, persistente)
    options.L2.Provider = CacheProviderType.Distributed;
    options.L2.ConnectionString = "localhost:6379";
    options.L2.Expiration = TimeSpan.FromMinutes(30);
    
    // Sincronização
    options.EnableL1Invalidation = true; // Sync pub/sub entre instâncias
});
```

### Uso

```csharp
public class ProductService
{
    private readonly IMultiLevelCache _cache;

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _cache.GetOrSetAsync(
            $"products:{id}",
            async () => await _repository.GetByIdAsync(id));
        
        // Fluxo:
        // 1. Verificar L1 (memória) - microssegundos
        // 2. Se miss, verificar L2 (Redis) - milissegundos
        // 3. Se miss, chamar loader
        // 4. Armazenar em L2, depois L1
    }
}
```

---

## Invalidação de Cache

### Invalidação Baseada em Tags

Agrupe entradas de cache relacionadas por tags:

```csharp
// Armazenar com tags
await _cache.SetAsync($"products:{id}", product, new CacheEntryOptions
{
    Tags = new[] { "products", $"category:{product.CategoryId}" }
});

// Invalidar por tag
await _cache.InvalidateByTagAsync("products"); // Todos os produtos
await _cache.InvalidateByTagAsync($"category:{categoryId}"); // Produtos na categoria
```

### Invalidação Baseada em Dependência

Invalidar automaticamente quando dependências mudam:

```csharp
services.AddMvp24HoursCacheDependencyManager();

// Registrar dependência
_dependencyManager.RegisterDependency(
    $"products:{id}", 
    $"categories:{product.CategoryId}");

// Quando categoria muda, cache de produto é invalidado
await _dependencyManager.InvalidateAsync($"categories:{categoryId}");
```

### Invalidação Baseada em Eventos (Pub/Sub)

Sincronize cache entre múltiplas instâncias:

```csharp
services.AddMvp24HoursCacheInvalidationEvents(options =>
{
    options.Provider = InvalidationProvider.Redis;
    options.Channel = "cache:invalidation";
});

// Publicar evento de invalidação
await _cacheInvalidator.PublishInvalidationAsync("products:123");

// Todas as instâncias inscritas no canal irão invalidar
```

### Prevenção de Stampede

Evite thundering herd em cache miss:

```csharp
services.AddMvp24HoursCacheStampedePrevention(options =>
{
    options.LockTimeout = TimeSpan.FromSeconds(5);
    options.MaxConcurrentRequests = 1; // Apenas uma requisição vai ao DB
});

// Uso - lock automático
var product = await _cache.GetOrSetWithLockAsync(
    $"products:{id}",
    async () => await _repository.GetByIdAsync(id));
```

---

## Resiliência

### Circuit Breaker

Proteja contra falhas de cache:

```csharp
services.AddMvp24HoursResilientCache(options =>
{
    options.CircuitBreaker.FailureThreshold = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
    options.CircuitBreaker.MinimumThroughput = 10;
});
```

### Estratégia de Fallback

Defina comportamento de fallback quando cache está indisponível:

```csharp
services.AddMvp24HoursCacheFallback(options =>
{
    options.Strategy = CacheFallbackStrategy.BypassCache;
    // Ou: ReturnDefault, ThrowException, UseStaleData
    
    options.OnFallback = async (key, ex) =>
    {
        _logger.LogWarning(ex, "Cache fallback para {Key}", key);
    };
});
```

---

## Performance

### Compressão

Reduza overhead de rede/armazenamento para valores grandes:

```csharp
services.AddMvp24HoursCacheCompression(options =>
{
    options.Algorithm = CompressionAlgorithm.Brotli; // ou Gzip
    options.MinimumSizeBytes = 1024; // Comprimir apenas > 1KB
    options.CompressionLevel = CompressionLevel.Optimal;
});
```

### Operações em Lote

```csharp
// Obter múltiplas chaves
var products = await _cache.GetManyAsync<Product>(
    new[] { "products:1", "products:2", "products:3" });

// Definir múltiplas chaves
await _cache.SetManyAsync(new Dictionary<string, Product>
{
    ["products:1"] = product1,
    ["products:2"] = product2,
    ["products:3"] = product3
});

// Remover múltiplas chaves
await _cache.RemoveManyAsync(new[] { "products:1", "products:2" });
```

### Cache Warming

Pré-popular cache na inicialização da aplicação:

```csharp
services.AddMvp24HoursCacheWarming(options =>
{
    options.WarmOnStartup = true;
    options.ParallelDegree = 4;
});

// Definir tarefas de warmup
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

## Observabilidade

### Métricas

```csharp
services.AddMvp24HoursCacheMetrics(options =>
{
    options.MeterName = "meuapp.cache";
    options.RecordHitMiss = true;
    options.RecordLatency = true;
    options.RecordSize = true;
});

// Métricas expostas:
// - cache_hits_total
// - cache_misses_total
// - cache_hit_ratio
// - cache_operation_duration_seconds
// - cache_entries_count
// - cache_size_bytes
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

---

## Integração com Repository

### Repository com Cache

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

    // GetById, List, etc. automaticamente usam cache
}
```

---

## Veja Também

- [Integração Cache CQRS](cqrs/integration-caching.md)
- [Cache em Serviços de Aplicação](application-services.md#cache)
- [Repository de Database](database/use-repository.md)

