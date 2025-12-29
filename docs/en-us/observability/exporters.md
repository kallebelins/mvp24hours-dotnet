# OpenTelemetry Exporters Configuration

This guide demonstrates how to configure OpenTelemetry exporters using the Mvp24Hours unified configuration API.

## Overview

Mvp24Hours provides a simplified configuration API for OpenTelemetry exporters through the `AddMvp24HoursOpenTelemetry()` extension method. This approach centralizes the configuration of OTLP, Console, and Prometheus exporters with sensible defaults for different environments.

### Supported Exporters

| Exporter | Purpose | Recommended For |
|----------|---------|-----------------|
| **OTLP** | Export to OTLP-compatible backends | Jaeger, Tempo, Grafana, Azure Monitor, Datadog |
| **Console** | Export to console/stdout | Development and debugging |
| **Prometheus** | Expose metrics in Prometheus format | Metrics scraping by Prometheus |

## Quick Start

### Development Environment

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Configure exporters with development defaults
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "MyService";
    opts.ServiceVersion = "1.0.0";
    opts.Environment = "Development";
    
    // Console exporter for immediate feedback
    opts.Console.Enabled = true;
    opts.Console.EnableTracing = true;
    opts.Console.EnableMetrics = false; // Avoid excessive output
    
    // OTLP to local Jaeger
    opts.Otlp.Endpoint = "http://localhost:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
});

// Integrate with OpenTelemetry SDK
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MyService", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// Expose Prometheus metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

### Production Environment

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure exporters with production defaults
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "OrderService";
    opts.ServiceVersion = "2.1.0";
    opts.Environment = "Production";
    
    // Disable console in production
    opts.Console.Enabled = false;
    
    // OTLP to Grafana Tempo
    opts.Otlp.Endpoint = builder.Configuration["Observability:OtlpEndpoint"] 
                         ?? "http://tempo:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
    opts.Otlp.BatchExportScheduledDelayMs = 5000;
    opts.Otlp.MaxExportBatchSize = 512;
    
    // Prometheus metrics
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.ScrapeResponseCacheDurationMs = 5000;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService("OrderService", serviceVersion: "2.1.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "Production",
            ["service.namespace"] = "ECommerce"
        }))
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
        .AddOtlpExporter());

// Configure logging with OTLP
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter();
});

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();
```

## OTLP Exporter Configuration

The OpenTelemetry Protocol (OTLP) exporter is the native and recommended way to export telemetry data.

### Basic Configuration

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "MyService";
    
    opts.Otlp.Enabled = true;
    opts.Otlp.Endpoint = "http://jaeger:4317"; // gRPC endpoint
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
});
```

### Common Backend Endpoints

#### Jaeger (All-in-One)
```csharp
opts.Otlp.Endpoint = "http://localhost:4317";  // gRPC
// or
opts.Otlp.Endpoint = "http://localhost:4318";  // HTTP
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
```

#### Grafana Tempo
```csharp
opts.Otlp.Endpoint = "http://tempo:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
```

#### Azure Monitor
```csharp
opts.Otlp.Endpoint = "https://<ingestion-endpoint>.azure.monitor.com/v1/traces";
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
opts.Otlp.Headers["Authorization"] = $"Bearer {apiKey}";
```

#### Datadog
```csharp
opts.Otlp.Endpoint = "https://api.datadoghq.com/api/v2/otlp";
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
opts.Otlp.Headers["DD-API-KEY"] = apiKey;
```

#### AWS X-Ray (via OTEL Collector)
```csharp
opts.Otlp.Endpoint = "http://otel-collector:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
```

### Advanced OTLP Configuration

```csharp
opts.Otlp.Enabled = true;
opts.Otlp.Endpoint = "http://tempo:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;

// Control what to export
opts.Otlp.EnableTracing = true;
opts.Otlp.EnableMetrics = true;
opts.Otlp.EnableLogging = true;

// Performance tuning
opts.Otlp.TimeoutMs = 10000;                      // 10 seconds timeout
opts.Otlp.BatchExportScheduledDelayMs = 5000;     // Export every 5 seconds
opts.Otlp.MaxExportBatchSize = 512;               // Max 512 items per batch
opts.Otlp.MaxQueueSize = 2048;                    // Queue up to 2048 items

// Authentication headers
opts.Otlp.Headers["Authorization"] = "Bearer token...";
opts.Otlp.Headers["X-Custom-Header"] = "value";
```

## Console Exporter Configuration

The Console exporter is useful for development and debugging but should **not** be enabled in production.

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.Console.Enabled = builder.Environment.IsDevelopment(); // Only in dev
    
    opts.Console.EnableTracing = true;    // Show traces
    opts.Console.EnableMetrics = false;   // Metrics can be verbose
    opts.Console.EnableLogging = true;    // Show logs
    
    opts.Console.EnableTimestamps = true; // Include timestamps
    opts.Console.UseColors = true;        // Colored output
});
```

### Console Output Example

```
Activity.TraceId:            4bf92f3577b34da6a3ce929d0e0e4736
Activity.SpanId:             00f067aa0ba902b7
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Mvp24Hours.Core
Activity.DisplayName:        GET /api/orders
Activity.Kind:               Server
Activity.StartTime:          2024-12-28T10:30:45.1234567Z
Activity.Duration:           00:00:00.0234567
Activity.Tags:
    http.method: GET
    http.url: https://api.example.com/api/orders
    http.status_code: 200
    service.name: OrderService
```

## Prometheus Exporter Configuration

The Prometheus exporter exposes metrics via an HTTP endpoint that Prometheus can scrape.

### Basic Configuration

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.ScrapeResponseCacheDurationMs = 5000; // Cache for 5 seconds
    opts.Prometheus.EnableExemplars = true; // Link metrics to traces
});
```

### Enable Scraping Endpoint

```csharp
var app = builder.Build();

// Add Prometheus scraping endpoint middleware
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

### Prometheus Configuration (prometheus.yml)

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'myservice'
    scrape_interval: 15s
    static_configs:
      - targets: ['myservice:8080']
    metrics_path: '/metrics'
```

### Secure Metrics Endpoint

```csharp
// Require authentication for metrics
opts.Prometheus.RequireAuthentication = true;

// In middleware configuration
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/metrics", async (HttpContext context) =>
{
    // Check if user is authenticated
    if (!context.User.Identity?.IsAuthenticated ?? false)
    {
        return Results.Unauthorized();
    }
    
    // Serve metrics
    await context.UseOpenTelemetryPrometheusScrapingEndpoint();
    return Results.Ok();
}).RequireAuthorization();
```

## Using Configuration Presets

### Development Preset

```csharp
var devOptions = OpenTelemetryExporterExtensions
    .GetDevelopmentDefaults("MyService");

services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = devOptions.ServiceName;
    opts.Console = devOptions.Console;
    opts.Otlp = devOptions.Otlp;
    opts.Prometheus = devOptions.Prometheus;
});
```

### Production Preset

```csharp
var prodOptions = OpenTelemetryExporterExtensions
    .GetProductionDefaults(
        serviceName: "OrderService",
        otlpEndpoint: "http://tempo:4317");

services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = prodOptions.ServiceName;
    opts.Console = prodOptions.Console;
    opts.Otlp = prodOptions.Otlp;
    opts.Prometheus = prodOptions.Prometheus;
});
```

## Configuration from appsettings.json

```json
{
  "Mvp24Hours": {
    "Observability": {
      "ServiceName": "OrderService",
      "ServiceVersion": "2.1.0",
      "Environment": "Production",
      "Otlp": {
        "Enabled": true,
        "Endpoint": "http://tempo:4317",
        "Protocol": "Grpc",
        "EnableTracing": true,
        "EnableMetrics": true,
        "EnableLogging": true,
        "BatchExportScheduledDelayMs": 5000,
        "MaxExportBatchSize": 512
      },
      "Console": {
        "Enabled": false
      },
      "Prometheus": {
        "Enabled": true,
        "ScrapeEndpoint": "/metrics",
        "ScrapeResponseCacheDurationMs": 5000
      }
    }
  }
}
```

```csharp
// Load configuration from appsettings.json
var config = builder.Configuration;

services.AddMvp24HoursOpenTelemetry(opts =>
{
    config.GetSection("Mvp24Hours:Observability").Bind(opts);
});
```

## Complete Example with All Features

```csharp
using Mvp24Hours.Core.Observability;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Configure Mvp24Hours observability
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "OrderService";
    opts.ServiceVersion = "2.1.0";
    opts.Environment = builder.Environment.EnvironmentName;
    opts.ServiceNamespace = "ECommerce";
    opts.ServiceInstanceId = Environment.MachineName;
    
    // OTLP configuration
    opts.Otlp.Enabled = true;
    opts.Otlp.Endpoint = builder.Configuration["Observability:OtlpEndpoint"] 
                         ?? "http://localhost:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
    opts.Otlp.EnableTracing = true;
    opts.Otlp.EnableMetrics = true;
    opts.Otlp.EnableLogging = true;
    opts.Otlp.BatchExportScheduledDelayMs = 5000;
    opts.Otlp.MaxExportBatchSize = 512;
    
    // Console for development only
    opts.Console.Enabled = builder.Environment.IsDevelopment();
    opts.Console.EnableTracing = true;
    opts.Console.EnableMetrics = false;
    
    // Prometheus metrics
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.EnableExemplars = true;
    
    // Custom resource attributes
    opts.ResourceAttributes["deployment.environment"] = builder.Environment.EnvironmentName;
    opts.ResourceAttributes["service.datacenter"] = "us-east-1";
});

// Validate configuration (throws if invalid)
var exporterOpts = builder.Services.BuildServiceProvider()
    .GetRequiredService<OpenTelemetryExporterOptions>();
exporterOpts.Validate();

// Configure OpenTelemetry SDK
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        var attrs = exporterOpts.GetResourceAttributes();
        resource.AddAttributes(attrs);
    })
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;
        })
        .AddConsoleExporter() // Only if Console.Enabled
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(exporterOpts.Otlp.Endpoint);
            opts.Protocol = exporterOpts.Otlp.Protocol == OtlpExportProtocol.Grpc
                ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
        .AddOtlpExporter());

// Configure logging with OpenTelemetry
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.ParseStateValues = true;
    
    if (exporterOpts.Otlp.EnableLogging)
    {
        logging.AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(exporterOpts.Otlp.Endpoint);
        });
    }
    
    if (exporterOpts.Console.EnableLogging)
    {
        logging.AddConsoleExporter();
    }
});

var app = builder.Build();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Prometheus metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

## Troubleshooting

### OTLP Connection Issues

```csharp
// Enable detailed logging for debugging
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);

// Increase timeout
opts.Otlp.TimeoutMs = 30000; // 30 seconds

// Check connectivity
curl -v http://localhost:4317
```

### High Memory Usage

```csharp
// Reduce batch sizes
opts.Otlp.MaxExportBatchSize = 256;
opts.Otlp.MaxQueueSize = 1024;

// Increase export frequency
opts.Otlp.BatchExportScheduledDelayMs = 2000; // Export every 2 seconds
```

### Missing Metrics in Prometheus

```csharp
// Verify scrape endpoint is accessible
curl http://localhost:8080/metrics

// Check Prometheus logs for scrape errors
docker logs prometheus

// Ensure metrics are being collected
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("*") // Collect all meters for debugging
        .AddPrometheusExporter());
```

## Best Practices

1. **Use Environment-Specific Configuration**: Enable Console exporter only in Development
2. **Batch Export Settings**: Tune batch size and delay based on your traffic patterns
3. **Authentication**: Secure OTLP endpoints with proper authentication headers
4. **Resource Attributes**: Add deployment environment, datacenter, and instance ID for better filtering
5. **Validation**: Always call `Validate()` on options in production to catch configuration errors early
6. **Sampling**: Consider trace sampling for high-traffic services to reduce costs

## See Also

- [Logging Guide](logging.md)
- [Tracing Guide](tracing.md)
- [Metrics Guide](metrics.md)
- [Migration from TelemetryHelper](migration.md)

