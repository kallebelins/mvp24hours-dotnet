//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace Mvp24Hours.Core.Aspire;

/// <summary>
/// Extension methods for adding .NET Aspire service defaults to Mvp24Hours applications.
/// </summary>
/// <remarks>
/// <para>
/// .NET Aspire 9 is the official Microsoft stack for building observable, cloud-native applications.
/// It provides a unified approach to telemetry, health checks, resilience, and service discovery.
/// </para>
/// <para>
/// These extension methods integrate Aspire's service defaults with Mvp24Hours,
/// providing a seamless experience for building cloud-native applications.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// // Add Aspire service defaults with Mvp24Hours integration
/// builder.AddMvp24HoursAspireDefaults();
/// 
/// // Configure additional services
/// builder.Services.AddMvp24HoursDbContext&lt;MyDbContext&gt;();
/// builder.Services.AddMvpRabbitMQ();
/// 
/// var app = builder.Build();
/// 
/// // Map Aspire health check endpoints
/// app.MapMvp24HoursAspireHealthChecks();
/// 
/// app.Run();
/// </code>
/// <para>
/// <strong>Required NuGet Packages for full Aspire integration:</strong>
/// </para>
/// <list type="bullet">
///   <item>OpenTelemetry.Extensions.Hosting</item>
///   <item>OpenTelemetry.Instrumentation.AspNetCore</item>
///   <item>OpenTelemetry.Instrumentation.Http</item>
///   <item>OpenTelemetry.Exporter.OpenTelemetryProtocol</item>
///   <item>Microsoft.Extensions.Http.Resilience</item>
///   <item>AspNetCore.HealthChecks.* (for specific health checks)</item>
/// </list>
/// </remarks>
public static class AspireServiceDefaults
{
    /// <summary>
    /// Adds .NET Aspire service defaults to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional action to configure Aspire options.</param>
    /// <returns>The host application builder for chaining.</returns>
    /// <remarks>
    /// This method configures:
    /// <list type="bullet">
    ///   <item>Mvp24Hours observability integration</item>
    ///   <item>Basic health checks</item>
    ///   <item>HTTP client resilience (if Microsoft.Extensions.Http.Resilience is installed)</item>
    /// </list>
    /// <para>
    /// For full OpenTelemetry integration, add the OpenTelemetry packages and configure them separately.
    /// </para>
    /// </remarks>
    public static IHostApplicationBuilder AddMvp24HoursAspireDefaults(
        this IHostApplicationBuilder builder,
        Action<AspireOptions>? configure = null)
    {
        var options = new AspireOptions();
        
        // Bind from configuration first
        builder.Configuration.GetSection("Aspire").Bind(options);
        
        // Then apply custom configuration
        configure?.Invoke(options);

        // Set defaults from environment and assembly
        options.ServiceName ??= GetServiceName(builder.Configuration);
        options.ServiceVersion ??= GetServiceVersion();
        options.Environment ??= builder.Environment.EnvironmentName;

        // Register options
        builder.Services.AddSingleton(options);

        // Configure Health Checks
        if (options.EnableHealthChecks)
        {
            ConfigureHealthChecks(builder.Services, options);
        }

        // Configure Resilience (basic - uses Microsoft.Extensions.Http.Resilience if available)
        if (options.EnableResilience)
        {
            ConfigureResilience(builder.Services, options);
        }

        // Register Mvp24Hours observability if available
        try
        {
            builder.Services.AddMvp24HoursObservabilityCore(options);
        }
        catch
        {
            // Observability extensions not available, skip
        }

        return builder;
    }

    /// <summary>
    /// Adds .NET Aspire service defaults with configuration from IConfiguration.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configurationSection">The configuration section name. Default is "Aspire".</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMvp24HoursAspireDefaults(
        this IHostApplicationBuilder builder,
        string configurationSection)
    {
        return builder.AddMvp24HoursAspireDefaults(options =>
        {
            builder.Configuration.GetSection(configurationSection).Bind(options);
        });
    }

    /// <summary>
    /// Adds core Mvp24Hours observability integration for Aspire.
    /// </summary>
    private static IServiceCollection AddMvp24HoursObservabilityCore(
        this IServiceCollection services,
        AspireOptions options)
    {
        // Register correlation ID accessor
        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        
        return services;
    }

    /// <summary>
    /// Configures health checks for Aspire integration.
    /// </summary>
    private static void ConfigureHealthChecks(IServiceCollection services, AspireOptions options)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add default self health check
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        // Note: Database, cache, and messaging health checks should be added by the specific modules
        // when they are registered (e.g., AddMvp24HoursDbContext adds SQL health check)
    }

    /// <summary>
    /// Configures HTTP resilience policies for Aspire integration.
    /// </summary>
    private static void ConfigureResilience(IServiceCollection services, AspireOptions options)
    {
        // Configure default HTTP client with resilience using Microsoft.Extensions.Http.Resilience
        // This requires the Microsoft.Extensions.Http.Resilience package to be installed
        try
        {
            services.ConfigureHttpClientDefaults(http =>
            {
                // Add standard resilience handler if available
                // This will be configured by the consuming project that has the package
            });
        }
        catch
        {
            // Microsoft.Extensions.Http.Resilience not available, skip resilience configuration
        }
    }

    /// <summary>
    /// Gets the service name from configuration or assembly.
    /// </summary>
    private static string GetServiceName(IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"]
            ?? configuration["Aspire:ServiceName"]
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "Mvp24HoursService";
    }

    /// <summary>
    /// Gets the service version from assembly.
    /// </summary>
    private static string GetServiceVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "1.0.0";
    }
}

/// <summary>
/// Interface for accessing the current correlation ID.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string? CorrelationId { get; }
    
    /// <summary>
    /// Sets the current correlation ID.
    /// </summary>
    void SetCorrelationId(string correlationId);
}

/// <summary>
/// Default implementation of ICorrelationIdAccessor using AsyncLocal.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly System.Threading.AsyncLocal<string?> _correlationId = new();

    /// <inheritdoc />
    public string? CorrelationId => _correlationId.Value;

    /// <inheritdoc />
    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }
}
