//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Cronos;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Scheduling
{
    /// <summary>
    /// Provides parsing utilities for CRON expressions with support for both
    /// 5-field (standard) and 6-field (with seconds) formats.
    /// </summary>
    public static class CronExpressionParser
    {
        /// <summary>
        /// Parses a CRON expression string with auto-detection of format.
        /// </summary>
        /// <param name="expression">The CRON expression to parse.</param>
        /// <returns>A parsed <see cref="CronExpression"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
        /// <exception cref="CronFormatException">Thrown when the expression is invalid.</exception>
        public static CronExpression Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var format = DetectFormat(expression);
            return Parse(expression, format);
        }

        /// <summary>
        /// Parses a CRON expression string with the specified format.
        /// </summary>
        /// <param name="expression">The CRON expression to parse.</param>
        /// <param name="format">The expected format of the expression.</param>
        /// <returns>A parsed <see cref="CronExpression"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
        /// <exception cref="CronFormatException">Thrown when the expression is invalid.</exception>
        public static CronExpression Parse(string expression, CronExpressionFormat format)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var cronosFormat = format == CronExpressionFormat.WithSeconds
                ? CronFormat.IncludeSeconds
                : CronFormat.Standard;

            return CronExpression.Parse(expression, cronosFormat);
        }

        /// <summary>
        /// Tries to parse a CRON expression string with auto-detection of format.
        /// </summary>
        /// <param name="expression">The CRON expression to parse.</param>
        /// <param name="result">The parsed expression, or null if parsing failed.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        public static bool TryParse(string expression, out CronExpression? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            try
            {
                result = Parse(expression);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to parse a CRON expression string with the specified format.
        /// </summary>
        /// <param name="expression">The CRON expression to parse.</param>
        /// <param name="format">The expected format of the expression.</param>
        /// <param name="result">The parsed expression, or null if parsing failed.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        public static bool TryParse(string expression, CronExpressionFormat format, out CronExpression? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            try
            {
                result = Parse(expression, format);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detects the format of a CRON expression based on the number of fields.
        /// </summary>
        /// <param name="expression">The CRON expression to analyze.</param>
        /// <returns>The detected format.</returns>
        public static CronExpressionFormat DetectFormat(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return CronExpressionFormat.Standard;
            }

            var fields = expression.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return fields.Length >= 6 ? CronExpressionFormat.WithSeconds : CronExpressionFormat.Standard;
        }

        /// <summary>
        /// Validates a CRON expression string.
        /// </summary>
        /// <param name="expression">The CRON expression to validate.</param>
        /// <returns>True if the expression is valid, false otherwise.</returns>
        public static bool IsValid(string expression)
        {
            return TryParse(expression, out _);
        }

        /// <summary>
        /// Validates a CRON expression string with the specified format.
        /// </summary>
        /// <param name="expression">The CRON expression to validate.</param>
        /// <param name="format">The expected format of the expression.</param>
        /// <returns>True if the expression is valid, false otherwise.</returns>
        public static bool IsValid(string expression, CronExpressionFormat format)
        {
            return TryParse(expression, format, out _);
        }

        /// <summary>
        /// Gets the next occurrence of a CRON expression from a given time.
        /// </summary>
        /// <param name="expression">The CRON expression.</param>
        /// <param name="from">The starting time (defaults to now).</param>
        /// <param name="timeZone">The timezone for evaluation (defaults to UTC).</param>
        /// <returns>The next occurrence, or null if none.</returns>
        public static DateTimeOffset? GetNextOccurrence(
            string expression,
            DateTimeOffset? from = null,
            TimeZoneInfo? timeZone = null)
        {
            var parsed = Parse(expression);
            var fromTime = from ?? DateTimeOffset.UtcNow;
            var zone = timeZone ?? TimeZoneInfo.Utc;

            return parsed.GetNextOccurrence(fromTime, zone);
        }

        /// <summary>
        /// Describes a CRON expression in human-readable format.
        /// </summary>
        /// <param name="expression">The CRON expression to describe.</param>
        /// <returns>A human-readable description.</returns>
        public static string Describe(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return "Run once immediately";
            }

            var format = DetectFormat(expression);
            var fields = expression.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return format == CronExpressionFormat.WithSeconds
                ? DescribeWithSeconds(fields)
                : DescribeStandard(fields);
        }

        private static string DescribeStandard(string[] fields)
        {
            if (fields.Length < 5) return "Invalid expression";

            var minute = fields[0];
            var hour = fields[1];
            var dayOfMonth = fields[2];
            var month = fields[3];
            var dayOfWeek = fields[4];

            // Common patterns
            if (minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Every minute";

            if (minute.StartsWith("*/") && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return $"Every {minute.Substring(2)} minutes";

            if (minute == "0" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Every hour";

            if (hour.StartsWith("*/") && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return $"Every {hour.Substring(2)} hours at minute {minute}";

            if (minute == "0" && hour == "0" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Daily at midnight";

            if (dayOfMonth == "*" && month == "*" && dayOfWeek == "0")
                return $"Weekly on Sunday at {hour}:{minute.PadLeft(2, '0')}";

            if (dayOfMonth == "1" && month == "*" && dayOfWeek == "*")
                return $"Monthly on the 1st at {hour}:{minute.PadLeft(2, '0')}";

            return $"At minute {minute}, hour {hour}, day {dayOfMonth}, month {month}, weekday {dayOfWeek}";
        }

        private static string DescribeWithSeconds(string[] fields)
        {
            if (fields.Length < 6) return "Invalid expression";

            var second = fields[0];
            var minute = fields[1];
            var hour = fields[2];
            var dayOfMonth = fields[3];
            var month = fields[4];
            var dayOfWeek = fields[5];

            // Common patterns
            if (second == "*" && minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Every second";

            if (second.StartsWith("*/") && minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return $"Every {second.Substring(2)} seconds";

            if (second == "0" && minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Every minute";

            if (second == "0" && minute == "0" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
                return "Every hour";

            return $"At second {second}, minute {minute}, hour {hour}, day {dayOfMonth}, month {month}, weekday {dayOfWeek}";
        }
    }
}

