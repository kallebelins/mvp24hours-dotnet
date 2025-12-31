//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Configuration
{
    /// <summary>
    /// Comprehensive configuration options for a CronJob service.
    /// Combines schedule configuration, resilience settings, and advanced options.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a unified configuration model that can be:
    /// </para>
    /// <list type="bullet">
    /// <item>Configured via code using the fluent API</item>
    /// <item>Bound from <c>appsettings.json</c> via <see cref="Microsoft.Extensions.Configuration.IConfiguration"/></item>
    /// <item>Validated at startup using <see cref="Microsoft.Extensions.Options.IValidateOptions{T}"/></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Via code configuration
    /// services.AddCronJobWithOptions&lt;MyJob&gt;(options =>
    /// {
    ///     options.CronExpression = "*/5 * * * *";
    ///     options.TimeZone = "UTC";
    ///     options.EnableRetry = true;
    ///     options.MaxRetryAttempts = 3;
    /// });
    /// 
    /// // Via appsettings.json
    /// // "CronJobs": {
    /// //   "MyJob": {
    /// //     "CronExpression": "*/5 * * * *",
    /// //     "TimeZone": "UTC",
    /// //     "EnableRetry": true
    /// //   }
    /// // }
    /// services.AddCronJobFromConfiguration&lt;MyJob&gt;();
    /// </code>
    /// </example>
    public class CronJobOptions<T> : IResilientScheduleConfig<T>
    {
        /// <summary>
        /// The configuration section name prefix for CronJobs in appsettings.json.
        /// </summary>
        public const string SectionName = "CronJobs";

        /// <summary>
        /// Gets the configuration section path for this job type.
        /// </summary>
        public static string GetSectionPath() => $"{SectionName}:{typeof(T).Name}";

        /// <summary>
        /// Gets or sets the CRON expression that defines the job schedule.
        /// </summary>
        /// <value>
        /// A valid CRON expression in 5-field (standard) or 6-field (with seconds) format.
        /// If null or empty, the job executes once immediately.
        /// </value>
        /// <remarks>
        /// <para>
        /// <strong>5-field format:</strong> minute hour day-of-month month day-of-week
        /// </para>
        /// <para>
        /// <strong>6-field format:</strong> second minute hour day-of-month month day-of-week
        /// </para>
        /// </remarks>
        /// <example>
        /// <list type="bullet">
        /// <item><c>"*/5 * * * *"</c> - Every 5 minutes</item>
        /// <item><c>"0 0 * * *"</c> - Daily at midnight</item>
        /// <item><c>"0 0 0 * * *"</c> - Daily at midnight (with seconds)</item>
        /// </list>
        /// </example>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Gets or sets the timezone identifier for CRON expression evaluation.
        /// </summary>
        /// <value>
        /// A valid timezone identifier (e.g., "UTC", "America/Sao_Paulo", "Pacific Standard Time").
        /// If null, defaults to the local timezone.
        /// </value>
        /// <remarks>
        /// Use standard IANA timezone identifiers on Linux/macOS or Windows timezone IDs on Windows.
        /// Consider using "UTC" for consistent behavior across environments.
        /// </remarks>
        public string? TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the timezone for CRON expression evaluation.
        /// </summary>
        /// <remarks>
        /// This property is derived from <see cref="TimeZone"/> string.
        /// Prefer using <see cref="TimeZone"/> for configuration binding.
        /// </remarks>
        public TimeZoneInfo? TimeZoneInfo
        {
            get => string.IsNullOrWhiteSpace(TimeZone)
                ? System.TimeZoneInfo.Local
                : System.TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            set => TimeZone = value?.Id;
        }

        /// <summary>
        /// Gets or sets an optional description for the job.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the job is enabled.
        /// When false, the job is registered but will not execute.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique instance name for this job configuration.
        /// Used when registering multiple instances of the same job type with different configurations.
        /// </summary>
        /// <remarks>
        /// When null or empty, the job type name is used as the instance name.
        /// </remarks>
        public string? InstanceName { get; set; }

        #region Resilience Settings

        /// <summary>
        /// Gets or sets whether retry is enabled for failed job executions.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        /// <value>Default is <c>3</c>.</value>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retry attempts.
        /// </summary>
        /// <value>Default is <c>1 second</c>.</value>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets whether circuit breaker is enabled.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableCircuitBreaker { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive failures before opening the circuit.
        /// </summary>
        /// <value>Default is <c>5</c>.</value>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration the circuit stays open before allowing a test execution.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to prevent overlapping executions.
        /// When true, a new execution will be skipped if a previous one is still running.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool PreventOverlapping { get; set; } = true;

        /// <summary>
        /// Gets or sets the graceful shutdown timeout.
        /// How long to wait for a running job to complete before forcing cancellation.
        /// </summary>
        /// <value>Default is <c>30 seconds</c>.</value>
        public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether distributed locking is enabled.
        /// Use this for cluster deployments to ensure only one instance runs the job.
        /// </summary>
        /// <value>Default is <c>false</c>.</value>
        public bool EnableDistributedLocking { get; set; }

        /// <summary>
        /// Gets or sets the distributed lock expiry time.
        /// </summary>
        /// <value>Default is <c>5 minutes</c>.</value>
        public TimeSpan DistributedLockExpiry { get; set; } = TimeSpan.FromMinutes(5);

        #endregion

        #region Observability Settings

        /// <summary>
        /// Gets or sets whether observability (metrics and tracing) is enabled for this job.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableObservability { get; set; } = true;

        /// <summary>
        /// Gets or sets whether health checks are enabled for this job.
        /// </summary>
        /// <value>Default is <c>true</c>.</value>
        public bool EnableHealthCheck { get; set; } = true;

        #endregion

        #region Dependency Settings

        /// <summary>
        /// Gets or sets the names of jobs that must complete before this job can run.
        /// </summary>
        public string[]? DependsOn { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets the resilience configuration.
        /// This property is automatically synchronized with the individual resilience properties.
        /// </summary>
        ICronJobResilienceConfig<T> IResilientScheduleConfig<T>.Resilience
        {
            get => ToResilienceConfig();
            set => FromResilienceConfig(value);
        }

        /// <summary>
        /// Converts the options to a resilience configuration.
        /// </summary>
        private CronJobResilienceConfig<T> ToResilienceConfig()
        {
            return new CronJobResilienceConfig<T>
            {
                EnableRetry = EnableRetry,
                MaxRetryAttempts = MaxRetryAttempts,
                RetryDelay = RetryDelay,
                UseExponentialBackoff = UseExponentialBackoff,
                EnableCircuitBreaker = EnableCircuitBreaker,
                CircuitBreakerFailureThreshold = CircuitBreakerFailureThreshold,
                CircuitBreakerDuration = CircuitBreakerBreakDuration,
                PreventOverlapping = PreventOverlapping,
                GracefulShutdownTimeout = GracefulShutdownTimeout
            };
        }

        /// <summary>
        /// Updates options from a resilience configuration.
        /// </summary>
        private void FromResilienceConfig(ICronJobResilienceConfig<T> config)
        {
            EnableRetry = config.EnableRetry;
            MaxRetryAttempts = config.MaxRetryAttempts;
            RetryDelay = config.RetryDelay;
            UseExponentialBackoff = config.UseExponentialBackoff;
            EnableCircuitBreaker = config.EnableCircuitBreaker;
            CircuitBreakerFailureThreshold = config.CircuitBreakerFailureThreshold;
            CircuitBreakerBreakDuration = config.CircuitBreakerDuration;
            PreventOverlapping = config.PreventOverlapping;
            GracefulShutdownTimeout = config.GracefulShutdownTimeout;
        }

        /// <summary>
        /// Gets the effective instance name (InstanceName or type name).
        /// </summary>
        public string GetEffectiveInstanceName()
        {
            return string.IsNullOrWhiteSpace(InstanceName) ? typeof(T).Name : InstanceName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var expression = CronExpression ?? "(run once)";
            var timezone = TimeZone ?? "Local";
            var instance = GetEffectiveInstanceName();
            return $"CronJobOptions<{typeof(T).Name}>[Instance='{instance}', Expression='{expression}', TimeZone='{timezone}', Enabled={Enabled}]";
        }
    }
}

