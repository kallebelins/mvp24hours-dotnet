//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability.Extensions;

/// <summary>
/// Extension methods for configuring RabbitMQ observability services.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods to easily configure all observability
/// features for RabbitMQ in your application including:
/// <list type="bullet">
/// <item>OpenTelemetry tracing integration</item>
/// <item>Prometheus-compatible metrics</item>
/// <item>Structured logging with ITelemetryService integration</item>
/// <item>Observer pattern for consume/publish lifecycle</item>
/// <item>Diagnostics and health monitoring</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add all observability features
/// services.AddMvpRabbitMQObservability(options =>
/// {
///     options.EnablePrometheusMetrics = true;
///     options.EnableOpenTelemetry = true;
///     options.EnableEnhancedLogging = true;
///     options.EnableDiagnostics = true;
/// });
///
/// // Or add specific features
/// services.AddMvpRabbitMQPrometheusMetrics();
/// services.AddMvpRabbitMQObservers();
/// services.AddMvpRabbitMQDiagnostics();
/// </code>
/// </example>
public static class ObservabilityServiceExtensions
{
    /// <summary>
    /// Adds all RabbitMQ observability features with configurable options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQObservability(
        this IServiceCollection services,
        Action<RabbitMQObservabilityOptions>? configure = null)
    {
        var options = new RabbitMQObservabilityOptions();
        configure?.Invoke(options);

        if (options.EnablePrometheusMetrics)
        {
            services.AddMvpRabbitMQPrometheusMetrics();
        }
        else if (options.EnableBasicMetrics)
        {
            services.TryAddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
        }

        if (options.EnableObservers)
        {
            services.AddMvpRabbitMQObservers();
        }

        if (options.EnableEnhancedLogging)
        {
            services.AddMvpRabbitMQEnhancedLogging(options.LoggingOptions);
        }

        if (options.EnableDiagnostics)
        {
            services.AddMvpRabbitMQDiagnostics(options.MaxErrorHistory);
        }

        return services;
    }

    /// <summary>
    /// Adds Prometheus-compatible metrics for RabbitMQ using System.Diagnostics.Metrics.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// To export metrics to Prometheus, configure OpenTelemetry:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics =>
    ///     {
    ///         metrics
    ///             .AddMeter(RabbitMQPrometheusMetrics.MeterName)
    ///             .AddPrometheusExporter();
    ///     });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMvpRabbitMQPrometheusMetrics(this IServiceCollection services)
    {
        services.TryAddSingleton<RabbitMQPrometheusMetrics>();
        services.TryAddSingleton<IRabbitMQMetrics>(sp => sp.GetRequiredService<RabbitMQPrometheusMetrics>());
        
        // Also register as observers for automatic metrics collection
        services.AddSingleton<IConsumeObserver>(sp => sp.GetRequiredService<RabbitMQPrometheusMetrics>());
        services.AddSingleton<IPublishObserver>(sp => sp.GetRequiredService<RabbitMQPrometheusMetrics>());

        return services;
    }

    /// <summary>
    /// Adds the observer manager and registers observer coordination.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQObservers(this IServiceCollection services)
    {
        services.TryAddSingleton<IObserverManager>(sp =>
        {
            var consumeObservers = sp.GetServices<IConsumeObserver>();
            var publishObservers = sp.GetServices<IPublishObserver>();
            var sendObservers = sp.GetServices<ISendObserver>();
            var connectionObservers = sp.GetServices<IConnectionObserver>();
            var logger = sp.GetService<ILogger<ObserverManager>>();

            return new ObserverManager(
                consumeObservers,
                publishObservers,
                sendObservers,
                connectionObservers,
                logger);
        });

        return services;
    }

    /// <summary>
    /// Adds a custom consume observer.
    /// </summary>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQConsumeObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IConsumeObserver
    {
        services.AddSingleton<IConsumeObserver, TObserver>();
        return services;
    }

    /// <summary>
    /// Adds a custom publish observer.
    /// </summary>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQPublishObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IPublishObserver
    {
        services.AddSingleton<IPublishObserver, TObserver>();
        return services;
    }

    /// <summary>
    /// Adds a custom send observer.
    /// </summary>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQSendObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, ISendObserver
    {
        services.AddSingleton<ISendObserver, TObserver>();
        return services;
    }

    /// <summary>
    /// Adds a custom connection observer.
    /// </summary>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQConnectionObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IConnectionObserver
    {
        services.AddSingleton<IConnectionObserver, TObserver>();
        return services;
    }

    /// <summary>
    /// Adds enhanced structured logging with ITelemetryService integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Optional logging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQEnhancedLogging(
        this IServiceCollection services,
        EnhancedLoggingOptions? options = null)
    {
        options ??= new EnhancedLoggingOptions();

        services.TryAddSingleton<IRabbitMQStructuredLogger>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EnhancedStructuredLogger>>();
            return new EnhancedStructuredLogger(
                logger,
                options.LogPayload,
                options.MaxPayloadLength,
                options.SensitiveHeaders);
        });

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ diagnostics for health monitoring and status reporting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxErrorHistory">Maximum number of errors to keep in history.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpRabbitMQDiagnostics(
        this IServiceCollection services,
        int maxErrorHistory = 100)
    {
        services.TryAddSingleton<IRabbitMQDiagnostics>(sp =>
        {
            var metrics = sp.GetService<IRabbitMQMetrics>();
            return new RabbitMQDiagnostics(metrics, maxErrorHistory);
        });

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry tracing support for RabbitMQ operations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// After calling this method, you should also configure OpenTelemetry to include
    /// the RabbitMQ activity source:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing =>
    ///     {
    ///         tracing.AddSource(RabbitMQActivitySource.SourceName);
    ///         tracing.AddOtlpExporter();
    ///     });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMvpRabbitMQOpenTelemetry(this IServiceCollection services)
    {
        // The ActivitySource is static and doesn't need to be registered,
        // but we can add configuration services if needed
        return services;
    }

    /// <summary>
    /// Adds all observability features optimized for production use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method configures:
    /// <list type="bullet">
    /// <item>Prometheus-compatible metrics</item>
    /// <item>Observer coordination</item>
    /// <item>Enhanced logging (without payload logging)</item>
    /// <item>Diagnostics with 100 error history</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvpRabbitMQObservabilityProduction(this IServiceCollection services)
    {
        return services.AddMvpRabbitMQObservability(options =>
        {
            options.EnablePrometheusMetrics = true;
            options.EnableObservers = true;
            options.EnableEnhancedLogging = true;
            options.EnableDiagnostics = true;
            options.LoggingOptions = new EnhancedLoggingOptions
            {
                LogPayload = false,
                MaxPayloadLength = 0
            };
        });
    }

    /// <summary>
    /// Adds all observability features optimized for development/debugging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method configures all observability features including payload logging,
    /// which should NOT be used in production with sensitive data.
    /// </remarks>
    public static IServiceCollection AddMvpRabbitMQObservabilityDevelopment(this IServiceCollection services)
    {
        return services.AddMvpRabbitMQObservability(options =>
        {
            options.EnablePrometheusMetrics = true;
            options.EnableObservers = true;
            options.EnableEnhancedLogging = true;
            options.EnableDiagnostics = true;
            options.MaxErrorHistory = 500;
            options.LoggingOptions = new EnhancedLoggingOptions
            {
                LogPayload = true,
                MaxPayloadLength = 5000
            };
        });
    }
}

/// <summary>
/// Configuration options for RabbitMQ observability.
/// </summary>
public class RabbitMQObservabilityOptions
{
    /// <summary>
    /// Gets or sets whether to enable Prometheus-compatible metrics.
    /// Default is true.
    /// </summary>
    public bool EnablePrometheusMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable basic metrics (if Prometheus is disabled).
    /// Default is true.
    /// </summary>
    public bool EnableBasicMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable the observer pattern for lifecycle events.
    /// Default is true.
    /// </summary>
    public bool EnableObservers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable enhanced structured logging.
    /// Default is true.
    /// </summary>
    public bool EnableEnhancedLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable diagnostics and health monitoring.
    /// Default is true.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of errors to keep in history.
    /// Default is 100.
    /// </summary>
    public int MaxErrorHistory { get; set; } = 100;

    /// <summary>
    /// Gets or sets the enhanced logging options.
    /// </summary>
    public EnhancedLoggingOptions LoggingOptions { get; set; } = new();
}

/// <summary>
/// Configuration options for enhanced logging.
/// </summary>
public class EnhancedLoggingOptions
{
    /// <summary>
    /// Gets or sets whether to log message payloads.
    /// Default is false (should be false in production).
    /// </summary>
    public bool LogPayload { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum payload length to log.
    /// Default is 1000 characters.
    /// </summary>
    public int MaxPayloadLength { get; set; } = 1000;

    /// <summary>
    /// Gets or sets headers that should be masked in logs.
    /// </summary>
    public string[] SensitiveHeaders { get; set; } = new[]
    {
        "Authorization",
        "x-api-key",
        "password",
        "secret",
        "token"
    };
}

