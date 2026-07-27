# Mvp24Hours.Infrastructure.RabbitMQ

RabbitMQ messaging infrastructure for .NET 10 applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
```

## Features

- Message publishers, consumers, batch consumers, and topology management.
- Filters, rate limiting, deduplication, scheduling, and transactional outbox.
- Saga persistence with in-memory, Redis, Entity Framework Core, or MongoDB stores.
- Multi-tenancy, OpenTelemetry, Prometheus metrics, health checks, and a test harness.

## Quick start

Register RabbitMQ and discover consumers from an assembly:

```csharp
builder.Services.AddMvp24HoursRabbitMQ(
    typeof(Program).Assembly,
    connection =>
    {
        connection.ConnectionString =
            builder.Configuration.GetConnectionString("RabbitMQ")!;
    });
```

Use `AddMvp24HoursRabbitMQAdvanced`, `AddTransactionalMessaging`, and the saga extensions when the application needs advanced delivery workflows.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
