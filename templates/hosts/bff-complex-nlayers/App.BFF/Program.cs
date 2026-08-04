using App.Application;
using App.Application.Logic;
using App.BFF.Extensions;
using App.Core.Contract.Logic;
using App.Core.Models;
using App.Core.Validations;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);
var httpHardeningSection = builder.Configuration.GetSection("HttpHardening");
var enableRateLimiting = httpHardeningSection.GetValue("RateLimiting:Enabled", true);
var rateLimit = httpHardeningSection.GetValue("RateLimiting:PermitLimit", 200);
var rateWindowSeconds = httpHardeningSection.GetValue("RateLimiting:WindowSeconds", 60);
var enableIdempotency = httpHardeningSection.GetValue("Idempotency:Enabled", true);
var idempotencyRequireKey = httpHardeningSection.GetValue("Idempotency:RequireKey", false);
var enableOutputCache = httpHardeningSection.GetValue("OutputCache:Enabled", true);
var outputCacheSeconds = httpHardeningSection.GetValue("OutputCache:DefaultExpirationSeconds", 60);

builder.Services.AddTimeProvider();
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursMapService(typeof(Item).Assembly);
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "App BFF Complex N-Layers";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
});
builder.Services.AddMvp24HoursWebGzip();
if (enableRateLimiting)
{
    builder.Services.AddMvp24HoursRateLimiting(rateLimit, TimeSpan.FromSeconds(rateWindowSeconds));
}

if (enableIdempotency)
{
    builder.Services.AddMvp24HoursIdempotency(options =>
    {
        options.StorageType = Mvp24Hours.WebAPI.Configuration.IdempotencyStorageType.InMemory;
        options.RequireIdempotencyKey = idempotencyRequireKey;
    });
}

if (enableOutputCache)
{
    builder.Services.AddMvp24HoursOutputCache(options =>
    {
        options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(outputCacheSeconds);
        options.ExcludedPaths.Add("/hc");
    });
}
builder.Services.AddMvp24HoursRequestObservability();
builder.Services.AddMyServices(builder.Configuration);
builder.Services.AddMyHealthChecks(builder.Environment);
builder.Services.AddControllers();
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

var app = builder.Build();

app.UseNativeProblemDetailsHandling();
app.UseRouting();
app.UseMvp24HoursRequestObservability();
app.UseMvp24HoursRateLimiting(enableRateLimiting);
app.UseAuthentication();
app.UseKeycloakCurrentUser();
app.UseAuthorization();
app.UseMvp24HoursIdempotency(enableIdempotency && !app.Environment.IsEnvironment("Testing"));
app.UseMvp24HoursOutputCache(enableOutputCache);
app.MapControllers();
app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

if (!app.Environment.IsProduction())
{
    app.MapMvp24HoursNativeOpenApi();
}

await app.RunAsync();

public partial class Program { }
