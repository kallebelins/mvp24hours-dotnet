//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Observability;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Caching.Extensions;

/// <summary>
/// Extension methods for registering cache observability components.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds cache metrics service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCacheMetrics(this IServiceCollection services)
    {
        services.AddSingleton<ICacheMetrics, CacheMetrics>();
        return services;
    }

    /// <summary>
    /// Wraps an ICacheProvider with observability (tracing, metrics, structured logging).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the observable wrapper.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservableCacheProvider(
        this IServiceCollection services,
        Action<ObservableCacheProviderOptions>? configure = null)
    {
        var options = new ObservableCacheProviderOptions();
        configure?.Invoke(options);

        // Register metrics if not already registered
        if (options.EnableMetrics)
        {
            services.AddSingleton<ICacheMetrics, CacheMetrics>();
        }

        // Decorate ICacheProvider registration with ObservableCacheProvider
        Decorate<ICacheProvider, ObservableCacheProvider>(services, options);

        return services;
    }

    /// <summary>
    /// Helper method to decorate an existing ICacheProvider registration.
    /// </summary>
    private static void Decorate<TInterface, TDecorator>(
        IServiceCollection services,
        ObservableCacheProviderOptions options)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        if (wrappedDescriptor == null)
        {
            throw new InvalidOperationException($"Service {typeof(TInterface).Name} is not registered. Register it before decorating.");
        }

        services.Replace(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp =>
            {
                var inner = wrappedDescriptor.ImplementationInstance
                    ?? (wrappedDescriptor.ImplementationFactory?.Invoke(sp)
                        ?? ActivatorUtilities.GetServiceOrCreateInstance(sp, wrappedDescriptor.ImplementationType!));

                var metrics = options.EnableMetrics ? sp.GetService<ICacheMetrics>() : null;
                var logger = options.EnableLogging ? sp.GetService<ILogger<ObservableCacheProvider>>() : null;

                return (TInterface)(object)new ObservableCacheProvider((ICacheProvider)inner, metrics, logger);
            },
            wrappedDescriptor.Lifetime));
    }

    /// <summary>
    /// Adds cache health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check (default: "cache").</param>
    /// <param name="failureStatus">The failure status (default: HealthStatus.Unhealthy).</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="configure">Optional configuration for the health check options.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddCacheHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "cache",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        Action<CacheHealthCheckOptions>? configure = null)
    {
        var options = new CacheHealthCheckOptions();
        configure?.Invoke(options);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new CacheHealthCheck(
                sp.GetRequiredService<ICacheProvider>(),
                options,
                sp.GetService<ILogger<CacheHealthCheck>>()),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Configures OpenTelemetry to include cache tracing and metrics.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers the ActivitySource and Meter names that should be added
    /// to OpenTelemetry configuration. You still need to configure OpenTelemetry separately:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(builder => builder.AddSource(CacheActivitySource.SourceName))
    ///     .WithMetrics(builder => builder.AddMeter(CacheActivitySource.MeterName));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddCacheObservability(this IServiceCollection services)
    {
        // Register metrics
        services.AddSingleton<ICacheMetrics, CacheMetrics>();

        // Note: OpenTelemetry configuration must be done separately in the application startup
        // This method just ensures the metrics service is registered

        return services;
    }
}

/// <summary>
/// Options for ObservableCacheProvider configuration.
/// </summary>
public class ObservableCacheProviderOptions
{
    /// <summary>
    /// Whether to enable metrics tracking. Default: true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Whether to enable structured logging. Default: true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}

