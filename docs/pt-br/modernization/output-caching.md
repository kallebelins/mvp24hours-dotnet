# Output Caching - Cache Nativo de Servidor do ASP.NET Core

## Visão Geral

**Output Caching** é um mecanismo de cache do lado do servidor introduzido no .NET 7 que armazena respostas HTTP completas no servidor e as serve diretamente sem re-executar a lógica do endpoint. Diferente do Response Caching (que depende de headers HTTP de cache), o Output Caching é totalmente controlado pelo servidor.

## Características Principais

| Característica | Response Caching | Output Caching |
|----------------|------------------|----------------|
| Local do Cache | Cliente/Proxy (headers HTTP) | Servidor |
| Controle | Headers Cache-Control HTTP | Políticas do servidor |
| Invalidação | Limitada (baseada em tempo) | ✅ Programática por tags |
| Suporte Distribuído | ❌ | ✅ Backend Redis |
| Baseado em Políticas | ❌ | ✅ Políticas nomeadas |
| Suporte a Vary | Limitado | ✅ Query, Header, Route |

## Começando

### Configuração Básica (Em Memória)

```csharp
// Program.cs
builder.Services.AddMvp24HoursOutputCache();

var app = builder.Build();

app.UseRouting();
app.UseMvp24HoursOutputCache();
app.MapControllers();
```

### Com Políticas Padrão

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
    options.AddStandardPolicies(); // Adiciona: Default, Short, Medium, Long, NoCache
});
```

### Com Backend Redis (Distribuído)

```csharp
// Para implantações com múltiplas instâncias
builder.Services.AddMvp24HoursOutputCacheWithRedis(
    "localhost:6379",
    options =>
    {
        options.AddStandardPolicies();
        options.RedisInstanceName = "myapp:oc:";
    });
```

## Políticas Nomeadas

O Mvp24Hours fornece várias políticas pré-definidas:

| Política | Duração | Caso de Uso |
|----------|---------|-------------|
| `NoCache` | Nenhuma | Desabilitar cache |
| `Short` | 1 minuto | Dados frequentemente alterados |
| `Medium` | 10 minutos | Dados moderadamente alterados |
| `Long` | 1 hora | Dados raramente alterados |
| `VeryLong` | 24 horas | Conteúdo estático |
| `Authenticated` | 5 minutos | Dados específicos do usuário (varia por Authorization) |
| `Api` | 5 minutos | Respostas de API (varia por Accept header) |

### Usando Políticas com Minimal APIs

```csharp
// Usando política nomeada
app.MapGet("/produtos", GetProdutos)
   .CacheOutput("Medium");

// Usando extensão do Mvp24Hours
app.MapGet("/produtos", GetProdutos)
   .CacheOutputWithPolicy("Medium");

// Configuração inline
app.MapGet("/produtos", GetProdutos)
   .CacheOutputFor(
       TimeSpan.FromMinutes(5),
       tags: new[] { "produtos" },
       varyByQuery: new[] { "categoria", "pagina" });

// Desabilitar cache
app.MapPost("/pedidos", CriarPedido)
   .NoCacheOutput();
```

### Usando Políticas com Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    // Usar política nomeada
    [HttpGet]
    [OutputCache(PolicyName = "Medium")]
    public async Task<IActionResult> GetAll()
    {
        // ...
    }

    // Configuração customizada
    [HttpGet("{id}")]
    [OutputCache(Duration = 60, VaryByRouteValueNames = new[] { "id" })]
    public async Task<IActionResult> GetById(int id)
    {
        // ...
    }

    // Desabilitar cache
    [HttpPost]
    [OutputCache(NoStore = true)]
    public async Task<IActionResult> Create([FromBody] ProdutoDto dto)
    {
        // ...
    }
}
```

## Políticas Customizadas

### Criando Políticas Nomeadas

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    // Política de produtos com tags para invalidação seletiva
    options.AddPolicy("Produtos", p => p
        .Expire(TimeSpan.FromMinutes(10))
        .SetTags("produtos", "catalogo")
        .SetVaryByQuery("categoria", "pagina", "ordenacao"));

    // Conteúdo específico do usuário
    options.AddPolicy("PerfilUsuario", p => p
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Authorization")
        .SetTags("usuarios")
        .AllowAuthenticatedRequests());

    // Resultados de busca com todos os parâmetros de query
    options.AddPolicy("Busca", p => p
    {
        p.ExpirationTimeSpan = TimeSpan.FromMinutes(2);
        p.VaryByAllQueryKeys = true;
        p.Tags.Add("busca");
    });

    // Conteúdo localizado
    options.AddPolicy("Localizado", p => p
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader("Accept-Language")
        .SetTags("conteudo"));
});
```

## Invalidação de Cache

### Invalidação por Tags

O Output Caching suporta invalidação por tags através do `IOutputCacheInvalidator`:

```csharp
public class ProdutoService
{
    private readonly IProdutoRepository _repository;
    private readonly IOutputCacheInvalidator _cacheInvalidator;

    public ProdutoService(
        IProdutoRepository repository,
        IOutputCacheInvalidator cacheInvalidator)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Produto> CriarProdutoAsync(ProdutoDto dto)
    {
        var produto = await _repository.CreateAsync(dto);
        
        // Invalidar todas as entradas de cache relacionadas a produtos
        await _cacheInvalidator.EvictByTagAsync("produtos");
        
        return produto;
    }

    public async Task AtualizarProdutoAsync(int id, ProdutoDto dto)
    {
        await _repository.UpdateAsync(id, dto);
        
        // Invalidar produto específico e lista geral de produtos
        await _cacheInvalidator.EvictByTagsAsync(new[] 
        { 
            "produtos",
            $"produto:{id}" 
        });
    }
}
```

### Usando com Commands CQRS

```csharp
public class CriarProdutoCommandHandler : ICommandHandler<CriarProdutoCommand, Produto>
{
    private readonly IProdutoRepository _repository;
    private readonly IOutputCacheInvalidator _cacheInvalidator;

    public async Task<Produto> Handle(
        CriarProdutoCommand command, 
        CancellationToken cancellationToken)
    {
        var produto = await _repository.CreateAsync(command.Data);
        
        // Invalidar cache após comando bem-sucedido
        await _cacheInvalidator.EvictByTagAsync("produtos", cancellationToken);
        
        return produto;
    }
}
```

## Estratégias de Vary-By

### Variar por Query String

```csharp
// Variar por chaves específicas
options.AddPolicy("Busca", p => p
    .SetVaryByQuery("q", "pagina", "tamanho")
    .Expire(TimeSpan.FromMinutes(2)));

// Variar por todas as chaves de query
options.AddPolicy("BuscaDinamica", p => p
{
    p.VaryByAllQueryKeys = true;
    p.ExpirationTimeSpan = TimeSpan.FromMinutes(2);
});
```

### Variar por Header

```csharp
// Variar por Accept-Language para conteúdo localizado
options.AddPolicy("Localizado", p => p
    .SetVaryByHeader("Accept-Language")
    .Expire(TimeSpan.FromHours(1)));

// Variar por múltiplos headers
options.AddPolicy("MultiHeader", p => p
    .SetVaryByHeader("Accept", "Accept-Language", "Accept-Encoding")
    .Expire(TimeSpan.FromMinutes(30)));
```

### Variar por Valores de Rota

```csharp
// Variar por parâmetros de rota
options.AddPolicy("DetalhesEntidade", p => p
    .SetVaryByRouteValue("id")
    .Expire(TimeSpan.FromMinutes(10)));

// Uso com Minimal API
app.MapGet("/produtos/{id}", GetProdutoPorId)
   .CacheOutput(policy => policy
       .Expire(TimeSpan.FromMinutes(10))
       .SetVaryByRouteValue("id")
       .Tag($"produtos"));
```

## Integração com Redis

### Por que Usar Redis para Output Caching?

- **Implantações multi-instância:** Cache compartilhado entre todas as instâncias
- **Persistência:** Cache sobrevive a reinicializações da aplicação
- **Escalabilidade:** Reduz uso de memória para o Redis
- **Invalidação centralizada:** Invalida em todas as instâncias

### Configuração

```csharp
builder.Services.AddMvp24HoursOutputCacheWithRedis(
    "localhost:6379,abortConnect=false",
    options =>
    {
        options.RedisInstanceName = "myapp:oc:";
        options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
        options.AddStandardPolicies();
        
        // Políticas customizadas
        options.AddPolicy("Produtos", p => p
            .Expire(TimeSpan.FromMinutes(5))
            .SetTags("produtos"));
    });
```

### Opções de Conexão Redis

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.UseDistributedCache = true;
    options.RedisConnectionString = 
        "redis-server:6379,password=secret,ssl=True,abortConnect=false";
    options.RedisInstanceName = "prod:oc:";
});
```

## Caminhos Excluídos

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    // Excluir caminhos específicos do cache
    options.ExcludedPaths.Add("/api/health");
    options.ExcludedPaths.Add("/api/admin/*");
    options.ExcludedPaths.Add("/api/auth/*");
});
```

## Opções de Configuração

### Propriedades de OutputCachingOptions

| Propriedade | Tipo | Padrão | Descrição |
|-------------|------|--------|-----------|
| `Enabled` | bool | `true` | Habilitar/desabilitar output caching |
| `DefaultExpirationTimeSpan` | TimeSpan | 5 min | Duração padrão do cache |
| `MaximumBodySize` | long | 100 MB | Tamanho máximo de resposta para cache |
| `SizeLimit` | long | 100 MB | Limite total de tamanho do cache |
| `UseDistributedCache` | bool | `false` | Habilitar backend Redis |
| `RedisConnectionString` | string? | null | String de conexão Redis |
| `RedisInstanceName` | string | `"mvp24h-oc:"` | Prefixo de chave Redis |
| `UseCaseSensitivePaths` | bool | `false` | Chaves de cache case-sensitive |
| `VaryByQueryStringByDefault` | bool | `true` | Variar por query padrão |
| `CacheableMethods` | HashSet | GET, HEAD | Métodos HTTP para cache |
| `CacheableStatusCodes` | HashSet | 200 | Códigos de status para cache |

### Propriedades de OutputCachePolicyOptions

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `ExpirationTimeSpan` | TimeSpan? | Duração do cache |
| `NoCache` | bool | Desabilitar cache |
| `Tags` | HashSet | Tags de invalidação |
| `VaryByHeader` | HashSet | Headers para variar |
| `VaryByQueryKeys` | HashSet | Chaves de query para variar |
| `VaryByAllQueryKeys` | bool | Variar por todas as chaves de query |
| `VaryByRouteValue` | HashSet | Valores de rota para variar |
| `LockDuringPopulation` | bool | Prevenir stampede |
| `CacheAuthenticatedRequests` | bool | Cache de requisições autenticadas |

## Melhores Práticas

### 1. Use Tags para Grupos de Invalidação

```csharp
options.AddPolicy("Produtos", p => p
    .Expire(TimeSpan.FromMinutes(10))
    .SetTags("produtos", "catalogo"));

// Invalidar por tag quando dados mudam
await _cacheInvalidator.EvictByTagAsync("produtos");
```

### 2. Durações de Cache Apropriadas

```csharp
// Dados frequentemente alterados - cache curto
options.AddPolicy("DadosTempoReal", p => p.Expire(TimeSpan.FromSeconds(30)));

// Dados de referência - cache longo
options.AddPolicy("DadosReferencia", p => p.Expire(TimeSpan.FromHours(24)));

// Dados específicos do usuário - cache médio com vary Authorization
options.AddPolicy("DadosUsuario", p => p
    .Expire(TimeSpan.FromMinutes(5))
    .SetVaryByHeader("Authorization"));
```

### 3. Exclua Endpoints Sensíveis

```csharp
options.ExcludedPaths.Add("/api/auth/*");
options.ExcludedPaths.Add("/api/pagamentos/*");
options.ExcludedPaths.Add("/api/admin/*");
```

### 4. Use Redis para Produção

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddMvp24HoursOutputCacheWithRedis(
        configuration["Redis:ConnectionString"]!);
}
else
{
    builder.Services.AddMvp24HoursOutputCache();
}
```

### 5. Combine com HybridCache

Output Caching e HybridCache servem propósitos diferentes:

- **Output Caching:** Cache de respostas HTTP completas
- **HybridCache:** Cache de dados a nível de aplicação

```csharp
// Output caching para respostas HTTP
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.AddStandardPolicies();
});

// HybridCache para dados da aplicação
builder.Services.AddMvpHybridCache();
```

## Posição no Pipeline

```csharp
var app = builder.Build();

// Tratamento de exceções primeiro
app.UseMvp24HoursProblemDetails();

// Depois CORS
app.UseCors();

// Depois autenticação/autorização
app.UseAuthentication();
app.UseAuthorization();

// Output caching após auth (para respeitar vary Authorization)
app.UseMvp24HoursOutputCache();

// Depois roteamento
app.MapControllers();
```

## Veja Também

- [HybridCache](hybrid-cache.md) - Cache a nível de aplicação
- [Rate Limiting](rate-limiting.md) - Limitação de requisições
- [HTTP Resilience](http-resilience.md) - Resiliência de cliente HTTP

