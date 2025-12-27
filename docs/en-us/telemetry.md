# Telemetry

> ⚠️ **DEPRECATED**: This API is deprecated and will be removed in a future version.
> 
> **Migrate to `ILogger<T>` (Microsoft.Extensions.Logging) and OpenTelemetry.**
> 
> See the [Migration Guide](/en-us/observability/migration.md) for complete instructions.

---

Solution created to track all application execution levels. You can inject actions for processing using any log manager, including metrics and trace.

## ⚠️ Deprecation Notice

Starting from this version, the following components are marked as `[Obsolete]`:

- `TelemetryHelper` - Use `ILogger<T>` instead
- `TelemetryLevels` - Use `Microsoft.Extensions.Logging.LogLevel` instead
- `ITelemetryService` - Use `ILogger<T>` or implement `ILoggerProvider` instead
- `AddMvp24HoursTelemetry()` - Use `AddLogging()` instead

### Level Comparison

| TelemetryLevels (Old) | LogLevel (New) |
|-----------------------|----------------|
| `Verbose` | `LogLevel.Debug` or `LogLevel.Trace` |
| `Information` | `LogLevel.Information` |
| `Warning` | `LogLevel.Warning` |
| `Error` | `LogLevel.Error` |
| `Critical` | `LogLevel.Critical` |

## Configuration (Legacy - Deprecated)

```csharp
/// Startup.cs
Logger logger = LogManager.GetCurrentClassLogger(); // any log manager

// trace
services.AddMvp24HoursTelemetry(TelemetryLevel.Information | TelemetryLevel.Verbose,
    (name, state) =>
    {
        logger.Trace($"{name}|{string.Join("|", state)}");
    }
);

// error
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

// ignore events
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");
```

## Run (Legacy - Deprecated)

```csharp
/// MyFile.cs
TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{tokenDefault}");
```

---

## ✅ New Recommended Approach

### Configuration with ILogger

```csharp
// Program.cs or Startup.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
    
    // Filter by category
    logging.AddFilter("Mvp24Hours", LogLevel.Debug);
    logging.AddFilter("Microsoft", LogLevel.Warning);
});
```

### Usage with ILogger<T>

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
        // Before (deprecated):
        // TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{token}");
        
        // After (recommended):
        _logger.LogDebug("RabbitMQ client publish started. Token: {Token}", token);
    }
    
    public void HandleError(Exception ex)
    {
        // Before (deprecated):
        // TelemetryHelper.Execute(TelemetryLevels.Error, "operation-failure", ex);
        
        // After (recommended):
        _logger.LogError(ex, "Operation failed");
    }
}
```

### High-Performance Structured Logging

```csharp
// Define log messages in a separate class for high performance
public static partial class LogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "RabbitMQ client publish started. Token: {Token}")]
    public static partial void RabbitMqPublishStarted(this ILogger logger, string token);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Operation failed")]
    public static partial void OperationFailed(this ILogger logger, Exception ex);
}

// Usage
_logger.RabbitMqPublishStarted(token);
_logger.OperationFailed(ex);
```

### OpenTelemetry for Distributed Tracing

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
// Using Activity for tracing
using System.Diagnostics;

public class MyService
{
    private static readonly ActivitySource ActivitySource = new("Mvp24Hours.MyService");
    
    public async Task ProcessAsync()
    {
        using var activity = ActivitySource.StartActivity("ProcessOperation");
        activity?.SetTag("custom.tag", "value");
        
        // ... operation
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
```

## See Also

- [Observability Migration Guide](/en-us/observability/migration.md)
- [Logging with ILogger](/en-us/logging.md)
- [OpenTelemetry Tracing](/en-us/cqrs/observability/tracing.md)
