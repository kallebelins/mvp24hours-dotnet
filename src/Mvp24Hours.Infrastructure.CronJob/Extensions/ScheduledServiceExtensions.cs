//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
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
    }
}
