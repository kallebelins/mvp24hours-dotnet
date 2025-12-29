//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for configuring OpenTelemetry Logging integration with Mvp24Hours.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a streamlined way to configure OpenTelemetry Logging
/// with OTLP export, trace correlation, and Mvp24Hours-specific defaults.
/// </para>
/// <para>
/// <strong>Usage with OpenTelemetry SDK:</strong>
/// </para>
/// <code>
/// // In Program.cs with OpenTelemetry SDK installed:
/// builder.Logging.AddOpenTelemetry(options =>
/// {
///     // Use Mvp24Hours defaults
///     options.ApplyMvp24HoursDefaults("MyService", "1.0.0");
///     
///     // Add OTLP exporter
///     options.AddOtlpExporter(otlp =>
///     {
///         otlp.Endpoint = new Uri("http://localhost:4317");
///     });
/// });
/// 
/// // Or using the all-in-one extension:
/// builder.Services.AddMvp24HoursOpenTelemetryLogging(options =>
/// {
///     options.ServiceName = "MyService";
///     options.EnableOtlpExporter = true;
///     options.OtlpEndpoint = "http://localhost:4317";
/// });
/// </code>
/// </remarks>
public static class OpenTelemetryLoggingBuilderExtensions
{
    /// <summary>
    /// Adds Mvp24Hours OpenTelemetry logging configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures ILogger with OpenTelemetry-compatible settings including:
    /// </para>
    /// <list type="bullet">
    /// <item>Automatic trace correlation (TraceId, SpanId)</item>
    /// <item>Standardized resource attributes</item>
    /// <item>Log sampling for high-load environments</item>
    /// <item>Namespace-based log level filtering</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursOpenTelemetryLogging(
        this IServiceCollection services,
        Action<OpenTelemetryLoggingOptions>? configure = null)
    {
        var options = new OpenTelemetryLoggingOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Add base Mvp24Hours logging services
        services.AddMvp24HoursLogging(loggingOptions =>
        {
            loggingOptions.ServiceName = options.ServiceName;
            loggingOptions.ServiceVersion = options.ServiceVersion;
            loggingOptions.Environment = options.Environment;
            loggingOptions.EnableTraceCorrelation = options.EnableTraceCorrelation;
            loggingOptions.EnableLogSampling = options.EnableLogSampling;
            loggingOptions.SamplingRatio = options.SamplingRatio;
            loggingOptions.EnableUserContextEnrichment = options.EnableUserContextEnrichment;
            loggingOptions.EnableTenantContextEnrichment = options.EnableTenantContextEnrichment;
        });

        // Configure ILogger with trace correlation
        services.AddLogging(builder =>
        {
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

            // Add namespace filtering if configuration is provided
            if (options.Configuration != null)
            {
                builder.AddNamespaceFiltering(options.Configuration);
            }

            // Apply minimum log level
            if (options.MinimumLevel.HasValue)
            {
                builder.SetMinimumLevel(options.MinimumLevel.Value);
            }
        });

        return services;
    }

    /// <summary>
    /// Adds Mvp24Hours OpenTelemetry logging with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursOpenTelemetryLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new OpenTelemetryLoggingOptions();
        configuration.GetSection("Mvp24Hours:OpenTelemetry:Logging").Bind(options);
        options.Configuration = configuration;

        return services.AddMvp24HoursOpenTelemetryLogging(opt =>
        {
            opt.ServiceName = options.ServiceName;
            opt.ServiceVersion = options.ServiceVersion;
            opt.Environment = options.Environment;
            opt.EnableTraceCorrelation = options.EnableTraceCorrelation;
            opt.EnableLogSampling = options.EnableLogSampling;
            opt.SamplingRatio = options.SamplingRatio;
            opt.EnableUserContextEnrichment = options.EnableUserContextEnrichment;
            opt.EnableTenantContextEnrichment = options.EnableTenantContextEnrichment;
            opt.EnableOtlpExporter = options.EnableOtlpExporter;
            opt.OtlpEndpoint = options.OtlpEndpoint;
            opt.IncludeFormattedMessage = options.IncludeFormattedMessage;
            opt.IncludeScopes = options.IncludeScopes;
            opt.ParseStateValues = options.ParseStateValues;
            opt.Configuration = configuration;
        });
    }
}

/// <summary>
/// Options for configuring OpenTelemetry Logging integration.
/// </summary>
public class OpenTelemetryLoggingOptions
{
    /// <summary>
    /// Gets or sets the service name for logs.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for logs.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic trace correlation (TraceId, SpanId).
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTraceCorrelation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable log sampling for high-load environments.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableLogSampling { get; set; } = false;

    /// <summary>
    /// Gets or sets the sampling ratio when log sampling is enabled.
    /// Value between 0.0 (sample none) and 1.0 (sample all).
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

    /// <summary>
    /// Gets or sets whether to enable OTLP exporter.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;

    /// <summary>
    /// Gets or sets the OTLP endpoint for log export.
    /// Default is <c>http://localhost:4317</c>.
    /// </summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets whether to include the formatted message in log records.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeFormattedMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include logging scopes in log records.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to parse state values into individual attributes.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ParseStateValues { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level.
    /// If not set, uses the configured default.
    /// </summary>
    public LogLevel? MinimumLevel { get; set; }

    /// <summary>
    /// Gets or sets custom resource attributes to add to all logs.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for namespace-based log filtering.
    /// </summary>
    public IConfiguration? Configuration { get; set; }
}

/// <summary>
/// Helper class for OpenTelemetry-specific log record attributes.
/// </summary>
/// <remarks>
/// <para>
/// Use these attributes when configuring OpenTelemetry LoggerProvider to ensure
/// consistent resource attributes across all logs.
/// </para>
/// </remarks>
public static class OtlpLogRecordAttributes
{
    /// <summary>
    /// Gets log record attributes for use with OpenTelemetry logging configuration.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="serviceVersion">Optional service version.</param>
    /// <param name="environment">Optional deployment environment.</param>
    /// <returns>Dictionary of attributes suitable for OpenTelemetry configuration.</returns>
    public static Dictionary<string, object> GetResourceAttributes(
        string serviceName,
        string? serviceVersion = null,
        string? environment = null)
    {
        return OpenTelemetryLoggingExtensions.GetMvp24HoursResourceAttributes(
            serviceName,
            serviceVersion,
            environment);
    }

    /// <summary>
    /// Gets recommended OpenTelemetry LoggerProviderBuilder configuration values.
    /// </summary>
    /// <returns>Configuration helper with recommended values.</returns>
    public static OpenTelemetryLoggingConfig GetRecommendedConfig()
    {
        return OpenTelemetryLoggingExtensions.GetRecommendedConfig();
    }
}

/// <summary>
/// Extension methods for ILoggingBuilder to add OpenTelemetry-compatible configuration.
/// </summary>
public static class LoggingBuilderOpenTelemetryExtensions
{
    /// <summary>
    /// Adds Mvp24Hours OpenTelemetry-compatible logging configuration.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="serviceName">The service name for resource attributes.</param>
    /// <param name="configure">Optional additional configuration.</param>
    /// <returns>The logging builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the logging builder with:
    /// </para>
    /// <list type="bullet">
    /// <item>Activity tracking for automatic trace correlation</item>
    /// <item>Proper scope handling for structured logging</item>
    /// <item>OpenTelemetry-compatible log enrichment</item>
    /// </list>
    /// <para>
    /// <strong>Usage:</strong>
    /// </para>
    /// <code>
    /// builder.Logging
    ///     .AddMvp24HoursOpenTelemetryConfig("MyService")
    ///     .AddOpenTelemetry(options =>
    ///     {
    ///         options.AddOtlpExporter();
    ///     });
    /// </code>
    /// </remarks>
    public static ILoggingBuilder AddMvp24HoursOpenTelemetryConfig(
        this ILoggingBuilder builder,
        string serviceName,
        Action<OpenTelemetryLoggingOptions>? configure = null)
    {
        var options = new OpenTelemetryLoggingOptions
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

        // Apply minimum log level
        if (options.MinimumLevel.HasValue)
        {
            builder.SetMinimumLevel(options.MinimumLevel.Value);
        }

        return builder;
    }

    /// <summary>
    /// Configures log levels by namespace from configuration.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configuration">The configuration containing log level settings.</param>
    /// <returns>The logging builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Configuration format in appsettings.json:
    /// </para>
    /// <code>
    /// {
    ///   "Logging": {
    ///     "LogLevel": {
    ///       "Default": "Information",
    ///       "Microsoft": "Warning",
    ///       "Microsoft.EntityFrameworkCore": "Warning",
    ///       "Mvp24Hours": "Debug",
    ///       "Mvp24Hours.Core": "Information",
    ///       "Mvp24Hours.Cqrs": "Debug"
    ///     }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static ILoggingBuilder ConfigureMvp24HoursLogLevels(
        this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        return builder.AddNamespaceFiltering(configuration);
    }

    /// <summary>
    /// Applies Mvp24Hours-recommended log level settings for development.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder ApplyMvp24HoursDevelopmentDefaults(this ILoggingBuilder builder)
    {
        return builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddFilter("Microsoft", LogLevel.Information)
            .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
            .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("Mvp24Hours", LogLevel.Debug);
    }

    /// <summary>
    /// Applies Mvp24Hours-recommended log level settings for production.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder ApplyMvp24HoursProductionDefaults(this ILoggingBuilder builder)
    {
        return builder
            .SetMinimumLevel(LogLevel.Information)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error)
            .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("Mvp24Hours", LogLevel.Information);
    }
}

/// <summary>
/// Provides extension methods for creating structured log entries with OpenTelemetry-compatible attributes.
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Logs an informational message with automatic trace context.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogInformationWithTrace(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        using (logger.BeginTraceScope())
        {
            logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a warning message with automatic trace context.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogWarningWithTrace(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        using (logger.BeginTraceScope())
        {
            logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs an error message with automatic trace context and structured exception details.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogErrorWithTrace(
        this ILogger logger,
        Exception exception,
        string message,
        params object?[] args)
    {
        using (logger.BeginTraceScope())
        using (LogScopeFactory.BeginErrorScope(logger, exception))
        {
            logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Logs a critical message with automatic trace context and structured exception details.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogCriticalWithTrace(
        this ILogger logger,
        Exception exception,
        string message,
        params object?[] args)
    {
        using (logger.BeginTraceScope())
        using (LogScopeFactory.BeginErrorScope(logger, exception))
        {
            logger.LogCritical(exception, message, args);
        }
    }

    /// <summary>
    /// Logs an HTTP request with structured attributes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void LogHttpRequest(
        this ILogger logger,
        string method,
        string path,
        int statusCode,
        long durationMs)
    {
        using (LogScopeFactory.BeginHttpScope(logger, method, path))
        {
            logger.LogInformation(
                "HTTP {HttpMethod} {UrlPath} responded {HttpStatusCode} in {DurationMs}ms",
                method,
                path,
                statusCode,
                durationMs);
        }
    }

    /// <summary>
    /// Logs a database operation with structured attributes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbSystem">Database system name.</param>
    /// <param name="operation">Operation type.</param>
    /// <param name="table">Table name.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="rowsAffected">Optional rows affected count.</param>
    public static void LogDatabaseOperation(
        this ILogger logger,
        string dbSystem,
        string operation,
        string table,
        long durationMs,
        int? rowsAffected = null)
    {
        using (LogScopeFactory.BeginDbScope(logger, dbSystem, operation, table))
        {
            if (rowsAffected.HasValue)
            {
                logger.LogInformation(
                    "Database {DbOperation} on {DbSqlTable} completed in {DurationMs}ms, {DbRowsAffected} rows affected",
                    operation,
                    table,
                    durationMs,
                    rowsAffected.Value);
            }
            else
            {
                logger.LogInformation(
                    "Database {DbOperation} on {DbSqlTable} completed in {DurationMs}ms",
                    operation,
                    table,
                    durationMs);
            }
        }
    }

    /// <summary>
    /// Logs a messaging operation with structured attributes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="system">Messaging system.</param>
    /// <param name="destination">Queue or topic name.</param>
    /// <param name="operation">Operation (publish, consume).</param>
    /// <param name="messageId">Optional message ID.</param>
    public static void LogMessagingOperation(
        this ILogger logger,
        string system,
        string destination,
        string operation,
        string? messageId = null)
    {
        using (LogScopeFactory.BeginMessagingScope(logger, system, destination, messageId))
        {
            logger.LogInformation(
                "Messaging {Operation} to {MessagingDestinationName} via {MessagingSystem}",
                operation,
                destination,
                system);
        }
    }

    /// <summary>
    /// Logs a CQRS/Mediator request with structured attributes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requestName">Request type name.</param>
    /// <param name="requestType">Request type (Command, Query, Notification).</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the request succeeded.</param>
    public static void LogMediatorRequest(
        this ILogger logger,
        string requestName,
        string requestType,
        long durationMs,
        bool success)
    {
        using (LogScopeFactory.BeginMediatorScope(logger, requestName, requestType))
        {
            if (success)
            {
                logger.LogInformation(
                    "Mediator {MediatorRequestType} {MediatorRequestName} completed successfully in {DurationMs}ms",
                    requestType,
                    requestName,
                    durationMs);
            }
            else
            {
                logger.LogWarning(
                    "Mediator {MediatorRequestType} {MediatorRequestName} failed after {DurationMs}ms",
                    requestType,
                    requestName,
                    durationMs);
            }
        }
    }
}



