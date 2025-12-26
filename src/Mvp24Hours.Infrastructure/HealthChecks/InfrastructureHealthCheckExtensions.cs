//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Sms.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Extension methods for registering Infrastructure health checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides easy registration of health checks for all Infrastructure subsystems:
    /// <list type="bullet">
    /// <item><strong>HTTP Clients</strong> - Typed HTTP client connectivity</item>
    /// <item><strong>Distributed Locks</strong> - Redis, SQL Server, PostgreSQL lock providers</item>
    /// <item><strong>File Storage</strong> - Local, Azure Blob, S3 storage providers</item>
    /// <item><strong>Email Service</strong> - SMTP, SendGrid, Azure Communication Services</item>
    /// <item><strong>SMS Service</strong> - Twilio, Azure Communication Services</item>
    /// <item><strong>Background Jobs</strong> - Hangfire, Quartz.NET job schedulers</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class InfrastructureHealthCheckExtensions
    {
        #region HTTP Client Health Check

        /// <summary>
        /// Adds a health check for a typed HTTP client.
        /// </summary>
        /// <typeparam name="TApi">The API marker type for the typed HTTP client.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "httpclient-{TApi}".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddHttpClientHealthCheck&lt;MyApiClient&gt;(
        ///         "http-client-api",
        ///         options =>
        ///         {
        ///             options.HealthEndpoint = "/health";
        ///             options.TimeoutSeconds = 5;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddHttpClientHealthCheck<TApi>(
            this IHealthChecksBuilder builder,
            string? name = null,
            Action<HttpClientHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
            where TApi : class
        {
            var options = new HttpClientHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name ?? $"httpclient-{typeof(TApi).Name}",
                sp =>
                {
                    var httpClient = sp.GetRequiredService<ITypedHttpClient<TApi>>();
                    var logger = sp.GetRequiredService<ILogger<HttpClientHealthCheck<TApi>>>();
                    return new HttpClientHealthCheck<TApi>(httpClient, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region Distributed Lock Health Check

        /// <summary>
        /// Adds a health check for distributed lock providers.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "distributed-lock".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddDistributedLockHealthCheck(
        ///         "distributed-lock-redis",
        ///         options =>
        ///         {
        ///             options.ProviderName = "Redis";
        ///             options.LockTimeoutSeconds = 5;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddDistributedLockHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "distributed-lock",
            Action<DistributedLockHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new DistributedLockHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var lockFactory = sp.GetRequiredService<IDistributedLockFactory>();
                    var logger = sp.GetRequiredService<ILogger<DistributedLockHealthCheck>>();
                    return new DistributedLockHealthCheck(lockFactory, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region File Storage Health Check

        /// <summary>
        /// Adds a health check for file storage providers.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "file-storage".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddFileStorageHealthCheck(
        ///         "file-storage",
        ///         options =>
        ///         {
        ///             options.TestFilePath = "health-check/test.txt";
        ///             options.TimeoutSeconds = 10;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddFileStorageHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "file-storage",
            Action<FileStorageHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new FileStorageHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var fileStorage = sp.GetRequiredService<IFileStorage>();
                    var logger = sp.GetRequiredService<ILogger<FileStorageHealthCheck>>();
                    return new FileStorageHealthCheck(fileStorage, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region Email Service Health Check

        /// <summary>
        /// Adds a health check for email service providers.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "email-service".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddEmailServiceHealthCheck(
        ///         "email-service",
        ///         options =>
        ///         {
        ///             options.SendTestEmail = false; // Don't send actual emails
        ///             options.TimeoutSeconds = 5;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddEmailServiceHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "email-service",
            Action<EmailServiceHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new EmailServiceHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var emailService = sp.GetRequiredService<IEmailService>();
                    var logger = sp.GetRequiredService<ILogger<EmailServiceHealthCheck>>();
                    return new EmailServiceHealthCheck(emailService, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region SMS Service Health Check

        /// <summary>
        /// Adds a health check for SMS service providers.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "sms-service".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddSmsServiceHealthCheck(
        ///         "sms-service",
        ///         options =>
        ///         {
        ///             options.SendTestSms = false; // Don't send actual SMS
        ///             options.TimeoutSeconds = 5;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddSmsServiceHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "sms-service",
            Action<SmsServiceHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new SmsServiceHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var smsService = sp.GetRequiredService<ISmsService>();
                    var logger = sp.GetRequiredService<ILogger<SmsServiceHealthCheck>>();
                    return new SmsServiceHealthCheck(smsService, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region Background Job Health Check

        /// <summary>
        /// Adds a health check for background job schedulers.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "background-jobs".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddBackgroundJobHealthCheck(
        ///         "background-jobs",
        ///         options =>
        ///         {
        ///             options.ScheduleTestJob = false; // Don't schedule actual jobs
        ///             options.TimeoutSeconds = 5;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddBackgroundJobHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "background-jobs",
            Action<BackgroundJobHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new BackgroundJobHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var jobScheduler = sp.GetRequiredService<IJobScheduler>();
                    var logger = sp.GetRequiredService<ILogger<BackgroundJobHealthCheck>>();
                    return new BackgroundJobHealthCheck(jobScheduler, options, logger);
                },
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region All Infrastructure Health Checks

        /// <summary>
        /// Adds all available Infrastructure health checks.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="configureOptions">Optional configuration action for all health checks.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method adds health checks for all registered Infrastructure services:
        /// <list type="bullet">
        /// <item>Distributed Lock (if IDistributedLockFactory is registered)</item>
        /// <item>File Storage (if IFileStorage is registered)</item>
        /// <item>Email Service (if IEmailService is registered)</item>
        /// <item>SMS Service (if ISmsService is registered)</item>
        /// <item>Background Jobs (if IJobScheduler is registered)</item>
        /// </list>
        /// </para>
        /// <para>
        /// HTTP Client health checks must be added individually using <see cref="AddHttpClientHealthCheck{TApi}"/>
        /// because they require a typed client registration.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddInfrastructureHealthChecks(options =>
        ///     {
        ///         // Configure all health checks
        ///         options.DistributedLock.LockTimeoutSeconds = 5;
        ///         options.FileStorage.TimeoutSeconds = 10;
        ///         options.Email.SendTestEmail = false;
        ///         options.Sms.SendTestSms = false;
        ///         options.BackgroundJobs.ScheduleTestJob = false;
        ///     });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddInfrastructureHealthChecks(
            this IHealthChecksBuilder builder,
            Action<InfrastructureHealthCheckOptions>? configureOptions = null)
        {
            var options = new InfrastructureHealthCheckOptions();
            configureOptions?.Invoke(options);

            // Helper method to check if a service is registered
            bool IsServiceRegistered<T>() where T : class
            {
                return builder.Services.Any(s => s.ServiceType == typeof(T));
            }

            // Add distributed lock health check if factory is registered
            if (IsServiceRegistered<IDistributedLockFactory>())
            {
                builder.AddDistributedLockHealthCheck(
                    "distributed-lock",
                    opt =>
                    {
                        opt.ProviderName = options.DistributedLock.ProviderName;
                        opt.LockTimeoutSeconds = options.DistributedLock.LockTimeoutSeconds;
                        opt.LockExpirationSeconds = options.DistributedLock.LockExpirationSeconds;
                        opt.DegradedThresholdMs = options.DistributedLock.DegradedThresholdMs;
                        opt.FailureThresholdMs = options.DistributedLock.FailureThresholdMs;
                    },
                    tags: options.DistributedLock.Tags);
            }

            // Add file storage health check if provider is registered
            if (IsServiceRegistered<IFileStorage>())
            {
                builder.AddFileStorageHealthCheck(
                    "file-storage",
                    opt =>
                    {
                        opt.TestFilePath = options.FileStorage.TestFilePath;
                        opt.TestContent = options.FileStorage.TestContent;
                        opt.TimeoutSeconds = options.FileStorage.TimeoutSeconds;
                        opt.SkipContentVerification = options.FileStorage.SkipContentVerification;
                        opt.DegradedThresholdMs = options.FileStorage.DegradedThresholdMs;
                        opt.FailureThresholdMs = options.FileStorage.FailureThresholdMs;
                    },
                    tags: options.FileStorage.Tags);
            }

            // Add email service health check if provider is registered
            if (IsServiceRegistered<IEmailService>())
            {
                builder.AddEmailServiceHealthCheck(
                    "email-service",
                    opt =>
                    {
                        opt.SendTestEmail = options.Email.SendTestEmail;
                        opt.TestEmailRecipient = options.Email.TestEmailRecipient;
                        opt.TestEmailSubject = options.Email.TestEmailSubject;
                        opt.TestEmailBody = options.Email.TestEmailBody;
                        opt.TimeoutSeconds = options.Email.TimeoutSeconds;
                        opt.DegradedThresholdMs = options.Email.DegradedThresholdMs;
                        opt.FailureThresholdMs = options.Email.FailureThresholdMs;
                    },
                    tags: options.Email.Tags);
            }

            // Add SMS service health check if provider is registered
            if (IsServiceRegistered<ISmsService>())
            {
                builder.AddSmsServiceHealthCheck(
                    "sms-service",
                    opt =>
                    {
                        opt.SendTestSms = options.Sms.SendTestSms;
                        opt.TestSmsRecipient = options.Sms.TestSmsRecipient;
                        opt.TestSmsBody = options.Sms.TestSmsBody;
                        opt.TimeoutSeconds = options.Sms.TimeoutSeconds;
                        opt.DegradedThresholdMs = options.Sms.DegradedThresholdMs;
                        opt.FailureThresholdMs = options.Sms.FailureThresholdMs;
                    },
                    tags: options.Sms.Tags);
            }

            // Add background job health check if scheduler is registered
            if (IsServiceRegistered<IJobScheduler>())
            {
                builder.AddBackgroundJobHealthCheck(
                    "background-jobs",
                    opt =>
                    {
                        opt.ScheduleTestJob = options.BackgroundJobs.ScheduleTestJob;
                        opt.TimeoutSeconds = options.BackgroundJobs.TimeoutSeconds;
                        opt.DegradedThresholdMs = options.BackgroundJobs.DegradedThresholdMs;
                        opt.FailureThresholdMs = options.BackgroundJobs.FailureThresholdMs;
                    },
                    tags: options.BackgroundJobs.Tags);
            }

            return builder;
        }

        #endregion
    }

    /// <summary>
    /// Configuration options for all Infrastructure health checks.
    /// </summary>
    public sealed class InfrastructureHealthCheckOptions
    {
        /// <summary>
        /// Options for distributed lock health check.
        /// </summary>
        public DistributedLockHealthCheckOptions DistributedLock { get; set; } = new();

        /// <summary>
        /// Options for file storage health check.
        /// </summary>
        public FileStorageHealthCheckOptions FileStorage { get; set; } = new();

        /// <summary>
        /// Options for email service health check.
        /// </summary>
        public EmailServiceHealthCheckOptions Email { get; set; } = new();

        /// <summary>
        /// Options for SMS service health check.
        /// </summary>
        public SmsServiceHealthCheckOptions Sms { get; set; } = new();

        /// <summary>
        /// Options for background job health check.
        /// </summary>
        public BackgroundJobHealthCheckOptions BackgroundJobs { get; set; } = new();
    }
}

