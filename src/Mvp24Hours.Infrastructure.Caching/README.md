# Mvp24Hours.Infrastructure.Caching

Flexible caching abstractions and implementations for .NET 10 applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Caching
```

## Features

- Memory, distributed, hybrid, and multi-level caches.
- Cache-aside, read-through, write-through, write-behind, and refresh-ahead patterns.
- Tag- and dependency-based invalidation.
- Stampede prevention, compression, warming, and health checks.
- Metrics, tracing, and structured logging.

## Quick start

Register the default caching services:

```csharp
builder.Services.AddMvp24HoursCaching(options =>
{
    options.DefaultKeyPrefix = "orders";
});
```

Use `AddMvpHybridCache`, `AddMultiLevelCache`, or the individual provider and caching-pattern extensions for tailored configurations.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
