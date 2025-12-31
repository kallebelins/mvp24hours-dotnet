//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Resiliency;

namespace Mvp24Hours.Infrastructure.CronJob.Interfaces
{
    /// <summary>
    /// Extended configuration interface for scheduled CronJob services with resilience policies.
    /// Combines schedule configuration with retry, circuit breaker, and overlapping execution control.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IScheduleConfig{T}"/> to include resilience configuration
    /// for robust production deployments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddResilientCronJob&lt;MyJobService&gt;(config =>
    /// {
    ///     // Schedule configuration
    ///     config.CronExpression = "*/5 * * * *"; // Every 5 minutes
    ///     config.TimeZoneInfo = TimeZoneInfo.Utc;
    ///     
    ///     // Resilience configuration
    ///     config.Resilience.EnableRetry = true;
    ///     config.Resilience.MaxRetryAttempts = 3;
    ///     config.Resilience.EnableCircuitBreaker = true;
    ///     config.Resilience.PreventOverlapping = true;
    /// });
    /// </code>
    /// </example>
    public interface IResilientScheduleConfig<T> : IScheduleConfig<T>
    {
        /// <summary>
        /// Gets or sets the resilience configuration for the CronJob.
        /// </summary>
        /// <value>
        /// The <see cref="ICronJobResilienceConfig{T}"/> containing retry, circuit breaker,
        /// and overlapping execution settings.
        /// </value>
        ICronJobResilienceConfig<T> Resilience { get; set; }
    }
}

