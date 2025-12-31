//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Extensions
{
    /// <summary>
    /// Extension methods for registering CronJob services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides fluent extension methods to configure and register CronJob services
    /// as hosted services in the ASP.NET Core dependency injection container.
    /// </para>
    /// <para>
    /// <strong>Registration Order:</strong>
    /// <list type="number">
    /// <item>The schedule configuration is registered as a singleton</item>
    /// <item>The CronJob service is registered as a hosted service</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddCronJob&lt;MyBackgroundJob&gt;(config =>
    /// {
    ///     config.CronExpression = "*/5 * * * *"; // Every 5 minutes
    ///     config.TimeZoneInfo = TimeZoneInfo.Utc;
    /// });
    /// </code>
    /// </example>
    public static class ScheduledServiceExtensions
    {
        /// <summary>
        /// Adds a CronJob service to the service collection with the specified schedule configuration.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="CronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="options">
        /// A delegate to configure the schedule options. Must not be null.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> or <paramref name="options"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The CronJob service is registered as a hosted service and will start automatically
        /// when the application starts. The service will continue running in the background
        /// according to the configured CRON expression.
        /// </para>
        /// <para>
        /// <strong>Configuration Options:</strong>
        /// <list type="bullet">
        /// <item>
        /// <term>CronExpression</term>
        /// <description>
        /// The CRON expression defining the schedule. If null or empty, the job runs once.
        /// </description>
        /// </item>
        /// <item>
        /// <term>TimeZoneInfo</term>
        /// <description>
        /// The timezone for evaluating the CRON expression. Defaults to local timezone.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Run every hour
        /// services.AddCronJob&lt;HourlyReportJob&gt;(c => c.CronExpression = "0 * * * *");
        /// 
        /// // Run daily at 2:30 AM UTC
        /// services.AddCronJob&lt;DailyCleanupJob&gt;(c =>
        /// {
        ///     c.CronExpression = "30 2 * * *";
        ///     c.TimeZoneInfo = TimeZoneInfo.Utc;
        /// });
        /// 
        /// // Run once immediately (no CRON expression)
        /// services.AddCronJob&lt;StartupJob&gt;(c => { });
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJob<T>(
            this IServiceCollection services,
            Action<IScheduleConfig<T>> options)
            where T : CronJobService<T>
        {
            // Guard clauses for input validation
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(options, nameof(options), "Please provide Schedule Configurations.");

            // Create and configure the schedule config
            var config = new ScheduleConfig<T>();
            options.Invoke(config);

            // Register the configuration as singleton
            services.AddSingleton<IScheduleConfig<T>>(config);

            // Register the CronJob as hosted service
            services.AddHostedService<T>();

            return services;
        }

        /// <summary>
        /// Adds a CronJob service to the service collection with a CRON expression.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="CronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule. Use standard 5-field format.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="cronExpression"/> is null or whitespace.
        /// </exception>
        /// <remarks>
        /// This is a convenience overload that uses the local timezone.
        /// For UTC or specific timezones, use the overload with the options delegate.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Run every 30 minutes
        /// services.AddCronJob&lt;MyJob&gt;("*/30 * * * *");
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJob<T>(
            this IServiceCollection services,
            string cronExpression)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));

            return services.AddCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = TimeZoneInfo.Local;
            });
        }

        /// <summary>
        /// Adds a CronJob service to the service collection with a CRON expression and timezone.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="CronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule. Use standard 5-field format.
        /// </param>
        /// <param name="timeZoneInfo">
        /// The timezone for evaluating the CRON expression.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> or <paramref name="timeZoneInfo"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="cronExpression"/> is null or whitespace.
        /// </exception>
        /// <example>
        /// <code>
        /// // Run every day at 9 AM São Paulo time
        /// services.AddCronJob&lt;MyJob&gt;(
        ///     "0 9 * * *",
        ///     TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJob<T>(
            this IServiceCollection services,
            string cronExpression,
            TimeZoneInfo timeZoneInfo)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));
            Guard.Against.Null(timeZoneInfo, nameof(timeZoneInfo));

            return services.AddCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = timeZoneInfo;
            });
        }

        /// <summary>
        /// Adds a CronJob service to run once immediately and then stop.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="CronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> is null.
        /// </exception>
        /// <remarks>
        /// This is useful for one-time startup tasks or migration jobs.
        /// The host application will be stopped after the job completes.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Run database migration once on startup
        /// services.AddCronJobRunOnce&lt;DatabaseMigrationJob&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobRunOnce<T>(this IServiceCollection services)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));

            return services.AddCronJob<T>(_ => { });
        }

        #region Resilient CronJob Extensions

        /// <summary>
        /// Adds a resilient CronJob service with retry, circuit breaker, overlapping prevention,
        /// and graceful shutdown capabilities.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="ResilientCronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="options">
        /// A delegate to configure the schedule and resilience options.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> or <paramref name="options"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method registers a resilient CronJob with the following features:
        /// </para>
        /// <list type="bullet">
        /// <item><b>Retry Policy:</b> Configurable retry with exponential backoff and jitter</item>
        /// <item><b>Circuit Breaker:</b> Prevents repeated execution of failing jobs</item>
        /// <item><b>Overlapping Prevention:</b> Ensures only one execution runs at a time</item>
        /// <item><b>Graceful Shutdown:</b> Properly handles application shutdown with configurable timeout</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddResilientCronJob&lt;MyJob&gt;(config =>
        /// {
        ///     // Schedule
        ///     config.CronExpression = "*/5 * * * *";
        ///     config.TimeZoneInfo = TimeZoneInfo.Utc;
        ///     
        ///     // Resilience
        ///     config.Resilience.EnableRetry = true;
        ///     config.Resilience.MaxRetryAttempts = 3;
        ///     config.Resilience.EnableCircuitBreaker = true;
        ///     config.Resilience.CircuitBreakerFailureThreshold = 5;
        ///     config.Resilience.PreventOverlapping = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCronJob<T>(
            this IServiceCollection services,
            Action<IResilientScheduleConfig<T>> options)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(options, nameof(options), "Please provide Schedule and Resilience Configurations.");

            // Create and configure the resilient schedule config
            var config = new ResilientScheduleConfig<T>();
            options.Invoke(config);

            // Register the configuration as singleton
            services.AddSingleton<IResilientScheduleConfig<T>>(config);

            // Register resilience infrastructure (once per application)
            services.TryAddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
            services.TryAddSingleton<CronJobCircuitBreaker>();

            // Register the CronJob as hosted service
            services.AddHostedService<T>();

            return services;
        }

        /// <summary>
        /// Adds a resilient CronJob service with a CRON expression and default resilience settings.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="ResilientCronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <example>
        /// <code>
        /// // Run every 30 minutes with default resilience (overlapping prevention only)
        /// services.AddResilientCronJob&lt;MyJob&gt;("*/30 * * * *");
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCronJob<T>(
            this IServiceCollection services,
            string cronExpression)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));

            return services.AddResilientCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = TimeZoneInfo.Local;
            });
        }

        /// <summary>
        /// Adds a resilient CronJob service with full resilience enabled (retry, circuit breaker, overlapping prevention).
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="ResilientCronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule.
        /// </param>
        /// <param name="timeZoneInfo">
        /// The timezone for evaluating the CRON expression.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <example>
        /// <code>
        /// // Run every day at 9 AM UTC with full resilience
        /// services.AddResilientCronJobWithFullResilience&lt;MyJob&gt;("0 9 * * *", TimeZoneInfo.Utc);
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCronJobWithFullResilience<T>(
            this IServiceCollection services,
            string cronExpression,
            TimeZoneInfo? timeZoneInfo = null)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));

            return services.AddResilientCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = timeZoneInfo ?? TimeZoneInfo.Local;
                config.Resilience = CronJobResilienceConfig<T>.FullResilience();
            });
        }

        /// <summary>
        /// Adds a resilient CronJob service with retry enabled.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="ResilientCronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule.
        /// </param>
        /// <param name="maxRetryAttempts">
        /// Maximum number of retry attempts. Default is 3.
        /// </param>
        /// <param name="useExponentialBackoff">
        /// Whether to use exponential backoff. Default is true.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <example>
        /// <code>
        /// // Run every hour with 5 retries using exponential backoff
        /// services.AddResilientCronJobWithRetry&lt;MyJob&gt;("0 * * * *", maxRetryAttempts: 5);
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCronJobWithRetry<T>(
            this IServiceCollection services,
            string cronExpression,
            int maxRetryAttempts = 3,
            bool useExponentialBackoff = true)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));

            return services.AddResilientCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = TimeZoneInfo.Local;
                config.Resilience = CronJobResilienceConfig<T>.WithRetry(maxRetryAttempts, useExponentialBackoff);
            });
        }

        /// <summary>
        /// Adds a resilient CronJob service with circuit breaker enabled.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the CronJob service. Must inherit from <see cref="ResilientCronJobService{T}"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="cronExpression">
        /// The CRON expression defining the schedule.
        /// </param>
        /// <param name="failureThreshold">
        /// Number of consecutive failures before opening the circuit. Default is 5.
        /// </param>
        /// <param name="breakDuration">
        /// Duration the circuit stays open. Default is 30 seconds.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <example>
        /// <code>
        /// // Run every minute with circuit breaker (opens after 3 failures, stays open for 1 minute)
        /// services.AddResilientCronJobWithCircuitBreaker&lt;MyJob&gt;(
        ///     "* * * * *",
        ///     failureThreshold: 3,
        ///     breakDuration: TimeSpan.FromMinutes(1));
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCronJobWithCircuitBreaker<T>(
            this IServiceCollection services,
            string cronExpression,
            int failureThreshold = 5,
            TimeSpan? breakDuration = null)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));

            return services.AddResilientCronJob<T>(config =>
            {
                config.CronExpression = cronExpression;
                config.TimeZoneInfo = TimeZoneInfo.Local;
                config.Resilience = CronJobResilienceConfig<T>.WithCircuitBreaker(failureThreshold, breakDuration);
            });
        }

        /// <summary>
        /// Adds CronJob resilience infrastructure (execution lock and circuit breaker) without registering a specific job.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="enableObservability">Whether to enable observability (metrics and health check support). Default is true.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <remarks>
        /// Call this method to register the resilience infrastructure before manually registering CronJob services.
        /// This is useful when using custom DI registration patterns.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddCronJobResilienceInfrastructure();
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobResilienceInfrastructure(
            this IServiceCollection services,
            bool enableObservability = true)
        {
            Guard.Against.Null(services, nameof(services));

            services.TryAddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
            services.TryAddSingleton<CronJobCircuitBreaker>();

            if (enableObservability)
            {
                services.AddCronJobObservability();
            }

            return services;
        }

        /// <summary>
        /// Adds CronJob resilience infrastructure with a custom execution lock implementation.
        /// </summary>
        /// <typeparam name="TLock">
        /// The type of the custom execution lock implementation.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <remarks>
        /// Use this method to provide a distributed lock implementation (e.g., Redis-based)
        /// for multi-instance deployments.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddCronJobResilienceInfrastructure&lt;RedisDistributedCronJobLock&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobResilienceInfrastructure<TLock>(this IServiceCollection services)
            where TLock : class, ICronJobExecutionLock
        {
            Guard.Against.Null(services, nameof(services));

            services.TryAddSingleton<ICronJobExecutionLock, TLock>();
            services.TryAddSingleton<CronJobCircuitBreaker>();

            return services;
        }

        #endregion
    }
}
