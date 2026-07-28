using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace ServiceDefaults;

/// <summary>
/// Shared Aspire-compatible defaults for all services in this sample.
/// Provides health-check registration and HTTP resilience without requiring the AppHost.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers standard health checks and HTTP resilience defaults.
    /// Call this in every service's Program.cs before building the app.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        builder.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler());

        return builder;
    }

    /// <summary>
    /// Maps the standard health check endpoints compatible with Aspire probes.
    /// </summary>
    /// <remarks>
    /// Endpoints mapped:
    /// <list type="bullet">
    ///   <item>/health/live  — liveness probe (tags: live)</item>
    ///   <item>/health/ready — readiness probe (tags: ready)</item>
    ///   <item>/health       — all checks</item>
    /// </list>
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        return app;
    }
}
