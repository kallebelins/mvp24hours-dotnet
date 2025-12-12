//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using System;

namespace Mvp24Hours.Extensions;

/// <summary>
/// Extension methods for configuring EF Core observability features.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods for adding comprehensive observability to EF Core including:
/// <list type="bullet">
/// <item><strong>Slow query logging</strong> - Detect and log queries exceeding thresholds</item>
/// <item><strong>Structured logging</strong> - Rich, parseable logs for development</item>
/// <item><strong>OpenTelemetry integration</strong> - Distributed tracing with Activities</item>
/// <item><strong>Metrics collection</strong> - Query counts, durations, pool statistics</item>
/// </list>
/// </para>
/// </remarks>
public static class EFCoreObservabilityExtensions
{
    #region Slow Query Monitoring

    /// <summary>
    /// Adds the slow query interceptor for detecting and logging slow database queries.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for slow query detection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The slow query interceptor monitors all database operations and logs warnings
    /// when queries exceed the configured threshold. It integrates with OpenTelemetry
    /// for distributed tracing.
    /// </para>
    /// <para>
    /// Remember to add the interceptor to your DbContext:
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.AddInterceptors(sp.GetRequiredService&lt;SlowQueryInterceptor&gt;());
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursSlowQueryInterceptor(options =>
    /// {
    ///     options.SlowQueryThresholdMs = 500;
    ///     options.WriteSlowQueryThresholdMs = 1000;
    ///     options.CreateActivities = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursSlowQueryInterceptor(
        this IServiceCollection services,
        Action<SlowQueryInterceptorOptions>? configureOptions = null)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-slowquery-interceptor");

        var options = new SlowQueryInterceptorOptions();
        configureOptions?.Invoke(options);

        services.AddScoped(sp =>
        {
            var logger = sp.GetService<ILogger<SlowQueryInterceptor>>();
            var metrics = sp.GetService<EFCoreMetrics>();

            return new SlowQueryInterceptor(
                slowQueryThreshold: TimeSpan.FromMilliseconds(options.SlowQueryThresholdMs),
                writeSlowQueryThreshold: options.WriteSlowQueryThresholdMs.HasValue
                    ? TimeSpan.FromMilliseconds(options.WriteSlowQueryThresholdMs.Value)
                    : null,
                logger: logger,
                metrics: metrics,
                createActivities: options.CreateActivities,
                onSlowQueryDetected: options.OnSlowQueryDetected);
        });

        return services;
    }

    #endregion

    #region Structured Logging

    /// <summary>
    /// Adds the structured logging interceptor for detailed query logging in development.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for structured logging.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This interceptor can expose sensitive data and impact performance.
    /// Use only in development environments.
    /// </para>
    /// <para>
    /// Remember to add the interceptor to your DbContext:
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.AddInterceptors(sp.GetRequiredService&lt;StructuredLoggingInterceptor&gt;());
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (env.IsDevelopment())
    /// {
    ///     services.AddMvp24HoursStructuredLoggingInterceptor(options =>
    ///     {
    ///         options.LogParameters = true;
    ///         options.OutputAsJson = true;
    ///         options.SensitiveParameters = new[] { "password", "ssn" };
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursStructuredLoggingInterceptor(
        this IServiceCollection services,
        Action<StructuredLoggingOptions>? configureOptions = null)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-structured-logging-interceptor");

        var options = new StructuredLoggingOptions();
        configureOptions?.Invoke(options);

        services.AddScoped(sp =>
        {
            var logger = sp.GetService<ILogger<StructuredLoggingInterceptor>>();
            var metrics = sp.GetService<EFCoreMetrics>();

            return new StructuredLoggingInterceptor(
                logger: logger,
                metrics: metrics,
                logParameters: options.LogParameters,
                outputAsJson: options.OutputAsJson,
                commandLogLevel: options.CommandLogLevel,
                errorLogLevel: options.ErrorLogLevel,
                sensitiveParameters: options.SensitiveParameters,
                maxSqlLength: options.MaxSqlLength,
                maxParameterValueLength: options.MaxParameterValueLength,
                includeStackTrace: options.IncludeStackTrace);
        });

        return services;
    }

    #endregion

    #region OpenTelemetry Integration

    /// <summary>
    /// Adds EFCore metrics collection for OpenTelemetry integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="EFCoreMetrics"/> as a singleton for collecting query metrics.
    /// Configure OpenTelemetry to include the meter:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics =>
    ///     {
    ///         metrics.AddMeter(EFCoreMetrics.MeterName);
    ///     });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursEFCoreMetrics();
    /// 
    /// // Then configure OpenTelemetry
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics =>
    ///     {
    ///         metrics
    ///             .AddMeter(EFCoreMetrics.MeterName)
    ///             .AddPrometheusExporter();
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursEFCoreMetrics(this IServiceCollection services)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-metrics");

        services.TryAddSingleton<EFCoreMetrics>();
        return services;
    }

    /// <summary>
    /// Adds the EFCore diagnostics listener for automatic OpenTelemetry tracing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The diagnostics listener automatically captures EF Core events and creates
    /// OpenTelemetry activities. Configure OpenTelemetry to include the source:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing =>
    ///     {
    ///         tracing.AddSource(EFCoreActivitySource.SourceName);
    ///     });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursEFCoreDiagnosticsListener();
    /// 
    /// // Configure OpenTelemetry
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing =>
    ///     {
    ///         tracing
    ///             .AddSource(EFCoreActivitySource.SourceName)
    ///             .AddJaegerExporter();
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursEFCoreDiagnosticsListener(this IServiceCollection services)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-diagnostics-listener");

        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<EFCoreDiagnosticsListener>>();
            var metrics = sp.GetService<EFCoreMetrics>();
            var listener = new EFCoreDiagnosticsListener(logger, metrics);
            listener.Subscribe();
            return listener;
        });

        return services;
    }

    #endregion

    #region Combined Observability

    /// <summary>
    /// Adds complete EFCore observability including metrics, tracing, and logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for observability features.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all observability components:
    /// <list type="bullet">
    /// <item><see cref="EFCoreMetrics"/> for OpenTelemetry metrics</item>
    /// <item><see cref="EFCoreDiagnosticsListener"/> for automatic tracing</item>
    /// <item><see cref="SlowQueryInterceptor"/> for slow query detection</item>
    /// <item><see cref="DbContextPoolMonitor"/> for pool statistics (optional)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Remember to configure your DbContext with interceptors:
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.AddInterceptors(sp.GetRequiredService&lt;SlowQueryInterceptor&gt;());
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursEFCoreObservability(options =>
    /// {
    ///     options.SlowQueryThresholdMs = 500;
    ///     options.EnablePoolMonitoring = true;
    ///     options.EnableDiagnosticsListener = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursEFCoreObservability(
        this IServiceCollection services,
        Action<EFCoreObservabilityOptions>? configureOptions = null)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-observability");

        var options = new EFCoreObservabilityOptions();
        configureOptions?.Invoke(options);

        // Always add metrics
        services.AddMvp24HoursEFCoreMetrics();

        // Add slow query interceptor
        services.AddMvp24HoursSlowQueryInterceptor(slowQueryOptions =>
        {
            slowQueryOptions.SlowQueryThresholdMs = options.SlowQueryThresholdMs;
            slowQueryOptions.WriteSlowQueryThresholdMs = options.WriteSlowQueryThresholdMs;
            slowQueryOptions.CreateActivities = options.CreateActivities;
            slowQueryOptions.OnSlowQueryDetected = options.OnSlowQueryDetected;
        });

        // Optionally add diagnostics listener
        if (options.EnableDiagnosticsListener)
        {
            services.AddMvp24HoursEFCoreDiagnosticsListener();
        }

        // Optionally add pool monitoring
        if (options.EnablePoolMonitoring)
        {
            services.AddMvp24HoursDbContextPoolMonitor(resilienceOptions =>
            {
                resilienceOptions.LogPoolStatistics = true;
                resilienceOptions.PoolStatisticsLogIntervalSeconds = options.PoolStatisticsIntervalSeconds;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds development-optimized observability with detailed logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures observability optimized for development:
    /// <list type="bullet">
    /// <item>Lower slow query threshold (200ms)</item>
    /// <item>Structured logging with parameters</item>
    /// <item>Full OpenTelemetry integration</item>
    /// <item>Pool monitoring enabled</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> Do not use in production as it may expose sensitive data.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (env.IsDevelopment())
    /// {
    ///     services.AddMvp24HoursEFCoreDevObservability();
    /// }
    /// else
    /// {
    ///     services.AddMvp24HoursEFCoreObservability();
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursEFCoreDevObservability(this IServiceCollection services)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-add-dev-observability");

        services.AddMvp24HoursEFCoreObservability(options =>
        {
            options.SlowQueryThresholdMs = 200;
            options.EnableDiagnosticsListener = true;
            options.EnablePoolMonitoring = true;
            options.PoolStatisticsIntervalSeconds = 30;
        });

        services.AddMvp24HoursStructuredLoggingInterceptor(options =>
        {
            options.LogParameters = true;
            options.OutputAsJson = false;
            options.CommandLogLevel = LogLevel.Debug;
        });

        return services;
    }

    #endregion
}

#region Options Classes

/// <summary>
/// Configuration options for the slow query interceptor.
/// </summary>
public sealed class SlowQueryInterceptorOptions
{
    /// <summary>
    /// Threshold in milliseconds for slow query detection. Default is 1000ms.
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Optional separate threshold for write operations. If null, uses SlowQueryThresholdMs.
    /// </summary>
    public int? WriteSlowQueryThresholdMs { get; set; }

    /// <summary>
    /// If true, creates OpenTelemetry activities for slow queries. Default is true.
    /// </summary>
    public bool CreateActivities { get; set; } = true;

    /// <summary>
    /// Optional callback invoked when a slow query is detected.
    /// Parameters: (sql, duration, dbName).
    /// </summary>
    public Action<string, TimeSpan, string?>? OnSlowQueryDetected { get; set; }
}

/// <summary>
/// Configuration options for structured logging.
/// </summary>
public sealed class StructuredLoggingOptions
{
    /// <summary>
    /// If true, logs parameter values. Default is true in DEBUG, false in RELEASE.
    /// </summary>
    public bool? LogParameters { get; set; }

    /// <summary>
    /// If true, outputs logs as JSON. Default is false.
    /// </summary>
    public bool OutputAsJson { get; set; }

    /// <summary>
    /// Log level for successful commands. Default is Debug.
    /// </summary>
    public LogLevel CommandLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Log level for errors. Default is Error.
    /// </summary>
    public LogLevel ErrorLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Parameter names to mask in logs. Default includes common sensitive names.
    /// </summary>
    public string[]? SensitiveParameters { get; set; }

    /// <summary>
    /// Maximum SQL length to log. Default is 4000.
    /// </summary>
    public int MaxSqlLength { get; set; } = 4000;

    /// <summary>
    /// Maximum parameter value length. Default is 200.
    /// </summary>
    public int MaxParameterValueLength { get; set; } = 200;

    /// <summary>
    /// If true, includes caller stack trace. Default is false.
    /// </summary>
    public bool IncludeStackTrace { get; set; }
}

/// <summary>
/// Configuration options for complete EFCore observability.
/// </summary>
public sealed class EFCoreObservabilityOptions
{
    /// <summary>
    /// Threshold in milliseconds for slow query detection. Default is 1000ms.
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Optional separate threshold for write operations.
    /// </summary>
    public int? WriteSlowQueryThresholdMs { get; set; }

    /// <summary>
    /// If true, creates OpenTelemetry activities for slow queries. Default is true.
    /// </summary>
    public bool CreateActivities { get; set; } = true;

    /// <summary>
    /// If true, enables the diagnostics listener for automatic tracing. Default is true.
    /// </summary>
    public bool EnableDiagnosticsListener { get; set; } = true;

    /// <summary>
    /// If true, enables DbContext pool monitoring. Default is false.
    /// </summary>
    public bool EnablePoolMonitoring { get; set; }

    /// <summary>
    /// Interval in seconds for pool statistics logging. Default is 60.
    /// </summary>
    public int PoolStatisticsIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Optional callback invoked when a slow query is detected.
    /// </summary>
    public Action<string, TimeSpan, string?>? OnSlowQueryDetected { get; set; }
}

#endregion

