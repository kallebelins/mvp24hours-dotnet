# Mvp24Hours.Infrastructure.Pipe

Composable synchronous and asynchronous pipelines for .NET 10 applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Pipe
```

## Features

- Typed pipeline operations and chain-of-responsibility workflows.
- Logging, timeout, validation, caching, and OpenTelemetry middleware.
- Retry, circuit breaker, bulkhead, and dead-letter strategies.
- Rate limiting, streaming, parallel branches, switches, and scoped execution.

## Quick start

Register the synchronous and asynchronous pipeline engines:

```csharp
builder.Services.AddMvp24HoursPipeline();
builder.Services.AddMvp24HoursPipelineAsync();
```

Use `AddTypedPipeline`, `AddMvpPipelineResiliency`, `AddPipelineFluentValidation`, and `AddPipelineRateLimiting` for focused behavior.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
