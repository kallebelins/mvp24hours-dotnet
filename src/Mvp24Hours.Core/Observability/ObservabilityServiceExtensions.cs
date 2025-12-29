//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Unified extension methods for configuring all Mvp24Hours observability features.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a single entry point for configuring logging, tracing, and metrics
/// following the three pillars of observability. It integrates with OpenTelemetry for
/// industry-standard telemetry data collection and export.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Configure all observability features at once
/// services.AddMvp24HoursObservability(options =>
/// {
///     options.ServiceName = "MyService";
///     options.ServiceVersion = "1.0.0";
///     options.Environment = "Production";
///     
///     // Enable all pillars
///     options.EnableLogging = true;
///     options.EnableTracing = true;
///     options.EnableMetrics = true;
///     
///     // Configure specific features
///     options.Logging.EnableTraceCorrelation = true;
///     options.Tracing.EnableCorrelationIdPropagation = true;
/// });
/// 
/// // Then configure OpenTelemetry exporters:
/// services.AddOpenTelemetry()
///     .WithLogging(builder => builder.AddOtlpExporter())
///     .WithTracing(builder => builder.AddMvp24HoursSources().AddOtlpExporter())
///     .WithMetrics(builder => builder.AddMvp24HoursMeters().AddPrometheusExporter());
/// </code>
/// </remarks>
public static class ObservabilityServiceExtensions
{
    /// <summary>
    /// Adds all Mvp24Hours observability services (logging, tracing, metrics).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursObservability(
        this IServiceCollection services,
        Action<ObservabilityOptions>? configure = null)
    {
        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        // Register unified options
        services.AddSingleton(options);

        // Configure Logging
        if (options.EnableLogging)
        {
            services.AddMvp24HoursLogging(loggingOptions =>
            {
                loggingOptions.ServiceName = options.ServiceName;
                loggingOptions.ServiceVersion = options.ServiceVersion;
                loggingOptions.Environment = options.Environment;
                loggingOptions.EnableTraceCorrelation = options.Logging.EnableTraceCorrelation;
                loggingOptions.EnableLogSampling = options.Logging.EnableLogSampling;
                loggingOptions.SamplingRatio = options.Logging.SamplingRatio;
                loggingOptions.EnableUserContextEnrichment = options.Logging.EnableUserContextEnrichment;
                loggingOptions.EnableTenantContextEnrichment = options.Logging.EnableTenantContextEnrichment;
            });
        }

        // Configure Tracing
        if (options.EnableTracing)
        {
            services.AddMvp24HoursTracing(tracingOptions =>
            {
                tracingOptions.ServiceName = options.ServiceName;
                tracingOptions.ServiceVersion = options.ServiceVersion;
                tracingOptions.EnableCorrelationIdPropagation = options.Tracing.EnableCorrelationIdPropagation;
                tracingOptions.EnableUserContext = options.Tracing.EnableUserContext;
                tracingOptions.EnableTenantContext = options.Tracing.EnableTenantContext;

                // Add default enrichers if configured
                if (options.Tracing.AddDefaultEnrichers)
                {
                    tracingOptions.AddEnricher(new CorrelationIdEnricher());
                    tracingOptions.AddEnricher(new UserContextEnricher());
                    tracingOptions.AddEnricher(new TenantContextEnricher());
                }
            });
        }

        // Configure Metrics
        if (options.EnableMetrics)
        {
            services.AddMvp24HoursMetrics(metricsOptions =>
            {
                metricsOptions.ServiceName = options.ServiceName;
                metricsOptions.ServiceVersion = options.ServiceVersion;
                metricsOptions.EnablePipelineMetrics = options.Metrics.EnablePipelineMetrics;
                metricsOptions.EnableRepositoryMetrics = options.Metrics.EnableRepositoryMetrics;
                metricsOptions.EnableCqrsMetrics = options.Metrics.EnableCqrsMetrics;
                metricsOptions.EnableMessagingMetrics = options.Metrics.EnableMessagingMetrics;
                metricsOptions.EnableCacheMetrics = options.Metrics.EnableCacheMetrics;
                metricsOptions.EnableHttpMetrics = options.Metrics.EnableHttpMetrics;
                metricsOptions.EnableCronJobMetrics = options.Metrics.EnableCronJobMetrics;
                metricsOptions.EnableInfrastructureMetrics = options.Metrics.EnableInfrastructureMetrics;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds Mvp24Hours observability with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new ObservabilityOptions();
        configuration.GetSection("Mvp24Hours:Observability").Bind(options);

        return services.AddMvp24HoursObservability(opt =>
        {
            opt.ServiceName = options.ServiceName;
            opt.ServiceVersion = options.ServiceVersion;
            opt.Environment = options.Environment;
            opt.EnableLogging = options.EnableLogging;
            opt.EnableTracing = options.EnableTracing;
            opt.EnableMetrics = options.EnableMetrics;
            
            // Copy nested options
            opt.Logging = options.Logging;
            opt.Tracing = options.Tracing;
            opt.Metrics = options.Metrics;
        });
    }

    /// <summary>
    /// Configures ILoggingBuilder with Mvp24Hours observability defaults.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="configure">Optional additional configuration.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddMvp24HoursObservability(
        this ILoggingBuilder builder,
        string serviceName,
        Action<LoggingOptions>? configure = null)
    {
        var options = new LoggingOptions
        {
            ServiceName = serviceName
        };
        configure?.Invoke(options);

        // Configure activity tracking for trace correlation
        if (options.EnableTraceCorrelation)
        {
            builder.Configure(loggerOptions =>
            {
                loggerOptions.ActivityTrackingOptions =
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.ParentId |
                    ActivityTrackingOptions.Baggage |
                    ActivityTrackingOptions.Tags;
            });
        }

        return builder;
    }
}

/// <summary>
/// Unified options for Mvp24Hours observability configuration.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Gets or sets the service name for all telemetry data.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for all telemetry data.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment (e.g., Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets whether to enable logging configuration.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable tracing configuration.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics configuration.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the logging-specific options.
    /// </summary>
    public ObservabilityLoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the tracing-specific options.
    /// </summary>
    public ObservabilityTracingOptions Tracing { get; set; } = new();

    /// <summary>
    /// Gets or sets the metrics-specific options.
    /// </summary>
    public ObservabilityMetricsOptions Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets custom resource attributes to add to all telemetry.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}

/// <summary>
/// Logging-specific options within observability configuration.
/// </summary>
public class ObservabilityLoggingOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic trace correlation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTraceCorrelation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable log sampling.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableLogSampling { get; set; } = false;

    /// <summary>
    /// Gets or sets the sampling ratio (0.0 to 1.0).
    /// Default is 1.0 (sample all).
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether to enable user context enrichment.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableUserContextEnrichment { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable tenant context enrichment.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTenantContextEnrichment { get; set; } = true;
}

/// <summary>
/// Tracing-specific options within observability configuration.
/// </summary>
public class ObservabilityTracingOptions
{
    /// <summary>
    /// Gets or sets whether to enable correlation ID propagation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCorrelationIdPropagation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add user context to traces.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableUserContext { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add tenant context to traces.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTenantContext { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add default enrichers automatically.
    /// Default is <c>true</c>.
    /// </summary>
    public bool AddDefaultEnrichers { get; set; } = true;
}

/// <summary>
/// Metrics-specific options within observability configuration.
/// </summary>
public class ObservabilityMetricsOptions
{
    /// <summary>
    /// Gets or sets whether to enable Pipeline metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnablePipelineMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Repository metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableRepositoryMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable CQRS metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCqrsMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Messaging metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMessagingMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Cache metrics.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCacheMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable HTTP metrics.
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
}


