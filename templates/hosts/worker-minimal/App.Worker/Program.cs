using App.Worker.Jobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Observability;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCronJob<ItemHeartbeatJob>(options =>
{
    options.CronExpression = builder.Configuration["CronJobs:ItemHeartbeatJob:CronExpression"]
                             ?? "* * * * *";
    options.TimeZoneInfo = TimeZoneInfo.Utc;
});

builder.Services.AddHttpClient("external-dependency")
    .AddStandardResilienceHandler();

builder.Services.AddCronJobObservability();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCronJobHealthCheck();

WebApplication app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("cronjob")
});

await app.RunAsync();

public partial class Program { }
