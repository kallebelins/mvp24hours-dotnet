# OpenTelemetry Metrics

The Mvp24Hours framework provides comprehensive metrics instrumentation using `System.Diagnostics.Metrics`, the native .NET API that integrates seamlessly with OpenTelemetry.

## Overview

Metrics allow you to monitor the performance, health, and behavior of your application in real-time. The framework provides pre-built metrics for all major modules:

- **Pipeline** - Operation execution counts and durations
- **Repository/Data** - Database queries, commands, and connections
- **CQRS/Mediator** - Commands, queries, notifications, and behaviors
- **Messaging/RabbitMQ** - Message publish/consume rates and durations
- **Cache** - Hit/miss ratios and operation durations
- **HTTP/WebAPI** - Request counts, durations, and sizes
- **CronJob** - Job execution counts and durations
- **Infrastructure** - HTTP clients, email, SMS, file storage, locks, background jobs

## Installation

Metrics are included in the `Mvp24Hours.Core` package:

```bash
dotnet add package Mvp24Hours.Core
```

For OpenTelemetry integration:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
# Or for OTLP:
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## Basic Configuration

### Registering Metrics in DI

```csharp
using Mvp24Hours.Core.Observability;

// Register all metrics
services.AddMvp24HoursMetrics();

// Or with configuration
services.AddMvp24HoursMetrics(options =>
{
    options.EnablePipelineMetrics = true;
    options.EnableCqrsMetrics = true;
    options.EnableRepositoryMetrics = true;
    options.EnableMessagingMetrics = true;
    options.EnableCacheMetrics = true;
    options.EnableHttpMetrics = true;
    options.EnableCronJobMetrics = true;
    options.EnableInfrastructureMetrics = true;
});

// Or register individual metrics
services.AddPipelineMetrics();
services.AddCqrsMetrics();
services.AddRepositoryMetrics();
```

### OpenTelemetry Integration

```csharp
using Mvp24Hours.Core.Observability;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        // Add all Mvp24Hours meters
        foreach (var meterName in OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        {
            metrics.AddMeter(meterName);
        }

        // Or add specific modules
        metrics.AddMeter(Mvp24HoursMeters.Core.Name);
        metrics.AddMeter(Mvp24HoursMeters.Pipe.Name);
        metrics.AddMeter(Mvp24HoursMeters.Cqrs.Name);
        metrics.AddMeter(Mvp24HoursMeters.Data.Name);

        // Add exporters
        metrics.AddPrometheusExporter();
        // Or OTLP:
        // metrics.AddOtlpExporter();
    });
```

### Prometheus Endpoint

```csharp
// In Program.cs
var app = builder.Build();

// Expose /metrics endpoint for Prometheus scraping
app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

## Using Metrics in Your Code

### Pipeline Metrics

```csharp
public class MyPipeline
{
    private readonly PipelineMetrics _metrics;

    public MyPipeline(PipelineMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task ExecuteAsync()
    {
        // Automatic tracking with scope
        using var scope = _metrics.BeginExecution("OrderProcessingPipeline");
        try
        {
            // Execute pipeline operations
            await ProcessOrderAsync();
            
            scope.Complete(); // Mark as successful
        }
        catch (Exception ex)
        {
            scope.Fail(); // Mark as failed
            throw;
        }
    }

    private async Task ExecuteOperationAsync(string operationName)
    {
        using var opScope = _metrics.BeginOperation("OrderProcessingPipeline", operationName);
        try
        {
            // Operation logic
            await Task.Delay(100);
            opScope.Complete();
        }
        catch
        {
            opScope.Fail();
            throw;
        }
    }
}
```

### Repository Metrics

```csharp
public class ProductRepository
{
    private readonly RepositoryMetrics _metrics;
    private readonly DbContext _context;

    public ProductRepository(RepositoryMetrics metrics, DbContext context)
    {
        _metrics = metrics;
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var scope = _metrics.BeginQuery("GetById", "Product", "sqlserver");
        try
        {
            var product = await _context.Products.FindAsync(id);
            scope.Complete();
            return product;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }

    public async Task<int> SaveAsync(Product product)
    {
        using var scope = _metrics.BeginSaveChanges("sqlserver");
        try
        {
            _context.Products.Add(product);
            var rowsAffected = await _context.SaveChangesAsync();
            scope.Complete(rowsAffected);
            return rowsAffected;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}
```

### CQRS Metrics

```csharp
public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly CqrsMetrics _metrics;

    public MetricsBehavior(CqrsMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var isCommand = typeof(TRequest).Name.EndsWith("Command");
        
        using var scope = isCommand 
            ? _metrics.BeginCommand(requestName)
            : _metrics.BeginQuery(requestName);

        try
        {
            var response = await next();
            scope.Complete();
            return response;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}

// Recording specific events
_metrics.RecordValidationFailure("CreateOrderCommand");
_metrics.RecordCacheHit("GetProductQuery");
_metrics.RecordCacheMiss("GetProductQuery");
_metrics.RecordRetry("CreateOrderCommand", attemptNumber: 2);
_metrics.RecordCircuitBreakerTrip("ExternalApiQuery");
```

### Messaging Metrics

```csharp
public class OrderPublisher
{
    private readonly MessagingMetrics _metrics;
    private readonly IRabbitMQClient _client;

    public OrderPublisher(MessagingMetrics metrics, IRabbitMQClient client)
    {
        _metrics = metrics;
        _client = client;
    }

    public async Task PublishAsync<T>(T message, string exchange)
    {
        using var scope = _metrics.BeginPublish(typeof(T).Name, exchange);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
            await _client.PublishAsync(exchange, message);
            scope.Complete(payloadSize: bytes.Length);
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}

public class OrderConsumer : IMessageConsumer<OrderCreatedEvent>
{
    private readonly MessagingMetrics _metrics;

    public async Task ConsumeAsync(ConsumeContext<OrderCreatedEvent> context)
    {
        using var scope = _metrics.BeginConsume(
            typeof(OrderCreatedEvent).Name,
            "orders-queue",
            "order-service");

        try
        {
            await ProcessOrderAsync(context.Message);
            scope.Complete();
            _metrics.RecordAcknowledge("orders-queue");
        }
        catch (Exception ex)
        {
            scope.Fail();
            _metrics.RecordReject("orders-queue", requeue: true);
            throw;
        }
    }
}
```

### Cache Metrics

```csharp
public class CachedProductService
{
    private readonly CacheMetrics _metrics;
    private readonly IDistributedCache _cache;

    public CachedProductService(CacheMetrics metrics, IDistributedCache cache)
    {
        _metrics = metrics;
        _cache = cache;
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";
        
        using var scope = _metrics.BeginGet("products");
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                scope.SetHit();
                return JsonSerializer.Deserialize<Product>(cached);
            }
            
            scope.SetMiss();
            return null;
        }
        catch
        {
            // Get operation doesn't fail on miss
            throw;
        }
    }

    public async Task SetProductAsync(Product product)
    {
        var cacheKey = $"product:{product.Id}";
        var json = JsonSerializer.Serialize(product);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var scope = _metrics.BeginSet("products");
        scope.SetItemSize(bytes.Length);
        
        await _cache.SetStringAsync(cacheKey, json);
    }
}
```

### HTTP Metrics

```csharp
public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpMetrics _metrics;

    public RequestMetricsMiddleware(RequestDelegate next, HttpMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path;

        using var scope = _metrics.BeginRequest(method, route);
        try
        {
            await _next(context);
            scope.SetStatusCode(context.Response.StatusCode);
        }
        catch
        {
            scope.SetStatusCode(500);
            throw;
        }
    }
}
```

## Available Metrics

### Pipeline Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `mvp24hours.pipe.executions_total` | Counter | Total pipeline executions |
| `mvp24hours.pipe.executions_failed_total` | Counter | Failed pipeline executions |
| `mvp24hours.pipe.execution_duration_ms` | Histogram | Pipeline execution duration |
| `mvp24hours.pipe.operations_total` | Counter | Total operation executions |
| `mvp24hours.pipe.operations_failed_total` | Counter | Failed operation executions |
| `mvp24hours.pipe.operation_duration_ms` | Histogram | Operation execution duration |
| `mvp24hours.pipe.active_count` | UpDownCounter | Currently active pipelines |

### Repository Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `mvp24hours.data.queries_total` | Counter | Total database queries |
| `mvp24hours.data.query_duration_ms` | Histogram | Query execution duration |
| `mvp24hours.data.commands_total` | Counter | Total database commands |
| `mvp24hours.data.command_duration_ms` | Histogram | Command execution duration |
| `mvp24hours.data.save_changes_total` | Counter | Total SaveChanges operations |
| `mvp24hours.data.rows_affected_total` | Counter | Total rows affected |
| `mvp24hours.data.connections_active` | UpDownCounter | Active connections |
| `mvp24hours.data.slow_queries_total` | Counter | Slow queries detected |
| `mvp24hours.data.transactions_total` | Counter | Total transactions |

### CQRS Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `mvp24hours.cqrs.commands_total` | Counter | Total commands processed |
| `mvp24hours.cqrs.command_duration_ms` | Histogram | Command processing duration |
| `mvp24hours.cqrs.queries_total` | Counter | Total queries processed |
| `mvp24hours.cqrs.query_duration_ms` | Histogram | Query processing duration |
| `mvp24hours.cqrs.notifications_total` | Counter | Total notifications |
| `mvp24hours.cqrs.domain_events_total` | Counter | Domain events dispatched |
| `mvp24hours.cqrs.validation_failures_total` | Counter | Validation failures |
| `mvp24hours.cqrs.cache_hits_total` | Counter | Cache hits |
| `mvp24hours.cqrs.cache_misses_total` | Counter | Cache misses |
| `mvp24hours.cqrs.retries_total` | Counter | Retry attempts |

### Messaging Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `mvp24hours.messaging.published_total` | Counter | Messages published |
| `mvp24hours.messaging.publish_duration_ms` | Histogram | Publish duration |
| `mvp24hours.messaging.consumed_total` | Counter | Messages consumed |
| `mvp24hours.messaging.consume_duration_ms` | Histogram | Consume duration |
| `mvp24hours.messaging.acknowledged_total` | Counter | Messages acknowledged |
| `mvp24hours.messaging.rejected_total` | Counter | Messages rejected |
| `mvp24hours.messaging.dead_lettered_total` | Counter | Messages sent to DLQ |
| `mvp24hours.messaging.queue_depth` | UpDownCounter | Queue depth |
| `mvp24hours.messaging.payload_size_bytes` | Histogram | Message payload size |

### Cache Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `mvp24hours.cache.gets_total` | Counter | Cache get operations |
| `mvp24hours.cache.hits_total` | Counter | Cache hits |
| `mvp24hours.cache.misses_total` | Counter | Cache misses |
| `mvp24hours.cache.sets_total` | Counter | Cache set operations |
| `mvp24hours.cache.invalidations_total` | Counter | Cache invalidations |
| `mvp24hours.cache.hit_ratio` | ObservableGauge | Cache hit ratio (%) |

## Prometheus Queries Examples

```promql
# Request rate per second
rate(mvp24hours_cqrs_commands_total[5m])

# Average command duration
histogram_quantile(0.95, rate(mvp24hours_cqrs_command_duration_ms_bucket[5m]))

# Error rate
sum(rate(mvp24hours_cqrs_commands_failed_total[5m])) / sum(rate(mvp24hours_cqrs_commands_total[5m]))

# Cache hit ratio
mvp24hours_cache_hit_ratio

# Active database connections
mvp24hours_data_connections_active

# Messages in queue
mvp24hours_messaging_queue_depth{queue_name="orders-queue"}
```

## Grafana Dashboard

Create dashboards with panels for:

1. **Request Rate** - Commands and queries per second
2. **Latency** - P50, P95, P99 durations
3. **Error Rate** - Failed operations percentage
4. **Cache Performance** - Hit/miss ratio
5. **Queue Depth** - Messages waiting to be processed
6. **Active Connections** - Database and messaging connections

## Best Practices

1. **Use Scopes** - Always use `BeginXxx()` scopes for automatic duration tracking
2. **Mark Success/Failure** - Call `Complete()` or `Fail()` before scope disposal
3. **Add Context Tags** - Include relevant dimensions (entity type, operation name)
4. **Monitor Key Metrics** - Focus on RED metrics (Rate, Errors, Duration)
5. **Set Alerts** - Configure alerts for error rates and latency thresholds

## Related Documentation

- [Tracing](tracing.md) - Distributed tracing with OpenTelemetry
- [Migration Guide](migration.md) - Migrating from TelemetryHelper

