//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.CronJob.Scheduling
{
    /// <summary>
    /// Specifies the format of a CRON expression.
    /// </summary>
    public enum CronExpressionFormat
    {
        /// <summary>
        /// Standard 5-field format: minute hour day-of-month month day-of-week
        /// </summary>
        /// <remarks>
        /// Examples:
        /// <list type="bullet">
        /// <item><c>* * * * *</c> - Every minute</item>
        /// <item><c>0 * * * *</c> - Every hour at minute 0</item>
        /// <item><c>0 0 * * *</c> - Daily at midnight</item>
        /// </list>
        /// </remarks>
        Standard = 5,

        /// <summary>
        /// Extended 6-field format with seconds: second minute hour day-of-month month day-of-week
        /// </summary>
        /// <remarks>
        /// Examples:
        /// <list type="bullet">
        /// <item><c>* * * * * *</c> - Every second</item>
        /// <item><c>0 * * * * *</c> - Every minute at second 0</item>
        /// <item><c>*/30 * * * * *</c> - Every 30 seconds</item>
        /// </list>
        /// </remarks>
        WithSeconds = 6
    }
}

