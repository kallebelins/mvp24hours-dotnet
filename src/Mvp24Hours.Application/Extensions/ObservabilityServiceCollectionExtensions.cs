//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Extensions;

/// <summary>
/// Extension methods for registering application observability services.
/// </summary>
/// <remarks>
/// <para>
/// This provides extensions for configuring:
/// <list type="bullet">
/// <item>Correlation ID tracking</item>
/// <item>Operation metrics collection</item>
/// <item>Audit trail storage</item>
/// <item>OpenTelemetry integration</item>
/// </list>
/// </para>
/// </remarks>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application observability services with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><see cref="ICorrelationIdAccessor"/> and <see cref="ICorrelationIdContext"/> as Singleton</item>
    /// <item><see cref="IOperationMetrics"/> as Singleton</item>
    /// <item><see cref="IApplicationAuditStore"/> as Singleton (InMemory implementation)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationObservability();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationObservability(this IServiceCollection services)
    {
        return services.AddMvp24HoursApplicationObservability(new ApplicationObservabilityOptions());
    }

    /// <summary>
    /// Adds all application observability services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The observability configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationObservability(new ApplicationObservabilityOptions
    /// {
    ///     EnableMetrics = true,
    ///     EnableAuditTrail = true,
    ///     AuditMaxEntries = 50000
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationObservability(
        this IServiceCollection services,
        ApplicationObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Always register correlation ID services
        services.AddCorrelationId();

        // Register metrics if enabled
        if (options.EnableMetrics)
        {
            services.AddApplicationMetrics(options.MetricsOptions);
        }
        else
        {
            services.TryAddSingleton<IOperationMetrics>(NullOperationMetrics.Instance);
        }

        // Register audit trail if enabled
        if (options.EnableAuditTrail)
        {
            services.AddInMemoryAuditStore(options.AuditMaxEntries, options.AuditRetentionDays);
        }

        return services;
    }

    /// <summary>
    /// Adds all application observability services with a configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationObservability(options =>
    /// {
    ///     options.EnableMetrics = true;
    ///     options.EnableAuditTrail = true;
    ///     options.AuditMaxEntries = 50000;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationObservability(
        this IServiceCollection services,
        Action<ApplicationObservabilityOptions> configure)
    {
        var options = new ApplicationObservabilityOptions();
        configure(options);
        return services.AddMvp24HoursApplicationObservability(options);
    }

    /// <summary>
    /// Adds correlation ID tracking services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="CorrelationIdAccessor"/> as Singleton implementing:
    /// <list type="bullet">
    /// <item><see cref="ICorrelationIdAccessor"/> - for reading correlation ID</item>
    /// <item><see cref="ICorrelationIdSetter"/> - for setting correlation ID</item>
    /// <item><see cref="ICorrelationIdContext"/> - combined interface</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.TryAddSingleton<CorrelationIdAccessor>();
        services.TryAddSingleton<ICorrelationIdContext>(sp => sp.GetRequiredService<CorrelationIdAccessor>());
        services.TryAddSingleton<ICorrelationIdAccessor>(sp => sp.GetRequiredService<CorrelationIdAccessor>());
        services.TryAddSingleton<ICorrelationIdSetter>(sp => sp.GetRequiredService<CorrelationIdAccessor>());

        return services;
    }

    /// <summary>
    /// Adds operation metrics collection services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Optional metrics configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="ApplicationOperationMetrics"/> as Singleton.
    /// Metrics are exposed via the System.Diagnostics.Metrics API and can be
    /// collected by OpenTelemetry or Prometheus.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddApplicationMetrics(
        this IServiceCollection services,
        OperationMetricsOptions? options = null)
    {
        var metricsOptions = options ?? new OperationMetricsOptions();
        services.TryAddSingleton(metricsOptions);
        services.TryAddSingleton<IOperationMetrics, ApplicationOperationMetrics>();

        return services;
    }

    /// <summary>
    /// Adds in-memory audit trail storage.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxEntries">Maximum number of entries to keep (default: 10000).</param>
    /// <param name="retentionDays">Number of days to retain entries (default: 7).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This is for development/testing only.
    /// Use a persistent implementation for production.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInMemoryAuditStore(
        this IServiceCollection services,
        int maxEntries = 10000,
        int retentionDays = 7)
    {
        services.TryAddSingleton<IApplicationAuditStore>(
            new InMemoryApplicationAuditStore(maxEntries, retentionDays));

        return services;
    }

    /// <summary>
    /// Adds a custom audit store implementation.
    /// </summary>
    /// <typeparam name="TAuditStore">The audit store implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddAuditStore&lt;EfCoreAuditStore&gt;(ServiceLifetime.Scoped);
    /// </code>
    /// </example>
    public static IServiceCollection AddAuditStore<TAuditStore>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TAuditStore : class, IApplicationAuditStore
    {
        services.Add(new ServiceDescriptor(typeof(IApplicationAuditStore), typeof(TAuditStore), lifetime));
        return services;
    }

    /// <summary>
    /// Adds a custom audit store factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the audit store.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditStore(
        this IServiceCollection services,
        Func<IServiceProvider, IApplicationAuditStore> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(typeof(IApplicationAuditStore), factory, lifetime));
        return services;
    }
}

/// <summary>
/// Configuration options for application observability.
/// </summary>
public class ApplicationObservabilityOptions
{
    /// <summary>
    /// Gets or sets whether to enable operation metrics collection.
    /// Default is true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable audit trail for command operations.
    /// Default is true.
    /// </summary>
    public bool EnableAuditTrail { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of audit entries to keep in memory.
    /// Default is 10000.
    /// </summary>
    public int AuditMaxEntries { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the number of days to retain audit entries.
    /// Default is 7.
    /// </summary>
    public int AuditRetentionDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the metrics configuration options.
    /// </summary>
    public OperationMetricsOptions MetricsOptions { get; set; } = new();
}

