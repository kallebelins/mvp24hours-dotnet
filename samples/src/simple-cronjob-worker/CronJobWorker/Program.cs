using CronJobWorker.Jobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Observability;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── CronJobs ────────────────────────────────────────────────────────────────

// 1. HeartbeatJob — simple scheduled job (every minute in production;
//    override via appsettings or env for faster demo runs).
builder.Services.AddCronJob<HeartbeatJob>(options =>
{
    options.CronExpression = builder.Configuration["CronJobs:HeartbeatJob:CronExpression"]
                             ?? "* * * * *"; // every minute (5-field Cronos standard format)
    options.TimeZoneInfo = TimeZoneInfo.Utc;
});

// 2. CleanupJob — resilient scheduled job: retry + circuit-breaker + overlapping prevention.
builder.Services.AddResilientCronJob<CleanupJob>(options =>
{
    options.CronExpression = builder.Configuration["CronJobs:CleanupJob:CronExpression"]
                             ?? "*/5 * * * *"; // every 5 minutes

    options.TimeZoneInfo = TimeZoneInfo.Utc;

    // Retry with exponential back-off — up to 3 attempts before giving up.
    options.Resilience.EnableRetry = true;
    options.Resilience.MaxRetryAttempts = 3;
    options.Resilience.UseExponentialBackoff = true;

    // Circuit breaker — open after 5 consecutive failures for 30 s.
    options.Resilience.EnableCircuitBreaker = true;
    options.Resilience.CircuitBreakerFailureThreshold = 5;
    options.Resilience.CircuitBreakerDuration = TimeSpan.FromSeconds(30);

    // Prevent a new run while the previous one is still executing.
    options.Resilience.PreventOverlapping = true;

    // Hook invoked on each retry — useful for alerting or custom counters.
    options.Resilience.OnRetry = (ex, attempt, delay) =>
    {
        Console.WriteLine($"[CleanupJob] Retry #{attempt} after {delay.TotalSeconds:F1}s. Reason: {ex.Message}");
    };
});

// ─── Observability ───────────────────────────────────────────────────────────

// Registers ICronJobMetrics (in-memory counters) and CronJobHealthCheckOptions.
builder.Services.AddCronJobObservability();

// ─── Health checks ───────────────────────────────────────────────────────────

builder.Services
    .AddHealthChecks()
    .AddCronJobHealthCheck(options =>
    {
        // Degrade (not fail outright) when a job's failure rate exceeds 10 %.
        options.MaxFailureRate = 0.10;
        // Only flip to Unhealthy once 50 % of executions fail.
        options.CriticalFailureRate = 0.50;
        // Require at least 5 executions before rate-based checks apply.
        options.MinExecutionsForRateCheck = 5;
    });

// ─── Build ────────────────────────────────────────────────────────────────────

WebApplication app = builder.Build();

// Minimal /health endpoint — reports aggregate status of all registered CronJobs.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Liveness endpoint — just returns 200 OK while the process is up.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness endpoint — cronjobs-specific.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("cronjob")
});

app.Run();
