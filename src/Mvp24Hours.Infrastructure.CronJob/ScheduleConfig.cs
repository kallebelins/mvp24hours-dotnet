//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using System;

namespace Mvp24Hours.Infrastructure.CronJob
{
    /// <summary>
    /// Default implementation of <see cref="IScheduleConfig{T}"/> for configuring CronJob schedules.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This class is used internally by the <c>AddCronJob&lt;T&gt;</c> extension method to store
    /// the schedule configuration for each CronJob service.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// <list type="bullet">
    /// <item><see cref="CronExpression"/>: <c>null</c> (job executes once immediately)</item>
    /// <item><see cref="TimeZoneInfo"/>: <c>null</c> (defaults to local timezone)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new ScheduleConfig&lt;MyJobService&gt;
    /// {
    ///     CronExpression = "0 0 * * *",  // Daily at midnight
    ///     TimeZoneInfo = TimeZoneInfo.Utc
    /// };
    /// </code>
    /// </example>
    public class ScheduleConfig<T> : IScheduleConfig<T>
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

        /// <summary>
        /// Returns a string representation of the schedule configuration.
        /// </summary>
        /// <returns>A string containing the CRON expression and timezone information.</returns>
        public override string ToString()
        {
            var expression = CronExpression ?? "(run once)";
            var timezone = TimeZoneInfo?.Id ?? "Local";
            return $"ScheduleConfig<{typeof(T).Name}>[Expression='{expression}', TimeZone='{timezone}']";
        }
    }
}
