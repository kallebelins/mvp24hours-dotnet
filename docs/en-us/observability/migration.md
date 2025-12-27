# Migration Guide: TelemetryHelper to ILogger/OpenTelemetry

This guide provides detailed instructions for migrating from the legacy telemetry system (`TelemetryHelper`, `TelemetryLevels`, `ITelemetryService`) to modern approaches using `ILogger<T>` and OpenTelemetry.

## Why Migrate?

The legacy telemetry system (`TelemetryHelper`) has been marked as **obsolete** for the following reasons:

1. **Industry Standard**: `ILogger<T>` is the .NET standard for structured logging
2. **Dependency Injection**: `ILogger` integrates seamlessly with .NET's DI system
3. **Testability**: Mocking `ILogger` is straightforward and well-documented
4. **Rich Ecosystem**: Native support for Serilog, NLog, Log4Net, Application Insights, etc.
5. **OpenTelemetry**: CNCF standard for distributed observability (logs + traces + metrics)
6. **Performance**: Source generators for high-performance logging

## Deprecation Timeline

| Version | Status |
|---------|--------|
| Current | Marked as `[Obsolete]` - compiler warnings |
| Next Minor | Maintained for compatibility |
| Next Major | **Completely removed** |

## API Mapping

### Log Levels

| TelemetryLevels | LogLevel | When to Use |
|-----------------|----------|-------------|
| `Verbose` | `Trace` | Detailed diagnostics (high volume) |
| `Verbose` | `Debug` | Debug information (development) |
| `Information` | `Information` | Normal application flow |
| `Warning` | `Warning` | Unexpected but recoverable situations |
| `Error` | `Error` | Errors preventing an operation |
| `Critical` | `Critical` | Catastrophic system failures |

### Configuration Methods

| Old | New |
|-----|-----|
| `AddMvp24HoursTelemetry()` | `AddLogging()` |
| `AddMvp24HoursTelemetryFiltered()` | `AddFilter()` on `ILoggingBuilder` |
| `AddMvp24HoursTelemetryIgnore()` | `AddFilter()` with `LogLevel.None` |

### Execution Methods

| Old | New |
|-----|-----|
| `TelemetryHelper.Execute(TelemetryLevels.Verbose, ...)` | `_logger.LogDebug(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Information, ...)` | `_logger.LogInformation(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Warning, ...)` | `_logger.LogWarning(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Error, ...)` | `_logger.LogError(...)` |
| `TelemetryHelper.Execute(TelemetryLevels.Critical, ...)` | `_logger.LogCritical(...)` |

## Migration Examples

### 1. Basic Configuration

**Before (Deprecated):**
```csharp
// Startup.cs
services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
    (name, state) =>
    {
        Console.WriteLine($"{name}|{string.Join("|", state)}");
    }
);
```

**After (Recommended):**
```csharp
// Program.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

### 2. Logging in Classes

**Before (Deprecated):**
```csharp
public class OrderService
{
    public void ProcessOrder(int orderId)
    {
        TelemetryHelper.Execute(TelemetryLevels.Information, 
            "order-processing-start", orderId);
        
        try
        {
            // ... processing
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

**After (Recommended):**
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
            // ... processing
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

### 3. Filters and Categories

**Before (Deprecated):**
```csharp
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");

services.AddMvp24HoursTelemetryFiltered("my-service",
    (name, state) => Console.WriteLine($"[FILTERED] {name}"));
```

**After (Recommended):**
```csharp
builder.Services.AddLogging(logging =>
{
    // Ignore logs from a specific category
    logging.AddFilter("RabbitMQ.Consumer", LogLevel.None);
    
    // Configure level per category
    logging.AddFilter("MyService", LogLevel.Debug);
    logging.AddFilter("Microsoft", LogLevel.Warning);
    logging.AddFilter("System", LogLevel.Warning);
});
```

### 4. Custom ITelemetryService

**Before (Deprecated):**
```csharp
public class CustomTelemetryService : ITelemetryService
{
    public void Execute(string eventName, params object[] args)
    {
        // Custom logic
        SendToExternalSystem(eventName, args);
    }
}

// Registration
TelemetryHelper.Add(TelemetryLevels.All, new CustomTelemetryService());
```

**After (Recommended):**
```csharp
// Option 1: Implement ILoggerProvider
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
        // Custom logic
        SendToExternalSystem(formatter(state, exception));
    }
}

// Registration
builder.Services.AddLogging(logging =>
{
    logging.AddProvider(new CustomLoggerProvider());
});
```

### 5. High-Performance Logging

**After (Recommended - Source Generators):**
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

// Usage
_logger.OrderProcessingStarted(orderId);
_logger.OrderProcessingCompleted(orderId, stopwatch.ElapsedMilliseconds);
_logger.OrderProcessingFailed(orderId, ex);
```

## OpenTelemetry for Tracing

For metrics and distributed tracing, use OpenTelemetry:

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

// Expose Prometheus metrics
app.MapPrometheusScrapingEndpoint();
```

### Creating Activities (Spans)

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

## Migration Checklist

- [ ] Identify all `TelemetryHelper.Execute()` calls
- [ ] Add `ILogger<T>` via dependency injection in classes
- [ ] Replace calls with equivalent `_logger.Log*()` methods
- [ ] Remove `AddMvp24HoursTelemetry()` registrations
- [ ] Configure `AddLogging()` in `Program.cs`
- [ ] (Optional) Implement source-generated logs for high performance
- [ ] (Optional) Configure OpenTelemetry for tracing/metrics
- [ ] Test that logs appear correctly
- [ ] Remove references to `TelemetryLevels` enum
- [ ] Remove `ITelemetryService` implementations

## Support

If you encounter difficulties during migration, open an issue on the Mvp24Hours GitHub repository with the `migration` tag.

