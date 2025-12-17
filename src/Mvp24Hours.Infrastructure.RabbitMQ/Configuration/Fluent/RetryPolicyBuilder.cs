//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Builder for configuring retry policies for message handling.
    /// </summary>
    public class RetryPolicyBuilder
    {
        private RetryPolicyConfiguration _configuration = new();

        /// <summary>
        /// Configures immediate retry (no delay between retries).
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Immediate(5));
        /// </code>
        /// </example>
        public RetryPolicyBuilder Immediate(int retryCount)
        {
            _configuration.RetryType = RetryType.Immediate;
            _configuration.RetryCount = retryCount;
            _configuration.InitialInterval = TimeSpan.Zero;
            return this;
        }

        /// <summary>
        /// Configures retry with fixed intervals.
        /// </summary>
        /// <param name="interval">The interval between retries.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Interval(TimeSpan.FromSeconds(5), 3));
        /// </code>
        /// </example>
        public RetryPolicyBuilder Interval(TimeSpan interval, int retryCount)
        {
            _configuration.RetryType = RetryType.FixedInterval;
            _configuration.RetryCount = retryCount;
            _configuration.InitialInterval = interval;
            return this;
        }

        /// <summary>
        /// Configures retry with specific intervals for each attempt.
        /// </summary>
        /// <param name="intervals">The intervals for each retry attempt.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Intervals(
        ///     TimeSpan.FromSeconds(1),
        ///     TimeSpan.FromSeconds(5),
        ///     TimeSpan.FromSeconds(10),
        ///     TimeSpan.FromSeconds(30)));
        /// </code>
        /// </example>
        public RetryPolicyBuilder Intervals(params TimeSpan[] intervals)
        {
            _configuration.RetryType = RetryType.CustomIntervals;
            _configuration.RetryCount = intervals.Length;
            _configuration.Intervals = new List<TimeSpan>(intervals);
            return this;
        }

        /// <summary>
        /// Configures retry with exponential backoff.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="initialInterval">The initial retry interval.</param>
        /// <param name="maxInterval">Optional maximum interval cap.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Exponential(
        ///     retryCount: 5,
        ///     initialInterval: TimeSpan.FromSeconds(1),
        ///     maxInterval: TimeSpan.FromMinutes(5)));
        /// </code>
        /// </example>
        public RetryPolicyBuilder Exponential(int retryCount, TimeSpan initialInterval, TimeSpan? maxInterval = null)
        {
            _configuration.RetryType = RetryType.Exponential;
            _configuration.RetryCount = retryCount;
            _configuration.InitialInterval = initialInterval;
            _configuration.MaxInterval = maxInterval ?? TimeSpan.FromMinutes(5);
            _configuration.ExponentialBase = 2.0;
            return this;
        }

        /// <summary>
        /// Configures retry with exponential backoff and a specific multiplier.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="initialInterval">The initial retry interval.</param>
        /// <param name="multiplier">The exponential multiplier. Default is 2.</param>
        /// <param name="maxInterval">Optional maximum interval cap.</param>
        /// <returns>The builder for chaining.</returns>
        public RetryPolicyBuilder ExponentialWithMultiplier(int retryCount, TimeSpan initialInterval, double multiplier, TimeSpan? maxInterval = null)
        {
            _configuration.RetryType = RetryType.Exponential;
            _configuration.RetryCount = retryCount;
            _configuration.InitialInterval = initialInterval;
            _configuration.MaxInterval = maxInterval ?? TimeSpan.FromMinutes(5);
            _configuration.ExponentialBase = multiplier;
            return this;
        }

        /// <summary>
        /// Configures retry with incremental intervals.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="initialInterval">The initial retry interval.</param>
        /// <param name="intervalIncrement">The increment added to each subsequent retry.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Incremental(
        ///     retryCount: 5,
        ///     initialInterval: TimeSpan.FromSeconds(1),
        ///     intervalIncrement: TimeSpan.FromSeconds(2)));
        /// // Results in: 1s, 3s, 5s, 7s, 9s
        /// </code>
        /// </example>
        public RetryPolicyBuilder Incremental(int retryCount, TimeSpan initialInterval, TimeSpan intervalIncrement)
        {
            _configuration.RetryType = RetryType.Incremental;
            _configuration.RetryCount = retryCount;
            _configuration.InitialInterval = initialInterval;
            _configuration.IntervalIncrement = intervalIncrement;
            return this;
        }

        /// <summary>
        /// Adds jitter (randomization) to retry intervals to prevent thundering herd.
        /// </summary>
        /// <param name="maxJitterPercent">The maximum jitter percentage (0-100). Default is 20%.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1)).WithJitter(25));
        /// </code>
        /// </example>
        public RetryPolicyBuilder WithJitter(int maxJitterPercent = 20)
        {
            _configuration.EnableJitter = true;
            _configuration.MaxJitterPercent = Math.Clamp(maxJitterPercent, 0, 100);
            return this;
        }

        /// <summary>
        /// Configures retry to only apply for specific exception types.
        /// </summary>
        /// <typeparam name="TException">The exception type to retry for.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1))
        ///     .Handle&lt;TimeoutException&gt;()
        ///     .Handle&lt;TransientException&gt;());
        /// </code>
        /// </example>
        public RetryPolicyBuilder Handle<TException>() where TException : Exception
        {
            _configuration.HandledExceptions.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Configures retry to ignore specific exception types.
        /// </summary>
        /// <typeparam name="TException">The exception type to ignore.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1))
        ///     .Ignore&lt;ValidationException&gt;());
        /// </code>
        /// </example>
        public RetryPolicyBuilder Ignore<TException>() where TException : Exception
        {
            _configuration.IgnoredExceptions.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Builds the retry policy configuration.
        /// </summary>
        /// <returns>The retry policy configuration.</returns>
        internal RetryPolicyConfiguration Build()
        {
            return _configuration;
        }
    }

    /// <summary>
    /// Configuration for retry policies.
    /// </summary>
    public class RetryPolicyConfiguration
    {
        /// <summary>
        /// Gets or sets the retry type.
        /// </summary>
        public RetryType RetryType { get; set; } = RetryType.Exponential;

        /// <summary>
        /// Gets or sets the number of retry attempts.
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial interval.
        /// </summary>
        public TimeSpan InitialInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum interval cap.
        /// </summary>
        public TimeSpan MaxInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the exponential base/multiplier.
        /// </summary>
        public double ExponentialBase { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the interval increment for incremental retry.
        /// </summary>
        public TimeSpan IntervalIncrement { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the custom intervals for custom interval retry.
        /// </summary>
        public List<TimeSpan> Intervals { get; set; } = new();

        /// <summary>
        /// Gets or sets whether jitter is enabled.
        /// </summary>
        public bool EnableJitter { get; set; }

        /// <summary>
        /// Gets or sets the maximum jitter percentage.
        /// </summary>
        public int MaxJitterPercent { get; set; } = 20;

        /// <summary>
        /// Gets the exception types to handle (retry for these).
        /// </summary>
        public HashSet<Type> HandledExceptions { get; } = new();

        /// <summary>
        /// Gets the exception types to ignore (don't retry for these).
        /// </summary>
        public HashSet<Type> IgnoredExceptions { get; } = new();

        /// <summary>
        /// Calculates the delay for a given retry attempt.
        /// </summary>
        /// <param name="attempt">The retry attempt number (1-based).</param>
        /// <returns>The delay before this retry.</returns>
        public TimeSpan GetDelay(int attempt)
        {
            if (attempt < 1) attempt = 1;
            if (attempt > RetryCount) attempt = RetryCount;

            TimeSpan baseDelay = RetryType switch
            {
                RetryType.Immediate => TimeSpan.Zero,
                RetryType.FixedInterval => InitialInterval,
                RetryType.CustomIntervals when Intervals.Count >= attempt => Intervals[attempt - 1],
                RetryType.CustomIntervals => InitialInterval,
                RetryType.Exponential => TimeSpan.FromTicks((long)(InitialInterval.Ticks * Math.Pow(ExponentialBase, attempt - 1))),
                RetryType.Incremental => InitialInterval + TimeSpan.FromTicks(IntervalIncrement.Ticks * (attempt - 1)),
                _ => InitialInterval
            };

            // Cap at max interval
            if (baseDelay > MaxInterval)
                baseDelay = MaxInterval;

            // Apply jitter if enabled
            if (EnableJitter && baseDelay > TimeSpan.Zero)
            {
                var jitterRange = baseDelay.TotalMilliseconds * MaxJitterPercent / 100.0;
                var jitter = (Random.Shared.NextDouble() - 0.5) * 2 * jitterRange;
                var adjustedMs = baseDelay.TotalMilliseconds + jitter;
                if (adjustedMs < 0) adjustedMs = 0;
                baseDelay = TimeSpan.FromMilliseconds(adjustedMs);
            }

            return baseDelay;
        }

        /// <summary>
        /// Determines whether the given exception should be retried.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception should be retried.</returns>
        public bool ShouldRetry(Exception exception)
        {
            var exceptionType = exception.GetType();

            // If we have ignored exceptions and this is one, don't retry
            if (IgnoredExceptions.Count > 0)
            {
                foreach (var ignored in IgnoredExceptions)
                {
                    if (ignored.IsAssignableFrom(exceptionType))
                        return false;
                }
            }

            // If we have handled exceptions specified, only retry for those
            if (HandledExceptions.Count > 0)
            {
                foreach (var handled in HandledExceptions)
                {
                    if (handled.IsAssignableFrom(exceptionType))
                        return true;
                }
                return false;
            }

            // Default: retry all exceptions
            return true;
        }
    }

    /// <summary>
    /// Types of retry strategies.
    /// </summary>
    public enum RetryType
    {
        /// <summary>
        /// No delay between retries.
        /// </summary>
        Immediate,

        /// <summary>
        /// Fixed interval between retries.
        /// </summary>
        FixedInterval,

        /// <summary>
        /// Custom intervals for each retry attempt.
        /// </summary>
        CustomIntervals,

        /// <summary>
        /// Exponential backoff between retries.
        /// </summary>
        Exponential,

        /// <summary>
        /// Incremental increase in interval between retries.
        /// </summary>
        Incremental
    }
}

