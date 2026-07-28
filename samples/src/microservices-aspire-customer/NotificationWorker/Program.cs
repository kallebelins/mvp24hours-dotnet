using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NotificationWorker.Extensions;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (health checks + HTTP resilience) ────────────────
builder.AddServiceDefaults();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddNotificationDbContext();
builder.Services.AddNotificationServices();
builder.Services.AddNotificationMessaging(builder.Configuration);
builder.Services.AddNotificationHealthChecks(builder.Configuration);
builder.Services.AddNotificationHostedService();

// Minimal routing needed for health endpoints.
builder.Services.AddRouting();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseRouting();

// ServiceDefaults health endpoints: /health/live, /health/ready, /health
app.MapDefaultEndpoints();

// Legacy /hc endpoint for HealthChecks UI compatibility.
app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();
