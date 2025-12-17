//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Extension methods for registering pipeline observability services.
    /// </summary>
    public static class PipelineObservabilityExtensions
    {
        #region [ Metrics ]

        /// <summary>
        /// Adds pipeline metrics collection services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="maxDurationSamples">Maximum duration samples for percentile calculations.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineMetrics(
            this IServiceCollection services,
            int maxDurationSamples = 1000)
        {
            services.TryAddSingleton<IPipelineMetrics>(new PipelineMetrics(maxDurationSamples));
            return services;
        }

        /// <summary>
        /// Adds pipeline metrics with a custom implementation.
        /// </summary>
        /// <typeparam name="TMetrics">The metrics implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineMetrics<TMetrics>(this IServiceCollection services)
            where TMetrics : class, IPipelineMetrics
        {
            services.TryAddSingleton<IPipelineMetrics, TMetrics>();
            return services;
        }

        #endregion

        #region [ Structured Logging ]

        /// <summary>
        /// Adds structured logging middleware for pipelines.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineStructuredLogging(
            this IServiceCollection services,
            Action<StructuredLoggingOptions>? configure = null)
        {
            var options = new StructuredLoggingOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddSingleton<IPipelineMiddleware, StructuredLoggingMiddleware>();
            services.TryAddSingleton<IPipelineMiddlewareSync, StructuredLoggingMiddlewareSync>();

            return services;
        }

        /// <summary>
        /// Adds structured logging middleware with slow operation detection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="slowOperationThreshold">Threshold for slow operation warnings.</param>
        /// <param name="trackMemory">Whether to track memory allocations.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineStructuredLogging(
            this IServiceCollection services,
            TimeSpan slowOperationThreshold,
            bool trackMemory = false)
        {
            return services.AddPipelineStructuredLogging(options =>
            {
                options.SlowOperationThreshold = slowOperationThreshold;
                options.TrackMemory = trackMemory;
            });
        }

        #endregion

        #region [ Visualizer ]

        /// <summary>
        /// Adds pipeline visualizer service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineVisualizer(this IServiceCollection services)
        {
            services.TryAddSingleton<IPipelineVisualizer, PipelineVisualizer>();
            return services;
        }

        /// <summary>
        /// Adds pipeline visualizer with a custom implementation.
        /// </summary>
        /// <typeparam name="TVisualizer">The visualizer implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineVisualizer<TVisualizer>(this IServiceCollection services)
            where TVisualizer : class, IPipelineVisualizer
        {
            services.TryAddSingleton<IPipelineVisualizer, TVisualizer>();
            return services;
        }

        #endregion

        #region [ Health Checks ]

        /// <summary>
        /// Adds pipeline health check.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineHealthCheck(
            this IServiceCollection services,
            Action<PipelineHealthCheckOptions>? configure = null)
        {
            var options = new PipelineHealthCheckOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);
            services.AddPipelineMetrics();

            return services;
        }

        /// <summary>
        /// Adds pipeline health check to the health checks builder.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The health check name.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <param name="failureStatus">The health status when check fails.</param>
        /// <param name="tags">Optional tags for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddPipelineHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "pipeline",
            Action<PipelineHealthCheckOptions>? configure = null,
            HealthStatus? failureStatus = null,
            string[]? tags = null)
        {
            var options = new PipelineHealthCheckOptions();
            configure?.Invoke(options);

            builder.Services.TryAddSingleton(options);
            builder.Services.AddPipelineMetrics();

            builder.Add(new HealthCheckRegistration(
                name,
                sp => new PipelineHealthCheck(
                    sp.GetRequiredService<IPipelineMetrics>(),
                    sp.GetService<PipelineHealthCheckOptions>()),
                failureStatus,
                tags));

            return builder;
        }

        /// <summary>
        /// Adds pipeline health monitor service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineHealthMonitor(
            this IServiceCollection services,
            Action<PipelineHealthCheckOptions>? configure = null)
        {
            services.AddPipelineHealthCheck(configure);
            services.TryAddSingleton<IPipelineHealthMonitor, PipelineHealthMonitor>();
            return services;
        }

        #endregion

        #region [ Observers ]

        /// <summary>
        /// Adds pipeline observer manager and related services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineObservers(this IServiceCollection services)
        {
            services.TryAddSingleton<IPipelineObserverManager, PipelineObserverManager>();
            return services;
        }

        /// <summary>
        /// Adds a pipeline observer.
        /// </summary>
        /// <typeparam name="TObserver">The observer type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineObserver<TObserver>(this IServiceCollection services)
            where TObserver : class, IPipelineObserver
        {
            services.AddPipelineObservers();
            services.AddSingleton<IPipelineObserver, TObserver>();
            return services;
        }

        /// <summary>
        /// Adds a sync pipeline observer.
        /// </summary>
        /// <typeparam name="TObserver">The observer type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineObserverSync<TObserver>(this IServiceCollection services)
            where TObserver : class, IPipelineObserverSync
        {
            services.AddPipelineObservers();
            services.AddSingleton<IPipelineObserverSync, TObserver>();
            return services;
        }

        /// <summary>
        /// Adds the metrics collector observer.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineMetricsObserver(this IServiceCollection services)
        {
            services.AddPipelineMetrics();
            services.AddPipelineObservers();
            services.AddSingleton<IPipelineObserver, MetricsCollectorObserver>();
            return services;
        }

        #endregion

        #region [ Complete Setup ]

        /// <summary>
        /// Adds complete pipeline observability services including metrics, logging, visualization, and health checks.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineObservability(
            this IServiceCollection services,
            Action<PipelineObservabilityOptions>? configure = null)
        {
            var options = new PipelineObservabilityOptions();
            configure?.Invoke(options);

            // Add context accessor
            services.TryAddSingleton<IPipelineContextAccessor, PipelineContextAccessor>();

            // Add metrics
            if (options.EnableMetrics)
            {
                services.AddPipelineMetrics(options.MaxDurationSamples);
                services.AddPipelineMetricsObserver();
            }

            // Add structured logging
            if (options.EnableStructuredLogging)
            {
                services.AddPipelineStructuredLogging(loggingOptions =>
                {
                    loggingOptions.LogOperationStart = options.LogOperationStart;
                    loggingOptions.LogOperationEnd = options.LogOperationEnd;
                    loggingOptions.SlowOperationThreshold = options.SlowOperationThreshold;
                    loggingOptions.TrackMemory = options.TrackMemory;
                });
            }

            // Add visualizer
            if (options.EnableVisualizer)
            {
                services.AddPipelineVisualizer();
            }

            // Add health monitor
            if (options.EnableHealthMonitor)
            {
                services.AddPipelineHealthMonitor(healthOptions =>
                {
                    healthOptions.MinimumSuccessRate = options.MinimumSuccessRate;
                    healthOptions.CriticalSuccessRate = options.CriticalSuccessRate;
                    healthOptions.MaxAverageDurationMs = options.MaxAverageDurationMs;
                    healthOptions.MaxP95DurationMs = options.MaxP95DurationMs;
                });
            }

            // Add observers
            if (options.EnableObservers)
            {
                services.AddPipelineObservers();
            }

            return services;
        }

        #endregion
    }

    /// <summary>
    /// Configuration options for pipeline observability.
    /// </summary>
    public class PipelineObservabilityOptions
    {
        /// <summary>
        /// Gets or sets whether to enable metrics collection (default: true).
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable structured logging (default: true).
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable the pipeline visualizer (default: true).
        /// </summary>
        public bool EnableVisualizer { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable the health monitor (default: true).
        /// </summary>
        public bool EnableHealthMonitor { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable observers (default: true).
        /// </summary>
        public bool EnableObservers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log operation start events (default: true).
        /// </summary>
        public bool LogOperationStart { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log operation end events (default: true).
        /// </summary>
        public bool LogOperationEnd { get; set; } = true;

        /// <summary>
        /// Gets or sets the slow operation threshold (default: null - disabled).
        /// </summary>
        public TimeSpan? SlowOperationThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to track memory (default: false).
        /// </summary>
        public bool TrackMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum duration samples for percentile calculations (default: 1000).
        /// </summary>
        public int MaxDurationSamples { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the minimum success rate for health checks (default: 0.95).
        /// </summary>
        public double MinimumSuccessRate { get; set; } = 0.95;

        /// <summary>
        /// Gets or sets the critical success rate for health checks (default: 0.80).
        /// </summary>
        public double CriticalSuccessRate { get; set; } = 0.80;

        /// <summary>
        /// Gets or sets the maximum average duration for health checks in milliseconds (default: null).
        /// </summary>
        public double? MaxAverageDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum P95 duration for health checks in milliseconds (default: null).
        /// </summary>
        public double? MaxP95DurationMs { get; set; }
    }
}

