# simple-observability-customer-api

Demonstrates how to wire OpenTelemetry logs, traces, and metrics end-to-end in a **Simple Minimal API** using Mvp24Hours observability helpers and the OpenTelemetry .NET SDK. A trivial in-memory Customer API generates traffic to observe.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- OpenTelemetry tracing, metrics, and logs (three pillars)
- Mvp24Hours activity sources (`Mvp24HoursActivitySources.AllSourceNames`) and meters (`Mvp24HoursMeters.AllMeterNames`) registered in the SDK
- Sample-specific `ActivitySource` and `Meter` to show the pattern for application code
- OTLP exporter (gRPC, `http://localhost:4317` by default) + Console exporter in Development
- ASP.NET Core, HTTP client, and .NET runtime instrumentation
- Native OpenAPI, ProblemDetails (RFC 7807), and health checks — no Swashbuckle
- In-memory Customer CRUD (no database required)

## Architecture

- Tier: `Simple`
- Shape: Single-project Minimal API
- Why this shape fits: observability wiring is the only concern; extra layers would obscure the OpenTelemetry integration

## Layers

- `CustomerAPI.WebAPI` — single host project; `Program.cs` wires all OTel SDK registrations, Mvp24Hours helpers, and Customer endpoints

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Optional** — one of the following OTLP backends to view signals (the app starts without any backend; signals are dropped if none is available):
  - [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) (easiest local option)
  - [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) → Jaeger / Grafana Tempo
  - [Jaeger all-in-one](https://www.jaegertracing.io/docs/latest/getting-started/) (OTLP gRPC on port 4317)

## Configuration

No secrets are required. Configure telemetry endpoints via environment variables or `appsettings.json`.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `OpenTelemetry:OtlpEndpoint` | No | OTLP gRPC endpoint for all signals | `http://localhost:4317` |

## Run

From this sample's solution directory:

```bash
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### View signals locally

**Option A — .NET Aspire Dashboard (recommended for development)**

```bash
docker run --rm -it -d \
  -p 18888:18888 -p 4317:18889 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Open `http://localhost:18888` — structured logs, traces, and metrics appear as soon as you call any endpoint.

**Option B — Jaeger all-in-one**

```bash
docker run --rm -d --name jaeger \
  -p 4317:4317 \
  -p 16686:16686 \
  jaegertracing/all-in-one:latest
```

Open `http://localhost:16686` for the Jaeger UI.

**Option C — Console exporter only (no backend)**

The Console exporter is enabled automatically in the `Development` environment. Trace and log lines are written to stdout.

## Explore the API

- OpenAPI document: `https://localhost:5001/openapi/v1.json`
- Health: `https://localhost:5001/health`
- Endpoints: `GET /api/customers`, `GET /api/customers/{id}`, `POST /api/customers`, `PUT /api/customers/{id}`, `DELETE /api/customers/{id}`

## Key code patterns

### 1 — Store Mvp24Hours options, then wire the SDK explicitly

```csharp
// Only stores option models — does NOT wire the SDK:
builder.Services.AddMvp24HoursOpenTelemetry(opts => { ... });

// You MUST call AddOpenTelemetry() yourself:
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t => t
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddSource(CustomerActivitySource.Name)   // application source
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddMeter(CustomerMeter.Name)             // application meter
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
```

### 2 — Custom activity source in an endpoint

```csharp
using var activity = CustomerActivitySource.Source.StartActivity("CreateCustomer");
activity?.SetTag("customer.id", created.Id);
```

## Related documentation

- [Getting started](../../../docs/en-us/getting-started.md)
- [Observability home](../../../docs/en-us/observability/home.md)
- [Tracing](../../../docs/en-us/observability/tracing.md)
- [Metrics](../../../docs/en-us/observability/metrics.md)
- [Logging](../../../docs/en-us/observability/logging.md)
- [Exporters](../../../docs/en-us/observability/exporters.md)
- [Migration guide](../../../docs/en-us/observability/migration.md)
- [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

## What this sample intentionally does not cover

- Persistent database (in-memory store only — no EF Core or Redis)
- Prometheus scrape endpoint (OTLP is the focus; add `OpenTelemetry.Exporter.Prometheus.AspNetCore` if needed)
- Sampling strategies (always-on sampler used for simplicity)
- Production OTLP authentication headers or mutual TLS
- Multi-service / distributed tracing across process boundaries
