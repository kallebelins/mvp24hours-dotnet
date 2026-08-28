# Telemetry Integration

## Overview

The Mediator emits telemetry through `ILogger<T>` and `System.Diagnostics` (`ActivitySource`/`Meter`),
integrated with OpenTelemetry for metrics collection and distributed traces.

> The legacy `ITelemetryService` interface was removed in 10.8.0 — see
> [Observability → Migration](../../observability/migration.md). The built-in `TelemetryBehavior`
> never depended on it.

## TelemetryBehavior

The shipped `TelemetryBehavior<TRequest, TResponse>` (registered by `RegisterTelemetryBehavior`) writes
structured log entries and enriches the current `Activity`. A hand-written equivalent looks like this:

```csharp
public sealed class TelemetryBehavior<TRequest, TResponse>(
    ILogger<TelemetryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            TResponse result = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "Mediator {RequestType} succeeded in {ElapsedMilliseconds}ms (response {ResponseType})",
                requestName, stopwatch.ElapsedMilliseconds, typeof(TResponse).Name);

            Activity.Current?.SetTag("mediator.request_type", requestName);
            Activity.Current?.SetTag("mediator.duration_ms", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex,
                "Mediator {RequestType} failed after {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
```

## OpenTelemetry

### Configuration

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "MyApp"))
    .WithTracing(tracing => tracing
        .AddSource("Mvp24Hours.Mediator")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("Mvp24Hours.Mediator")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### OpenTelemetryBehavior

```csharp
public sealed class OpenTelemetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = 
        new("Mvp24Hours.Mediator");
    
    private static readonly Meter Meter = 
        new("Mvp24Hours.Mediator");
    
    private static readonly Counter<long> RequestCounter = 
        Meter.CreateCounter<long>("mediator_requests_total");
    
    private static readonly Histogram<double> RequestDuration = 
        Meter.CreateHistogram<double>("mediator_request_duration_ms");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        using var activity = ActivitySource.StartActivity(
            $"Mediator.{requestName}",
            ActivityKind.Internal);

        activity?.SetTag("mediator.request_type", requestName);
        activity?.SetTag("mediator.response_type", typeof(TResponse).Name);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await next();
            success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            var tags = new TagList
            {
                { "request_type", requestName },
                { "success", success.ToString() }
            };
            
            RequestCounter.Add(1, tags);
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, tags);
        }
    }
}
```

## Custom Metrics

### Request Counter

```csharp
// Total requests by type
mediator_requests_total{request_type="CreateOrderCommand", success="true"} 150
mediator_requests_total{request_type="CreateOrderCommand", success="false"} 5
```

### Duration Histogram

```csharp
// Response time distribution
mediator_request_duration_ms{request_type="CreateOrderCommand", le="10"} 50
mediator_request_duration_ms{request_type="CreateOrderCommand", le="50"} 120
mediator_request_duration_ms{request_type="CreateOrderCommand", le="100"} 145
mediator_request_duration_ms{request_type="CreateOrderCommand", le="+Inf"} 155
```

### In-Flight Requests Gauge

```csharp
private static readonly UpDownCounter<long> InFlightRequests = 
    Meter.CreateUpDownCounter<long>("mediator_requests_in_flight");

// In behavior
InFlightRequests.Add(1, tags);
try { ... }
finally { InFlightRequests.Add(-1, tags); }
```

## Application Insights Integration

```csharp
public class ApplicationInsightsTelemetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly TelemetryClient _telemetryClient;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>(
            $"Mediator.{requestName}");
        
        operation.Telemetry.Type = "Mediator";
        operation.Telemetry.Data = JsonSerializer.Serialize(request);

        try
        {
            var result = await next();
            operation.Telemetry.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
}
```

## Grafana Dashboard

### Request Panel

```promql
# Requests per second rate
rate(mediator_requests_total[5m])

# Error rate
sum(rate(mediator_requests_total{success="false"}[5m])) 
/ sum(rate(mediator_requests_total[5m]))

# P95 latency
histogram_quantile(0.95, rate(mediator_request_duration_ms_bucket[5m]))
```

### Alerts

```yaml
groups:
  - name: mediator
    rules:
      - alert: HighErrorRate
        expr: |
          sum(rate(mediator_requests_total{success="false"}[5m]))
          / sum(rate(mediator_requests_total[5m])) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate in Mediator"

      - alert: HighLatency
        expr: |
          histogram_quantile(0.95, rate(mediator_request_duration_ms_bucket[5m])) > 1000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency in Mediator requests"
```

## Best Practices

1. **Useful Dimensions**: Add relevant tags (request_type, success)
2. **Cardinality**: Avoid high cardinality tags
3. **Histograms**: Use for latency (not average)
4. **Exporters**: Configure OTLP for centralized backends
5. **Sampling**: Configure sampling for high volume
6. **Context**: Propagate trace context in events

