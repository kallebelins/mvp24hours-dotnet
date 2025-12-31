# .NET Aspire 9 Integration

## Overview

.NET Aspire 9 is Microsoft's official stack for building observable, cloud-native applications. It provides a unified approach to telemetry, health checks, resilience, and service discovery, making it easier to develop and operate distributed systems.

Mvp24Hours provides seamless integration with .NET Aspire, allowing you to leverage its powerful features while using the Mvp24Hours framework.

## Key Features

### Observability
- **OpenTelemetry Integration**: Built-in support for logs, traces, and metrics
- **Developer Dashboard**: Real-time visualization of telemetry data
- **Browser Telemetry**: Support for SPAs to send telemetry directly to the dashboard

### Orchestration
- **AppHost Pattern**: Simplified local development with container orchestration
- **Service Discovery**: Automatic connection string injection
- **Health Checks**: Liveness and readiness probes out of the box

### Resilience
- **Retry Policies**: Automatic retry with exponential backoff
- **Circuit Breaker**: Protection against cascading failures
- **Timeout Policies**: Configurable timeouts for all operations

## Installation

Add the required NuGet packages to your project:

```bash
# For the API/Service project
dotnet add package Aspire.Hosting.AppHost --version 9.*

# For component integrations
dotnet add package Aspire.StackExchange.Redis
dotnet add package Aspire.RabbitMQ.Client
dotnet add package Aspire.Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Aspire.MongoDB.Driver
```

## Quick Start

### 1. Create an AppHost Project

The AppHost project orchestrates your distributed application:

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure components
var redis = builder.AddRedis("cache")
    .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("appdb");

var mongo = builder.AddMongoDB("mongo")
    .AddDatabase("documents");

// Add your API project with references
builder.AddProject<Projects.MyApi>("api")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(sql)
    .WithReference(mongo)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### 2. Configure Your API Project

Use Mvp24Hours Aspire extensions in your API:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults with Mvp24Hours integration
builder.AddMvp24HoursAspireDefaults(options =>
{
    options.ServiceName = "MyApi";
    options.EnableOpenTelemetry = true;
    options.EnableHealthChecks = true;
    options.EnableResilience = true;
    
    // Configure telemetry
    options.Telemetry.EnableTracing = true;
    options.Telemetry.EnableMetrics = true;
    options.Telemetry.EnableMvp24HoursInstrumentation = true;
});

// Add Mvp24Hours components using Aspire connections
builder.Services.AddMvp24HoursRedisFromAspire("cache");
builder.Services.AddMvp24HoursRabbitMQFromAspire("messaging");
builder.Services.AddMvp24HoursSqlServerFromAspire("appdb");
builder.Services.AddMvp24HoursMongoDbFromAspire("documents");

// Add your services
builder.Services.AddMvp24HoursDbContext<MyDbContext>();
builder.Services.AddMvpRabbitMQ();

var app = builder.Build();

// Map Aspire health check endpoints
app.MapMvp24HoursAspireHealthChecks();

app.Run();
```

## Configuration Options

### AspireOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | string | Assembly name | Service name for telemetry |
| `ServiceVersion` | string | Assembly version | Service version for telemetry |
| `Environment` | string | Host environment | Deployment environment |
| `EnableOpenTelemetry` | bool | true | Enable OpenTelemetry integration |
| `EnableHealthChecks` | bool | true | Enable health checks |
| `EnableResilience` | bool | true | Enable resilience policies |
| `EnableServiceDiscovery` | bool | true | Enable service discovery |
| `OtlpEndpoint` | string | null | OTLP exporter endpoint |

### Telemetry Options

```csharp
options.Telemetry.EnableLogging = true;
options.Telemetry.EnableTracing = true;
options.Telemetry.EnableMetrics = true;
options.Telemetry.EnableAspNetCoreInstrumentation = true;
options.Telemetry.EnableHttpClientInstrumentation = true;
options.Telemetry.EnableEfCoreInstrumentation = true;
options.Telemetry.EnableMvp24HoursInstrumentation = true;
options.Telemetry.TraceSamplingRatio = 1.0;
```

### Health Check Options

```csharp
options.HealthChecks.LivenessPath = "/health/live";
options.HealthChecks.ReadinessPath = "/health/ready";
options.HealthChecks.StartupPath = "/health/startup";
options.HealthChecks.EnableDatabaseHealthChecks = true;
options.HealthChecks.EnableCacheHealthChecks = true;
options.HealthChecks.EnableMessagingHealthChecks = true;
```

### Resilience Options

```csharp
options.Resilience.EnableRetry = true;
options.Resilience.EnableCircuitBreaker = true;
options.Resilience.EnableTimeout = true;
options.Resilience.MaxRetryAttempts = 3;
options.Resilience.CircuitBreakerFailureThreshold = 5;
options.Resilience.CircuitBreakerBreakDurationSeconds = 30;
options.Resilience.TimeoutSeconds = 30;
```

## Component Integration

### Redis

```csharp
// In AppHost
var redis = builder.AddRedis("cache");

// In API
builder.Services.AddMvp24HoursRedisFromAspire("cache");

// Use with Mvp24Hours caching
builder.Services.AddMvpHybridCache();
```

### RabbitMQ

```csharp
// In AppHost
var rabbitmq = builder.AddRabbitMQ("messaging");

// In API
builder.Services.AddMvp24HoursRabbitMQFromAspire("messaging", options =>
{
    options.AutoDeclareQueues = true;
    options.EnableMessageDeduplication = true;
    options.PrefetchCount = 10;
});

// Use with Mvp24Hours messaging
builder.Services.AddMvpRabbitMQ(cfg =>
{
    cfg.ConfigureEndpoints(context);
});
```

### SQL Server

```csharp
// In AppHost
var sql = builder.AddSqlServer("sql").AddDatabase("mydb");

// In API
builder.Services.AddMvp24HoursSqlServerFromAspire("mydb");

// Use with Mvp24Hours EFCore
builder.Services.AddMvp24HoursDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(builder.GetAspireConnectionString("mydb"));
});
```

### MongoDB

```csharp
// In AppHost
var mongo = builder.AddMongoDB("mongo").AddDatabase("documents");

// In API
builder.Services.AddMvp24HoursMongoDbFromAspire("documents");

// Use with Mvp24Hours MongoDB
builder.Services.AddMvp24HoursMongoDb<MyContext>();
```

## Health Check Endpoints

The integration provides standard Kubernetes-compatible health check endpoints:

| Endpoint | Purpose | Tags |
|----------|---------|------|
| `/health/live` | Liveness probe | live |
| `/health/ready` | Readiness probe | ready |
| `/health/startup` | Startup probe | startup, live |
| `/health` | Overall health | all |

### Health Check Response

```json
{
  "status": "Healthy",
  "duration": 45.23,
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": 0.12,
      "tags": ["live"]
    },
    {
      "name": "redis-cache",
      "status": "Healthy",
      "duration": 15.45,
      "tags": ["ready"]
    },
    {
      "name": "sqlserver-mydb",
      "status": "Healthy",
      "duration": 28.67,
      "tags": ["ready"]
    }
  ]
}
```

## Developer Dashboard

The Aspire Developer Dashboard provides real-time observability:

### Features
- **Structured Logs**: Filter and search logs with trace correlation
- **Distributed Traces**: Visualize request flows across services
- **Metrics**: View graphs and dashboards for key metrics
- **Resources**: Monitor health and status of all components

### Accessing the Dashboard

When running with the AppHost, the dashboard URL is displayed in the console:

```
Dashboard: https://localhost:18888
```

## Comparison: Aspire vs Manual Configuration

| Feature | Aspire | Manual |
|---------|--------|--------|
| OpenTelemetry Setup | Automatic | Manual configuration required |
| Health Checks | Built-in endpoints | Manual mapping |
| Connection Strings | Injected automatically | Manual configuration |
| Service Discovery | Built-in | External library required |
| Container Orchestration | Integrated | Docker Compose/K8s |
| Developer Dashboard | Included | External tools (Jaeger, Grafana) |

### When to Use Aspire

- New cloud-native applications
- Microservices architectures
- Applications requiring comprehensive observability
- Teams wanting simplified local development

### When to Use Manual Configuration

- Existing applications with custom observability
- Simple applications without distribution
- Environments without container support
- Specific compliance requirements

## Best Practices

### 1. Service Naming

Use consistent, descriptive service names:

```csharp
options.ServiceName = "order-api";
options.ServiceVersion = "1.2.3";
```

### 2. Health Check Tags

Use appropriate tags for probes:

```csharp
services.AddHealthChecks()
    .AddCheck("db", () => ..., tags: new[] { "ready" })
    .AddCheck("cache", () => ..., tags: new[] { "ready" })
    .AddCheck("self", () => ..., tags: new[] { "live" });
```

### 3. Resilience Configuration

Configure resilience based on your SLOs:

```csharp
options.Resilience.MaxRetryAttempts = 3;
options.Resilience.TimeoutSeconds = 30;
options.Resilience.CircuitBreakerFailureThreshold = 10;
```

### 4. Telemetry Sampling

Adjust sampling for production environments:

```csharp
options.Telemetry.TraceSamplingRatio = 0.1; // 10% sampling in production
```

## Troubleshooting

### Connection String Not Found

Ensure the connection name matches in AppHost and API:

```csharp
// AppHost
var redis = builder.AddRedis("cache"); // name: "cache"

// API
builder.Services.AddMvp24HoursRedisFromAspire("cache"); // must match
```

### Health Checks Failing

Check that services are properly referenced:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(redis)  // Add reference
    .WithReference(sql);   // Add reference
```

### Telemetry Not Appearing

Verify OTLP endpoint is configured:

```csharp
options.OtlpEndpoint = "http://localhost:4317"; // Or use OTEL_EXPORTER_OTLP_ENDPOINT env var
```

## See Also

- [Observability Overview](../observability/home.md)
- [OpenTelemetry Configuration](../observability/exporters.md)
- [Health Checks](../webapi/health-checks.md)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)

