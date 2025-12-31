//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Interfaces
{
    /// <summary>
    /// Configuration interface for scheduled CronJob services.
    /// Defines the CRON expression and timezone for job execution.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// The CRON expression follows the standard 5-field format: minute hour day-of-month month day-of-week.
    /// </para>
    /// <para>
    /// <strong>Examples:</strong>
    /// <list type="bullet">
    /// <item><c>0 * * * *</c> - Every hour at minute 0</item>
    /// <item><c>*/5 * * * *</c> - Every 5 minutes</item>
    /// <item><c>0 0 * * *</c> - Daily at midnight</item>
    /// <item><c>0 0 * * 0</c> - Weekly on Sunday at midnight</item>
    /// <item><c>0 0 1 * *</c> - Monthly on the 1st at midnight</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddCronJob&lt;MyJobService&gt;(config =>
    /// {
    ///     config.CronExpression = "*/5 * * * *"; // Every 5 minutes
    ///     config.TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    /// });
    /// </code>
    /// </example>
    public interface IScheduleConfig<T>
    {
        /// <summary>
        /// Gets or sets the CRON expression that defines the job schedule.
        /// </summary>
        /// <value>
        /// A valid CRON expression string in 5-field format (minute hour day-of-month month day-of-week).
        /// If null or empty, the job executes once immediately.
        /// </value>
        /// <example>
        /// <c>"0 */2 * * *"</c> - Every 2 hours at minute 0
        /// </example>
        string? CronExpression { get; set; }

        /// <summary>
        /// Gets or sets the timezone for CRON expression evaluation.
        /// </summary>
        /// <value>
        /// The <see cref="System.TimeZoneInfo"/> used to interpret the CRON expression.
        /// Defaults to <see cref="TimeZoneInfo.Local"/> if not specified.
        /// </value>
        /// <remarks>
        /// Use <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/> to get a specific timezone.
        /// For UTC, use <see cref="TimeZoneInfo.Utc"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// config.TimeZoneInfo = TimeZoneInfo.Utc;
        /// // or
        /// config.TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        /// </code>
        /// </example>
        TimeZoneInfo? TimeZoneInfo { get; set; }
    }
}
