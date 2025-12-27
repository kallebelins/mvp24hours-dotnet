# OpenTelemetry Tracing

The Mvp24Hours framework provides complete integration with **OpenTelemetry** for distributed tracing across all modules. This documentation covers how to configure and use tracing in your applications.

## Overview

Distributed tracing allows you to follow request flows through your microservices architecture. Mvp24Hours provides:

- **ActivitySources** per module for creating spans
- **Semantic tags** following OpenTelemetry conventions
- **Activity enrichers** for adding custom context
- **Trace propagation** following W3C Trace Context
- **Helper methods** for simplified tracing

## Quick Start

### 1. Install OpenTelemetry Packages

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.Otlp
```

### 2. Configure OpenTelemetry

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add Mvp24Hours tracing services
builder.Services.AddMvp24HoursTracing(options =>
{
    options.EnableCorrelationIdPropagation = true;
    options.EnableUserContext = true;
    options.EnableTenantContext = true;
    options.ServiceName = "MyService";
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            // Add all Mvp24Hours activity sources
            .AddSource(Mvp24HoursActivitySources.Core.Name)
            .AddSource(Mvp24HoursActivitySources.Pipe.Name)
            .AddSource(Mvp24HoursActivitySources.Cqrs.Name)
            .AddSource(Mvp24HoursActivitySources.Data.Name)
            .AddSource(Mvp24HoursActivitySources.RabbitMQ.Name)
            .AddSource(Mvp24HoursActivitySources.WebAPI.Name)
            .AddSource(Mvp24HoursActivitySources.Caching.Name)
            // Add ASP.NET Core instrumentation
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Export to console (development)
            .AddConsoleExporter()
            // Export to OTLP (Jaeger, Tempo, etc.)
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://localhost:4317");
            });
    });
```

## Activity Sources

Each module has its own `ActivitySource`:

| Module | Source Name | Description |
|--------|-------------|-------------|
| Core | `Mvp24Hours.Core` | Fundamental operations |
| Pipe | `Mvp24Hours.Pipe` | Pipeline and operations |
| CQRS | `Mvp24Hours.Cqrs` | Commands, Queries, Events |
| Data | `Mvp24Hours.Data` | Repository and database |
| RabbitMQ | `Mvp24Hours.RabbitMQ` | Messaging |
| WebAPI | `Mvp24Hours.WebAPI` | HTTP requests |
| Caching | `Mvp24Hours.Caching` | Cache operations |
| CronJob | `Mvp24Hours.CronJob` | Scheduled jobs |
| Infrastructure | `Mvp24Hours.Infrastructure` | Cross-cutting concerns |

### Accessing All Source Names

```csharp
// Get all source names for bulk registration
var allSources = Mvp24HoursActivitySources.AllSourceNames;

// Or select specific modules
var selectedSources = OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames(
    includeCore: true,
    includePipe: true,
    includeCqrs: true,
    includeData: true,
    includeRabbitMQ: false, // Exclude RabbitMQ
    includeWebAPI: true
);
```

## Creating Activities

### Using ActivityHelper

```csharp
using Mvp24Hours.Core.Observability;
using System.Diagnostics;

public class OrderService
{
    public async Task<Order> ProcessOrderAsync(CreateOrderCommand command)
    {
        // Start a command activity
        using var activity = ActivityHelper.StartCommandActivity("ProcessOrder");
        
        try
        {
            // Add custom tags
            activity?.SetTag(SemanticTags.OperationId, command.OrderId);
            
            // Business logic...
            var order = await CreateOrder(command);
            
            // Mark as successful
            activity?.SetSuccess();
            
            return order;
        }
        catch (Exception ex)
        {
            // Record error with exception details
            activity?.SetError(ex);
            throw;
        }
    }
}
```

### Using ScopedActivity

```csharp
public async Task ProcessAsync()
{
    using var scope = Mvp24HoursActivitySources.Core.Source
        .StartScopedActivity("MyOperation");
    
    try
    {
        scope.SetTag("order.id", orderId);
        
        // If an exception is thrown, it will be recorded automatically
        await DoWorkAsync();
        
        // Success is set automatically on dispose if no exception
    }
    catch (Exception ex)
    {
        scope.SetException(ex);
        throw;
    }
}
```

### Module-Specific Helpers

```csharp
// Pipeline activity
using var pipelineActivity = ActivityHelper.StartPipelineActivity("OrderPipeline", totalOperations: 5);

// Database activity
using var dbActivity = ActivityHelper.StartDatabaseActivity(
    operationName: "GetOrderById",
    dbOperation: "SELECT",
    dbSystem: "sqlserver",
    dbName: "OrdersDb");

// Message publishing
using var publishActivity = ActivityHelper.StartMessagePublishActivity(
    destinationName: "orders.created",
    routingKey: "orders.#");

// Cache operation
using var cacheActivity = ActivityHelper.StartCacheActivity(
    operation: "GET",
    cacheKey: "order:123",
    cacheSystem: "redis");
```

## Recording Events

Activities can contain events marking important points:

```csharp
using var activity = ActivityHelper.StartOperation(
    Mvp24HoursActivitySources.Core.Source,
    "ProcessPayment");

// Record retry attempts
activity?.RecordRetryAttempt(
    attemptNumber: 2,
    delay: TimeSpan.FromSeconds(5),
    reason: "Timeout");

// Record cache hit/miss
activity?.RecordCacheHit("order:123");
activity?.RecordCacheMiss("order:456");

// Record slow query
activity?.RecordSlowQuery(
    durationMs: 5000,
    thresholdMs: 1000,
    statement: "SELECT * FROM Orders WHERE...");

// Record validation failure
activity?.RecordValidationFailure(new[] 
{
    "Email is required",
    "Amount must be positive"
});

// Custom event
activity?.RecordEvent("order.validated", 
    ("items_count", 5),
    ("total_amount", 150.00));
```

## Activity Enrichers

Enrichers automatically add context to all activities:

### Built-in Enrichers

```csharp
builder.Services.AddMvp24HoursTracing(options =>
{
    // Add correlation ID from current context
    options.AddEnricher(new CorrelationIdEnricher
    {
        GetCorrelationId = () => HttpContext.Current?.Request.Headers["X-Correlation-Id"]
    });
    
    // Add user context
    options.AddEnricher(new UserContextEnricher
    {
        GetUserId = () => currentUserService.GetUserId(),
        GetUserName = () => currentUserService.GetUserName(),
        GetUserRoles = () => currentUserService.GetRoles()
    });
    
    // Add tenant context
    options.AddEnricher(new TenantContextEnricher
    {
        GetTenantId = () => tenantProvider.GetTenantId(),
        GetTenantName = () => tenantProvider.GetTenantName()
    });
});
```

### Custom Enricher

```csharp
public class OrderContextEnricher : ActivityEnricherBase
{
    private readonly IOrderContextAccessor _orderContext;
    
    public OrderContextEnricher(IOrderContextAccessor orderContext)
    {
        _orderContext = orderContext;
    }
    
    public override int Order => 10; // Execute after built-in enrichers
    
    public override void EnrichOnStart(Activity activity, object? context = null)
    {
        var orderId = _orderContext.CurrentOrderId;
        if (!string.IsNullOrEmpty(orderId))
        {
            activity.SetTag("order.id", orderId);
        }
    }
    
    public override void EnrichOnEnd(Activity activity, object? context = null, Exception? exception = null)
    {
        // Add end-of-activity enrichment
        activity.SetTag("order.completed", exception == null);
    }
}

// Register
builder.Services.AddActivityEnricher<OrderContextEnricher>();
```

## Trace Context Propagation

### Injecting Context (Outgoing Requests)

```csharp
// For HTTP requests
var headers = new Dictionary<string, string>();
TracePropagation.InjectTraceContext(headers);

httpClient.DefaultRequestHeaders.Add("traceparent", headers["traceparent"]);
if (headers.TryGetValue("tracestate", out var tracestate))
{
    httpClient.DefaultRequestHeaders.Add("tracestate", tracestate);
}

// Using typed injection
TracePropagation.InjectTraceContext(
    httpRequest.Headers, 
    (headers, key, value) => headers.Add(key, value));
```

### Extracting Context (Incoming Requests)

```csharp
// From HTTP headers
var headers = new Dictionary<string, string?>
{
    ["traceparent"] = request.Headers["traceparent"],
    ["tracestate"] = request.Headers["tracestate"],
    ["baggage"] = request.Headers["baggage"]
};

var traceContext = TracePropagation.ExtractTraceContext(headers);

if (traceContext != null)
{
    // Start activity with extracted parent context
    using var activity = Mvp24HoursActivitySources.WebAPI.Source
        .StartActivityWithParent("HandleRequest", traceContext, ActivityKind.Server);
    
    // Process request...
}
```

### Using ITraceContextAccessor

```csharp
public class MyService
{
    private readonly ITraceContextAccessor _traceContext;
    
    public MyService(ITraceContextAccessor traceContext)
    {
        _traceContext = traceContext;
    }
    
    public void DoWork()
    {
        // Get current trace information
        var traceId = _traceContext.TraceId;
        var spanId = _traceContext.SpanId;
        var correlationId = _traceContext.CorrelationId;
        
        // Get/set baggage
        var customValue = _traceContext.GetBaggageItem("custom.key");
        _traceContext.SetBaggageItem("custom.key", "value");
    }
}
```

## Semantic Tags

Use `SemanticTags` for consistent tag naming:

```csharp
using static Mvp24Hours.Core.Observability.SemanticTags;

activity?.SetTag(CorrelationId, "abc-123");
activity?.SetTag(EnduserId, "user-456");
activity?.SetTag(TenantId, "tenant-789");
activity?.SetTag(DbOperation, "SELECT");
activity?.SetTag(MessagingSystem, "rabbitmq");
activity?.SetTag(CacheHit, true);
```

### Available Tag Categories

- **General**: `CorrelationId`, `CausationId`, `OperationId`, `OperationName`, `OperationType`
- **User**: `EnduserId`, `EnduserName`, `EnduserRoles`
- **Tenant**: `TenantId`, `TenantName`
- **Error**: `ErrorType`, `ErrorMessage`, `ErrorCode`
- **Database**: `DbSystem`, `DbName`, `DbStatement`, `DbOperation`
- **HTTP**: `HttpMethod`, `HttpStatusCode`, `UrlPath`
- **Messaging**: `MessagingSystem`, `MessagingDestinationName`, `MessagingMessageId`
- **Cache**: `CacheSystem`, `CacheKey`, `CacheHit`
- **Pipeline**: `PipelineName`, `PipelineOperationName`, `PipelineOperationIndex`

## Integration with Exporters

### Jaeger

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddJaegerExporter(opts =>
            {
                opts.AgentHost = "localhost";
                opts.AgentPort = 6831;
            });
    });
```

### OTLP (Grafana Tempo, etc.)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://tempo:4317");
                opts.Protocol = OtlpExportProtocol.Grpc;
            });
    });
```

### Azure Application Insights

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddAzureMonitorTraceExporter(opts =>
            {
                opts.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
            });
    });
```

## Best Practices

1. **Always dispose activities** - Use `using` statements
2. **Set status on completion** - Call `SetSuccess()` or `SetError()`
3. **Use semantic tags** - For consistent naming across services
4. **Propagate context** - Pass trace context to downstream services
5. **Don't over-trace** - Focus on meaningful operations
6. **Mask sensitive data** - Never log passwords, tokens, or PII in tags

## See Also

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Migration from TelemetryHelper](migration.md)

