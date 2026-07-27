# Mvp24Hours.Core

Core building blocks for .NET 10 and ASP.NET Core applications built with Mvp24Hours.

## Install

```bash
dotnet add package Mvp24Hours.Core
```

## Features

- Domain and application contracts, business results, specifications, repositories, and value objects.
- FluentValidation and AutoMapper integration helpers.
- OpenTelemetry tracing, metrics, and Aspire configuration support.
- Keyed services, channels, rate limiting, options validation, and JSON source-generation helpers.

## Quick start

Register the observability services required by your application:

```csharp
builder.Services.AddMvp24HoursObservability(builder.Configuration);
```

Use the focused `AddMvp24HoursTracing` and `AddMvp24HoursMetrics` extensions when you do not need the complete observability setup.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
