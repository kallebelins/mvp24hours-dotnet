//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Configuration
{
    /// <summary>
    /// Global configuration options that apply to all CronJob services.
    /// These settings serve as defaults that can be overridden per-job.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configure these settings in <c>appsettings.json</c> under the "CronJobs:Global" section
    /// or via code using <c>AddCronJobGlobalOptions</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In appsettings.json
    /// {
    ///   "CronJobs": {
    ///     "Global": {
    ///       "DefaultTimeZone": "UTC",
    ///       "EnableObservability": true,
    ///       "EnableHealthChecks": true,
    ///       "ValidateCronExpressionsOnStartup": true
    ///     }
    ///   }
    /// }
    /// 
    /// // In code
    /// services.AddCronJobGlobalOptions(options =>
    /// {
    ///     options.DefaultTimeZone = "UTC";
    ///     options.EnableObservability = true;
    /// });
    /// </code>
    /// </example>
    public class CronJobGlobalOptions
    {
        /// <summary>
        /// The configuration section name for global CronJob options.
        /// </summary>
        public const string SectionName = "CronJobs:Global";

        #region Default Settings

        /// <summary>
        /// Gets or sets the default timezone for all CronJobs.
        /// Individual jobs can override this setting.
        /// </summary>
        /// <value>Default is null (uses local timezone).</value>
        public string? DefaultTimeZone { get; set; }

        /// <summary>
        /// Gets the default TimeZoneInfo based on <see cref="DefaultTimeZone"/>.
        /// </summary>
        public TimeZoneInfo? DefaultTimeZoneInfo
        {
            get => string.IsNullOrWhiteSpace(DefaultTimeZone)
                ? null
                : TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZone);
        }

        /// <summary>
        /// Gets or sets whether all jobs are enabled by default.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool JobsEnabledByDefault { get; set; } = true;

        #endregion

        #region Default Resilience Settings

        /// <summary>
        /// Gets or sets the default retry setting for all jobs.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableRetryByDefault { get; set; }

        /// <summary>
        /// Gets or sets the default maximum retry attempts.
        /// </summary>
        /// <value>Default is <c>3</c>.</value>
        public int DefaultMaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the default retry delay.
        /// </summary>
        /// <value>Default is <c>1 second</c>.</value>
        public TimeSpan DefaultRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff by default.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool UseExponentialBackoffByDefault { get; set; } = true;

        /// <summary>
        /// Gets or sets the default circuit breaker setting for all jobs.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableCircuitBreakerByDefault { get; set; }

        /// <summary>
        /// Gets or sets the default circuit breaker failure threshold.
        /// </summary>
        /// <value>Default is <c>5</c>.</value>
        public int DefaultCircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the default circuit breaker break duration.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        public TimeSpan DefaultCircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to prevent overlapping executions by default.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool PreventOverlappingByDefault { get; set; } = true;

        /// <summary>
        /// Gets or sets the default graceful shutdown timeout.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        public TimeSpan DefaultGracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether distributed locking is enabled by default.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableDistributedLockingByDefault { get; set; }

        /// <summary>
        /// Gets or sets the default distributed lock expiry time.
        /// </summary>
        /// <value>Default is <c>5 minutes</c>.</value>
        public TimeSpan DefaultDistributedLockExpiry { get; set; } = TimeSpan.FromMinutes(5);

        #endregion

        #region Observability Settings

        /// <summary>
        /// Gets or sets whether observability (metrics and tracing) is enabled globally.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableObservability { get; set; } = true;

        /// <summary>
        /// Gets or sets whether health checks are enabled globally.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableHealthChecks { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to register the aggregate health check.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool RegisterAggregateHealthCheck { get; set; } = true;

        /// <summary>
        /// Gets or sets the health check name for the aggregate check.
        /// </summary>
        /// <value>Default is "cronjobs".</value>
        public string AggregateHealthCheckName { get; set; } = "cronjobs";

        /// <summary>
        /// Gets or sets the health check tags.
        /// </summary>
        public string[]? HealthCheckTags { get; set; } = new[] { "cronjob", "background" };

        #endregion

        #region Validation Settings

        /// <summary>
        /// Gets or sets whether to validate CRON expressions on startup.
        /// When true, invalid CRON expressions will cause application startup to fail.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool ValidateCronExpressionsOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log warnings for potentially problematic configurations.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool LogConfigurationWarnings { get; set; } = true;

        #endregion

        #region Runtime Control

        /// <summary>
        /// Gets or sets whether runtime control (pause/resume) is enabled.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableRuntimeControl { get; set; } = true;

        /// <summary>
        /// Gets or sets whether state persistence is enabled.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableStatePersistence { get; set; } = true;

        #endregion

        /// <summary>
        /// Applies global defaults to a job-specific options instance.
        /// Only applies values that are not explicitly set in the job options.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="jobOptions">The job options to apply defaults to.</param>
        public void ApplyDefaultsTo<T>(CronJobOptions<T> jobOptions)
        {
            // Apply timezone default if not set
            if (string.IsNullOrWhiteSpace(jobOptions.TimeZone) && !string.IsNullOrWhiteSpace(DefaultTimeZone))
            {
                jobOptions.TimeZone = DefaultTimeZone;
            }

            // Note: We don't override explicit job settings
            // The job options take precedence over global defaults
            // This method is primarily for initializing new job options
        }

        /// <summary>
        /// Creates a new CronJobOptions with global defaults applied.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <returns>A new CronJobOptions instance with defaults applied.</returns>
        public CronJobOptions<T> CreateWithDefaults<T>()
        {
            return new CronJobOptions<T>
            {
                TimeZone = DefaultTimeZone,
                Enabled = JobsEnabledByDefault,
                EnableRetry = EnableRetryByDefault,
                MaxRetryAttempts = DefaultMaxRetryAttempts,
                RetryDelay = DefaultRetryDelay,
                UseExponentialBackoff = UseExponentialBackoffByDefault,
                EnableCircuitBreaker = EnableCircuitBreakerByDefault,
                CircuitBreakerFailureThreshold = DefaultCircuitBreakerFailureThreshold,
                CircuitBreakerBreakDuration = DefaultCircuitBreakerBreakDuration,
                PreventOverlapping = PreventOverlappingByDefault,
                GracefulShutdownTimeout = DefaultGracefulShutdownTimeout,
                EnableDistributedLocking = EnableDistributedLockingByDefault,
                DistributedLockExpiry = DefaultDistributedLockExpiry,
                EnableObservability = EnableObservability,
                EnableHealthCheck = EnableHealthChecks
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CronJobGlobalOptions[TimeZone='{DefaultTimeZone ?? "Local"}', " +
                   $"Observability={EnableObservability}, HealthChecks={EnableHealthChecks}, " +
                   $"ValidateOnStartup={ValidateCronExpressionsOnStartup}]";
        }
    }
}

