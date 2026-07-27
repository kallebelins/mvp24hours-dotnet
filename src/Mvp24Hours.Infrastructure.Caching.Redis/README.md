# Mvp24Hours.Infrastructure.Caching.Redis

Redis distributed-cache integration for Mvp24Hours applications using StackExchange.Redis.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Caching.Redis
```

## Quick start

Register Redis using a connection string:

```csharp
builder.Services.AddMvp24HoursCachingRedis(
    builder.Configuration.GetConnectionString("Redis")!);
```

An overload accepting `ConfigurationOptions` is available for advanced StackExchange.Redis settings. You can also provide an instance name to prefix cache keys.

## Related package

Use `Mvp24Hours.Infrastructure.Caching` for hybrid and multi-level caching, invalidation, caching patterns, health checks, and observability.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
