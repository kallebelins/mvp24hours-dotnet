# HybridCache - Caching Multi-Nível Nativo do .NET 9

## Visão Geral

**HybridCache** é a solução nativa do .NET 9 para caching multi-nível que combina o melhor de ambos os mundos:

- **L1 (In-Memory):** Cache rápido, local por instância da aplicação
- **L2 (Distribuído):** Cache compartilhado via IDistributedCache (Redis, SQL Server, etc.)
- **Proteção contra Stampede:** Prevenção nativa de cache stampedes

## Por que usar HybridCache?

| Funcionalidade | MultiLevelCache (Custom) | HybridCache (.NET 9) |
|----------------|--------------------------|----------------------|
| Suporte L1 + L2 | ✅ Implementação manual | ✅ Nativo |
| Proteção Stampede | ⚠️ SemaphoreSlim customizado | ✅ Nativo |
| Invalidação por Tags | ⚠️ Implementação customizada | ✅ RemoveByTagAsync nativo |
| Serialização | ⚠️ Configuração manual | ✅ Otimizada nativa |
| Performance | Boa | Melhor (otimização nativa) |
| Manutenção | Código do framework | .NET Runtime |

## Primeiros Passos

### Configuração Básica (Apenas In-Memory)

```csharp
// Program.cs
services.AddMvpHybridCache();
```

### Com Redis como L2 (Distribuído)

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

### Usando o Método de Conveniência

```csharp
// Atalho para configuração Redis
services.AddMvpHybridCacheWithRedis("localhost:6379", options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
});
```

## Padrões de Uso

### GetOrCreateAsync (Recomendado)

O padrão `GetOrCreateAsync` é a forma **recomendada** de usar HybridCache. Ele fornece:

- Busca automática no cache antes de executar a factory
- Proteção nativa contra stampede
- Execução única da factory mesmo com requisições concorrentes

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

### Padrão Simples Get/Set

```csharp
// Buscar do cache
var product = await _cache.GetAsync<Product>($"product:{id}");

if (product == null)
{
    // Carregar do banco de dados
    product = await _repository.GetByIdAsync(id);
    
    // Armazenar no cache com tags
    await _cache.SetWithTagsAsync(
        $"product:{id}",
        product,
        tags: new[] { "products", $"category:{product.CategoryId}" },
        expirationMinutes: 30);
}

return product;
```

### Invalidação por Tags

Tags permitem invalidar grupos de entradas de cache relacionadas:

```csharp
// Invalidar todos os produtos quando o catálogo mudar
await _cache.InvalidateByTagAsync("products");

// Invalidar todas as entradas de uma categoria específica
await _cache.InvalidateByTagAsync($"category:{categoryId}");

// Invalidar múltiplas tags de uma vez
await _cache.InvalidateByTagsAsync(new[] { "products", "categories", "inventory" });
```

## Opções de Configuração

### MvpHybridCacheOptions

| Opção | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `DefaultExpiration` | TimeSpan | 5 minutos | Expiração padrão para entradas |
| `DefaultLocalCacheExpiration` | TimeSpan? | null | Expiração L1 (usa DefaultExpiration se null) |
| `MaximumPayloadBytes` | long | 1MB | Tamanho máximo para entradas L1 |
| `MaximumKeyLength` | int | 1024 | Tamanho máximo da chave antes de hash |
| `UseRedisAsL2` | bool | false | Habilitar Redis como L2 |
| `RedisConnectionString` | string? | null | String de conexão Redis |
| `RedisInstanceName` | string? | "mvp24h:" | Prefixo de chaves Redis |
| `EnableStampedeProtection` | bool | true | Habilitar proteção stampede |
| `DefaultTags` | IList<string> | [] | Tags aplicadas a todas entradas |
| `EnableCompression` | bool | false | Comprimir valores grandes |
| `CompressionThresholdBytes` | int | 1024 | Tamanho mínimo para compressão |
| `EnableDetailedLogging` | bool | false | Habilitar logging de debug |
| `KeyPrefix` | string? | null | Prefixo para todas as chaves |
| `SerializerType` | enum | SystemTextJson | Serializador a usar |

### Exemplo de Configuração Completa

```csharp
services.AddMvpHybridCache(options =>
{
    // Expiração
    options.DefaultExpiration = TimeSpan.FromMinutes(15);
    options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(5);
    
    // Limites de tamanho
    options.MaximumPayloadBytes = 2 * 1024 * 1024; // 2MB
    options.MaximumKeyLength = 512;
    
    // Redis L2
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379,abortConnect=false";
    options.RedisInstanceName = "myapp:cache:";
    
    // Funcionalidades
    options.EnableStampedeProtection = true;
    options.EnableCompression = true;
    options.CompressionThresholdBytes = 4096; // 4KB
    
    // Tags
    options.DefaultTags = new List<string> { "v1" };
    
    // Multi-tenancy
    options.KeyPrefix = "tenant-123:";
    
    // Desenvolvimento
    options.EnableDetailedLogging = true;
});
```

## Gerenciamento de Tags

### Tag Manager In-Memory (Padrão)

Para aplicações de instância única:

```csharp
// Já registrado por padrão
services.AddMvpHybridCache();
```

### Tag Manager Redis (Distribuído)

Para aplicações multi-instância compartilhando cache:

```csharp
services.AddMvpHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});

// Substituir tag manager padrão pelo baseado em Redis
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
services.AddHybridCacheTagManager<RedisHybridCacheTagManager>();

// Configurar opções do tag manager Redis
services.Configure<RedisHybridCacheTagManagerOptions>(options =>
{
    options.DatabaseId = 1; // Usar DB diferente para tags
    options.KeyPrefix = "myapp:tags:";
    options.TagExpiration = TimeSpan.FromHours(24);
});
```

### Estatísticas de Tags

Monitore o uso de tags para otimização:

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

## Migração do MultiLevelCache

### Antes (MultiLevelCache - Deprecado)

```csharp
// Registro
services.AddSingleton<ICacheProvider>(sp =>
{
    var memory = sp.GetRequiredService<IMemoryCache>();
    var distributed = sp.GetRequiredService<IDistributedCache>();
    return new MultiLevelCache(
        new MemoryCacheProvider(memory),
        new DistributedCacheProvider(distributed));
});

// Uso
var value = await _cache.GetOrSetAsync(
    "key",
    async ct => await LoadDataAsync(),
    new CacheEntryOptions { ... });
```

### Depois (HybridCache - Recomendado)

```csharp
// Registro (muito mais simples!)
services.AddMvpHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});

// Uso (mesma interface, melhor performance)
var value = await _cache.GetOrCreateAsync(
    "key",
    async ct => await LoadDataAsync(),
    new CacheEntryOptions { ... },
    tags: new[] { "data" });
```

### Substituir Provider Existente

```csharp
// Remover ICacheProvider existente e substituir por HybridCache
services.ReplaceCacheProviderWithHybridCache(options =>
{
    options.UseRedisAsL2 = true;
    options.RedisConnectionString = "localhost:6379";
});
```

## Boas Práticas

### 1. Use Tags para Dados Relacionados

```csharp
// Produtos com tags por tipo de entidade e categoria
await _cache.SetWithTagsAsync(
    $"product:{product.Id}",
    product,
    new[] { "products", $"category:{product.CategoryId}", $"brand:{product.BrandId}" });

// Quando a categoria mudar, invalidar todos os produtos relacionados
await _cache.InvalidateByTagAsync($"category:{categoryId}");
```

### 2. Use GetOrCreateAsync em vez de Get/Set

```csharp
// ❌ Evitar: Condição de corrida, sem proteção stampede
var data = await _cache.GetAsync<Data>(key);
if (data == null)
{
    data = await LoadDataAsync();
    await _cache.SetAsync(key, data);
}

// ✅ Preferir: Atômico, protegido contra stampede
var data = await _cache.GetOrCreateAsync(key, ct => LoadDataAsync());
```

### 3. Configure Expirações Apropriadas

```csharp
// Dados estáticos: expiração mais longa
options.DefaultExpiration = TimeSpan.FromHours(1);

// Dados que mudam frequentemente: expiração L1 mais curta
options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(1);
```

### 4. Monitore a Performance do Cache

```csharp
// Use métricas nativas com OpenTelemetry
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMvp24HoursMetrics();
    });
```

## Serialização

### System.Text.Json (Padrão)

Melhor compatibilidade, boa performance:

```csharp
options.SerializerType = HybridCacheSerializerType.SystemTextJson;
options.SerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
```

### MessagePack (Mais Rápido)

Payloads menores, melhor performance:

```csharp
options.SerializerType = HybridCacheSerializerType.MessagePack;
```

## Solução de Problemas

### Cache Miss Quando Esperava Hit

1. Verifique a consistência das chaves (use helper de geração)
2. Verifique as configurações de expiração
3. Verifique a conexão L2 (Redis)
4. Habilite logging detalhado

```csharp
options.EnableDetailedLogging = true;
```

### Alto Uso de Memória

1. Reduza `MaximumPayloadBytes`
2. Habilite compressão
3. Use expiração L1 mais curta

```csharp
options.MaximumPayloadBytes = 512 * 1024; // 512KB
options.EnableCompression = true;
options.DefaultLocalCacheExpiration = TimeSpan.FromMinutes(2);
```

### Tags Não Funcionando

1. Certifique-se que HybridCacheProvider está registrado
2. Para apps distribuídos, use RedisHybridCacheTagManager
3. Verifique a consistência do formato das tags

## Referência da API

### Extensões ICacheProvider

| Método | Descrição |
|--------|-----------|
| `GetOrCreateAsync<T>` | Obter ou criar com factory e tags |
| `GetOrDefaultAsync<T>` | Obter com valor padrão de fallback |
| `SetWithTagsAsync<T>` | Definir com tags |
| `InvalidateByTagAsync` | Invalidar todas entradas com tag |
| `InvalidateByTagsAsync` | Invalidar múltiplas tags |
| `SetWithSlidingExpirationAsync<T>` | Definir com expiração deslizante |
| `ContainsKeyAsync` | Verificar se chave existe |
| `RemoveByPrefixAsync` | Remover chaves por prefixo |

### IHybridCacheTagManager

| Método | Descrição |
|--------|-----------|
| `TrackKeyWithTagsAsync` | Associar chave com tags |
| `RemoveKeyFromTagsAsync` | Remover chave do tracking de tags |
| `GetKeysByTagAsync` | Obter todas as chaves de uma tag |
| `GetTagsByKeyAsync` | Obter todas as tags de uma chave |
| `InvalidateTagAsync` | Invalidar uma tag |
| `GetStatistics` | Obter estatísticas de uso de tags |
| `ClearAsync` | Limpar todo tracking de tags |

## Veja Também

- [Rate Limiting](rate-limiting.md) - Rate limiting nativo com System.Threading.RateLimiting
- [Time Provider](time-provider.md) - Abstração de tempo para testabilidade
- [Observabilidade](../observability/home.md) - Logging, tracing e métricas

