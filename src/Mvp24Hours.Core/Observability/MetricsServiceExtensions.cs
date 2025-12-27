//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Observability.Metrics;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for configuring Mvp24Hours metrics with dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for registering metric classes
/// and configuring metrics-related services in the DI container.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Register all Mvp24Hours metrics
/// services.AddMvp24HoursMetrics();
/// 
/// // Or register specific metrics
/// services.AddMvp24HoursMetrics(options =>
/// {
///     options.EnablePipelineMetrics = true;
///     options.EnableCqrsMetrics = true;
///     options.EnableRepositoryMetrics = true;
///     options.EnableMessagingMetrics = false;
/// });
/// 
/// // With OpenTelemetry:
/// services.AddOpenTelemetry()
///     .WithMetrics(builder =>
///     {
///         builder.AddMvp24HoursMeters();
///     });
/// </code>
/// </remarks>
public static class MetricsServiceExtensions
{
    /// <summary>
    /// Adds all Mvp24Hours metrics services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursMetrics(this IServiceCollection services)
    {
        return services.AddMvp24HoursMetrics(null);
    }

    /// <summary>
    /// Adds Mvp24Hours metrics services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursMetrics(
        this IServiceCollection services,
        Action<MetricsOptions>? configure)
    {
        var options = new MetricsOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Register metrics based on options
        if (options.EnablePipelineMetrics)
        {
            services.AddSingleton<PipelineMetrics>();
        }

        if (options.EnableRepositoryMetrics)
        {
            services.AddSingleton<RepositoryMetrics>();
        }

        if (options.EnableCqrsMetrics)
        {
            services.AddSingleton<CqrsMetrics>();
        }

        if (options.EnableMessagingMetrics)
        {
            services.AddSingleton<MessagingMetrics>();
        }

        if (options.EnableCacheMetrics)
        {
            services.AddSingleton<CacheMetrics>();
        }

        if (options.EnableHttpMetrics)
        {
            services.AddSingleton<HttpMetrics>();
        }

        if (options.EnableCronJobMetrics)
        {
            services.AddSingleton<CronJobMetrics>();
        }

        if (options.EnableInfrastructureMetrics)
        {
            services.AddSingleton<InfrastructureMetrics>();
        }

        return services;
    }

    /// <summary>
    /// Adds Pipeline metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPipelineMetrics(this IServiceCollection services)
    {
        services.AddSingleton<PipelineMetrics>();
        return services;
    }

    /// <summary>
    /// Adds Repository metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositoryMetrics(this IServiceCollection services)
    {
        services.AddSingleton<RepositoryMetrics>();
        return services;
    }

    /// <summary>
    /// Adds CQRS/Mediator metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrsMetrics(this IServiceCollection services)
    {
        services.AddSingleton<CqrsMetrics>();
        return services;
    }

    /// <summary>
    /// Adds Messaging/RabbitMQ metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessagingMetrics(this IServiceCollection services)
    {
        services.AddSingleton<MessagingMetrics>();
        return services;
    }

    /// <summary>
    /// Adds Cache metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCacheMetrics(this IServiceCollection services)
    {
        services.AddSingleton<CacheMetrics>();
        return services;
    }

    /// <summary>
    /// Adds HTTP/WebAPI metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHttpMetrics(this IServiceCollection services)
    {
        services.AddSingleton<HttpMetrics>();
        return services;
    }

    /// <summary>
    /// Adds CronJob metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCronJobMetrics(this IServiceCollection services)
    {
        services.AddSingleton<CronJobMetrics>();
        return services;
    }

    /// <summary>
    /// Adds Infrastructure metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureMetrics(this IServiceCollection services)
    {
        services.AddSingleton<InfrastructureMetrics>();
        return services;
    }
}

/// <summary>
/// Options for configuring Mvp24Hours metrics.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Gets or sets whether to enable Pipeline metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnablePipelineMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Repository/Data metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableRepositoryMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable CQRS/Mediator metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCqrsMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Messaging/RabbitMQ metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMessagingMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Cache metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCacheMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable HTTP/WebAPI metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableHttpMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable CronJob metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCronJobMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Infrastructure metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableInfrastructureMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for metrics.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for metrics.
    /// </summary>
    public string? ServiceVersion { get; set; }
}

/// <summary>
/// Extension methods for OpenTelemetry MeterProviderBuilder to add Mvp24Hours meters.
/// </summary>
/// <remarks>
/// <para>
/// Use these extensions when configuring OpenTelemetry metrics:
/// </para>
/// <code>
/// services.AddOpenTelemetry()
///     .WithMetrics(builder =>
///     {
///         builder
///             .AddMvp24HoursMeters()
///             .AddAspNetCoreInstrumentation()
///             .AddPrometheusExporter();
///     });
/// </code>
/// </remarks>
public static class OpenTelemetryMeterBuilderExtensions
{
    /// <summary>
    /// Gets all Mvp24Hours Meter names.
    /// </summary>
    /// <returns>Array of meter names.</returns>
    public static string[] GetMvp24HoursMeterNames()
    {
        return Mvp24HoursMeters.AllMeterNames;
    }

    /// <summary>
    /// Gets meter names for specific modules.
    /// </summary>
    /// <param name="includeCore">Include Core module.</param>
    /// <param name="includePipe">Include Pipe module.</param>
    /// <param name="includeCqrs">Include CQRS module.</param>
    /// <param name="includeData">Include Data module.</param>
    /// <param name="includeRabbitMQ">Include RabbitMQ module.</param>
    /// <param name="includeWebAPI">Include WebAPI module.</param>
    /// <param name="includeCaching">Include Caching module.</param>
    /// <param name="includeCronJob">Include CronJob module.</param>
    /// <param name="includeInfrastructure">Include Infrastructure module.</param>
    /// <returns>Array of selected meter names.</returns>
    public static string[] GetMvp24HoursMeterNames(
        bool includeCore = true,
        bool includePipe = true,
        bool includeCqrs = true,
        bool includeData = true,
        bool includeRabbitMQ = true,
        bool includeWebAPI = true,
        bool includeCaching = true,
        bool includeCronJob = true,
        bool includeInfrastructure = true)
    {
        var meters = new List<string>();

        if (includeCore) meters.Add(Mvp24HoursMeters.Core.Name);
        if (includePipe) meters.Add(Mvp24HoursMeters.Pipe.Name);
        if (includeCqrs) meters.Add(Mvp24HoursMeters.Cqrs.Name);
        if (includeData) meters.Add(Mvp24HoursMeters.Data.Name);
        if (includeRabbitMQ) meters.Add(Mvp24HoursMeters.RabbitMQ.Name);
        if (includeWebAPI) meters.Add(Mvp24HoursMeters.WebAPI.Name);
        if (includeCaching) meters.Add(Mvp24HoursMeters.Caching.Name);
        if (includeCronJob) meters.Add(Mvp24HoursMeters.CronJob.Name);
        if (includeInfrastructure) meters.Add(Mvp24HoursMeters.Infrastructure.Name);

        return meters.ToArray();
    }
}

