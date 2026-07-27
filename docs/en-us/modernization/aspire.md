# .NET Aspire Integration

.NET Aspire provides orchestration and tooling for observable, cloud-native .NET
applications. Mvp24Hours.Core supplies service-default options, a correlation
accessor, a self health check, Aspire-compatible health endpoints, and
connection-name helpers for .NET 10 applications.

There are **no presets** on Aspire option types. Configure them through
`appsettings.json`, a registration callback, or application-owned factories.

## What Core actually registers

| Registration | Condition |
|---|---|
| `AspireOptions` singleton | Always |
| Core `ICorrelationIdAccessor` → `CorrelationIdAccessor` | Always |
| Health Checks plus a healthy `self` check tagged `live` | `EnableHealthChecks == true` |
| Empty `ConfigureHttpClientDefaults` hook | `EnableResilience == true` |

The following properties are configuration contracts only. Core stores them on
the singleton but does not install exporters, instrumentation packages, service
discovery, module health checks, timeouts, or concrete resilience strategies
from them:

- `EnableOpenTelemetry`
- `EnableServiceDiscovery`
- `OtlpEndpoint`
- `Telemetry` and `ResourceAttributes`
- nested health enable flags and `TimeoutSeconds`
- nested `Resilience` properties

Wire those integrations through the consuming service and the relevant
Mvp24Hours observability, HTTP resilience, and module packages.

## Registration

`AddMvp24HoursAspireDefaults` extends `IHostApplicationBuilder`. It first binds
the `Aspire` configuration section, then applies the callback, fills service
identity defaults when still null, and registers the resulting `AspireOptions`
singleton. It does **not** register `IOptions<AspireOptions>` or options
validation.

```csharp
using Mvp24Hours.Core.Aspire;

var builder = WebApplication.CreateBuilder(args);

builder.AddMvp24HoursAspireDefaults(options =>
{
    options.ServiceName = "orders-api";
    options.ServiceVersion = "10.0.0";
    options.ResourceAttributes["service.region"] = "us-east-1";
});

var app = builder.Build();
app.MapMvp24HoursAspireHealthChecks();
app.Run();
```

To bind another section, pass its name:

```csharp
builder.AddMvp24HoursAspireDefaults("Platform:Aspire");
```

The default overload always reads `Aspire` before applying the callback. The
named-section overload still binds `Aspire` first and then binds the named
section in the callback, so values from the named section take precedence.
Service-name fallback still inspects the fixed key `Aspire:ServiceName` even
when a custom section name is used.

Identity defaults filled only when still null:

- `ServiceName` ← `OTEL_SERVICE_NAME`, `Aspire:ServiceName`, entry assembly
  name, or `Mvp24HoursService`
- `ServiceVersion` ← entry assembly version or `1.0.0`
- `Environment` ← `IHostEnvironment.EnvironmentName`

## AspireOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `ServiceName` | `string?` | `null` | Telemetry service name; filled as described above |
| `ServiceVersion` | `string?` | `null` | Service version; filled from the entry assembly or `1.0.0` |
| `Environment` | `string?` | `null` | Filled from `IHostEnvironment.EnvironmentName` |
| `EnableOpenTelemetry` | `bool` | `true` | Intent flag for OpenTelemetry integration |
| `EnableHealthChecks` | `bool` | `true` | Registers Health Checks and the `self` check |
| `EnableResilience` | `bool` | `true` | Runs the default HTTP-client resilience configuration hook |
| `EnableServiceDiscovery` | `bool` | `true` | Intent flag for service discovery |
| `OtlpEndpoint` | `string?` | `null` | Intended OTLP endpoint; otherwise use `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `Telemetry` | `AspireTelemetryOptions` | New instance | Nested telemetry settings |
| `HealthChecks` | `AspireHealthCheckOptions` | New instance | Nested health settings |
| `Resilience` | `AspireResilienceOptions` | New instance | Nested resilience settings |
| `ResourceAttributes` | `Dictionary<string, object>` | Empty | Additional OpenTelemetry resource attributes |

## AspireTelemetryOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `EnableLogging` | `bool` | `true` | Enable OTLP log export |
| `EnableTracing` | `bool` | `true` | Enable distributed tracing |
| `EnableMetrics` | `bool` | `true` | Enable metrics |
| `EnableAspNetCoreInstrumentation` | `bool` | `true` | Enable ASP.NET Core instrumentation |
| `EnableHttpClientInstrumentation` | `bool` | `true` | Enable HttpClient instrumentation |
| `EnableEfCoreInstrumentation` | `bool` | `true` | Enable EF Core instrumentation |
| `EnableMvp24HoursInstrumentation` | `bool` | `true` | Enable Mvp24Hours activity/meter sources |
| `TraceSamplingRatio` | `double` | `1.0` | Trace sampling ratio (`1.0` means all) |
| `AdditionalActivitySources` | `List<string>` | Empty | Additional `ActivitySource` names |
| `AdditionalMeterNames` | `List<string>` | Empty | Additional meter names |

## AspireHealthCheckOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `LivenessPath` | `string` | `/health/live` | Liveness endpoint |
| `ReadinessPath` | `string` | `/health/ready` | Readiness endpoint |
| `StartupPath` | `string` | `/health/startup` | Startup endpoint |
| `EnableDatabaseHealthChecks` | `bool` | `true` | Intent flag for database checks |
| `EnableCacheHealthChecks` | `bool` | `true` | Intent flag for cache checks |
| `EnableMessagingHealthChecks` | `bool` | `true` | Intent flag for messaging checks |
| `TimeoutSeconds` | `int` | `5` | Intended health-check timeout |

`AddMvp24HoursAspireDefaults` adds only a healthy `self` check tagged `live`.
Database, cache, and messaging modules must register their own checks. The
three enable flags and `TimeoutSeconds` do not add or configure those checks in
Core, and endpoint mapping does not apply `TimeoutSeconds`.

## AspireResilienceOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `EnableRetry` | `bool` | `true` | Intent flag for HTTP retry |
| `EnableCircuitBreaker` | `bool` | `true` | Intent flag for an HTTP circuit breaker |
| `EnableTimeout` | `bool` | `true` | Intent flag for timeout |
| `MaxRetryAttempts` | `int` | `3` | Maximum retry attempts |
| `CircuitBreakerFailureThreshold` | `int` | `5` | Failure-count threshold |
| `CircuitBreakerBreakDurationSeconds` | `int` | `30` | Open-circuit duration |
| `TimeoutSeconds` | `int` | `30` | Default timeout |

The current hook calls `ConfigureHttpClientDefaults` but does not add a
standard resilience handler or translate this nested object into policies.
Configure concrete HTTP resilience through
[HTTP resilience](http-resilience.md) or the
[resilience selection guide](resilience-guide.md).

## Component connection helpers

These methods extend `IServiceCollection` and register metadata singletons
only. They do not register Redis, RabbitMQ, EF Core, MongoDB clients, or health
checks. Resolve the Aspire connection string and call the module registration
yourself.

```csharp
builder.AddMvp24HoursAspireDefaults();

builder.Services.AddMvp24HoursRedisFromAspire("cache");
builder.Services.AddMvp24HoursRabbitMQFromAspire("messaging", options =>
{
    options.PrefetchCount = 20;
});
builder.Services.AddMvp24HoursSqlServerFromAspire("sqldb");
builder.Services.AddMvp24HoursPostgreSqlFromAspire("postgresdb");
builder.Services.AddMvp24HoursMongoDbFromAspire("mongodb");

string? redis = builder.GetAspireConnectionString("cache");
// Still required: AddMvpHybridCache / AddMvpRabbitMQ / AddMvp24HoursDbContext
// using that connection string.
```

`GetAspireConnectionString` returns `GetConnectionString(connectionName)` or
`Configuration["ConnectionStrings:{connectionName}"]`.

`AspireDatabaseType.MySql` exists on the enum, but there is no
`AddMvp24HoursMySqlFromAspire` helper.

### AspireRedisOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `ConnectionName` | `string` | `"cache"` | Aspire connection name |
| `InstanceName` | `string?` | `null` | Redis cache instance name |

### AspireRabbitMQOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `ConnectionName` | `string` | `"messaging"` | Aspire connection name |
| `AutoDeclareQueues` | `bool` | `true` | Automatic queue declaration intent |
| `EnableMessageDeduplication` | `bool` | `true` | Message deduplication intent |
| `PrefetchCount` | `ushort` | `10` | Consumer prefetch count |

### AspireDatabaseOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `ConnectionName` | `string` | `"database"` | Aspire connection name; helpers override per call |
| `DatabaseType` | `AspireDatabaseType` | enum default | Set by the helper (`SqlServer`, `PostgreSql`, `MySql`, `MongoDB`) |
| `EnableAutoMigration` | `bool` | `false` | Automatic migrations intent |
| `EnableResiliency` | `bool` | `true` | Connection resiliency intent |
| `CommandTimeoutSeconds` | `int` | `30` | Command timeout intent |

## Configuration example

All nested properties bind through the normal .NET configuration binder:

```json
{
  "Aspire": {
    "ServiceName": "orders-api",
    "ServiceVersion": "10.0.0",
    "Environment": "Production",
    "EnableOpenTelemetry": true,
    "EnableHealthChecks": true,
    "EnableResilience": true,
    "EnableServiceDiscovery": true,
    "OtlpEndpoint": "http://otel-collector:4317",
    "Telemetry": {
      "EnableLogging": true,
      "EnableTracing": true,
      "EnableMetrics": true,
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableEfCoreInstrumentation": true,
      "EnableMvp24HoursInstrumentation": true,
      "TraceSamplingRatio": 0.25,
      "AdditionalActivitySources": [ "Orders.Application" ],
      "AdditionalMeterNames": [ "Orders.Application" ]
    },
    "HealthChecks": {
      "LivenessPath": "/health/live",
      "ReadinessPath": "/health/ready",
      "StartupPath": "/health/startup",
      "EnableDatabaseHealthChecks": true,
      "EnableCacheHealthChecks": true,
      "EnableMessagingHealthChecks": true,
      "TimeoutSeconds": 5
    },
    "Resilience": {
      "EnableRetry": true,
      "EnableCircuitBreaker": true,
      "EnableTimeout": true,
      "MaxRetryAttempts": 3,
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerBreakDurationSeconds": 30,
      "TimeoutSeconds": 30
    },
    "ResourceAttributes": {
      "deployment.environment": "production",
      "service.region": "us-east-1"
    }
  }
}
```

## Health endpoints

`MapMvp24HoursAspireHealthChecks(options)` maps:

| Endpoint | Selection | Unhealthy status |
|---|---|---|
| `LivenessPath` | Fixed healthy JSON; does not call `HealthCheckService` | Not applicable |
| `ReadinessPath` | Checks tagged `ready` | `503` |
| `StartupPath` | Checks tagged `startup` or `live` | `503` |
| `/health` | All registered checks; path is hard-coded | `503` |

Degraded reports return `200`. Responses contain overall status, total duration
in milliseconds, and each check's name, status, duration, description,
exception message, and tags.

Pass the configured nested object when custom paths are used:

```csharp
AspireOptions options = app.Services.GetRequiredService<AspireOptions>();
app.MapMvp24HoursAspireHealthChecks(options.HealthChecks);
```

Calling `MapMvp24HoursAspireHealthChecks()` without an argument creates a fresh
`AspireHealthCheckOptions`; it does not resolve the registered `AspireOptions`.

## Dashboard support

`UseAspireDashboardSupport` adds permissive CORS
(`AllowAnyOrigin` / `AllowAnyMethod` / `AllowAnyHeader`). It does not configure
OTLP, exporters, or Aspire dashboard endpoints.

```csharp
app.UseAspireDashboardSupport();
```

## Correlation context

Registration adds singleton Core
`Mvp24Hours.Core.Aspire.ICorrelationIdAccessor` backed by `AsyncLocal<string?>`:

```csharp
using Mvp24Hours.Core.Aspire;

public sealed class Worker(ICorrelationIdAccessor correlation)
{
    public void Begin(string correlationId) =>
        correlation.SetCorrelationId(correlationId);
}
```

Application has a different type with the same short name:
`Mvp24Hours.Application.Contract.Observability.ICorrelationIdAccessor`. The
Application accessor is read-oriented and also exposes causation metadata.
Qualify the namespace when both packages are referenced. Prefer the Application
accessor when Application services own correlation propagation.

## Packages for full Aspire-style observability

Core itself does not install these packages. Full Aspire-style telemetry and
HTTP resilience typically require:

- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `Microsoft.Extensions.Http.Resilience`
- optional `AspNetCore.HealthChecks.*` packages for module checks

## Testing

`src/Tests/Mvp24Hours.Core.Test/Aspire/AspireOptionsTest.cs` covers option
defaults, setters, and the Core correlation accessor. There are currently no DI
or HTTP health-endpoint integration tests for Aspire helpers.

## See also

- [Observability overview](../observability/home.md)
- [OpenTelemetry exporters](../observability/exporters.md)
- [Health Checks catalog](../infrastructure/health-checks.md)
- [HTTP resilience](http-resilience.md)
- [Resilience selection guide](resilience-guide.md)
- [Options validation](../core/options-validation.md)
- [.NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/)
