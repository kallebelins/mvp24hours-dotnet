# OpenTelemetry Logging Integration

> Modern structured logging with automatic trace correlation

The Mvp24Hours framework provides deep integration between ILogger and OpenTelemetry, enabling automatic correlation between logs and distributed traces, structured logging with semantic conventions, and configurable log sampling for high-load environments.

## Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Mvp24Hours Logging Architecture                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐   │
│   │   Your Code     │───▶│    ILogger<T>    │───▶│  OpenTelemetry SDK  │   │
│   │                 │    │                  │    │                     │   │
│   │  _logger.Log()  │    │  + Trace Context │    │  + OTLP Exporter    │   │
│   │                 │    │  + Enrichers     │    │  + Console          │   │
│   └─────────────────┘    └──────────────────┘    └─────────────────────┘   │
│                                   │                         │               │
│                                   │                         ▼               │
│   ┌─────────────────┐             │              ┌─────────────────────┐   │
│   │  Activity.Current│◀────────────┘              │  Observability      │   │
│   │                  │                            │  Backend            │   │
│   │  TraceId, SpanId │                            │  (Jaeger, Seq,      │   │
│   │  CorrelationId   │                            │   Loki, etc.)       │   │
│   └─────────────────┘                             └─────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Configure Services

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Mvp24Hours logging with defaults
builder.Services.AddMvp24HoursLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableTraceCorrelation = true;
});

// Configure logging builder
builder.Logging.AddMvp24HoursDefaults();
```

### 2. Configure OpenTelemetry (Optional)

```csharp
// Option A: Use Mvp24Hours OpenTelemetry Logging extension
builder.Services.AddMvp24HoursOpenTelemetryLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableOtlpExporter = true;
    options.OtlpEndpoint = "http://localhost:4317";
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

// Option B: Configure OpenTelemetry SDK directly
builder.Logging
    .AddMvp24HoursOpenTelemetryConfig("MyService")
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        
        // Export to OTLP endpoint (Jaeger, Grafana, etc.)
        options.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri("http://localhost:4317");
        });
    });
```

### 3. Use Logging with Trace Context

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessOrder(Order order)
    {
        // Trace context is automatically included
        using (_logger.BeginTraceScope())
        {
            _logger.LogInformation(
                "Processing order {OrderId} for customer {CustomerId}",
                order.Id,
                order.CustomerId);
            
            // ... process order
            
            _logger.LogInformation(
                "Order {OrderId} processed successfully",
                order.Id);
        }
    }
}
```

## Configuration Options

### LoggingOptions

```csharp
services.AddMvp24HoursLogging(options =>
{
    // Service identification
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.Environment = "Production";
    
    // Trace correlation (default: true)
    options.EnableTraceCorrelation = true;
    
    // Log sampling for high-load environments (default: false)
    options.EnableLogSampling = true;
    options.SamplingRatio = 0.1; // Sample 10% of logs
    
    // Context enrichment (default: true)
    options.EnableUserContextEnrichment = true;
    options.EnableTenantContextEnrichment = true;
    
    // Custom resource attributes
    options.ResourceAttributes["deployment.region"] = "us-east-1";
    options.ResourceAttributes["app.team"] = "payments";
});
```

### Configuration via appsettings.json

```json
{
  "Mvp24Hours": {
    "Logging": {
      "ServiceName": "MyService",
      "ServiceVersion": "1.0.0",
      "EnableTraceCorrelation": true,
      "EnableLogSampling": false,
      "SamplingRatio": 1.0,
      "EnableUserContextEnrichment": true,
      "EnableTenantContextEnrichment": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Mvp24Hours": "Debug"
    }
  }
}
```

## Log Enrichment

### Automatic Trace Context

When `EnableTraceCorrelation` is true, logs automatically include:

| Property | Description |
|----------|-------------|
| `TraceId` | OpenTelemetry trace ID |
| `SpanId` | Current span ID |
| `ParentSpanId` | Parent span ID |
| `correlation.id` | Correlation ID from baggage |
| `causation.id` | Causation ID from baggage |

### User Context Enrichment

When `EnableUserContextEnrichment` is true:

| Property | Description |
|----------|-------------|
| `enduser.id` | User ID |
| `enduser.name` | User name |
| `enduser.roles` | User roles (comma-separated) |

### Tenant Context Enrichment

When `EnableTenantContextEnrichment` is true:

| Property | Description |
|----------|-------------|
| `tenant.id` | Tenant ID |
| `tenant.name` | Tenant name |

### Custom Enrichers

```csharp
public class RequestIdEnricher : ILogEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var requestId = _httpContextAccessor.HttpContext?
            .Request.Headers["X-Request-ID"].FirstOrDefault();
            
        return new Dictionary<string, object?>
        {
            ["request.id"] = requestId
        };
    }
}

// Register
services.AddSingleton<ILogEnricher, RequestIdEnricher>();
```

## Log Scopes

### Built-in Scope Factories

```csharp
// HTTP Request scope
using (LogScopeFactory.BeginHttpScope(_logger, "POST", "/api/orders"))
{
    _logger.LogInformation("Processing HTTP request");
}

// Database operation scope
using (LogScopeFactory.BeginDbScope(_logger, "sqlserver", "INSERT", "Orders"))
{
    _logger.LogInformation("Inserting order into database");
}

// Messaging scope
using (LogScopeFactory.BeginMessagingScope(_logger, "rabbitmq", "orders-queue", messageId))
{
    _logger.LogInformation("Processing message");
}

// CQRS/Mediator scope
using (LogScopeFactory.BeginMediatorScope(_logger, "CreateOrderCommand", "Command"))
{
    _logger.LogInformation("Handling command");
}

// Pipeline scope
using (LogScopeFactory.BeginPipelineScope(_logger, "OrderPipeline", "ValidateOrder", 1))
{
    _logger.LogInformation("Executing pipeline operation");
}

// Cache scope
using (LogScopeFactory.BeginCacheScope(_logger, "redis", "get", "order:123"))
{
    _logger.LogInformation("Cache operation");
}

// Background job scope
using (LogScopeFactory.BeginJobScope(_logger, "job-123", "ProcessOrdersJob", attempt: 1))
{
    _logger.LogInformation("Executing background job");
}

// Error scope
using (LogScopeFactory.BeginErrorScope(_logger, exception, "ORDER_001", "Validation"))
{
    _logger.LogError(exception, "Order validation failed");
}
```

## Log Sampling

For high-load environments, enable log sampling to reduce volume:

### Ratio-Based Sampling

```csharp
services.AddMvp24HoursLogging(options =>
{
    options.EnableLogSampling = true;
    options.SamplingRatio = 0.1; // 10% of logs
});
```

### Level-Based Sampling

```csharp
// Register custom sampler
services.AddSingleton<ILogSampler>(
    LevelBasedLogSampler.CreateHighLoadDefaults());

// Defaults:
// - Trace: 1%
// - Debug: 5%
// - Information: 10%
// - Warning: 50%
// - Error: 100%
// - Critical: 100%
```

### Trace-Context-Aware Sampling

```csharp
// Sample logs only for sampled traces
services.AddSingleton<ILogSampler>(
    new TraceContextLogSampler(fallbackRatio: 0.1));
```

## Log Context Accessor

Access log context programmatically:

```csharp
public class MyService
{
    private readonly ILogContextAccessor _logContext;
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogContextAccessor logContext, ILogger<MyService> logger)
    {
        _logContext = logContext;
        _logger = logger;
    }
    
    public void DoWork()
    {
        // Access trace context
        var traceId = _logContext.TraceId;
        var correlationId = _logContext.CorrelationId;
        
        // Begin scope with enrichment
        using (_logContext.BeginTraceScope(_logger))
        {
            _logger.LogInformation("Working with trace {TraceId}", traceId);
        }
    }
}
```

## High-Performance Logging

### Source-Generated Log Methods

For best performance, use source-generated logging:

```csharp
public static partial class OrderLogMessages
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Processing order {OrderId} for customer {CustomerId}")]
    public static partial void ProcessingOrder(
        ILogger logger,
        string orderId,
        string customerId);
    
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Error,
        Message = "Order {OrderId} processing failed: {ErrorMessage}")]
    public static partial void OrderProcessingFailed(
        ILogger logger,
        string orderId,
        string errorMessage,
        Exception exception);
}

// Usage
OrderLogMessages.ProcessingOrder(_logger, order.Id, order.CustomerId);
```

### Event ID Conventions

| Module | Event ID Range |
|--------|---------------|
| Core | 1000-1999 |
| Pipe | 2000-2999 |
| CQRS | 3000-3999 |
| Data | 4000-4999 |
| RabbitMQ | 5000-5999 |
| WebAPI | 6000-6999 |
| Caching | 7000-7999 |
| CronJob | 8000-8999 |
| Infrastructure | 9000-9999 |

## Resource Attributes

### Standard Attributes

```csharp
var attributes = OpenTelemetryLoggingExtensions
    .GetMvp24HoursResourceAttributes(
        serviceName: "MyService",
        serviceVersion: "1.0.0",
        environment: "Production");

// Includes:
// - service.name
// - service.version
// - service.instance.id
// - deployment.environment
// - host.name
// - process.pid
// - process.runtime.name
// - process.runtime.version
// - telemetry.sdk.name
// - telemetry.sdk.language
// - telemetry.sdk.version
```

## Environment-Specific Defaults

Apply pre-configured log level settings for different environments:

```csharp
// For Development - verbose logging
builder.Logging.ApplyMvp24HoursDevelopmentDefaults();
// Sets: Debug level, with filters for Microsoft and System namespaces

// For Production - optimized for performance
builder.Logging.ApplyMvp24HoursProductionDefaults();
// Sets: Information level, with stricter filters for framework namespaces
```

## Structured Logging Helpers

Use the structured logging extension methods for consistent logging with trace context:

```csharp
// Log with automatic trace context
_logger.LogInformationWithTrace("Processing order {OrderId}", orderId);
_logger.LogWarningWithTrace("Order {OrderId} has low stock", orderId);
_logger.LogErrorWithTrace(exception, "Order {OrderId} failed", orderId);
_logger.LogCriticalWithTrace(exception, "System failure in order processing");

// Log HTTP requests with structured attributes
_logger.LogHttpRequest("POST", "/api/orders", 201, durationMs: 45);

// Log database operations with structured attributes
_logger.LogDatabaseOperation("sqlserver", "INSERT", "Orders", durationMs: 12, rowsAffected: 1);

// Log messaging operations with structured attributes
_logger.LogMessagingOperation("rabbitmq", "orders-queue", "publish", messageId);

// Log CQRS/Mediator requests with structured attributes
_logger.LogMediatorRequest("CreateOrderCommand", "Command", durationMs: 150, success: true);
```

## All-in-One Configuration

Use `AddMvp24HoursObservability` to configure logging, tracing, and metrics together:

```csharp
services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.Environment = "Production";
    
    // Enable all pillars
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
    
    // Logging-specific options
    options.Logging.EnableTraceCorrelation = true;
    options.Logging.EnableLogSampling = false;
    
    // Tracing-specific options
    options.Tracing.EnableCorrelationIdPropagation = true;
    options.Tracing.AddDefaultEnrichers = true;
    
    // Metrics-specific options
    options.Metrics.EnablePipelineMetrics = true;
    options.Metrics.EnableCqrsMetrics = true;
});
```

## Integration with Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "MyService")
        .Enrich.With<TraceIdEnricher>()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://localhost:4317";
            options.Protocol = OtlpProtocol.Grpc;
        });
});

// Custom Serilog enricher for trace context
public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(
                factory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(
                factory.CreateProperty("SpanId", activity.SpanId.ToString()));
        }
    }
}
```

## Best Practices

### 1. Always Use Message Templates

```csharp
// Good - structured logging
_logger.LogInformation(
    "Processing order {OrderId} for {CustomerId}",
    order.Id,
    order.CustomerId);

// Bad - string interpolation
_logger.LogInformation(
    $"Processing order {order.Id} for {order.CustomerId}");
```

### 2. Use Appropriate Log Levels

| Level | Use For |
|-------|---------|
| Trace | Detailed diagnostic information |
| Debug | Development debugging information |
| Information | Normal application flow |
| Warning | Unusual but recoverable situations |
| Error | Errors that prevent operation completion |
| Critical | System-wide failures |

### 3. Include Context in Scopes

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"] = order.Id,
    ["CustomerId"] = order.CustomerId
}))
{
    // All logs within this scope include OrderId and CustomerId
    _logger.LogInformation("Starting order processing");
    // ... more operations
    _logger.LogInformation("Order processing completed");
}
```

### 4. Don't Log Sensitive Data

```csharp
// Bad
_logger.LogInformation("User password: {Password}", user.Password);

// Good - mask sensitive data
_logger.LogInformation("User authenticated: {UserId}", user.Id);
```

## See Also

- [Tracing with OpenTelemetry](tracing.md)
- [Metrics and Monitoring](metrics.md)
- [Migration from TelemetryHelper](migration.md)


