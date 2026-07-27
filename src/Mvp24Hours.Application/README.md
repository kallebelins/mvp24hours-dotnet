# Mvp24Hours.Application

Application-layer services and conventions for .NET 10 applications built with Mvp24Hours.

## Install

```bash
dotnet add package Mvp24Hours.Application
```

## Features

- CRUD, command, and query application services.
- AutoMapper and FluentValidation registration.
- Specifications, pagination, transactions, and bulk operations.
- Application events, outbox workflows, and query caching.
- Convention-based service discovery and registration.

## Quick start

Register the full application module:

```csharp
builder.Services.AddMvp24HoursApplicationFull(typeof(Program).Assembly);
```

Use `AddMvp24HoursApplicationModule` or `AddMvp24HoursApplicationForApi` for a more focused setup.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
