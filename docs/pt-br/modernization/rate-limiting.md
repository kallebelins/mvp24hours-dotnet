# Rate Limiting com System.Threading.RateLimiting

> Rate limiting nativo do .NET 7+ usando `System.Threading.RateLimiting`.

## Visão Geral

O framework Mvp24Hours fornece rate limiting nativo usando o namespace `System.Threading.RateLimiting` do .NET, introduzido no .NET 7. Isso substitui implementações customizadas pelas APIs de rate limiting padrão e de alta performance.

### Algoritmos Suportados

| Algoritmo | Descrição | Caso de Uso |
|-----------|-----------|-------------|
| **FixedWindow** | Conta requisições em janelas de tempo fixas | Rate limiting simples com potenciais picos nos limites das janelas |
| **SlidingWindow** | Suaviza os limites das janelas fixas | Rate limiting mais preciso, recomendado para a maioria das APIs |
| **TokenBucket** | Permite picos controlados com reabastecimento suave | APIs que permitem picos ocasionais |
| **Concurrency** | Limita requisições concorrentes | Operações intensivas em recursos |

## Instalação

Rate limiting está incluído nos seguintes pacotes:

- `Mvp24Hours.Core` - Abstrações base e provider
- `Mvp24Hours.Infrastructure.Pipe` - Middleware de pipeline
- `Mvp24Hours.Infrastructure.RabbitMQ` - Filtros de Consumer/Publisher
- `Mvp24Hours.WebAPI` - Middleware HTTP (já implementado)

## Abstrações Core

### IRateLimiterProvider

A interface `IRateLimiterProvider` fornece rate limiters para diferentes chaves/partições:

```csharp
public interface IRateLimiterProvider : IDisposable
{
    RateLimiter GetRateLimiter(string key, NativeRateLimiterOptions options);
    
    ValueTask<RateLimitLease> AcquireAsync(
        string key,
        NativeRateLimiterOptions options,
        int permitCount = 1,
        CancellationToken cancellationToken = default);
    
    bool TryRemoveRateLimiter(string key);
}
```

### NativeRateLimiterOptions

Opções de configuração para rate limiters:

```csharp
var options = new NativeRateLimiterOptions
{
    Algorithm = RateLimitingAlgorithm.SlidingWindow,
    PermitLimit = 100,
    Window = TimeSpan.FromMinutes(1),
    SegmentsPerWindow = 4,
    QueueLimit = 10,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
};
```

### Métodos Factory

```csharp
// Fixed Window
var fixedWindow = NativeRateLimiterOptions.FixedWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1));

// Sliding Window
var slidingWindow = NativeRateLimiterOptions.SlidingWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1),
    segmentsPerWindow: 4);

// Token Bucket
var tokenBucket = NativeRateLimiterOptions.TokenBucket(
    tokenLimit: 100,
    replenishmentPeriod: TimeSpan.FromSeconds(10),
    tokensPerPeriod: 10);

// Concurrency
var concurrency = NativeRateLimiterOptions.Concurrency(
    permitLimit: 10,
    queueLimit: 5);
```

## Rate Limiting WebAPI

Rate limiting para WebAPI já está implementado usando o rate limiter nativo do .NET. Veja a documentação do WebAPI para detalhes de configuração.

## Rate Limiting de Pipeline

### Configuração Básica

```csharp
services.AddPipelineRateLimiting(options =>
{
    options.DefaultKey = "pipeline_default";
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 100,
        window: TimeSpan.FromMinutes(1));
    options.OnRateLimited = (key, retryAfter) =>
    {
        Console.WriteLine($"Rate limit excedido para {key}. Retry após: {retryAfter}");
    };
});
```

### Extensões Específicas por Algoritmo

```csharp
// Sliding Window
services.AddPipelineRateLimitingSlidingWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1),
    segmentsPerWindow: 4);

// Token Bucket
services.AddPipelineRateLimitingTokenBucket(
    tokenLimit: 100,
    replenishmentPeriod: TimeSpan.FromSeconds(10),
    tokensPerPeriod: 10);

// Concurrency
services.AddPipelineRateLimitingConcurrency(
    permitLimit: 10,
    queueLimit: 5);
```

### Rate Limiting Específico por Operação

Implemente `IRateLimitedOperation` nas suas operações de pipeline:

```csharp
public class MyRateLimitedOperation : OperationBase, IRateLimitedOperation
{
    public string RateLimiterKey => "my_operation";
    public RateLimitingAlgorithm Algorithm => RateLimitingAlgorithm.TokenBucket;
    public int PermitLimit => 50;
    public TimeSpan Window => TimeSpan.FromSeconds(30);
    public int SegmentsPerWindow => 4;
    public TimeSpan ReplenishmentPeriod => TimeSpan.FromSeconds(5);
    public int TokensPerPeriod => 5;
    public bool AutoReplenishment => true;
    public int QueueLimit => 10;
    public QueueProcessingOrder QueueProcessingOrder => QueueProcessingOrder.OldestFirst;
    public TimeSpan? QueueTimeout => TimeSpan.FromSeconds(5);

    public void OnRateLimited(TimeSpan? retryAfter)
    {
        // Tratar notificação de rate limit
    }

    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        // Lógica da operação
    }
}
```

## Rate Limiting RabbitMQ

### Rate Limiting de Consumer

```csharp
services.AddRabbitMQConsumerRateLimiting(options =>
{
    options.KeyMode = RateLimitKeyMode.ByQueue;
    options.ExceededBehavior = RateLimitExceededBehavior.Retry;
    options.DefaultRetryDelay = TimeSpan.FromSeconds(5);
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 100,
        window: TimeSpan.FromSeconds(1));
});
```

### Modos de Chave de Rate Limit

| Modo | Descrição |
|------|-----------|
| `ByQueue` | Rate limit por fila |
| `ByMessageType` | Rate limit por tipo de mensagem |
| `ByExchange` | Rate limit por exchange |
| `ByRoutingKey` | Rate limit por routing key |
| `ByConsumerTag` | Rate limit por consumer tag |
| `Global` | Rate limit global para todos os consumers |

### Comportamentos ao Exceder Limite

| Comportamento | Descrição |
|---------------|-----------|
| `Throw` | Lança `RateLimitExceededException` |
| `Retry` | Solicita retry com delay |
| `DeadLetter` | Envia mensagem para dead letter queue |
| `Skip` | Ignora a mensagem (acknowledge sem processar) |

### Rate Limiting de Publisher

```csharp
services.AddRabbitMQPublisherRateLimiting(options =>
{
    options.KeyMode = PublishRateLimitKeyMode.Global;
    options.WaitWhenExceeded = true; // Esperar e retentar ao invés de lançar exceção
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.TokenBucket(
        tokenLimit: 1000,
        replenishmentPeriod: TimeSpan.FromSeconds(1),
        tokensPerPeriod: 100);
});
```

### Consumer e Publisher Combinados

```csharp
services.AddRabbitMQRateLimiting(
    configureConsumeOptions: consume =>
    {
        consume.KeyMode = RateLimitKeyMode.ByQueue;
        consume.ExceededBehavior = RateLimitExceededBehavior.Retry;
    },
    configurePublishOptions: publish =>
    {
        publish.KeyMode = PublishRateLimitKeyMode.Global;
    });
```

### Rate Limiting Específico por Tipo

```csharp
services.AddRabbitMQConsumerRateLimiting(options =>
{
    // Opções padrão
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(100);

    // Mais restritivo para tipos específicos de mensagem
    options.TypeSpecificOptions[typeof(HighVolumeMessage)] = 
        NativeRateLimiterOptions.TokenBucket(10, TimeSpan.FromSeconds(1), 1);
    
    options.TypeSpecificOptions[typeof(CriticalMessage)] = 
        NativeRateLimiterOptions.Concurrency(5);
});
```

## Rate Limiting Genérico

Use as extensões core para qualquer cenário:

```csharp
// Registrar o provider
services.AddNativeRateLimiting();

// Pré-configurar rate limiters específicos
services.AddSlidingWindowRateLimiter("api_calls", permitLimit: 100);
services.AddTokenBucketRateLimiter("batch_processing", tokenLimit: 50);
services.AddConcurrencyRateLimiter("heavy_operations", permitLimit: 5);
```

### Uso Manual

```csharp
public class MyService
{
    private readonly IRateLimiterProvider _rateLimiterProvider;

    public MyService(IRateLimiterProvider rateLimiterProvider)
    {
        _rateLimiterProvider = rateLimiterProvider;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var options = NativeRateLimiterOptions.SlidingWindow(100);
        
        using var lease = await _rateLimiterProvider.AcquireAsync(
            "my_operation",
            options,
            permitCount: 1,
            cancellationToken);

        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? retry
                : TimeSpan.FromSeconds(60);
            
            throw RateLimitExceededException.ForKey("my_operation", retryAfter);
        }

        // Prosseguir com a operação
    }
}
```

## Tratamento de Exceções

A `RateLimitExceededException` fornece informações detalhadas:

```csharp
try
{
    await ProcessAsync();
}
catch (RateLimitExceededException ex)
{
    Console.WriteLine($"Rate limit excedido para: {ex.RateLimiterKey}");
    Console.WriteLine($"Retry após: {ex.RetryAfter}");
    Console.WriteLine($"Limite de permits: {ex.PermitLimit}");
    Console.WriteLine($"Código de erro: {ex.ErrorCode}");
    
    // Resposta HTTP 429 com header Retry-After
    return Results.Problem(
        title: "Muitas Requisições",
        statusCode: 429,
        extensions: new Dictionary<string, object?>
        {
            ["retryAfter"] = ex.RetryAfter?.TotalSeconds
        });
}
```

## Boas Práticas

1. **Escolha o Algoritmo Certo**
   - Use **SlidingWindow** para a maioria dos rate limiting de API
   - Use **TokenBucket** quando picos são aceitáveis
   - Use **Concurrency** para operações intensivas em recursos
   - Use **FixedWindow** apenas para casos simples

2. **Seleção de Chave**
   - Use chaves granulares (por-usuário, por-tenant) para justiça
   - Use chaves amplas (global) para proteção do sistema

3. **Configuração de Fila**
   - Defina `QueueLimit > 0` para permitir enfileiramento ao invés de rejeição imediata
   - Use `QueueTimeout` para evitar espera indefinida

4. **Monitoramento**
   - Registre eventos de rate limit para debugging
   - Rastreie métricas de rate limit para planejamento de capacidade
   - Alerte sobre rate limiting excessivo

5. **Degradação Graciosa**
   - Retorne headers `Retry-After` apropriados
   - Implemente backoff exponencial nos clientes
   - Considere estratégias de fallback

## Migração de Implementações Customizadas

Se você tem implementações de rate limiting customizadas, migre para a API nativa:

```csharp
// Antes (implementação customizada)
public class CustomRateLimiter
{
    public bool TryAcquire() { /* lógica customizada */ }
}

// Depois (implementação nativa)
services.AddNativeRateLimiting();

// Usar o provider
public class MyService
{
    private readonly IRateLimiterProvider _provider;
    
    public async Task<bool> TryAcquireAsync(CancellationToken ct)
    {
        using var lease = await _provider.AcquireAsync(
            "my_key",
            NativeRateLimiterOptions.SlidingWindow(100),
            cancellationToken: ct);
        
        return lease.IsAcquired;
    }
}
```

## Veja Também

- [Resiliência HTTP](http-resilience.md)
- [Resiliência Genérica](generic-resilience.md)
- [Documentação Microsoft.Extensions.RateLimiting](https://learn.microsoft.com/pt-br/dotnet/core/extensions/http-ratelimiter)

