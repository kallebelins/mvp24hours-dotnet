# OpenTelemetry tracing

Mvp24Hours exposes module `ActivitySource` instances, semantic tags, propagation helpers and DI enrichers.

## Register

```csharp
builder.Services.AddMvp24HoursTracing(options =>
{
    options.ServiceName = "Orders";
    options.ServiceVersion = "1.0.0";
    options.EnableCorrelationIdPropagation = true;
    options.AddEnricher<OrdersActivityEnricher>();
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

## `TracingOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `EnableCorrelationIdPropagation` | `bool` | `true` | Declares correlation propagation behavior. |
| `EnableUserContext` | `bool` | `true` | Declares user enrichment behavior. |
| `EnableTenantContext` | `bool` | `true` | Declares tenant enrichment behavior. |
| `ServiceName` | `string?` | `null` | Trace service identity stored in options. |
| `ServiceVersion` | `string?` | `null` | Trace service version stored in options. |

`AddEnricher<T>()` and `AddEnricher(instance)` populate registrations. `AddMvp24HoursDefaultEnrichers()` registers correlation, user and tenant enrichers directly.

## Sources

| Module | Source name |
|---|---|
| Core | `Mvp24Hours.Core` |
| Pipe | `Mvp24Hours.Pipe` |
| CQRS | `Mvp24Hours.Cqrs` |
| Data | `Mvp24Hours.Data` |
| RabbitMQ | `Mvp24Hours.RabbitMQ` |
| WebAPI | `Mvp24Hours.WebAPI` |
| Caching | `Mvp24Hours.Caching` |
| CronJob | `Mvp24Hours.CronJob` |
| Infrastructure | `Mvp24Hours.Infrastructure` |

## Create and enrich activities

```csharp
using var activity = ActivityHelper.StartCommandActivity("CreateOrder");
try
{
    activity?.WithCorrelationId(correlationId);
    await handler(cancellationToken);
    activity?.SetSuccess();
}
catch (Exception exception)
{
    activity?.SetError(exception);
    throw;
}
```

Use `SemanticTags`, `ActivityExtensions`, and `TracePropagation` rather than inventing incompatible tag/header names. `ITraceContextAccessor` exposes the current `Activity`, trace/span IDs and baggage.

ASP.NET Core and `HttpClient` instrumentation already propagate W3C trace context; use manual `TracePropagation` only at boundaries not covered by instrumentation.

## Aspire

With Aspire service defaults, add `GetMvp24HoursActivitySourceNames()` to the existing tracing pipeline and use the environment-provided OTLP exporter. See [Aspire](../modernization/aspire.md).

Related: [home](home.md), [logging](logging.md), [metrics](metrics.md), [exporters](exporters.md).
