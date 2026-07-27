# OpenTelemetry metrics

Mvp24Hours metric classes use `System.Diagnostics.Metrics`. Register the classes in DI and subscribe to their meters in the OpenTelemetry SDK.

## Register

```csharp
builder.Services.AddMvp24HoursMetrics(options =>
{
    options.EnablePipelineMetrics = true;
    options.EnableRepositoryMetrics = true;
    options.EnableCqrsMetrics = true;
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

Individual registrations include `AddPipelineMetrics`, `AddRepositoryMetrics`, `AddCqrsMetrics`, `AddMessagingMetrics`, `AddCacheMetrics`, `AddHttpMetrics`, `AddCronJobMetrics`, and `AddInfrastructureMetrics`.

## `MetricsOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `EnablePipelineMetrics` | `bool` | `true` | Registers `PipelineMetrics`. |
| `EnableRepositoryMetrics` | `bool` | `true` | Registers `RepositoryMetrics`. |
| `EnableCqrsMetrics` | `bool` | `true` | Registers `CqrsMetrics`. |
| `EnableMessagingMetrics` | `bool` | `true` | Registers `MessagingMetrics`. |
| `EnableCacheMetrics` | `bool` | `true` | Registers `CacheMetrics`. |
| `EnableHttpMetrics` | `bool` | `true` | Registers `HttpMetrics`. |
| `EnableCronJobMetrics` | `bool` | `true` | Registers `CronJobMetrics`. |
| `EnableInfrastructureMetrics` | `bool` | `true` | Registers `InfrastructureMetrics`. |
| `ServiceName` | `string?` | `null` | Metric service identity stored in options. |
| `ServiceVersion` | `string?` | `null` | Metric service version stored in options. |

## Meter names

`Mvp24HoursMeters.AllMeterNames` covers Core, Pipe, CQRS, Data, RabbitMQ, WebAPI, Caching, CronJob and Infrastructure. `GetMvp24HoursMeterNames(...)` can select modules.

Use instrument names from `MetricNames` and the concrete metric classes as the source of truth. Do not derive alert names from converted Prometheus names without checking exporter normalization.

## Prometheus or OTLP

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint();
```

The endpoint extension comes from the OpenTelemetry Prometheus ASP.NET Core package, not Mvp24Hours. OTLP is usually simpler with Aspire because the dashboard and collector use the service-default pipeline.

## Cardinality guidance

Use bounded dimensions such as operation type, status and job type. Do not attach entity IDs, raw URLs, cache keys, user IDs or exception messages as metric tags.

Related: [home](home.md), [tracing](tracing.md), [exporters](exporters.md), [CronJob metrics](../cronjob-observability.md).
