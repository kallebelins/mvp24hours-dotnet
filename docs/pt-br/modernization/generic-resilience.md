# Resiliência Genérica com Microsoft.Extensions.Resilience

> **Novo no Mvp24Hours para .NET 9**: Resiliência nativa usando `Microsoft.Extensions.Resilience` e Polly v8.

## Visão Geral

A partir do .NET 9, o Mvp24Hours adota o pacote nativo `Microsoft.Extensions.Resilience` para operações de resiliência genérica (não-HTTP). Isso substitui implementações customizadas de políticas de retry, circuit breakers e timeouts por componentes padrão da indústria, bem testados.

## Benefícios da Resiliência Nativa

| Funcionalidade | Implementação Customizada | Nativa (Microsoft.Extensions.Resilience) |
|----------------|---------------------------|------------------------------------------|
| **Configuração** | Classes de options customizadas | Padrão IOptions + IConfiguration |
| **Telemetria** | Integração manual | Integração automática com OpenTelemetry |
| **Integração DI** | Registros customizados | Nativo com Keyed Services |
| **Performance** | Variável | Otimizado para .NET 9 |
| **Manutenção** | Equipe Mvp24Hours | Microsoft + equipe Polly |

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                    Microsoft.Extensions.Resilience               │
│                         (fundação Polly v8)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │   Timeout    │  │Circuit Breaker│  │       Retry         │  │
│  │  (externo)   │→ │              │→ │    (interno)         │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                    Camada de Integração Mvp24Hours               │
│                                                                  │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────────┐  │
│  │   Resiliência  │ │  Resiliência   │ │   Resiliência      │  │
│  │   Banco Dados  │ │    MongoDB     │ │     Pipeline       │  │
│  └────────────────┘ └────────────────┘ └────────────────────┘  │
│                                                                  │
│  ┌────────────────┐ ┌────────────────┐                         │
│  │   Behavior     │ │   Operações    │                         │
│  │   CQRS         │ │    Genéricas   │                         │
│  └────────────────┘ └────────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

## Início Rápido

### 1. Operações Genéricas

```csharp
// Registrar pipeline de resiliência
services.AddNativeResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
    options.EnableTimeout = true;
    options.TimeoutDuration = TimeSpan.FromSeconds(30);
});

// Usar no seu código
public class MeuServico
{
    private readonly INativeResiliencePipeline _pipeline;
    
    public MeuServico(INativeResiliencePipeline pipeline)
    {
        _pipeline = pipeline;
    }
    
    public async Task<string> ObterDadosAsync(CancellationToken ct)
    {
        return await _pipeline.ExecuteTaskAsync(async token =>
        {
            // Sua operação aqui
            return await servicoExterno.ChamarAsync(token);
        }, ct);
    }
}
```

### 2. Operações de Banco de Dados (EF Core)

```csharp
// Registrar resiliência de banco de dados
services.AddNativeDbResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 5;
    options.EnableCircuitBreaker = true;
});

// Usar com Keyed Services
public class ClienteRepository
{
    private readonly ResiliencePipeline _pipeline;
    
    public ClienteRepository(
        [FromKeyedServices("database")] ResiliencePipeline pipeline,
        ApplicationDbContext dbContext)
    {
        _pipeline = pipeline;
        _dbContext = dbContext;
    }
    
    public async Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            return await _dbContext.Clientes.FindAsync(new object[] { id }, token);
        }, ct);
    }
}
```

### 3. Operações MongoDB

```csharp
// Registrar resiliência MongoDB
services.AddNativeMongoDbResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
});

// Usar no repositório
public class ProdutoRepository
{
    private readonly ResiliencePipeline _pipeline;
    private readonly IMongoCollection<Produto> _collection;
    
    public ProdutoRepository(
        [FromKeyedServices("mongodb")] ResiliencePipeline pipeline,
        IMongoDatabase database)
    {
        _pipeline = pipeline;
        _collection = database.GetCollection<Produto>("produtos");
    }
    
    public async Task<Produto?> ObterPorIdAsync(string id, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            return await _collection.Find(p => p.Id == id).FirstOrDefaultAsync(token);
        }, ct);
    }
}
```

### 4. Operações de Pipeline

```csharp
// Registrar resiliência de pipeline
services.AddNativePipelineResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
});
```

### 5. CQRS Mediator

```csharp
// Registrar behavior de resiliência CQRS
services.AddNativeCqrsResilience(options =>
{
    options.ApplyToAllRequests = true; // Aplicar a todas as requisições
    options.EnableRetry = true;
    options.EnableCircuitBreaker = true;
});

// Ou opt-in por requisição
public class ObterClienteQuery : IMediatorQuery<Cliente>, INativeResilient
{
    public int ClienteId { get; set; }
    
    // Opcional: opções customizadas para esta requisição
    public NativeCqrsResilienceOptions? ResilienceOptions => new()
    {
        RetryMaxAttempts = 5,
        TimeoutDuration = TimeSpan.FromSeconds(10)
    };
}
```

## Presets

### Presets Genéricos

```csharp
// Alta disponibilidade (mais retries, timeouts maiores)
services.AddNativeResilience(NativeResilienceOptions.HighAvailability);

// Baixa latência (menos retries, timeouts menores)
services.AddNativeResilience(NativeResilienceOptions.LowLatency);

// Processamento em lote (muitos retries, sem timeout)
services.AddNativeResilience(NativeResilienceOptions.BatchProcessing);
```

### Presets de Banco de Dados

```csharp
// Otimizado para SQL Server
services.AddNativeDbResilience("sqlserver", NativeDbResilienceOptions.SqlServer);

// Otimizado para PostgreSQL
services.AddNativeDbResilience("postgres", NativeDbResilienceOptions.PostgreSql);

// Otimizado para MySQL
services.AddNativeDbResilience("mysql", NativeDbResilienceOptions.MySql);
```

### Presets MongoDB

```csharp
// Otimizado para replica set
services.AddNativeMongoDbResilience("replica", NativeMongoDbResilienceOptions.ReplicaSet);

// Otimizado para cluster sharded
services.AddNativeMongoDbResilience("sharded", NativeMongoDbResilienceOptions.ShardedCluster);
```

## Configuração via appsettings.json

```json
{
  "Resilience": {
    "Database": {
      "EnableRetry": true,
      "RetryMaxAttempts": 3,
      "RetryDelayMs": 500,
      "EnableCircuitBreaker": true,
      "CircuitBreakerFailureRatio": 0.5,
      "EnableTimeout": true,
      "TimeoutSeconds": 30
    }
  }
}
```

```csharp
services.AddNativeDbResilience(options =>
{
    configuration.GetSection("Resilience:Database").Bind(options);
});
```

## Migração de Implementações Legadas

### Antes (Depreciado)

```csharp
// ❌ Depreciado - será removido em versões futuras
services.AddSingleton<IRetryPolicy<Cliente>>(sp =>
{
    var options = new RetryOptions { MaxRetries = 3 };
    return new RetryPolicy<Cliente>(options);
});

var result = await retryPolicy.ExecuteAsync(async ct =>
{
    return await ObterClienteAsync(id, ct);
}, cancellationToken);
```

### Depois (Nativo)

```csharp
// ✅ Abordagem moderna usando Microsoft.Extensions.Resilience
services.AddNativeResilience<Cliente>(options =>
{
    options.RetryMaxAttempts = 3;
});

var result = await pipeline.ExecuteAsync(async ct =>
{
    return await ObterClienteAsync(id, ct);
}, cancellationToken);
```

## Classes Depreciadas

As seguintes classes estão depreciadas e serão removidas em uma futura versão major:

| Classe Depreciada | Substituto |
|-------------------|------------|
| `MvpExecutionStrategy` | `AddNativeDbResilience()` |
| `MongoDbResiliencyPolicy` | `AddNativeMongoDbResilience()` |
| `RetryPipelineMiddleware` | `AddNativePipelineResilience()` |
| `CircuitBreakerPipelineMiddleware` | `AddNativePipelineResilience()` |
| `RetryPolicy<T>` | `INativeResiliencePipeline<T>` |
| `CircuitBreaker<T>` | `INativeResiliencePipeline<T>` |
| `RetryBehavior` | `NativeResilienceBehavior` |
| `CircuitBreakerBehavior` | `NativeResilienceBehavior` |
| `TimeoutBehavior` | `NativeResilienceBehavior` |

## Configuração Avançada

### Tratamento de Exceções Customizado

```csharp
services.AddNativeResilience(options =>
{
    options.ShouldRetryOnException = ex =>
    {
        // Retry apenas em exceções específicas
        return ex is TimeoutException or 
               TransientException or
               DbUpdateConcurrencyException;
    };
    
    options.OnRetry = (ex, attempt, delay) =>
    {
        // Logging ou métricas customizadas
        logger.LogWarning(ex, 
            "Retry {Attempt} após {Delay}ms", 
            attempt, 
            delay.TotalMilliseconds);
    };
});
```

### Usando ResiliencePipeline Diretamente

Para cenários avançados, você pode usar o `ResiliencePipelineBuilder` do Polly diretamente:

```csharp
services.AddResiliencePipeline("custom", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
});
```

## Telemetria

A resiliência nativa integra automaticamente com OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddSource("Polly"); // Activity source do Polly
    })
    .WithMetrics(builder =>
    {
        builder.AddMeter("Polly"); // Meter do Polly
    });
```

## Veja Também

- [Resiliência HTTP (Microsoft.Extensions.Http.Resilience)](http-resilience.md)
- [Rate Limiting (.NET 9)](rate-limiting.md)
- [Documentação Polly v8](https://www.pollydocs.org/)
- [Documentação Microsoft.Extensions.Resilience](https://learn.microsoft.com/pt-br/dotnet/core/resilience/)

