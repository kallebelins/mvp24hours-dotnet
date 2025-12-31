//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using System;

namespace Mvp24Hours.Infrastructure.CronJob
{
    /// <summary>
    /// Default implementation of <see cref="IResilientScheduleConfig{T}"/> combining
    /// schedule and resilience configuration for CronJob services.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This class extends the basic schedule configuration with comprehensive resilience settings.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// <list type="bullet">
    /// <item><see cref="CronExpression"/>: <c>null</c> (job executes once immediately)</item>
    /// <item><see cref="TimeZoneInfo"/>: <c>null</c> (defaults to local timezone)</item>
    /// <item><see cref="Resilience"/>: Default resilience config (no features enabled)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new ResilientScheduleConfig&lt;MyJobService&gt;
    /// {
    ///     CronExpression = "0 0 * * *",  // Daily at midnight
    ///     TimeZoneInfo = TimeZoneInfo.Utc,
    ///     Resilience = CronJobResilienceConfig&lt;MyJobService&gt;.FullResilience()
    /// };
    /// </code>
    /// </example>
    public class ResilientScheduleConfig<T> : IResilientScheduleConfig<T>
    {
        /// <inheritdoc />
        /// <remarks>
        /// When null or empty, the CronJob will execute once immediately and then stop.
        /// For recurring execution, provide a valid CRON expression.
        /// </remarks>
        public string? CronExpression { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// When null, the CronJob service defaults to <see cref="System.TimeZoneInfo.Local"/>.
        /// For consistent behavior across environments, consider using <see cref="System.TimeZoneInfo.Utc"/>.
        /// </remarks>
        public TimeZoneInfo? TimeZoneInfo { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Configure resilience policies including retry, circuit breaker, and overlapping execution control.
        /// </remarks>
        public ICronJobResilienceConfig<T> Resilience { get; set; } = new CronJobResilienceConfig<T>();

        /// <summary>
        /// Returns a string representation of the schedule and resilience configuration.
        /// </summary>
        /// <returns>A string containing the CRON expression, timezone, and resilience information.</returns>
        public override string ToString()
        {
            var expression = CronExpression ?? "(run once)";
            var timezone = TimeZoneInfo?.Id ?? "Local";
            return $"ResilientScheduleConfig<{typeof(T).Name}>[Expression='{expression}', TimeZone='{timezone}', {Resilience}]";
        }
    }
}

