# Observabilidade

> Observabilidade moderna para o framework Mvp24Hours usando ILogger + OpenTelemetry.

## Visão Geral

O Mvp24Hours fornece uma solução de observabilidade abrangente baseada nos padrões modernos do .NET:

- **Logging**: Logging estruturado com `ILogger<T>` (Microsoft.Extensions.Logging)
- **Tracing**: Rastreamento distribuído com OpenTelemetry e API `Activity`
- **Metrics**: Métricas de performance com `Meter` e contadores do OpenTelemetry

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Observabilidade Mvp24Hours                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                 │
│   │   Logging   │    │   Tracing   │    │   Metrics   │                 │
│   │  (ILogger)  │    │ (Activity)  │    │   (Meter)   │                 │
│   └──────┬──────┘    └──────┬──────┘    └──────┬──────┘                 │
│          │                  │                  │                        │
│          ▼                  ▼                  ▼                        │
│   ┌─────────────────────────────────────────────────────────────┐       │
│   │              OpenTelemetry Collector / OTLP                  │       │
│   └─────────────────────────────────────────────────────────────┘       │
│          │                  │                  │                        │
│          ▼                  ▼                  ▼                        │
│   ┌───────────┐      ┌───────────┐      ┌───────────┐                   │
│   │  Console  │      │   Jaeger  │      │Prometheus │                   │
│   │  Serilog  │      │   Zipkin  │      │  Grafana  │                   │
│   │   Seq     │      │   Tempo   │      │           │                   │
│   └───────────┘      └───────────┘      └───────────┘                   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Início Rápido

### 1. Instalar Pacote

```bash
dotnet add package Mvp24Hours.Core
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
```

### 2. Configurar Observabilidade

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Opção 1: Configuração tudo-em-um
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.EnableLogging = true;
});

// Opção 2: Configurar cada pilar separadamente
builder.Services.AddMvp24HoursLogging(options =>
{
    options.EnableTraceCorrelation = true;
});

builder.Services.AddMvp24HoursTracing(options =>
{
    options.ServiceName = "MeuServico";
});

builder.Services.AddMvp24HoursMetrics(options =>
{
    options.ServiceName = "MeuServico";
});
```

### 3. Usar com OpenTelemetry

```csharp
builder.Services.AddMvp24HoursOpenTelemetry(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.OtlpEndpoint = "http://localhost:4317"; // Jaeger/Tempo/Collector
    
    // Incluir activity sources do Mvp24Hours
    options.AddMvp24HoursActivitySources = true;
    options.AddMvp24HoursMeters = true;
});
```

## Activity Sources

O Mvp24Hours fornece `ActivitySource` dedicado para cada módulo:

| Módulo | Nome do ActivitySource | Descrição |
|--------|------------------------|-----------|
| Core | `Mvp24Hours.Core` | Operações core |
| Pipeline | `Mvp24Hours.Pipe` | Execução de pipeline |
| CQRS | `Mvp24Hours.Cqrs` | Commands, Queries, Notifications |
| EF Core | `Mvp24Hours.EFCore` | Operações de banco de dados |
| RabbitMQ | `Mvp24Hours.RabbitMQ` | Operações de mensageria |
| Caching | `Mvp24Hours.Caching` | Operações de cache |
| CronJob | `Mvp24Hours.CronJob` | Jobs agendados |
| HTTP | `Mvp24Hours.Infrastructure.Http` | Chamadas HTTP client |

### Configurar ActivitySources no OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddSource(Mvp24HoursActivitySources.Core.SourceName)
            .AddSource(Mvp24HoursActivitySources.Pipe.SourceName)
            .AddSource(Mvp24HoursActivitySources.Cqrs.SourceName)
            .AddSource(Mvp24HoursActivitySources.Data.SourceName)
            .AddSource(Mvp24HoursActivitySources.RabbitMQ.SourceName)
            .AddSource(Mvp24HoursActivitySources.Caching.SourceName)
            .AddSource(CronJobActivitySource.SourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });
```

## Correlação e Contexto

O Mvp24Hours propaga automaticamente o contexto através de:

- Requisições HTTP (via W3C Trace Context headers)
- Mensagens RabbitMQ (via baggage)
- Operações de Pipeline
- Handlers CQRS

### Propagação de Correlation ID

```csharp
// Extrair do contexto atual
var correlationId = CorrelationIdPropagation.GetCorrelationId();

// Definir no contexto atual
CorrelationIdPropagation.SetCorrelationId(correlationId);

// Usar com scope do ILogger
using (logger.BeginTraceScope())
{
    logger.LogInformation("Operação concluída");
}
```

## Documentação

| Tópico | Descrição |
|--------|-----------|
| [Logging](logging.md) | Logging estruturado com ILogger |
| [Tracing](tracing.md) | Rastreamento distribuído com OpenTelemetry |
| [Metrics](metrics.md) | Métricas de performance |
| [Exporters](exporters.md) | Configurar OTLP, Console, Prometheus |
| [Migração](migration.md) | Migrar do TelemetryHelper |

## Aviso de Deprecação

> ⚠️ **DEPRECATED**: O legado `TelemetryHelper` e `ITelemetryService` foram descontinuados.
> Por favor, use `ILogger<T>` e OpenTelemetry em seu lugar.
> Consulte o [Guia de Migração](migration.md) para detalhes.

## Boas Práticas

1. **Use ILogger<T>**: Injete `ILogger<T>` para todas as necessidades de logging
2. **Habilite Correlação**: Use `BeginTraceScope()` para correlação automática
3. **Adicione Tags Semânticas**: Use constantes `SemanticTags` para tagueamento consistente
4. **Configure Sampling**: Use sampling para ambientes de produção com alto volume
5. **Exporte para OTLP**: Use OTLP para máxima compatibilidade com backends

## Veja Também

- [Documentação OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Microsoft.Extensions.Logging](https://learn.microsoft.com/pt-br/dotnet/core/extensions/logging)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)

