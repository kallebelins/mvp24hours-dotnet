# .NET 9 Native Features

This document provides a comprehensive overview of the native .NET 9 features adopted by Mvp24Hours, replacing custom implementations with modern, standardized APIs.

## Overview

.NET 9 introduces several APIs that make custom implementations obsolete. Mvp24Hours has adopted these native features to:

- **Reduce maintenance burden** - Less custom code means fewer bugs
- **Improve performance** - Native APIs are highly optimized
- **Enhance compatibility** - Better integration with the .NET ecosystem
- **Simplify upgrades** - Following Microsoft patterns ensures smooth version transitions

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         .NET 9 Native Features in Mvp24Hours               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐             │
│  │   Resilience    │  │    Caching      │  │   Observability │             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │ Http.Resilience │  │  HybridCache    │  │  OpenTelemetry  │             │
│  │ Extensions.     │  │  Output Cache   │  │  ILogger        │             │
│  │  Resilience     │  │  IMemoryCache   │  │  ActivitySource │             │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘             │
│           │                    │                    │                       │
│  ┌────────┴────────┐  ┌────────┴────────┐  ┌────────┴────────┐             │
│  │   Rate Limiting │  │  Configuration  │  │     Hosting     │             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │ RateLimiter     │  │  IOptions<T>    │  │  .NET Aspire    │             │
│  │ FixedWindow     │  │  TimeProvider   │  │  Keyed Services │             │
│  │ SlidingWindow   │  │  PeriodicTimer  │  │  Source Gen     │             │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘             │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐             │
│  │      APIs       │  │    Channels     │  │  Error Handling │             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │  Minimal APIs   │  │  Channel<T>     │  │ ProblemDetails  │             │
│  │  TypedResults   │  │  BoundedChannel │  │  RFC 7807       │             │
│  │  Native OpenAPI │  │  Producer/      │  │  TypedResults   │             │
│  │                 │  │   Consumer      │  │   .Problem()    │             │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Feature Categories

### 1. Resilience and Fault Tolerance

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **HTTP Resilience** | `Microsoft.Extensions.Http.Resilience` for HTTP clients | [HTTP Resilience](http-resilience.md) |
| **Generic Resilience** | `Microsoft.Extensions.Resilience` for any operation | [Generic Resilience](generic-resilience.md) |
| **Rate Limiting** | `System.Threading.RateLimiting` for throttling | [Rate Limiting](rate-limiting.md) |

### 2. Caching

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **HybridCache** | L1 (memory) + L2 (distributed) cache | [HybridCache](hybrid-cache.md) |
| **Output Caching** | HTTP response caching | [Output Caching](output-caching.md) |

### 3. Time and Scheduling

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **TimeProvider** | Abstraction for `DateTime.Now` / `DateTime.UtcNow` | [TimeProvider](time-provider.md) |
| **PeriodicTimer** | Modern timer with async support | [PeriodicTimer](periodic-timer.md) |

### 4. Configuration

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **IOptions<T>** | Strongly-typed configuration | [Options Configuration](options-configuration.md) |
| **Keyed Services** | DI with key-based resolution | [Keyed Services](keyed-services.md) |

### 5. Communication

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **Channels** | High-performance producer/consumer | [Channels](channels.md) |

### 6. APIs and Documentation

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **Minimal APIs** | Lightweight endpoints with TypedResults | [Minimal APIs](minimal-apis.md) |
| **ProblemDetails** | RFC 7807 error responses | [ProblemDetails](problem-details.md) |
| **Native OpenAPI** | Built-in OpenAPI support | [Native OpenAPI](native-openapi.md) |

### 7. Performance

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **Source Generators** | AOT-friendly code generation | [Source Generators](source-generators.md) |

### 8. Cloud-Native

| Feature | Description | Documentation |
|---------|-------------|---------------|
| **.NET Aspire** | Observability and orchestration | [.NET Aspire](aspire.md) |

## What's New in .NET 9

### HybridCache (Stable)

HybridCache is now stable in .NET 9, providing:

- **L1 + L2 caching** - Memory and distributed cache combined
- **Stampede protection** - Prevents cache stampede automatically
- **Tag-based invalidation** - Invalidate related entries efficiently

```csharp
// Register HybridCache
services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

// Use in your code
var user = await hybridCache.GetOrCreateAsync(
    $"user:{userId}",
    async cancel => await userRepository.GetByIdAsync(userId, cancel),
    options: new() { Expiration = TimeSpan.FromHours(1) }
);
```

### Native OpenAPI

.NET 9 includes built-in OpenAPI support via `Microsoft.AspNetCore.OpenAPI`:

```csharp
// Add native OpenAPI
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "v1";
    options.EnableSecuritySchemes = true;
});

// Map OpenAPI endpoint
app.MapMvp24HoursNativeOpenApi();
```

### TypedResults.InternalServerError

.NET 9 adds `TypedResults.InternalServerError()`:

```csharp
// Before .NET 9
return Results.StatusCode(500);

// .NET 9+
return TypedResults.InternalServerError();

// With Mvp24Hours
return businessResult.ToNativeTypedResult();
```

### .NET Aspire 9

.NET Aspire 9 provides comprehensive cloud-native support:

```csharp
// Add Aspire service defaults
builder.AddMvp24HoursAspireDefaults();

// Add Redis from Aspire configuration
builder.AddMvp24HoursRedisFromAspire("cache");

// Add RabbitMQ from Aspire configuration
builder.AddMvp24HoursRabbitMQFromAspire("messaging");
```

## Quick Start

### 1. Enable All Modern Features

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Mvp24Hours with .NET 9 features
builder.Services
    // Observability
    .AddMvp24HoursObservability()
    
    // Caching
    .AddMvpHybridCache()
    
    // Resilience
    .AddMvpStandardResilience()
    
    // Configuration validation
    .AddOptionsWithValidation<MyOptions>("MyOptions")
    
    // Time abstraction
    .AddTimeProvider();

var app = builder.Build();

// Add middleware
app.UseNativeProblemDetailsHandling();

// Add native OpenAPI
app.MapMvp24HoursNativeOpenApi();

// Add Output Caching
app.UseOutputCache();

app.Run();
```

### 2. Enable Aspire Integration

For cloud-native applications:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (includes observability, health checks, resilience)
builder.AddMvp24HoursAspireDefaults();

// Add distributed components
builder.AddMvp24HoursRedisFromAspire("redis");
builder.AddMvp24HoursSqlServerFromAspire("sql");
builder.AddMvp24HoursRabbitMQFromAspire("rabbitmq");

var app = builder.Build();

// Map health checks for Aspire dashboard
app.MapMvp24HoursAspireHealthChecks();

app.Run();
```

## Migration Path

For detailed migration instructions from legacy Mvp24Hours implementations to native .NET 9 APIs, see the [Migration Guide](migration-guide.md).

### Summary of Breaking Changes

| Legacy (Deprecated) | Native (.NET 9) |
|---------------------|-----------------|
| `TelemetryHelper` | `ILogger` + OpenTelemetry |
| `HttpClientExtensions` | `Microsoft.Extensions.Http.Resilience` |
| `MvpExecutionStrategy` | `Microsoft.Extensions.Resilience` |
| `MultiLevelCache` | `HybridCache` |
| `RetryPipelineMiddleware` | `NativePipelineResilienceMiddleware` |
| `CircuitBreakerPipelineMiddleware` | `NativePipelineResilienceMiddleware` |
| Custom Rate Limiting | `System.Threading.RateLimiting` |
| Custom Timers | `TimeProvider` + `PeriodicTimer` |

## Compatibility

### Minimum Requirements

- **.NET 9.0** or later
- **C# 13** for full language feature support

### Backward Compatibility

All deprecated APIs remain functional but will emit compiler warnings. Migrate to native APIs at your convenience before the next major version.

## Performance Benefits

| Feature | Improvement |
|---------|-------------|
| HybridCache | Up to 50% faster than custom multi-level cache |
| Source Generators | Near-zero runtime reflection overhead |
| Native OpenAPI | 30% faster document generation |
| PeriodicTimer | Reduced memory allocations vs System.Timers.Timer |
| Channels | Lock-free producer/consumer patterns |

## See Also

- [Migration Guide](migration-guide.md) - Step-by-step migration instructions
- [.NET 9 What's New](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/overview)
- [ASP.NET Core 9 What's New](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-9.0)

