# Telemetry

> ⚠️ **DEPRECATED / OBSOLETO**: Esta API está obsoleta e será removida em uma versão futura.
> 
> **Migre para `ILogger<T>` (Microsoft.Extensions.Logging) e OpenTelemetry.**
> 
> Consulte o [Guia de Migração](/pt-br/observability/migration.md) para instruções completas.

---

Solução criada para rastrear todos os níveis de execução da aplicação. Poderá injetar ações para tratamento usando qualquer gerenciador de log, incluindo métricas e trace.

## ⚠️ Aviso de Deprecação

A partir desta versão, os seguintes componentes estão marcados como `[Obsolete]`:

- `TelemetryHelper` - Use `ILogger<T>` em seu lugar
- `TelemetryLevels` - Use `Microsoft.Extensions.Logging.LogLevel` em seu lugar
- `ITelemetryService` - Use `ILogger<T>` ou implemente `ILoggerProvider` em seu lugar
- `AddMvp24HoursTelemetry()` - Use `AddLogging()` em seu lugar

### Comparação de Níveis

| TelemetryLevels (Antigo) | LogLevel (Novo) |
|--------------------------|-----------------|
| `Verbose` | `LogLevel.Debug` ou `LogLevel.Trace` |
| `Information` | `LogLevel.Information` |
| `Warning` | `LogLevel.Warning` |
| `Error` | `LogLevel.Error` |
| `Critical` | `LogLevel.Critical` |

## Configuração (Legado - Deprecated)

```csharp
/// Startup.cs
Logger logger = LogManager.GetCurrentClassLogger(); // qualquer gerenciador de log

// trace
services.AddMvp24HoursTelemetry(TelemetryLevel.Information | TelemetryLevel.Verbose,
    (name, state) =>
    {
        logger.Trace($"{name}|{string.Join("|", state)}");
    }
);

// erro
services.AddMvp24HoursTelemetry(TelemetryLevel.Error,
    (name, state) =>
    {
        if (name.EndsWith("-failure"))
        {
            logger.Error(state.ElementAtOrDefault(0) as Exception);
        }
        else
        {
            logger.Error($"{name}|{string.Join("|", state)}");
        }
    }
);

// ignorar eventos
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");
```

## Rodar / Executar (Legado - Deprecated)

```csharp
/// MyFile.cs
TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{tokenDefault}");
```

---

## ✅ Nova Abordagem Recomendada

### Configuração com ILogger

```csharp
// Program.cs ou Startup.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
    
    // Filtrar por categoria
    logging.AddFilter("Mvp24Hours", LogLevel.Debug);
    logging.AddFilter("Microsoft", LogLevel.Warning);
});
```

### Uso com ILogger<T>

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoSomething(string token)
    {
        // Antes (deprecated):
        // TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{token}");
        
        // Depois (recomendado):
        _logger.LogDebug("RabbitMQ client publish started. Token: {Token}", token);
    }
    
    public void HandleError(Exception ex)
    {
        // Antes (deprecated):
        // TelemetryHelper.Execute(TelemetryLevels.Error, "operation-failure", ex);
        
        // Depois (recomendado):
        _logger.LogError(ex, "Operation failed");
    }
}
```

### Logging Estruturado de Alta Performance

```csharp
// Defina mensagens de log em uma classe separada para alta performance
public static partial class LogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "RabbitMQ client publish started. Token: {Token}")]
    public static partial void RabbitMqPublishStarted(this ILogger logger, string token);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Operation failed")]
    public static partial void OperationFailed(this ILogger logger, Exception ex);
}

// Uso
_logger.RabbitMqPublishStarted(token);
_logger.OperationFailed(ex);
```

### OpenTelemetry para Tracing Distribuído

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mvp24Hours")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Mvp24Hours")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });
```

```csharp
// Uso de Activity para tracing
using System.Diagnostics;

public class MyService
{
    private static readonly ActivitySource ActivitySource = new("Mvp24Hours.MyService");
    
    public async Task ProcessAsync()
    {
        using var activity = ActivitySource.StartActivity("ProcessOperation");
        activity?.SetTag("custom.tag", "value");
        
        // ... operação
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
```

## Consulte Também

- [Guia de Migração de Observabilidade](/pt-br/observability/migration.md)
- [Logging com ILogger](/pt-br/logging.md)
- [OpenTelemetry Tracing](/pt-br/cqrs/observability/tracing.md)
