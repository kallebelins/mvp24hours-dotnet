# Observability

> Modern observability for the Mvp24Hours framework using ILogger + OpenTelemetry.

## Overview

Mvp24Hours provides a comprehensive observability solution built on modern .NET standards:

- **Logging**: Structured logging with `ILogger<T>` (Microsoft.Extensions.Logging)
- **Tracing**: Distributed tracing with OpenTelemetry and `Activity` API
- **Metrics**: Performance metrics with OpenTelemetry `Meter` and counters

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Mvp24Hours Observability                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                 │
│   │   Logging   │    │   Tracing   │    │   Metrics   │                 │
│   │  (ILogger)  │    │ (Activity)  │    │   (Meter)   │                 │
│   └──────┬──────┘    └──────┬──────┘    └──────┬──────┘                 │
│          │                  │                  │                        │
│          ▼                  ▼                  ▼                        │
│   ┌─────────────────────────────────────────────────────────────┐       │
│   │              OpenTelemetry Collector / OTLP                  │       │
│   └─────────────────────────────────────────────────────────────┘       │
│          │                  │                  │                        │
│          ▼                  ▼                  ▼                        │
│   ┌───────────┐      ┌───────────┐      ┌───────────┐                   │
│   │  Console  │      │   Jaeger  │      │Prometheus │                   │
│   │  Serilog  │      │   Zipkin  │      │  Grafana  │                   │
│   │   Seq     │      │   Tempo   │      │           │                   │
│   └───────────┘      └───────────┘      └───────────┘                   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Install Package

```bash
dotnet add package Mvp24Hours.Core
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
```

### 2. Configure Observability

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Option 1: All-in-one configuration
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.EnableLogging = true;
});

// Option 2: Configure each pillar separately
builder.Services.AddMvp24HoursLogging(options =>
{
    options.EnableTraceCorrelation = true;
});

builder.Services.AddMvp24HoursTracing(options =>
{
    options.ServiceName = "MyService";
});

builder.Services.AddMvp24HoursMetrics(options =>
{
    options.ServiceName = "MyService";
});
```

### 3. Use with OpenTelemetry

```csharp
builder.Services.AddMvp24HoursOpenTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.OtlpEndpoint = "http://localhost:4317"; // Jaeger/Tempo/Collector
    
    // Include Mvp24Hours activity sources
    options.AddMvp24HoursActivitySources = true;
    options.AddMvp24HoursMeters = true;
});
```

## Activity Sources

Mvp24Hours provides dedicated `ActivitySource` for each module:

| Module | ActivitySource Name | Description |
|--------|---------------------|-------------|
| Core | `Mvp24Hours.Core` | Core operations |
| Pipeline | `Mvp24Hours.Pipe` | Pipeline execution |
| CQRS | `Mvp24Hours.Cqrs` | Commands, Queries, Notifications |
| EF Core | `Mvp24Hours.EFCore` | Database operations |
| RabbitMQ | `Mvp24Hours.RabbitMQ` | Messaging operations |
| Caching | `Mvp24Hours.Caching` | Cache operations |
| CronJob | `Mvp24Hours.CronJob` | Scheduled jobs |
| HTTP | `Mvp24Hours.Infrastructure.Http` | HTTP client calls |

### Configure ActivitySources in OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddSource(Mvp24HoursActivitySources.Core.SourceName)
            .AddSource(Mvp24HoursActivitySources.Pipe.SourceName)
            .AddSource(Mvp24HoursActivitySources.Cqrs.SourceName)
            .AddSource(Mvp24HoursActivitySources.Data.SourceName)
            .AddSource(Mvp24HoursActivitySources.RabbitMQ.SourceName)
            .AddSource(Mvp24HoursActivitySources.Caching.SourceName)
            .AddSource(CronJobActivitySource.SourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });
```

## Correlation and Context

Mvp24Hours automatically propagates context across:

- HTTP requests (via W3C Trace Context headers)
- RabbitMQ messages (via baggage)
- Pipeline operations
- CQRS handlers

### Correlation ID Propagation

```csharp
// Extract from current context
var correlationId = CorrelationIdPropagation.GetCorrelationId();

// Set in current context
CorrelationIdPropagation.SetCorrelationId(correlationId);

// Use with ILogger scope
using (logger.BeginTraceScope())
{
    logger.LogInformation("Operation completed");
}
```

## Documentation

| Topic | Description |
|-------|-------------|
| [Logging](logging.md) | Structured logging with ILogger |
| [Tracing](tracing.md) | Distributed tracing with OpenTelemetry |
| [Metrics](metrics.md) | Performance metrics |
| [Exporters](exporters.md) | Configure OTLP, Console, Prometheus |
| [Migration](migration.md) | Migrate from TelemetryHelper |

## Deprecation Notice

> ⚠️ **DEPRECATED**: The legacy `TelemetryHelper` and `ITelemetryService` have been deprecated.
> Please use `ILogger<T>` and OpenTelemetry instead.
> See [Migration Guide](migration.md) for details.

## Best Practices

1. **Use ILogger<T>**: Inject `ILogger<T>` for all logging needs
2. **Enable Correlation**: Use `BeginTraceScope()` for automatic correlation
3. **Add Semantic Tags**: Use `SemanticTags` constants for consistent tagging
4. **Configure Sampling**: Use sampling for high-volume production environments
5. **Export to OTLP**: Use OTLP for maximum compatibility with backends

## See Also

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)

