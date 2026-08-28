# Telemetry

> 🚫 **REMOVED in 10.8.0**: the legacy static telemetry facade no longer exists.
>
> **Use `ILogger<T>` (Microsoft.Extensions.Logging) and OpenTelemetry.**
>
> See the [Migration Guide](/en-us/observability/migration.md) for complete instructions.

---

## Removal Notice

The following types were `[Obsolete]` since 9.1.200 and were **deleted** in 10.8.0. They no longer
ship in any package, so code referencing them does not compile.

| Removed | Replacement |
|---------|-------------|
| `TelemetryHelper` (`Mvp24Hours.Helpers`) | `ILogger<T>` for logs; `Mvp24HoursActivitySources`/`Mvp24HoursMeters` for traces and metrics |
| `TelemetryLevels` (`Mvp24Hours.Core.Enums.Infrastructure`) | `Microsoft.Extensions.Logging.LogLevel` |
| `ITelemetryService` (`Mvp24Hours.Core.Contract.Infrastructure.Logging`) | `ILogger<T>`, or `ILoggerProvider` for a custom sink |
| `AddMvp24HoursTelemetry()` | `AddMvp24HoursLogging()` / `AddLogging()` |
| `AddMvp24HoursTelemetryFiltered()` | log-level filtering by category (`AddFilter`, `Logging:LogLevel`) |
| `AddMvp24HoursTelemetryIgnore()` | log-level filtering by category (`AddFilter`, `Logging:LogLevel`) |

The `Add*` extensions took an `IServiceCollection` but registered nothing in it — every overload only
pushed handlers into the static helper. Nothing in DI was lost by their removal.

### Level Comparison

| TelemetryLevels (Removed) | LogLevel (New) |
|---------------------------|----------------|
| `Verbose` | `LogLevel.Debug` or `LogLevel.Trace` |
| `Information` | `LogLevel.Information` |
| `Warning` | `LogLevel.Warning` |
| `Error` | `LogLevel.Error` |
| `Critical` | `LogLevel.Critical` |

### Migrating the registration

```csharp
// Before (removed in 10.8.0):
services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
    (name, state) => logger.Trace($"{name}|{string.Join("|", state)}"));
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");

// After:
services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("Mvp24Hours.Infrastructure.RabbitMQ", LogLevel.Warning);
});
```

### Migrating the call site

```csharp
// Before (removed in 10.8.0):
TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{token}");

// After:
_logger.LogDebug("RabbitMQ client publish started. Token: {Token}", token);
```

---

## ✅ Recommended Approach

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
