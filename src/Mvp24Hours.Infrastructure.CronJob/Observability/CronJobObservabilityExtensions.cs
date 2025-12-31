//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Core.Observability.Metrics;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// Extension methods for configuring CronJob observability services.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods to register CronJob metrics, health checks,
/// and other observability features.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add CronJob observability with default settings
/// builder.Services.AddCronJobObservability();
/// 
/// // Add health check only
/// builder.Services.AddHealthChecks()
///     .AddCronJobHealthCheck();
/// 
/// // Add metrics only
/// builder.Services.AddCronJobMetrics();
/// </code>
/// </example>
public static class CronJobObservabilityExtensions
{
    #region Metrics Extensions

    /// <summary>
    /// Adds CronJob metrics services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This registers:
    /// <list type="bullet">
    /// <item><see cref="ICronJobMetrics"/> - Interface for custom metrics</item>
    /// <item><see cref="CronJobMetricsService"/> - Default metrics implementation</item>
    /// <item><see cref="CronJobMetrics"/> - Core metrics from Mvp24Hours.Core</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddCronJobMetrics(this IServiceCollection services)
    {
        Guard.Against.Null(services, nameof(services));

        services.TryAddSingleton<CronJobMetrics>();
        services.TryAddSingleton<CronJobMetricsService>();
        services.TryAddSingleton<ICronJobMetrics>(sp => sp.GetRequiredService<CronJobMetricsService>());

        return services;
    }

    /// <summary>
    /// Adds CronJob metrics with a custom implementation.
    /// </summary>
    /// <typeparam name="TMetrics">The custom metrics implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCronJobMetrics<TMetrics>(this IServiceCollection services)
        where TMetrics : class, ICronJobMetrics
    {
        Guard.Against.Null(services, nameof(services));

        services.TryAddSingleton<CronJobMetrics>();
        services.TryAddSingleton<ICronJobMetrics, TMetrics>();

        return services;
    }

    #endregion

    #region Health Check Extensions

    /// <summary>
    /// Adds a CronJob health check to the health check builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Default is "cronjobs".</param>
    /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
    /// <param name="tags">The tags for the health check.</param>
    /// <param name="timeout">The timeout for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddCronJobHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "cronjobs",
        HealthStatus? failureStatus = null,
        string[]? tags = null,
        TimeSpan? timeout = null)
    {
        Guard.Against.Null(builder, nameof(builder));

        // Ensure metrics service is registered
        builder.Services.AddCronJobMetrics();

        return builder.AddCheck<CronJobHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? new[] { "cronjob", "scheduled", "background" },
            timeout);
    }

    /// <summary>
    /// Adds a CronJob health check with custom options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configureOptions">Action to configure health check options.</param>
    /// <param name="name">The health check name. Default is "cronjobs".</param>
    /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
    /// <param name="tags">The tags for the health check.</param>
    /// <param name="timeout">The timeout for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddCronJobHealthCheck(
        this IHealthChecksBuilder builder,
        Action<CronJobHealthCheckOptions> configureOptions,
        string name = "cronjobs",
        HealthStatus? failureStatus = null,
        string[]? tags = null,
        TimeSpan? timeout = null)
    {
        Guard.Against.Null(builder, nameof(builder));
        Guard.Against.Null(configureOptions, nameof(configureOptions));

        // Register options
        builder.Services.Configure(configureOptions);

        // Ensure metrics service is registered
        builder.Services.AddCronJobMetrics();

        return builder.AddCheck<CronJobHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? new[] { "cronjob", "scheduled", "background" },
            timeout);
    }

    #endregion

    #region Full Observability Extensions

    /// <summary>
    /// Adds complete CronJob observability including metrics and health checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This is a convenience method that registers:
    /// <list type="bullet">
    /// <item>CronJob metrics (ICronJobMetrics, CronJobMetricsService)</item>
    /// <item>CronJob health check options</item>
    /// </list>
    /// Note: You still need to call AddHealthChecks().AddCronJobHealthCheck() to register the health check.
    /// </remarks>
    public static IServiceCollection AddCronJobObservability(this IServiceCollection services)
    {
        Guard.Against.Null(services, nameof(services));

        services.AddCronJobMetrics();
        services.TryAddSingleton<CronJobHealthCheckOptions>();

        return services;
    }

    /// <summary>
    /// Adds complete CronJob observability with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure health check options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCronJobObservability(
        this IServiceCollection services,
        Action<CronJobHealthCheckOptions> configureOptions)
    {
        Guard.Against.Null(services, nameof(services));
        Guard.Against.Null(configureOptions, nameof(configureOptions));

        services.AddCronJobMetrics();
        services.Configure(configureOptions);

        return services;
    }

    #endregion
}

