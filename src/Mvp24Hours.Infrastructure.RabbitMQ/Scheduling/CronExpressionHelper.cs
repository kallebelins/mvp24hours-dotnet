//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// Helper class for parsing and calculating next execution times from CRON expressions.
    /// Supports standard 5-field CRON format: minute hour day-of-month month day-of-week
    /// </summary>
    public static class CronExpressionHelper
    {
        /// <summary>
        /// Parses a CRON expression and returns the next execution time.
        /// </summary>
        /// <param name="cronExpression">The CRON expression (5 fields: minute hour day-of-month month day-of-week).</param>
        /// <param name="fromTime">The time to calculate from. Defaults to UTC now.</param>
        /// <param name="timeZone">The timezone for the schedule. Defaults to UTC.</param>
        /// <returns>The next execution time, or null if no valid time can be calculated.</returns>
        public static DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset? fromTime = null, string timeZone = "UTC")
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                throw new ArgumentException("CRON expression cannot be null or empty.", nameof(cronExpression));
            }

            var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                throw new ArgumentException("CRON expression must have exactly 5 fields: minute hour day-of-month month day-of-week", nameof(cronExpression));
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            var now = fromTime ?? DateTimeOffset.UtcNow;
            var localNow = TimeZoneInfo.ConvertTime(now, tz);

            var minutes = ParseField(parts[0], 0, 59);
            var hours = ParseField(parts[1], 0, 23);
            var daysOfMonth = ParseField(parts[2], 1, 31);
            var months = ParseField(parts[3], 1, 12);
            var daysOfWeek = ParseField(parts[4], 0, 6);

            // Start searching from the next minute
            var candidate = new DateTimeOffset(
                localNow.Year, localNow.Month, localNow.Day,
                localNow.Hour, localNow.Minute, 0,
                localNow.Offset).AddMinutes(1);

            // Search up to 4 years ahead
            var maxDate = candidate.AddYears(4);

            while (candidate < maxDate)
            {
                if (!months.Contains(candidate.Month))
                {
                    // Move to next month
                    candidate = new DateTimeOffset(
                        candidate.Year, candidate.Month, 1, 0, 0, 0, candidate.Offset)
                        .AddMonths(1);
                    continue;
                }

                if (!daysOfMonth.Contains(candidate.Day) || !daysOfWeek.Contains((int)candidate.DayOfWeek))
                {
                    // Move to next day
                    candidate = new DateTimeOffset(
                        candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, candidate.Offset)
                        .AddDays(1);
                    continue;
                }

                if (!hours.Contains(candidate.Hour))
                {
                    // Move to next hour
                    candidate = new DateTimeOffset(
                        candidate.Year, candidate.Month, candidate.Day, candidate.Hour, 0, 0, candidate.Offset)
                        .AddHours(1);
                    continue;
                }

                if (!minutes.Contains(candidate.Minute))
                {
                    candidate = candidate.AddMinutes(1);
                    continue;
                }

                // Found a match
                return TimeZoneInfo.ConvertTimeToUtc(candidate.DateTime, tz);
            }

            return null;
        }

        /// <summary>
        /// Validates a CRON expression.
        /// </summary>
        /// <param name="cronExpression">The CRON expression to validate.</param>
        /// <returns>True if valid; false otherwise.</returns>
        public static bool IsValid(string cronExpression)
        {
            try
            {
                _ = GetNextOccurrence(cronExpression);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets human-readable description of common CRON expressions.
        /// </summary>
        /// <param name="cronExpression">The CRON expression.</param>
        /// <returns>A human-readable description.</returns>
        public static string GetDescription(string cronExpression)
        {
            var commonExpressions = new Dictionary<string, string>
            {
                { "* * * * *", "Every minute" },
                { "0 * * * *", "Every hour" },
                { "0 0 * * *", "Every day at midnight" },
                { "0 0 * * 0", "Every Sunday at midnight" },
                { "0 0 1 * *", "First day of every month at midnight" },
                { "0 0 1 1 *", "Every January 1st at midnight" },
                { "*/5 * * * *", "Every 5 minutes" },
                { "*/15 * * * *", "Every 15 minutes" },
                { "*/30 * * * *", "Every 30 minutes" },
                { "0 */2 * * *", "Every 2 hours" },
                { "0 9 * * *", "Every day at 9:00 AM" },
                { "0 9 * * 1-5", "Every weekday at 9:00 AM" }
            };

            return commonExpressions.TryGetValue(cronExpression, out var description)
                ? description
                : $"Custom: {cronExpression}";
        }

        private static HashSet<int> ParseField(string field, int min, int max)
        {
            var result = new HashSet<int>();

            foreach (var part in field.Split(','))
            {
                if (part == "*")
                {
                    for (var i = min; i <= max; i++)
                    {
                        result.Add(i);
                    }
                }
                else if (part.Contains('/'))
                {
                    // Step values: */5, 0-30/5
                    var stepParts = part.Split('/');
                    var range = ParseRange(stepParts[0], min, max);
                    var step = int.Parse(stepParts[1], CultureInfo.InvariantCulture);

                    var rangeMin = range.Min();
                    var rangeMax = range.Max();

                    for (var i = rangeMin; i <= rangeMax; i += step)
                    {
                        result.Add(i);
                    }
                }
                else if (part.Contains('-'))
                {
                    // Range: 1-5
                    foreach (var value in ParseRange(part, min, max))
                    {
                        result.Add(value);
                    }
                }
                else
                {
                    // Single value
                    var value = int.Parse(part, CultureInfo.InvariantCulture);
                    if (value >= min && value <= max)
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<int> ParseRange(string part, int min, int max)
        {
            if (part == "*")
            {
                return Enumerable.Range(min, max - min + 1);
            }

            var rangeParts = part.Split('-');
            var start = int.Parse(rangeParts[0], CultureInfo.InvariantCulture);
            var end = int.Parse(rangeParts[1], CultureInfo.InvariantCulture);

            return Enumerable.Range(start, end - start + 1).Where(v => v >= min && v <= max);
        }
    }
}

