//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Observability;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering MongoDB observability services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides methods to configure:
    /// <list type="bullet">
    ///   <item>Slow query logging</item>
    ///   <item>OpenTelemetry tracing</item>
    ///   <item>Connection pool metrics</item>
    ///   <item>Structured logging</item>
    ///   <item>Duration tracking</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class MongoDbObservabilityExtensions
    {
        /// <summary>
        /// Adds MongoDB observability services (slow query logging, metrics, tracing).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure observability options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMongoDbObservability(options =>
        /// {
        ///     options.EnableSlowQueryLogging = true;
        ///     options.SlowQueryThreshold = TimeSpan.FromSeconds(1);
        ///     options.EnableOpenTelemetry = true;
        ///     options.EnableConnectionPoolMetrics = true;
        ///     options.EnableStructuredLogging = true;
        ///     options.EnableDurationTracking = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbObservability(
            this IServiceCollection services,
            Action<MongoDbObservabilityOptions> configureOptions = null)
        {
            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<MongoDbObservabilityOptions>(options => { });
            }

            // Register metrics (singleton for aggregation)
            services.TryAddSingleton<IMongoDbMetrics, MongoDbMetrics>();

            // Register observability components
            services.TryAddSingleton<MongoDbSlowQueryLogger>();
            services.TryAddSingleton<MongoDbOpenTelemetryInstrumentation>();
            services.TryAddSingleton<MongoDbConnectionPoolMetrics>();
            services.TryAddSingleton<MongoDbStructuredLogger>();
            services.TryAddSingleton<MongoDbDurationTracker>();

            return services;
        }

        /// <summary>
        /// Adds MongoDB observability with all features enabled.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="slowQueryThreshold">The slow query threshold.</param>
        /// <param name="enableOpenTelemetry">Whether to enable OpenTelemetry.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMongoDbFullObservability(
        ///     slowQueryThreshold: TimeSpan.FromMilliseconds(500),
        ///     enableOpenTelemetry: true);
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbFullObservability(
            this IServiceCollection services,
            TimeSpan? slowQueryThreshold = null,
            bool enableOpenTelemetry = false)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableAll = true;
                options.EnableOpenTelemetry = enableOpenTelemetry;

                if (slowQueryThreshold.HasValue)
                {
                    options.SlowQueryThreshold = slowQueryThreshold.Value;
                }
            });
        }

        /// <summary>
        /// Configures MongoDB client settings to use observability components.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The configured settings.</returns>
        /// <remarks>
        /// <para>
        /// This method should be called after creating MongoClientSettings to add
        /// observability event subscribers. Call this before creating the MongoClient.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var settings = MongoClientSettings.FromConnectionString(connectionString);
        /// settings.ConfigureObservability(serviceProvider);
        /// var client = new MongoClient(settings);
        /// </code>
        /// </example>
        public static MongoClientSettings ConfigureObservability(
            this MongoClientSettings settings,
            IServiceProvider serviceProvider)
        {
            // Get all observability components and configure them
            var slowQueryLogger = serviceProvider.GetService<MongoDbSlowQueryLogger>();
            slowQueryLogger?.ConfigureClusterBuilder(settings);

            var telemetry = serviceProvider.GetService<MongoDbOpenTelemetryInstrumentation>();
            telemetry?.ConfigureClusterBuilder(settings);

            var poolMetrics = serviceProvider.GetService<MongoDbConnectionPoolMetrics>();
            poolMetrics?.ConfigureClusterBuilder(settings);

            var structuredLogger = serviceProvider.GetService<MongoDbStructuredLogger>();
            structuredLogger?.ConfigureClusterBuilder(settings);

            var durationTracker = serviceProvider.GetService<MongoDbDurationTracker>();
            durationTracker?.ConfigureClusterBuilder(settings);

            return settings;
        }

        /// <summary>
        /// Adds MongoDB slow query logging only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="threshold">The slow query threshold.</param>
        /// <param name="logFilter">Whether to log the query filter.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbSlowQueryLogging(
            this IServiceCollection services,
            TimeSpan threshold,
            bool logFilter = false)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableSlowQueryLogging = true;
                options.SlowQueryThreshold = threshold;
                options.LogSlowQueryFilter = logFilter;
            });
        }

        /// <summary>
        /// Adds MongoDB OpenTelemetry instrumentation only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="activitySourceName">The activity source name.</param>
        /// <param name="includeStatement">Whether to include statements in traces.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Remember to add the activity source to your OpenTelemetry configuration:
        /// <code>
        /// builder.Services.AddOpenTelemetry()
        ///     .WithTracing(tracing =>
        ///     {
        ///         tracing.AddSource("Mvp24Hours.MongoDb");
        ///         // ... other configuration
        ///     });
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMongoDbOpenTelemetry(
            this IServiceCollection services,
            string activitySourceName = "Mvp24Hours.MongoDb",
            bool includeStatement = false)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableOpenTelemetry = true;
                options.ActivitySourceName = activitySourceName;
                options.IncludeStatementInTrace = includeStatement;
            });
        }

        /// <summary>
        /// Adds MongoDB connection pool metrics only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionInterval">The metrics collection interval.</param>
        /// <param name="alertThreshold">The utilization alert threshold (0.0 to 1.0).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbConnectionPoolMetrics(
            this IServiceCollection services,
            TimeSpan? collectionInterval = null,
            double alertThreshold = 0.8)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableConnectionPoolMetrics = true;
                options.EnableConnectionPoolAlerts = true;
                options.ConnectionPoolAlertThreshold = alertThreshold;

                if (collectionInterval.HasValue)
                {
                    options.ConnectionPoolMetricsInterval = collectionInterval.Value;
                }
            });
        }

        /// <summary>
        /// Adds MongoDB structured logging only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="logParameters">Whether to log command parameters (development only).</param>
        /// <param name="logResultCounts">Whether to log result counts.</param>
        /// <param name="sensitiveFields">Field names to mask in logs.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbStructuredLogging(
            this IServiceCollection services,
            bool logParameters = false,
            bool logResultCounts = true,
            string[] sensitiveFields = null)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableStructuredLogging = true;
                options.LogCommandParameters = logParameters;
                options.LogResultCounts = logResultCounts;

                if (sensitiveFields != null)
                {
                    options.SensitiveFields = sensitiveFields;
                }
            });
        }

        /// <summary>
        /// Adds MongoDB duration tracking only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="aggregationWindow">The duration statistics aggregation window.</param>
        /// <param name="collectPercentiles">Whether to collect percentiles.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbDurationTracking(
            this IServiceCollection services,
            TimeSpan? aggregationWindow = null,
            bool collectPercentiles = true)
        {
            return services.AddMongoDbObservability(options =>
            {
                options.EnableDurationTracking = true;
                options.CollectDurationPercentiles = collectPercentiles;

                if (aggregationWindow.HasValue)
                {
                    options.DurationAggregationWindow = aggregationWindow.Value;
                }
            });
        }

        /// <summary>
        /// Gets the MongoDB metrics collector from the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The metrics collector.</returns>
        public static IMongoDbMetrics GetMongoDbMetrics(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IMongoDbMetrics>();
        }

        /// <summary>
        /// Gets the MongoDB duration tracker from the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The duration tracker.</returns>
        public static MongoDbDurationTracker GetMongoDbDurationTracker(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<MongoDbDurationTracker>();
        }
    }
}

