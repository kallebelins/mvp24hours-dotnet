# Integração com Telemetria

## Visão Geral

O Mediator integra-se com o `ITelemetryService` existente no Mvp24Hours e com OpenTelemetry para coleta de métricas e traces distribuídos.

## ITelemetryService Existente

```csharp
public interface ITelemetryService
{
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
    void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
    void TrackDependency(string name, string data, DateTimeOffset startTime, TimeSpan duration, bool success);
}
```

## TelemetryBehavior

```csharp
public sealed class TelemetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<TelemetryBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            // Track success
            _telemetry.TrackEvent($"Mediator.{requestName}.Success", new Dictionary<string, string>
            {
                ["RequestType"] = requestName,
                ["ResponseType"] = typeof(TResponse).Name,
                ["Duration"] = stopwatch.ElapsedMilliseconds.ToString()
            });

            _telemetry.TrackMetric($"Mediator.{requestName}.Duration", 
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Track failure
            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["RequestType"] = requestName,
                ["Duration"] = stopwatch.ElapsedMilliseconds.ToString()
            });

            _telemetry.TrackEvent($"Mediator.{requestName}.Failure", new Dictionary<string, string>
            {
                ["RequestType"] = requestName,
                ["ExceptionType"] = ex.GetType().Name,
                ["ErrorMessage"] = ex.Message
            });

            throw;
        }
    }
}
```

## OpenTelemetry

### Configuração

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

## Métricas Customizadas

### Contador de Requests

```csharp
// Número total de requests por tipo
mediator_requests_total{request_type="CreateOrderCommand", success="true"} 150
mediator_requests_total{request_type="CreateOrderCommand", success="false"} 5
```

### Histograma de Duração

```csharp
// Distribuição de tempo de resposta
mediator_request_duration_ms{request_type="CreateOrderCommand", le="10"} 50
mediator_request_duration_ms{request_type="CreateOrderCommand", le="50"} 120
mediator_request_duration_ms{request_type="CreateOrderCommand", le="100"} 145
mediator_request_duration_ms{request_type="CreateOrderCommand", le="+Inf"} 155
```

### Gauge de Requests em Andamento

```csharp
private static readonly UpDownCounter<long> InFlightRequests = 
    Meter.CreateUpDownCounter<long>("mediator_requests_in_flight");

// No behavior
InFlightRequests.Add(1, tags);
try { ... }
finally { InFlightRequests.Add(-1, tags); }
```

## Integração com Application Insights

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

## Dashboard Grafana

### Painel de Requests

```promql
# Taxa de requests por segundo
rate(mediator_requests_total[5m])

# Taxa de erros
sum(rate(mediator_requests_total{success="false"}[5m])) 
/ sum(rate(mediator_requests_total[5m]))

# P95 de latência
histogram_quantile(0.95, rate(mediator_request_duration_ms_bucket[5m]))
```

### Alertas

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

## Boas Práticas

1. **Dimensões Úteis**: Adicione tags relevantes (request_type, success)
2. **Cardinalidade**: Evite tags com alta cardinalidade
3. **Histogramas**: Use para latência (não média)
4. **Exporters**: Configure OTLP para backends centralizados
5. **Sampling**: Configure sampling para alto volume
6. **Contexto**: Propague trace context em eventos

