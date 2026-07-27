# Observability

This is the canonical starting point for Mvp24Hours logging, tracing and metrics on .NET 10. The library supplies DI services, activity sources, meters and exporter option models. The OpenTelemetry SDK still owns instrumentation and exporter pipelines.

## Entry points

| Need | Mvp24Hours API | Continue with |
|---|---|---|
| All three pillars | `AddMvp24HoursObservability` | [Unified options](#unified-options) |
| Logging context | `AddMvp24HoursLogging` | [Logging](logging.md) |
| Tracing context | `AddMvp24HoursTracing` | [Tracing](tracing.md) |
| Metric instruments | `AddMvp24HoursMetrics` | [Metrics](metrics.md) |
| Exporter configuration model | `AddMvp24HoursOpenTelemetry` | [Exporters](exporters.md) |
| Legacy migration | — | [Migration](migration.md) |
| Health probes | Module-owned checks | [Health Checks Catalog](../infrastructure/health-checks.md) |
| Resilience selection | Area wrappers | [Resilience Selection Guide](../modernization/resilience-guide.md) |
| Compatibility logging overview | Root overview | [Logging Overview](../logging.md) |

```csharp
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "Orders";
    options.ServiceVersion = "1.0.0";
    options.Environment = builder.Environment.EnvironmentName;
});

builder.Logging.AddMvp24HoursDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

## Unified options

### `ObservabilityOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string?` | `null` | Resource service name. |
| `ServiceVersion` | `string?` | `null` | Resource service version. |
| `Environment` | `string?` | `null` | Deployment environment. |
| `EnableLogging` | `bool` | `true` | Registers logging services. |
| `EnableTracing` | `bool` | `true` | Registers tracing services. |
| `EnableMetrics` | `bool` | `true` | Registers metric classes. |
| `Logging` | `ObservabilityLoggingOptions` | new instance | Nested logging switches. |
| `Tracing` | `ObservabilityTracingOptions` | new instance | Nested tracing switches. |
| `Metrics` | `ObservabilityMetricsOptions` | new instance | Nested module metric switches. |
| `ResourceAttributes` | `Dictionary<string,object>` | empty | Reserved unified resource attributes. |

The overload accepting `IConfiguration` binds `Mvp24Hours:Observability`.

## Sources and meters

`Mvp24HoursActivitySources.AllSourceNames` and `Mvp24HoursMeters.AllMeterNames` cover Core, Pipe, CQRS, Data, RabbitMQ, WebAPI, Caching, CronJob and Infrastructure. Prefer the builder helper methods shown above over duplicating the list.

## Aspire

.NET Aspire service defaults normally configure OpenTelemetry and provide the OTLP destination. Register Mvp24Hours sources/meters in that pipeline; Aspire then displays the emitted telemetry. `AddMvp24HoursOpenTelemetry` stores exporter choices but does not replace the OpenTelemetry SDK or Aspire service defaults.

See [.NET Aspire integration](../modernization/aspire.md).

## Deprecation

`TelemetryHelper` and `ITelemetryService` are deprecated. Use `ILogger<T>`, `ActivitySource`, `Meter`, and the APIs above. Follow [Migration from legacy telemetry](migration.md).
