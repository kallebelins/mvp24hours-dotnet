# .NET 10 Modernization Overview

Mvp24Hours currently targets `net10.0` and builds on native platform features
adopted across .NET 9 and .NET 10. This page is the map of those features. Use
the linked module pages for API and option details.

> **Package readiness:** the source tree targets .NET 10, but production
> projects still declare package version `9.1.21`. Verify the NuGet feed before
> referencing `10.8.0`. See the
> [9.1.x → 10.8.0 migration](../migration.md?id=_91x-1000).

## Platform baseline

The repository baseline is:

- `net10.0`;
- nullable reference types enabled;
- `LangVersion=latest` by default (the MongoDB project currently overrides it
  with C# 12);
- Central Package Management through `Directory.Packages.props`;
- .NET 10 Microsoft.Extensions, ASP.NET Core, and EF Core dependencies.

These build settings are separate from package publication. Target-framework
adoption alone does not prove that a matching package version has shipped.

## Feature map

### Resilience and rate limiting

| Capability | Current path | Documentation |
|---|---|---|
| HTTP resilience | `Microsoft.Extensions.Http.Resilience` with Mvp24Hours named/typed client helpers | [HTTP resilience](http-resilience.md) |
| Generic operations | `Microsoft.Extensions.Resilience` and Polly v8 pipelines | [Generic resilience](generic-resilience.md) |
| Area-specific policies | EF Core, MongoDB, cache, CronJob, pipeline, and CQRS integrations | [Resilience selection guide](resilience-guide.md) |
| Rate limiting | `System.Threading.RateLimiting` providers and module adapters | [Rate limiting](rate-limiting.md) |

HTTP and generic resilience both expose a `NativeResilienceOptions` type in
different namespaces. Qualify the type name when both namespaces are imported.

### Caching

| Capability | Current path | Documentation |
|---|---|---|
| Hybrid cache | `HybridCache` with local and distributed tiers, stampede protection, tags, warming, and Mvp24Hours adapters | [HybridCache](hybrid-cache.md) |
| HTTP output cache | ASP.NET Core Output Caching integration | [Output caching](output-caching.md) |

`HybridCache` became stable in .NET 9 and remains the preferred native cache
abstraction on .NET 10.

### Time, concurrency, and dependency injection

| Capability | Current path | Documentation |
|---|---|---|
| Testable time | `TimeProvider` and `FakeTimeProvider` | [TimeProvider](time-provider.md) |
| Async periodic work | `PeriodicTimer` | [PeriodicTimer](periodic-timer.md) |
| Producer/consumer flows | `Channel<T>` and bounded channels | [Channels](channels.md) |
| Multiple implementations | Keyed DI services | [Keyed services](keyed-services.md) |

### Web APIs

| Capability | Current path | Documentation |
|---|---|---|
| Lightweight endpoints | Minimal APIs and typed results | [Minimal APIs](minimal-apis.md) |
| Error contracts | ASP.NET Core Problem Details | [Problem Details](problem-details.md) |
| API description | `Microsoft.AspNetCore.OpenApi` plus Mvp24Hours registration and mapping helpers | [Native OpenAPI](native-openapi.md) |

Native OpenAPI support was introduced in ASP.NET Core 9 and remains current in
ASP.NET Core 10. Swagger UI is still a separate visualization concern.

### Configuration and generated code

| Capability | Current path | Documentation |
|---|---|---|
| Strongly typed settings | Options binding, validation, and `ValidateOnStart` | [Options configuration](options-configuration.md) |
| Generated implementations | AOT-oriented source generators supplied by the library | [Source generators](source-generators.md) |

### Cloud-native operation

| Capability | Current path | Documentation |
|---|---|---|
| Service defaults contract | `AddMvp24HoursAspireDefaults`, health endpoints, service identity, and correlation context | [.NET Aspire](aspire.md) |
| Telemetry | OpenTelemetry logs, traces, metrics, and exporters | [Observability](../observability/home.md) |

The current Core Aspire integration does not install an AppHost, exporters,
service-discovery provider, or concrete resilience strategies. Its nested
telemetry, discovery, and resilience settings are configuration contracts;
wire the corresponding consuming-service integrations explicitly.

## Minimal composition example

Register only the capabilities the application uses:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddMvp24HoursAspireDefaults(options =>
{
    options.ServiceName = "orders-api";
    options.ServiceVersion = "10.8.0";
});

builder.Services.AddMvpHybridCache();
builder.Services.AddTimeProvider();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Orders API";
    options.Version = "v1";
});

builder.Services
    .AddHttpClient("Payments", client =>
        client.BaseAddress = new Uri("https://payments.example.com"))
    .AddMvpStandardResilience();

var app = builder.Build();
app.MapMvp24HoursNativeOpenApi();
app.MapMvp24HoursAspireHealthChecks();
app.Run();
```

The `AddMvp24HoursAspireDefaults` flags do not replace the explicit
observability, HTTP-resilience, cache, database, or messaging registrations
required by those modules.

## What changed for the .NET 10 source baseline

- Projects now target `net10.0`.
- Microsoft platform dependencies and EF Core use .NET 10-compatible versions
  through Central Package Management.
- Nullable reference types and strict Release builds expose compatibility
  issues that may not appear in older consumers.
- Native patterns adopted in .NET 9 remain current; consumers do not need a
  second implementation migration merely because the target framework changed.

For source-level breaking changes such as required option members, SMTP
certificate validation, SQL client changes, and security verification, follow
the canonical [version migration](../migration.md?id=_91x-1000).

## Choose the right migration

- Moving a consumer from 9.1.x to the .NET 10 source/package line:
  [version migration](../migration.md?id=_91x-1000).
- Replacing legacy custom implementations with native platform APIs:
  [native API migration](migration-guide.md).
- Choosing among overlapping retry, timeout, and circuit-breaker integrations:
  [resilience selection guide](resilience-guide.md).

## See also

- [.NET 10 overview](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview)
- [ASP.NET Core 10 release notes](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0)
- [.NET 10 version migration](../migration.md?id=_91x-1000)
- [Migration to native APIs](migration-guide.md)
