# Mvp24Hours.Infrastructure.Data.MongoDb

MongoDB persistence infrastructure for .NET 10 applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb
```

## Features

- Synchronous and asynchronous repositories, specifications, pagination, and bulk operations.
- Transactions, aggregations, GridFS, change streams, and text search.
- Time-series, geospatial, sharding, and CQRS integration.
- Resilience, health checks, metrics, tracing, and structured logging.

## Quick start

Register the MongoDB context and repositories:

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.ConnectionString =
        builder.Configuration.GetConnectionString("MongoDb")!;
    options.DatabaseName = "application";
});

builder.Services.AddMvp24HoursRepositoryAsync();
```

Advanced features can be enabled independently through the `AddMvpMongoDbAdvanced`, transaction, change-stream, and CQRS extensions.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
