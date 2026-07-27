# OpenTelemetry exporters

`AddMvp24HoursOpenTelemetry` registers exporter option objects and base Mvp24Hours observability services. It does **not** call OpenTelemetry SDK exporter extensions for you. Configure the SDK pipeline explicitly or through Aspire service defaults.

## Register the option model

```csharp
builder.Services.AddMvp24HoursOpenTelemetry(options =>
{
    options.ServiceName = "Orders";
    options.ServiceVersion = "1.0.0";
    options.Environment = builder.Environment.EnvironmentName;
    options.Otlp.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        ?? "http://localhost:4317";
    options.Console.Enabled = builder.Environment.IsDevelopment();
});
```

Then configure packages installed by the application:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
```

## `OpenTelemetryExporterOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string?` | `null` | OpenTelemetry resource service name. |
| `ServiceVersion` | `string?` | `null` | Service version. |
| `Environment` | `string?` | `null` | Deployment environment. |
| `ServiceNamespace` | `string?` | `null` | Service namespace. |
| `ServiceInstanceId` | `string?` | `null` | Service instance ID. |
| `Otlp` | `OtlpExporterOptions` | new instance | OTLP model. |
| `Console` | `ConsoleExporterOptions` | new instance | Console model. |
| `Prometheus` | `PrometheusExporterOptions` | new instance | Prometheus model. |
| `ResourceAttributes` | `Dictionary<string,object>` | empty | Additional resource attributes. |

### `OtlpExporterOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enables OTLP in application wiring. |
| `Endpoint` | `string` | `http://localhost:4317` | Collector endpoint. |
| `Protocol` | `OtlpExportProtocol` | `Grpc` | gRPC or HTTP/protobuf model. |
| `EnableTracing` | `bool` | `true` | Trace export switch. |
| `EnableMetrics` | `bool` | `true` | Metric export switch. |
| `EnableLogging` | `bool` | `true` | Log export switch. |
| `TimeoutMs` | `int` | `10000` | Export timeout. |
| `Headers` | `Dictionary<string,string>` | empty | OTLP request headers. |
| `BatchExportScheduledDelayMs` | `int` | `5000` | Batch interval. |
| `MaxExportBatchSize` | `int` | `512` | Maximum batch. |
| `MaxQueueSize` | `int` | `2048` | Pending export queue. |

### Console and Prometheus

| Name | Type | Default | Description |
|---|---|---|---|
| `Console.Enabled` | `bool` | `false` | Console export switch. |
| `Console.EnableTracing` | `bool` | `true` | Trace output. |
| `Console.EnableMetrics` | `bool` | `false` | Metric output. |
| `Console.EnableLogging` | `bool` | `true` | Log output. |
| `Console.EnableTimestamps` | `bool` | `true` | Timestamp output. |
| `Console.UseColors` | `bool` | `true` | Color preference model. |
| `Prometheus.Enabled` | `bool` | `true` | Prometheus switch. |
| `Prometheus.ScrapeEndpoint` | `string` | `/metrics` | Intended route. |
| `Prometheus.ScrapeResponseCacheDurationMs` | `int` | `5000` | Intended response cache. |
| `Prometheus.EnableExemplars` | `bool` | `true` | Exemplar preference. |
| `Prometheus.RequireAuthentication` | `bool` | `false` | Endpoint security preference. |

These options are not automatically projected into third-party exporter builders. Read the registered options and apply relevant values in your host composition code.

## Presets and validation

`GetDevelopmentDefaults(serviceName)` enables Console, local OTLP and Prometheus. `GetProductionDefaults(serviceName, endpoint)` disables Console and enables OTLP/Prometheus. `Validate()` checks service name, OTLP URI and Prometheus route.

## Aspire

Prefer Aspire service defaults when available. They configure resource identity, OTLP and standard instrumentations from environment variables. Add Mvp24Hours sources/meters to that existing pipeline instead of creating a second provider.

Related: [home](home.md), [logging](logging.md), [tracing](tracing.md), [metrics](metrics.md), [Aspire](../modernization/aspire.md).
