//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.CronJob.Scheduling;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.CronJob.Configuration
{
    /// <summary>
    /// Validates <see cref="CronJobOptions{T}"/> at application startup.
    /// Ensures CRON expressions are valid and configurations are correct.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service.</typeparam>
    /// <remarks>
    /// <para>
    /// This validator is automatically registered when using <c>AddCronJobWithOptions</c>
    /// with validation enabled. It runs at startup and will prevent the application
    /// from starting if any configuration is invalid.
    /// </para>
    /// <para>
    /// <strong>Validations performed:</strong>
    /// <list type="bullet">
    /// <item>CRON expression syntax validation</item>
    /// <item>TimeZone identifier validation</item>
    /// <item>Retry and circuit breaker parameter validation</item>
    /// <item>Timeout value validation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CronJobOptionsValidator<T> : IValidateOptions<CronJobOptions<T>>
    {
        /// <summary>
        /// Validates the specified CronJob options.
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>
        /// <see cref="ValidateOptionsResult.Success"/> if validation passes,
        /// otherwise <see cref="ValidateOptionsResult.Fail(string)"/> with error messages.
        /// </returns>
        public ValidateOptionsResult Validate(string? name, CronJobOptions<T> options)
        {
            var errors = new List<string>();
            var jobName = typeof(T).Name;

            // Validate CRON expression
            if (!string.IsNullOrWhiteSpace(options.CronExpression))
            {
                if (!CronExpressionParser.TryParse(options.CronExpression, out _))
                {
                    errors.Add($"[{jobName}] Invalid CRON expression: '{options.CronExpression}'. " +
                               $"Use 5-field (minute hour day-of-month month day-of-week) or " +
                               $"6-field (second minute hour day-of-month month day-of-week) format.");
                }
            }

            // Validate TimeZone
            if (!string.IsNullOrWhiteSpace(options.TimeZone))
            {
                try
                {
                    _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    errors.Add($"[{jobName}] Invalid TimeZone identifier: '{options.TimeZone}'. " +
                               $"Use a valid timezone ID like 'UTC', 'America/Sao_Paulo', or 'Pacific Standard Time'.");
                }
                catch (InvalidTimeZoneException)
                {
                    errors.Add($"[{jobName}] Invalid TimeZone data: '{options.TimeZone}'.");
                }
            }

            // Validate retry settings
            if (options.EnableRetry)
            {
                if (options.MaxRetryAttempts < 1)
                {
                    errors.Add($"[{jobName}] MaxRetryAttempts must be at least 1 when retry is enabled. Got: {options.MaxRetryAttempts}");
                }

                if (options.MaxRetryAttempts > 100)
                {
                    errors.Add($"[{jobName}] MaxRetryAttempts exceeds maximum allowed (100). Got: {options.MaxRetryAttempts}");
                }

                if (options.RetryDelay < TimeSpan.Zero)
                {
                    errors.Add($"[{jobName}] RetryDelay cannot be negative. Got: {options.RetryDelay}");
                }

                if (options.RetryDelay > TimeSpan.FromHours(1))
                {
                    errors.Add($"[{jobName}] RetryDelay exceeds maximum allowed (1 hour). Got: {options.RetryDelay}");
                }
            }

            // Validate circuit breaker settings
            if (options.EnableCircuitBreaker)
            {
                if (options.CircuitBreakerFailureThreshold < 1)
                {
                    errors.Add($"[{jobName}] CircuitBreakerFailureThreshold must be at least 1. Got: {options.CircuitBreakerFailureThreshold}");
                }

                if (options.CircuitBreakerFailureThreshold > 1000)
                {
                    errors.Add($"[{jobName}] CircuitBreakerFailureThreshold exceeds maximum allowed (1000). Got: {options.CircuitBreakerFailureThreshold}");
                }

                if (options.CircuitBreakerBreakDuration < TimeSpan.FromSeconds(1))
                {
                    errors.Add($"[{jobName}] CircuitBreakerBreakDuration must be at least 1 second. Got: {options.CircuitBreakerBreakDuration}");
                }

                if (options.CircuitBreakerBreakDuration > TimeSpan.FromHours(24))
                {
                    errors.Add($"[{jobName}] CircuitBreakerBreakDuration exceeds maximum allowed (24 hours). Got: {options.CircuitBreakerBreakDuration}");
                }
            }

            // Validate timeout settings
            if (options.GracefulShutdownTimeout < TimeSpan.Zero)
            {
                errors.Add($"[{jobName}] GracefulShutdownTimeout cannot be negative. Got: {options.GracefulShutdownTimeout}");
            }

            if (options.GracefulShutdownTimeout > TimeSpan.FromMinutes(30))
            {
                errors.Add($"[{jobName}] GracefulShutdownTimeout exceeds maximum allowed (30 minutes). Got: {options.GracefulShutdownTimeout}");
            }

            // Validate distributed locking settings
            if (options.EnableDistributedLocking)
            {
                if (options.DistributedLockExpiry < TimeSpan.FromSeconds(5))
                {
                    errors.Add($"[{jobName}] DistributedLockExpiry must be at least 5 seconds. Got: {options.DistributedLockExpiry}");
                }

                if (options.DistributedLockExpiry > TimeSpan.FromHours(24))
                {
                    errors.Add($"[{jobName}] DistributedLockExpiry exceeds maximum allowed (24 hours). Got: {options.DistributedLockExpiry}");
                }
            }

            // Validate instance name
            if (!string.IsNullOrWhiteSpace(options.InstanceName))
            {
                if (options.InstanceName.Length > 100)
                {
                    errors.Add($"[{jobName}] InstanceName exceeds maximum length (100). Got: {options.InstanceName.Length} characters");
                }

                // Check for invalid characters
                foreach (char c in options.InstanceName)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    {
                        errors.Add($"[{jobName}] InstanceName contains invalid character: '{c}'. Only letters, digits, hyphens, and underscores are allowed.");
                        break;
                    }
                }
            }

            // Return result
            if (errors.Count > 0)
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates <see cref="CronJobGlobalOptions"/> at application startup.
    /// </summary>
    public class CronJobGlobalOptionsValidator : IValidateOptions<CronJobGlobalOptions>
    {
        /// <summary>
        /// Validates the specified global options.
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>
        /// <see cref="ValidateOptionsResult.Success"/> if validation passes,
        /// otherwise <see cref="ValidateOptionsResult.Fail(string)"/> with error messages.
        /// </returns>
        public ValidateOptionsResult Validate(string? name, CronJobGlobalOptions options)
        {
            var errors = new List<string>();

            // Validate default TimeZone
            if (!string.IsNullOrWhiteSpace(options.DefaultTimeZone))
            {
                try
                {
                    _ = TimeZoneInfo.FindSystemTimeZoneById(options.DefaultTimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    errors.Add($"[CronJobGlobalOptions] Invalid DefaultTimeZone identifier: '{options.DefaultTimeZone}'.");
                }
                catch (InvalidTimeZoneException)
                {
                    errors.Add($"[CronJobGlobalOptions] Invalid DefaultTimeZone data: '{options.DefaultTimeZone}'.");
                }
            }

            // Validate retry settings
            if (options.DefaultMaxRetryAttempts < 1 || options.DefaultMaxRetryAttempts > 100)
            {
                errors.Add($"[CronJobGlobalOptions] DefaultMaxRetryAttempts must be between 1 and 100. Got: {options.DefaultMaxRetryAttempts}");
            }

            if (options.DefaultRetryDelay < TimeSpan.Zero || options.DefaultRetryDelay > TimeSpan.FromHours(1))
            {
                errors.Add($"[CronJobGlobalOptions] DefaultRetryDelay must be between 0 and 1 hour. Got: {options.DefaultRetryDelay}");
            }

            // Validate circuit breaker settings
            if (options.DefaultCircuitBreakerFailureThreshold < 1 || options.DefaultCircuitBreakerFailureThreshold > 1000)
            {
                errors.Add($"[CronJobGlobalOptions] DefaultCircuitBreakerFailureThreshold must be between 1 and 1000. Got: {options.DefaultCircuitBreakerFailureThreshold}");
            }

            if (options.DefaultCircuitBreakerBreakDuration < TimeSpan.FromSeconds(1) || options.DefaultCircuitBreakerBreakDuration > TimeSpan.FromHours(24))
            {
                errors.Add($"[CronJobGlobalOptions] DefaultCircuitBreakerBreakDuration must be between 1 second and 24 hours. Got: {options.DefaultCircuitBreakerBreakDuration}");
            }

            // Validate timeout settings
            if (options.DefaultGracefulShutdownTimeout < TimeSpan.Zero || options.DefaultGracefulShutdownTimeout > TimeSpan.FromMinutes(30))
            {
                errors.Add($"[CronJobGlobalOptions] DefaultGracefulShutdownTimeout must be between 0 and 30 minutes. Got: {options.DefaultGracefulShutdownTimeout}");
            }

            // Validate distributed lock settings
            if (options.DefaultDistributedLockExpiry < TimeSpan.FromSeconds(5) || options.DefaultDistributedLockExpiry > TimeSpan.FromHours(24))
            {
                errors.Add($"[CronJobGlobalOptions] DefaultDistributedLockExpiry must be between 5 seconds and 24 hours. Got: {options.DefaultDistributedLockExpiry}");
            }

            // Validate health check name
            if (string.IsNullOrWhiteSpace(options.AggregateHealthCheckName))
            {
                errors.Add("[CronJobGlobalOptions] AggregateHealthCheckName cannot be null or empty.");
            }

            if (errors.Count > 0)
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }
}

