# Resiliência HTTP com Microsoft.Extensions.Http.Resilience

> **Versão**: .NET 9+ | **Pacote**: `Microsoft.Extensions.Http.Resilience`

## Visão Geral

O .NET 9 introduz resiliência HTTP nativa através do pacote `Microsoft.Extensions.Http.Resilience`. Isso substitui configurações manuais de Polly com uma API simplificada e integrada que oferece:

- **Configuração Simplificada**: Use o padrão `IOptions` para configuração
- **Integração OpenTelemetry Nativa**: Tracing e métricas automáticos
- **Suporte a Métricas Nativas**: Métricas compatíveis com Prometheus prontas para uso
- **Melhor Performance**: Usa Polly v8 com performance aprimorada
- **Menos Código Boilerplate**: Menos código para configurar estratégias de resiliência

## Handler de Resiliência Padrão

O handler de resiliência padrão inclui quatro camadas de proteção:

1. **Timeout Total da Requisição** (30s padrão) - Timeout geral incluindo retentativas
2. **Retry** (3 tentativas com backoff exponencial) - Retry automático em falhas transientes
3. **Circuit Breaker** (baseado em taxa de falha) - Previne falhas em cascata
4. **Timeout por Tentativa** (10s por tentativa) - Timeout para cada tentativa individual

### Uso Básico

```csharp
// Adicionar HttpClient com resiliência padrão
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
}).AddStandardResilienceHandler();
```

### Usando Extensões Mvp24Hours

```csharp
using Mvp24Hours.Infrastructure.Http.Resilience;

// Abordagem simples com resiliência padrão
services.AddHttpClientWithStandardResilience("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

// Com opções customizadas
services.AddHttpClientWithStandardResilience("MyApi",
    client => client.BaseAddress = new Uri("https://api.example.com"),
    options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
    });
```

## Configuração de Resiliência Customizada

Para cenários avançados, use o handler de resiliência customizado:

```csharp
services.AddHttpClientWithCustomResilience("MyApi", "custom-pipeline",
    client => client.BaseAddress = new Uri("https://api.example.com"),
    builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
        
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(60)
        });
        
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    });
```

## API Fluent Builder

O Mvp24Hours fornece um builder fluente para cenários comuns:

```csharp
services.AddHttpClient("MyApi")
    .AddMvpResilience(builder => builder
        .WithOptions(NativeResilienceOptions.HighAvailability)
        .OnRetry((args, delay) => 
            logger.LogWarning("Tentativa de retry {Attempt} após {Delay}", 
                args.AttemptNumber, delay))
        .OnCircuitBreak(args => 
            logger.LogError("Circuito aberto devido a {Exception}", 
                args.Outcome.Exception?.Message)));
```

### Opções Predefinidas

Use opções predefinidas para cenários comuns:

```csharp
// Alta disponibilidade - mais retries, timeouts maiores
services.AddHttpClient("CriticalApi")
    .AddMvpResilience(NativeResilienceOptions.HighAvailability);

// Baixa latência - menos retries, timeouts menores
services.AddHttpClient("RealTimeApi")
    .AddMvpResilience(NativeResilienceOptions.LowLatency);

// Processamento em batch - tolerância a falhas
services.AddHttpClient("BatchApi")
    .AddMvpResilience(NativeResilienceOptions.BatchProcessing);

// Testes - sem resiliência
services.AddHttpClient("TestApi")
    .AddMvpResilience(NativeResilienceOptions.Disabled);
```

## Typed HTTP Clients

Para typed HTTP clients:

```csharp
// Usando typed client do Mvp24Hours com resiliência
services.AddMvpTypedHttpClient<IMyApi>(options =>
{
    options.BaseAddress = new Uri("https://api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
}).AddStandardResilienceHandler();

// Ou com opções predefinidas
services.AddTypedHttpClientWithStandardResilience<IMyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

## Migração da API Legada

### Antes (Obsoleto)

```csharp
// ❌ Abordagem antiga - OBSOLETA
services.AddHttpClient("MyApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(HttpStatusCode.TooManyRequests, 3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(HttpStatusCode.ServiceUnavailable));

// Ou usando as extensões antigas
services.AddHttpClientWithPolly("MyApi", builder =>
{
    builder.AddRetryPolicy(o => o.MaxRetries = 3);
    builder.AddCircuitBreakerPolicy(o => o.BreakDuration = TimeSpan.FromSeconds(30));
});
```

### Depois (Recomendado)

```csharp
// ✅ Nova abordagem - Recomendada
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
}).AddStandardResilienceHandler();

// Ou com configuração customizada
services.AddHttpClient("MyApi")
    .AddMvpResilience(builder => builder
        .ConfigureOptions(o =>
        {
            o.MaxRetryAttempts = 3;
            o.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
        }));
```

## Configuração via appsettings.json

Configure opções de resiliência via configuração:

```json
{
  "HttpClients": {
    "MyApi": {
      "BaseAddress": "https://api.example.com",
      "Resilience": {
        "TotalRequestTimeout": "00:02:00",
        "Retry": {
          "MaxRetryAttempts": 5,
          "Delay": "00:00:02"
        },
        "CircuitBreaker": {
          "FailureRatio": 0.1,
          "BreakDuration": "00:00:30"
        }
      }
    }
  }
}
```

```csharp
services.AddHttpClient("MyApi")
    .AddStandardResilienceHandler(options =>
    {
        configuration.GetSection("HttpClients:MyApi:Resilience").Bind(options);
    });
```

## Observabilidade

A API nativa integra automaticamente com OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("Polly"))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddMeter("Polly"));
```

### Métricas Disponíveis

- `http.client.request.duration` - Histograma de duração das requisições
- `polly.resilience.pipeline.duration` - Duração da execução do pipeline
- `polly.strategy.attempt_count` - Número de tentativas por estratégia

## Melhores Práticas

1. **Use o Handler Padrão por Default**: Comece com `AddStandardResilienceHandler()` e customize apenas quando necessário

2. **Configure Timeouts Apropriados**: Defina timeouts baseados nos seus requisitos de SLA

3. **Monitore o Estado do Circuit Breaker**: Use callbacks para logar mudanças de estado do circuit breaker

4. **Use Jitter**: Habilite jitter para prevenir problemas de thundering herd

5. **Teste a Resiliência**: Teste sua configuração de resiliência sob condições de falha

## Veja Também

- [Documentação Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/pt-br/dotnet/core/resilience/http-resilience)
- [Documentação Polly v8](https://www.thepollyproject.org/)
- [Integração OpenTelemetry](../observability/tracing.md)

