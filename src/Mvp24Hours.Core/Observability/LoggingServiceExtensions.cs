//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for configuring Mvp24Hours logging with OpenTelemetry integration.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for configuring structured logging
/// with automatic correlation to traces (TraceId, SpanId), standardized attributes,
/// and integration with OpenTelemetry Logs.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Configure logging with Mvp24Hours defaults
/// services.AddMvp24HoursLogging(options =>
/// {
///     options.ServiceName = "MyService";
///     options.ServiceVersion = "1.0.0";
///     options.EnableTraceCorrelation = true;
///     options.EnableLogSampling = false;
/// });
/// 
/// // With OpenTelemetry:
/// builder.Logging.AddOpenTelemetry(options =>
/// {
///     options.AddMvp24HoursDefaults();
///     options.AddOtlpExporter();
/// });
/// </code>
/// </remarks>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Adds Mvp24Hours logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursLogging(
        this IServiceCollection services,
        Action<LoggingOptions>? configure = null)
    {
        var options = new LoggingOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Register log enricher
        services.AddSingleton<ILogEnricher, TraceContextLogEnricher>();
        
        if (options.EnableUserContextEnrichment)
        {
            services.AddSingleton<ILogEnricher, UserContextLogEnricher>();
        }

        if (options.EnableTenantContextEnrichment)
        {
            services.AddSingleton<ILogEnricher, TenantContextLogEnricher>();
        }

        // Register composite enricher
        services.AddSingleton<ILogEnricher>(sp =>
        {
            var enrichers = sp.GetServices<ILogEnricher>();
            return new CompositeLogEnricher(enrichers);
        });

        // Register log context accessor
        services.AddSingleton<ILogContextAccessor, LogContextAccessor>();

        // Register log sampler if enabled
        if (options.EnableLogSampling)
        {
            services.AddSingleton<ILogSampler>(new RatioBasedLogSampler(options.SamplingRatio));
        }

        return services;
    }

    /// <summary>
    /// Adds Mvp24Hours logging with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new LoggingOptions();
        configuration.GetSection("Mvp24Hours:Logging").Bind(options);

        return services.AddMvp24HoursLogging(opt =>
        {
            opt.ServiceName = options.ServiceName;
            opt.ServiceVersion = options.ServiceVersion;
            opt.EnableTraceCorrelation = options.EnableTraceCorrelation;
            opt.EnableLogSampling = options.EnableLogSampling;
            opt.SamplingRatio = options.SamplingRatio;
            opt.EnableUserContextEnrichment = options.EnableUserContextEnrichment;
            opt.EnableTenantContextEnrichment = options.EnableTenantContextEnrichment;
        });
    }

    /// <summary>
    /// Configures logging builder with Mvp24Hours defaults.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddMvp24HoursDefaults(
        this ILoggingBuilder builder,
        Action<LoggingOptions>? configure = null)
    {
        var options = new LoggingOptions();
        configure?.Invoke(options);

        // Add trace correlation enricher
        if (options.EnableTraceCorrelation)
        {
            builder.Configure(loggerOptions =>
            {
                // Configure activity tracking
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

    /// <summary>
    /// Configures log level filtering based on namespace patterns from configuration.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configuration">The configuration section for log levels.</param>
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
    public static ILoggingBuilder AddNamespaceFiltering(
        this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        var logLevelSection = configuration.GetSection("Logging:LogLevel");
        
        foreach (var child in logLevelSection.GetChildren())
        {
            if (Enum.TryParse<LogLevel>(child.Value, out var level))
            {
                builder.AddFilter(child.Key, level);
            }
        }

        return builder;
    }
}

/// <summary>
/// Options for configuring Mvp24Hours logging.
/// </summary>
public class LoggingOptions
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
    /// Gets or sets the environment name for logs.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets custom resource attributes to add to all logs.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}

/// <summary>
/// Interface for enriching log entries with additional context.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Enriches a log scope with additional properties.
    /// </summary>
    /// <returns>A dictionary of properties to add to the log scope.</returns>
    Dictionary<string, object?> GetEnrichmentProperties();
}

/// <summary>
/// Log enricher that adds trace context (TraceId, SpanId) to logs.
/// </summary>
public class TraceContextLogEnricher : ILogEnricher
{
    /// <inheritdoc />
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var activity = Activity.Current;
        var properties = new Dictionary<string, object?>();

        if (activity != null)
        {
            properties["TraceId"] = activity.TraceId.ToString();
            properties["SpanId"] = activity.SpanId.ToString();
            properties["ParentSpanId"] = activity.ParentSpanId.ToString();

            // Add correlation ID from baggage if available
            var correlationId = activity.GetBaggageItem("correlation.id");
            if (!string.IsNullOrEmpty(correlationId))
            {
                properties[SemanticTags.CorrelationId] = correlationId;
            }

            // Add causation ID from baggage if available
            var causationId = activity.GetBaggageItem("causation.id");
            if (!string.IsNullOrEmpty(causationId))
            {
                properties[SemanticTags.CausationId] = causationId;
            }
        }

        return properties;
    }
}

/// <summary>
/// Log enricher that adds user context to logs.
/// </summary>
public class UserContextLogEnricher : ILogEnricher
{
    /// <inheritdoc />
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var activity = Activity.Current;
        var properties = new Dictionary<string, object?>();

        if (activity != null)
        {
            var userId = activity.GetBaggageItem("enduser.id");
            if (!string.IsNullOrEmpty(userId))
            {
                properties[SemanticTags.EnduserId] = userId;
            }

            var userName = activity.GetBaggageItem("enduser.name");
            if (!string.IsNullOrEmpty(userName))
            {
                properties[SemanticTags.EnduserName] = userName;
            }

            var userRoles = activity.GetBaggageItem("enduser.roles");
            if (!string.IsNullOrEmpty(userRoles))
            {
                properties[SemanticTags.EnduserRoles] = userRoles;
            }
        }

        return properties;
    }
}

/// <summary>
/// Log enricher that adds tenant context to logs.
/// </summary>
public class TenantContextLogEnricher : ILogEnricher
{
    /// <inheritdoc />
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var activity = Activity.Current;
        var properties = new Dictionary<string, object?>();

        if (activity != null)
        {
            var tenantId = activity.GetBaggageItem("tenant.id");
            if (!string.IsNullOrEmpty(tenantId))
            {
                properties[SemanticTags.TenantId] = tenantId;
            }

            var tenantName = activity.GetBaggageItem("tenant.name");
            if (!string.IsNullOrEmpty(tenantName))
            {
                properties[SemanticTags.TenantName] = tenantName;
            }
        }

        return properties;
    }
}

/// <summary>
/// Composite log enricher that combines multiple enrichers.
/// </summary>
public class CompositeLogEnricher : ILogEnricher
{
    private readonly IEnumerable<ILogEnricher> _enrichers;

    /// <summary>
    /// Initializes a new instance of <see cref="CompositeLogEnricher"/>.
    /// </summary>
    /// <param name="enrichers">The enrichers to compose.</param>
    public CompositeLogEnricher(IEnumerable<ILogEnricher> enrichers)
    {
        _enrichers = enrichers;
    }

    /// <inheritdoc />
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var combined = new Dictionary<string, object?>();

        foreach (var enricher in _enrichers)
        {
            var properties = enricher.GetEnrichmentProperties();
            foreach (var prop in properties)
            {
                combined[prop.Key] = prop.Value;
            }
        }

        return combined;
    }
}

/// <summary>
/// Interface for accessing log context information.
/// </summary>
public interface ILogContextAccessor
{
    /// <summary>
    /// Gets the current trace ID.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// Gets the current span ID.
    /// </summary>
    string? SpanId { get; }

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Creates a logging scope with trace context.
    /// </summary>
    /// <param name="logger">The logger to create scope for.</param>
    /// <returns>An IDisposable scope.</returns>
    IDisposable? BeginTraceScope(ILogger logger);

    /// <summary>
    /// Gets all enrichment properties for the current context.
    /// </summary>
    /// <returns>Dictionary of properties.</returns>
    Dictionary<string, object?> GetEnrichmentProperties();
}

/// <summary>
/// Default implementation of <see cref="ILogContextAccessor"/>.
/// </summary>
public class LogContextAccessor : ILogContextAccessor
{
    private readonly ILogEnricher? _enricher;

    /// <summary>
    /// Initializes a new instance of <see cref="LogContextAccessor"/>.
    /// </summary>
    public LogContextAccessor() { }

    /// <summary>
    /// Initializes a new instance of <see cref="LogContextAccessor"/> with an enricher.
    /// </summary>
    /// <param name="enricher">The log enricher.</param>
    public LogContextAccessor(ILogEnricher enricher)
    {
        _enricher = enricher;
    }

    /// <inheritdoc />
    public string? TraceId => Activity.Current?.TraceId.ToString();

    /// <inheritdoc />
    public string? SpanId => Activity.Current?.SpanId.ToString();

    /// <inheritdoc />
    public string? CorrelationId
    {
        get
        {
            var activity = Activity.Current;
            if (activity == null) return null;

            return activity.GetBaggageItem("correlation.id")
                   ?? activity.TraceId.ToString();
        }
    }

    /// <inheritdoc />
    public IDisposable? BeginTraceScope(ILogger logger)
    {
        var properties = GetEnrichmentProperties();
        if (properties.Count == 0) return null;

        return logger.BeginScope(properties);
    }

    /// <inheritdoc />
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        return _enricher?.GetEnrichmentProperties() ?? new Dictionary<string, object?>();
    }
}

/// <summary>
/// Interface for log sampling to reduce log volume in high-load environments.
/// </summary>
public interface ILogSampler
{
    /// <summary>
    /// Determines if a log entry should be sampled (included).
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="categoryName">The logger category name.</param>
    /// <returns>True if the log should be included, false to drop.</returns>
    bool ShouldSample(LogLevel logLevel, string categoryName);
}

/// <summary>
/// Ratio-based log sampler that samples a percentage of logs.
/// </summary>
/// <remarks>
/// <para>
/// This sampler uses a probabilistic approach to sample logs.
/// It always includes Error and Critical logs, and samples other levels
/// based on the configured ratio.
/// </para>
/// </remarks>
public class RatioBasedLogSampler : ILogSampler
{
    private readonly double _ratio;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of <see cref="RatioBasedLogSampler"/>.
    /// </summary>
    /// <param name="ratio">The sampling ratio (0.0 to 1.0).</param>
    public RatioBasedLogSampler(double ratio)
    {
        _ratio = Math.Clamp(ratio, 0.0, 1.0);
    }

    /// <inheritdoc />
    public bool ShouldSample(LogLevel logLevel, string categoryName)
    {
        // Always sample Error and Critical logs
        if (logLevel >= LogLevel.Error)
        {
            return true;
        }

        // Sample based on ratio
        return _random.NextDouble() < _ratio;
    }
}

/// <summary>
/// Trace-context-aware log sampler that samples based on trace sampling decision.
/// </summary>
/// <remarks>
/// <para>
/// This sampler respects the trace sampling decision from the parent Activity.
/// If the trace is sampled, all logs within that trace are included.
/// This ensures consistent observability for sampled traces.
/// </para>
/// </remarks>
public class TraceContextLogSampler : ILogSampler
{
    private readonly double _fallbackRatio;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of <see cref="TraceContextLogSampler"/>.
    /// </summary>
    /// <param name="fallbackRatio">
    /// The sampling ratio to use when no trace context is available.
    /// </param>
    public TraceContextLogSampler(double fallbackRatio = 1.0)
    {
        _fallbackRatio = Math.Clamp(fallbackRatio, 0.0, 1.0);
    }

    /// <inheritdoc />
    public bool ShouldSample(LogLevel logLevel, string categoryName)
    {
        // Always sample Error and Critical logs
        if (logLevel >= LogLevel.Error)
        {
            return true;
        }

        var activity = Activity.Current;
        if (activity != null)
        {
            // Sample if the trace is recorded
            return activity.IsAllDataRequested || activity.Recorded;
        }

        // Fallback to ratio-based sampling
        return _random.NextDouble() < _fallbackRatio;
    }
}

/// <summary>
/// Log sampler that applies different rates per log level.
/// </summary>
public class LevelBasedLogSampler : ILogSampler
{
    private readonly Dictionary<LogLevel, double> _levelRatios;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of <see cref="LevelBasedLogSampler"/>.
    /// </summary>
    /// <param name="levelRatios">Dictionary of log level to sampling ratio.</param>
    public LevelBasedLogSampler(Dictionary<LogLevel, double> levelRatios)
    {
        _levelRatios = levelRatios;
    }

    /// <summary>
    /// Creates a sampler with common defaults for high-load environments.
    /// </summary>
    /// <returns>A configured sampler.</returns>
    public static LevelBasedLogSampler CreateHighLoadDefaults()
    {
        return new LevelBasedLogSampler(new Dictionary<LogLevel, double>
        {
            [LogLevel.Trace] = 0.01,      // 1%
            [LogLevel.Debug] = 0.05,      // 5%
            [LogLevel.Information] = 0.1, // 10%
            [LogLevel.Warning] = 0.5,     // 50%
            [LogLevel.Error] = 1.0,       // 100%
            [LogLevel.Critical] = 1.0     // 100%
        });
    }

    /// <inheritdoc />
    public bool ShouldSample(LogLevel logLevel, string categoryName)
    {
        if (_levelRatios.TryGetValue(logLevel, out var ratio))
        {
            return _random.NextDouble() < ratio;
        }

        // Default to including logs for unconfigured levels
        return true;
    }
}

/// <summary>
/// Semantic resource attributes for logs following OpenTelemetry conventions.
/// </summary>
public static class LogResourceAttributes
{
    /// <summary>
    /// Service name resource attribute.
    /// </summary>
    public const string ServiceName = "service.name";

    /// <summary>
    /// Service version resource attribute.
    /// </summary>
    public const string ServiceVersion = "service.version";

    /// <summary>
    /// Service instance ID resource attribute.
    /// </summary>
    public const string ServiceInstanceId = "service.instance.id";

    /// <summary>
    /// Service namespace resource attribute.
    /// </summary>
    public const string ServiceNamespace = "service.namespace";

    /// <summary>
    /// Deployment environment resource attribute.
    /// </summary>
    public const string DeploymentEnvironment = "deployment.environment";

    /// <summary>
    /// Host name resource attribute.
    /// </summary>
    public const string HostName = "host.name";

    /// <summary>
    /// Process ID resource attribute.
    /// </summary>
    public const string ProcessPid = "process.pid";

    /// <summary>
    /// Process runtime name resource attribute.
    /// </summary>
    public const string ProcessRuntimeName = "process.runtime.name";

    /// <summary>
    /// Process runtime version resource attribute.
    /// </summary>
    public const string ProcessRuntimeVersion = "process.runtime.version";

    /// <summary>
    /// Telemetry SDK name resource attribute.
    /// </summary>
    public const string TelemetrySdkName = "telemetry.sdk.name";

    /// <summary>
    /// Telemetry SDK language resource attribute.
    /// </summary>
    public const string TelemetrySdkLanguage = "telemetry.sdk.language";

    /// <summary>
    /// Telemetry SDK version resource attribute.
    /// </summary>
    public const string TelemetrySdkVersion = "telemetry.sdk.version";
}

/// <summary>
/// Extension methods for ILogger to add trace context to logs.
/// </summary>
public static class LoggerTraceContextExtensions
{
    /// <summary>
    /// Begins a logging scope with trace context (TraceId, SpanId, CorrelationId).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <returns>An IDisposable scope, or null if no trace context is available.</returns>
    public static IDisposable? BeginTraceScope(this ILogger logger)
    {
        var activity = Activity.Current;
        if (activity == null) return null;

        var correlationId = activity.GetBaggageItem("correlation.id") ?? activity.TraceId.ToString();

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = activity.TraceId.ToString(),
            ["SpanId"] = activity.SpanId.ToString(),
            [SemanticTags.CorrelationId] = correlationId
        });
    }

    /// <summary>
    /// Begins a logging scope with operation context.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operationId">Optional operation ID.</param>
    /// <returns>An IDisposable scope.</returns>
    public static IDisposable BeginOperationScope(
        this ILogger logger,
        string operationName,
        string? operationId = null)
    {
        var properties = new Dictionary<string, object>
        {
            [SemanticTags.OperationName] = operationName
        };

        if (!string.IsNullOrEmpty(operationId))
        {
            properties[SemanticTags.OperationId] = operationId;
        }

        // Include trace context if available
        var activity = Activity.Current;
        if (activity != null)
        {
            properties["TraceId"] = activity.TraceId.ToString();
            properties["SpanId"] = activity.SpanId.ToString();
        }

        return logger.BeginScope(properties);
    }

    /// <summary>
    /// Logs a structured message with trace context automatically included.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogWithTrace(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        params object?[] args)
    {
        using (logger.BeginTraceScope())
        {
            logger.Log(logLevel, message, args);
        }
    }
}


