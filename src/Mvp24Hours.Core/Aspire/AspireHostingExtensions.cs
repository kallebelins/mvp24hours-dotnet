//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Aspire;

/// <summary>
/// Extension methods for Aspire hosting and AppHost patterns.
/// </summary>
/// <remarks>
/// <para>
/// .NET Aspire uses an AppHost project to orchestrate distributed applications during development.
/// The AppHost defines resources (databases, caches, messaging) and projects that depend on them.
/// </para>
/// <para>
/// <strong>AppHost Pattern:</strong>
/// </para>
/// <code>
/// // AppHost/Program.cs
/// var builder = DistributedApplication.CreateBuilder(args);
/// 
/// // Infrastructure
/// var redis = builder.AddRedis("cache")
///     .WithDataVolume();
/// 
/// var rabbitmq = builder.AddRabbitMQ("messaging")
///     .WithManagementPlugin();
/// 
/// var sql = builder.AddSqlServer("sql")
///     .WithDataVolume()
///     .AddDatabase("appdb");
/// 
/// // API Project
/// builder.AddProject&lt;Projects.Api&gt;("api")
///     .WithReference(redis)
///     .WithReference(rabbitmq)
///     .WithReference(sql)
///     .WithExternalHttpEndpoints();
/// 
/// builder.Build().Run();
/// </code>
/// <para>
/// <strong>Benefits:</strong>
/// </para>
/// <list type="bullet">
///   <item>Automatic service discovery</item>
///   <item>Connection string injection</item>
///   <item>Health check integration</item>
///   <item>Developer dashboard for observability</item>
///   <item>Container orchestration in development</item>
/// </list>
/// </remarks>
public static class AspireHostingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Maps Aspire-compatible health check endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="options">Optional health check options.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method maps the standard Aspire health check endpoints:
    /// </para>
    /// <list type="bullet">
    ///   <item>/health/live - Liveness probe (is the app running?)</item>
    ///   <item>/health/ready - Readiness probe (is the app ready to receive traffic?)</item>
    ///   <item>/health - Overall health (all checks)</item>
    /// </list>
    /// <para>
    /// <strong>Usage:</strong>
    /// </para>
    /// <code>
    /// var app = builder.Build();
    /// app.MapMvp24HoursAspireHealthChecks();
    /// app.Run();
    /// </code>
    /// </remarks>
    public static IEndpointRouteBuilder MapMvp24HoursAspireHealthChecks(
        this IEndpointRouteBuilder app,
        AspireHealthCheckOptions? options = null)
    {
        options ??= new AspireHealthCheckOptions();

        // Liveness probe - checks if the application is running
        app.MapGet(options.LivenessPath, async (HttpContext context) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new HealthCheckResponse
            {
                Status = "Healthy",
                Duration = 0,
                Checks = new List<HealthCheckEntry>
                {
                    new() { Name = "self", Status = "Healthy", Duration = 0, Tags = new List<string> { "live" } }
                }
            }, JsonOptions);
        });

        // Readiness probe - checks if the application is ready to receive traffic
        app.MapGet(options.ReadinessPath, async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains("ready"));
            await WriteHealthCheckResponse(context, report);
        });

        // Startup probe - checks if the application has started
        app.MapGet(options.StartupPath, async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains("startup") || registration.Tags.Contains("live"));
            await WriteHealthCheckResponse(context, report);
        });

        // Overall health - runs all health checks
        app.MapGet("/health", async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            await WriteHealthCheckResponse(context, report);
        });

        return app;
    }

    /// <summary>
    /// Writes a JSON health check response.
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        
        var statusCode = report.Status == HealthStatus.Healthy ? 200 : 
                        report.Status == HealthStatus.Degraded ? 200 : 503;
        context.Response.StatusCode = statusCode;

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration.TotalMilliseconds,
            Checks = new List<HealthCheckEntry>()
        };

        foreach (var entry in report.Entries)
        {
            response.Checks.Add(new HealthCheckEntry
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration.TotalMilliseconds,
                Description = entry.Value.Description,
                Exception = entry.Value.Exception?.Message,
                Tags = entry.Value.Tags != null ? new List<string>(entry.Value.Tags) : null
            });
        }

        await context.Response.WriteAsJsonAsync(response, JsonOptions);
    }

    /// <summary>
    /// Adds Aspire dashboard support to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The Aspire Dashboard provides real-time observability features:
    /// </para>
    /// <list type="bullet">
    ///   <item>Structured logs with filtering</item>
    ///   <item>Distributed traces visualization</item>
    ///   <item>Metrics graphs and dashboards</item>
    ///   <item>Resource health monitoring</item>
    /// </list>
    /// <para>
    /// The dashboard is automatically available when running with the AppHost.
    /// </para>
    /// </remarks>
    public static WebApplication UseAspireDashboardSupport(this WebApplication app)
    {
        // The Aspire Dashboard is automatically configured when running with the AppHost
        // This method can be used for additional dashboard-related configuration

        // Enable CORS for browser telemetry (Aspire 9 feature)
        app.UseCors(policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

        return app;
    }
}

/// <summary>
/// Health check response model for JSON serialization.
/// </summary>
internal class HealthCheckResponse
{
    /// <summary>
    /// Gets or sets the overall status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Gets or sets the individual health check entries.
    /// </summary>
    public List<HealthCheckEntry> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check entry model.
/// </summary>
internal class HealthCheckEntry
{
    /// <summary>
    /// Gets or sets the check name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check duration in milliseconds.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Gets or sets the check description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the exception message if any.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets the check tags.
    /// </summary>
    public List<string>? Tags { get; set; }
}
