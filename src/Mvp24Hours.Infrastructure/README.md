# Mvp24Hours.Infrastructure

Cross-cutting infrastructure services for .NET 10 applications built with Mvp24Hours.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure
```

## Features

- Email through SMTP, SendGrid, or Azure and SMS through Twilio or Azure.
- Local, Azure Blob, and Amazon S3 file storage.
- Typed HTTP clients with native and Polly resilience.
- Redis, SQL Server, and PostgreSQL distributed locking.
- Environment, Azure Key Vault, and AWS Secrets Manager providers.
- Background jobs, health checks, and infrastructure observability.

## Quick start

Register the infrastructure modules needed by the application:

```csharp
builder.Services.AddMvpInfrastructure(builder.Configuration);
```

Focused extensions such as `AddEmailService`, `AddFileStorage`, `AddMvpTypedHttpClient`, `AddDistributedLocking`, and `AddBackgroundJobs` are also available.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
