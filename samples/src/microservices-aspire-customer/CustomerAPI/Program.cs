using CustomerAPI.Data;
using CustomerAPI.Extensions;
using CustomerAPI.Models;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (health checks + HTTP resilience) ────────────────
builder.AddServiceDefaults();

// ── Mvp24Hours web essentials ────────────────────────────────────────────────
builder.Services.AddTimeProvider();
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursMapService(typeof(CreateCustomerRequest).Assembly);
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Customer API — Microservices + Aspire Sample";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
});
builder.Services.AddMvp24HoursWebGzip();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddCustomerServices();
builder.Services.AddCustomerDbContext(builder.Configuration);
builder.Services.AddCustomerMessaging(builder.Configuration);
builder.Services.AddCustomerHealthChecks(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Ensure database exists / apply migrations at startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseNativeProblemDetailsHandling();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// ServiceDefaults health endpoints: /health/live, /health/ready, /health
app.MapDefaultEndpoints();

// Legacy /hc endpoint for HealthChecks UI compatibility.
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
