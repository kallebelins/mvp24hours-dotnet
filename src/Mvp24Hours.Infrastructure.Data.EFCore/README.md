# Mvp24Hours.Infrastructure.Data.EFCore

Entity Framework Core persistence infrastructure for .NET 10 applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
```

## Features

- Synchronous and asynchronous repositories, unit of work, specifications, and streaming.
- Bulk operations and CQRS read/write repositories.
- Multi-tenancy, encryption, row-level security, and read/write splitting.
- Automatic migrations, seeding, and schema validation.
- Database resilience, OpenTelemetry instrumentation, and health checks.

## Quick start

Register a DbContext and repositories:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMvp24HoursDbContext<AppDbContext>();
builder.Services.AddMvp24HoursRepositoryAsync();
```

Provider-specific EF Core packages may be required for databases other than SQL Server.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
