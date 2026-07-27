# Mvp24Hours.Infrastructure.CronJob

Scheduled background tasks using CRON expressions in .NET 10 hosted applications.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Features

- Strongly typed hosted services scheduled with Cronos.
- Retry, circuit-breaker, and distributed-lock support.
- Persistent job state and configuration binding.
- OpenTelemetry tracing, metrics, and health checks.

## Quick start

Register a scheduled job:

```csharp
builder.Services.AddCronJob<MyCronJob>(options =>
{
    options.CronExpression = "0 */5 * * * *";
    options.TimeZoneInfo = TimeZoneInfo.Utc;
});
```

Use `AddResilientCronJob`, `AddAdvancedCronJob`, or `AddCronJobFromConfiguration` when the job needs additional behavior.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
