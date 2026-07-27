# Logging

The canonical observability entry point is [Observability](observability/home.md). For logging configuration, option defaults, trace-correlated scopes and OpenTelemetry log export, use [Observability: logging](observability/logging.md).

Mvp24Hours uses the standard .NET `ILogger<T>` abstraction:

```csharp
builder.Services.AddMvp24HoursLogging();
builder.Logging.AddMvp24HoursDefaults();
```

Use message templates rather than interpolation, and never place credentials, tokens or personal data in logs.

Legacy `TelemetryHelper` and `ITelemetryService` are deprecated. Follow the [observability migration guide](observability/migration.md).

Related: [tracing](observability/tracing.md), [metrics](observability/metrics.md), [exporters](observability/exporters.md), and [.NET Aspire](modernization/aspire.md).
