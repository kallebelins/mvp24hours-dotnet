using App.Application.Logic;
using App.Core.Contract.Data;
using App.Core.Contract.Logic;
using App.Infrastructure.Stores;
using App.Worker.Jobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.CronJob.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IItemStore, InMemoryItemStore>();
builder.Services.AddSingleton<IItemProcessor, ItemProcessor>();

builder.Services.AddCronJob<ItemProcessingJob>(options =>
{
    options.CronExpression = builder.Configuration["CronJobs:ItemProcessingJob:CronExpression"]
                             ?? "* * * * *";
    options.TimeZoneInfo = TimeZoneInfo.Utc;
});

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

WebApplication app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();

public partial class Program { }
