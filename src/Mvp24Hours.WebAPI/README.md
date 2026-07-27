# Mvp24Hours.WebAPI

ASP.NET Core Web API conventions and cross-cutting services for .NET 10.

## Install

```bash
dotnet add package Mvp24Hours.WebAPI
```

## Features

- Native problem details, exception handling, OpenAPI/Swagger, and API versioning.
- CORS, security headers, API keys, IP filtering, anti-forgery, and sanitization.
- Rate limiting, response compression, output caching, ETags, and idempotency.
- Minimal API groups and typed-result helpers.
- Request metrics, tracing, and structured logging.

## Quick start

Register the essential API services:

```csharp
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursNativeOpenApi();

var app = builder.Build();
app.UseMvp24HoursExceptionHandling();
app.UseMvp24HoursRequestObservability();
```

Enable versioning, security, rate limiting, caching, and idempotency through their focused extensions.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
