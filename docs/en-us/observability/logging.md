# Observability logging

Mvp24Hours augments `Microsoft.Extensions.Logging`; it does not replace `ILogger<T>`.

## Register

```csharp
builder.Services.AddMvp24HoursLogging(options =>
{
    options.ServiceName = "Orders";
    options.ServiceVersion = "1.0.0";
    options.Environment = builder.Environment.EnvironmentName;
    options.EnableTraceCorrelation = true;
});

builder.Logging
    .AddMvp24HoursDefaults()
    .AddNamespaceFiltering(builder.Configuration);
```

`AddMvp24HoursDefaults` configures activity tracking (`TraceId`, `SpanId`, parent, baggage and tags). `AddNamespaceFiltering` reads `Logging:LogLevel`.

## `LoggingOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string?` | `null` | Service identity stored in options. |
| `ServiceVersion` | `string?` | `null` | Service version stored in options. |
| `EnableTraceCorrelation` | `bool` | `true` | Enables activity tracking/scopes. |
| `EnableLogSampling` | `bool` | `false` | Registers `RatioBasedLogSampler`. |
| `SamplingRatio` | `double` | `1.0` | Non-error sampling ratio, clamped to 0–1. |
| `EnableUserContextEnrichment` | `bool` | `true` | Registers user baggage enricher. |
| `EnableTenantContextEnrichment` | `bool` | `true` | Registers tenant baggage enricher. |
| `Environment` | `string?` | `null` | Deployment environment stored in options. |
| `ResourceAttributes` | `Dictionary<string,object>` | empty | Custom resource metadata model. |

The configuration overload binds `Mvp24Hours:Logging`.

## Use structured logging

```csharp
using (logger.BeginOperationScope("ProcessOrder", order.Id.ToString()))
using (logger.BeginTraceScope())
{
    logger.LogInformation(
        "Processing order {OrderId} for customer {CustomerId}",
        order.Id,
        order.CustomerId);
}
```

`ILogContextAccessor` exposes the current trace, span, correlation ID and a trace scope. `ILogEnricher` can add custom scope properties. Registered samplers are services for consumers; registering one does not install an `ILoggerProvider` that globally drops records.

## OpenTelemetry log export

Configure the OpenTelemetry logging provider explicitly:

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.ParseStateValues = true;
    logging.AddOtlpExporter();
});
```

The library also exposes `AddMvp24HoursOpenTelemetryLogging(...)` and OpenTelemetry logging option helpers. Exporter packages and endpoints remain application dependencies.

## Production guidance

- Use message templates, not interpolated strings.
- Keep request/response body logging disabled unless a specific diagnostic need justifies it.
- Never emit secrets or unbounded/high-cardinality values.
- Configure OpenTelemetry export once, usually in Aspire service defaults or a shared host extension.

Related: [home](home.md), [tracing](tracing.md), [exporters](exporters.md), [migration](migration.md).
