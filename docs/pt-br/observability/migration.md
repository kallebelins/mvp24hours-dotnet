# Guia de Migração: TelemetryHelper para ILogger/OpenTelemetry

Este guia fornece instruções detalhadas para migrar do sistema de telemetria legado (`TelemetryHelper`, `TelemetryLevels`, `ITelemetryService`) para as abordagens modernas usando `ILogger<T>` e OpenTelemetry.

## Por que Migrar?

O sistema de telemetria antigo (`TelemetryHelper`) foi marcado como **obsoleto** pelos seguintes motivos:

1. **Padrão da Indústria**: `ILogger<T>` é o padrão .NET para logging estruturado
2. **Injeção de Dependência**: `ILogger` integra-se perfeitamente com o sistema de DI do .NET
3. **Testabilidade**: Mocking de `ILogger` é simples e bem documentado
4. **Ecossistema Rico**: Suporte nativo para Serilog, NLog, Log4Net, Application Insights, etc.
5. **OpenTelemetry**: Padrão CNCF para observabilidade distribuída (logs + traces + métricas)
6. **Performance**: Source generators para logging de alta performance

## Cronograma de Deprecação

| Versão | Status |
|--------|--------|
| Atual | Marcado como `[Obsolete]` - warnings de compilação |
| Próxima Minor | Mantido para compatibilidade |
| Próxima Major | **Removido completamente** |

## Mapeamento de APIs

### Níveis de Log

| TelemetryLevels | LogLevel | Quando Usar |
|-----------------|----------|-------------|
| `Verbose` | `Trace` | Diagnóstico detalhado (volume alto) |
| `Verbose` | `Debug` | Informações de debug (desenvolvimento) |
| `Information` | `Information` | Fluxo normal da aplicação |
| `Warning` | `Warning` | Situações inesperadas mas recuperáveis |
| `Error` | `Error` | Erros que impedem uma operação |
| `Critical` | `Critical` | Falhas catastróficas do sistema |

### Métodos de Configuração

| Antigo | Novo |
|--------|------|
| `AddMvp24HoursTelemetry()` | `AddLogging()` |
| `AddMvp24HoursTelemetryFiltered()` | `AddFilter()` em `ILoggingBuilder` |
| `AddMvp24HoursTelemetryIgnore()` | `AddFilter()` com `LogLevel.None` |

### Métodos de Execução

| Antigo | Novo |
|--------|------|
| `TelemetryHelper.Execute(TelemetryLevels.Verbose, ...)` | `_logger.LogDebug(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Information, ...)` | `_logger.LogInformation(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Warning, ...)` | `_logger.LogWarning(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Error, ...)` | `_logger.LogError(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Critical, ...)` | `_logger.LogCritical(...)` |

## Exemplos de Migração

### 1. Configuração Básica

**Antes (Deprecated):**
```csharp
// Startup.cs
services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
    (name, state) =>
    {
        Console.WriteLine($"{name}|{string.Join("|", state)}");
    }
);
```

**Depois (Recomendado):**
```csharp
// Program.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

### 2. Logging em Classes

**Antes (Deprecated):**
```csharp
public class OrderService
{
    public void ProcessOrder(int orderId)
    {
        TelemetryHelper.Execute(TelemetryLevels.Information, 
            "order-processing-start", orderId);
        
        try
        {
            // ... processamento
            TelemetryHelper.Execute(TelemetryLevels.Information, 
                "order-processing-complete", orderId);
        }
        catch (Exception ex)
        {
            TelemetryHelper.Execute(TelemetryLevels.Error, 
                "order-processing-failure", orderId, ex);
            throw;
        }
    }
}
```

**Depois (Recomendado):**
```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    
    public void ProcessOrder(int orderId)
    {
        _logger.LogInformation("Order processing started. OrderId: {OrderId}", orderId);
        
        try
        {
            // ... processamento
            _logger.LogInformation("Order processing completed. OrderId: {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order processing failed. OrderId: {OrderId}", orderId);
            throw;
        }
    }
}
```

### 3. Filtros e Categorias

**Antes (Deprecated):**
```csharp
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");

services.AddMvp24HoursTelemetryFiltered("my-service",
    (name, state) => Console.WriteLine($"[FILTERED] {name}"));
```

**Depois (Recomendado):**
```csharp
builder.Services.AddLogging(logging =>
{
    // Ignorar logs de uma categoria específica
    logging.AddFilter("RabbitMQ.Consumer", LogLevel.None);
    
    // Configurar nível por categoria
    logging.AddFilter("MyService", LogLevel.Debug);
    logging.AddFilter("Microsoft", LogLevel.Warning);
    logging.AddFilter("System", LogLevel.Warning);
});
```

### 4. ITelemetryService Personalizado

**Antes (Deprecated):**
```csharp
public class CustomTelemetryService : ITelemetryService
{
    public void Execute(string eventName, params object[] args)
    {
        // Lógica customizada
        SendToExternalSystem(eventName, args);
    }
}

// Registro
TelemetryHelper.Add(TelemetryLevels.All, new CustomTelemetryService());
```

**Depois (Recomendado):**
```csharp
// Opção 1: Implementar ILoggerProvider
public class CustomLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CustomLogger(categoryName);
    }
    
    public void Dispose() { }
}

public class CustomLogger : ILogger
{
    private readonly string _categoryName;
    
    public CustomLogger(string categoryName)
    {
        _categoryName = categoryName;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Lógica customizada
        SendToExternalSystem(formatter(state, exception));
    }
}

// Registro
builder.Services.AddLogging(logging =>
{
    logging.AddProvider(new CustomLoggerProvider());
});
```

### 5. Logging de Alta Performance

**Depois (Recomendado - Source Generators):**
```csharp
public static partial class ApplicationLogs
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Order processing started. OrderId: {OrderId}")]
    public static partial void OrderProcessingStarted(
        this ILogger logger, int orderId);
    
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Order processing completed. OrderId: {OrderId}, Duration: {Duration}ms")]
    public static partial void OrderProcessingCompleted(
        this ILogger logger, int orderId, long duration);
    
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Order processing failed. OrderId: {OrderId}")]
    public static partial void OrderProcessingFailed(
        this ILogger logger, int orderId, Exception exception);
}

// Uso
_logger.OrderProcessingStarted(orderId);
_logger.OrderProcessingCompleted(orderId, stopwatch.ElapsedMilliseconds);
_logger.OrderProcessingFailed(orderId, ex);
```

## OpenTelemetry para Tracing

Para métricas e tracing distribuído, use OpenTelemetry:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyService", serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mvp24Hours.*")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://localhost:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Mvp24Hours.*")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });

// Expor métricas Prometheus
app.MapPrometheusScrapingEndpoint();
```

### Criando Activities (Spans)

```csharp
using System.Diagnostics;

public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new("Mvp24Hours.OrderService");
    
    public async Task<Order> ProcessOrderAsync(int orderId)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var order = await GetOrderAsync(orderId);
            activity?.SetTag("order.total", order.Total);
            
            await ProcessPaymentAsync(order);
            activity?.AddEvent(new ActivityEvent("PaymentProcessed"));
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Checklist de Migração

- [ ] Identificar todas as chamadas `TelemetryHelper.Execute()`
- [ ] Adicionar `ILogger<T>` via injeção de dependência nas classes
- [ ] Substituir chamadas por `_logger.Log*()` equivalentes
- [ ] Remover registros de `AddMvp24HoursTelemetry()`
- [ ] Configurar `AddLogging()` no `Program.cs`
- [ ] (Opcional) Implementar source-generated logs para alta performance
- [ ] (Opcional) Configurar OpenTelemetry para tracing/métricas
- [ ] Testar que os logs aparecem corretamente
- [ ] Remover referências a `TelemetryLevels` enum
- [ ] Remover implementações de `ITelemetryService`

## Suporte

Se encontrar dificuldades na migração, abra uma issue no repositório GitHub do Mvp24Hours com a tag `migration`.

