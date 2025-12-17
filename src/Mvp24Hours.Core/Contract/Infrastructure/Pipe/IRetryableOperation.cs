//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines retry behavior for an operation.
    /// Operations implementing this interface can configure their own retry policy.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyRetryableOperation : OperationBaseAsync, IRetryableOperation
    /// {
    ///     public int MaxRetryAttempts => 3;
    ///     public TimeSpan InitialRetryDelay => TimeSpan.FromSeconds(1);
    ///     public double BackoffMultiplier => 2.0;
    ///     public TimeSpan? MaxRetryDelay => TimeSpan.FromSeconds(30);
    ///     public Type[]? RetryableExceptions => new[] { typeof(TimeoutException), typeof(HttpRequestException) };
    /// 
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         // Operation logic - will be retried on failure
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IRetryableOperation
    {
        /// <summary>
        /// Gets the maximum number of retry attempts.
        /// Default implementation returns 3.
        /// </summary>
        int MaxRetryAttempts => 3;

        /// <summary>
        /// Gets the initial delay before the first retry.
        /// Default implementation returns 1 second.
        /// </summary>
        TimeSpan InitialRetryDelay => TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets the multiplier for exponential backoff.
        /// Each retry delay is multiplied by this value.
        /// Default implementation returns 2.0 (double the delay each retry).
        /// </summary>
        double BackoffMultiplier => 2.0;

        /// <summary>
        /// Gets the maximum delay between retries.
        /// Null means no maximum limit.
        /// Default implementation returns 30 seconds.
        /// </summary>
        TimeSpan? MaxRetryDelay => TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the types of exceptions that should trigger a retry.
        /// Null or empty means all exceptions are retryable.
        /// Default implementation returns null (all exceptions).
        /// </summary>
        Type[]? RetryableExceptions => null;

        /// <summary>
        /// Determines whether a specific exception should trigger a retry.
        /// Override this for custom retry logic.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        bool ShouldRetry(Exception exception, int attemptNumber)
        {
            if (attemptNumber >= MaxRetryAttempts)
                return false;

            if (RetryableExceptions == null || RetryableExceptions.Length == 0)
                return true;

            foreach (var type in RetryableExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a retry attempt is about to be made.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="exception">The exception that triggered the retry.</param>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <param name="delay">The delay before the next retry.</param>
        void OnRetry(Exception exception, int attemptNumber, TimeSpan delay) { }
    }

    /// <summary>
    /// Configuration options for retry behavior.
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// Default: 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay before the first retry.
        /// Default: 1 second.
        /// </summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the multiplier for exponential backoff.
        /// Default: 2.0.
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// Null means no maximum limit.
        /// Default: 30 seconds.
        /// </summary>
        public TimeSpan? MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to add random jitter to retry delays.
        /// Jitter helps prevent thundering herd problems.
        /// Default: true.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum jitter percentage (0-1).
        /// Default: 0.25 (25% jitter).
        /// </summary>
        public double JitterFactor { get; set; } = 0.25;

        /// <summary>
        /// Gets or sets the types of exceptions that should trigger a retry.
        /// Null or empty means all exceptions are retryable.
        /// Default: null (all exceptions).
        /// </summary>
        public Type[]? RetryableExceptions { get; set; }

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should be retried.
        /// When set, this takes precedence over RetryableExceptions.
        /// Default: null.
        /// </summary>
        public Func<Exception, bool>? ShouldRetryPredicate { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when a retry occurs.
        /// Default: null.
        /// </summary>
        public Action<Exception, int, TimeSpan>? OnRetry { get; set; }

        /// <summary>
        /// Creates default retry options.
        /// </summary>
        public static RetryOptions Default => new();

        /// <summary>
        /// Creates retry options with no retries.
        /// </summary>
        public static RetryOptions NoRetry => new() { MaxRetryAttempts = 0 };

        /// <summary>
        /// Creates aggressive retry options for transient failures.
        /// </summary>
        public static RetryOptions Aggressive => new()
        {
            MaxRetryAttempts = 5,
            InitialRetryDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 2.0,
            MaxRetryDelay = TimeSpan.FromSeconds(10),
            UseJitter = true
        };

        /// <summary>
        /// Creates conservative retry options for expensive operations.
        /// </summary>
        public static RetryOptions Conservative => new()
        {
            MaxRetryAttempts = 2,
            InitialRetryDelay = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 3.0,
            MaxRetryDelay = TimeSpan.FromMinutes(1),
            UseJitter = true
        };

        /// <summary>
        /// Calculates the delay for the next retry attempt.
        /// </summary>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <returns>The delay before the next retry.</returns>
        public TimeSpan CalculateDelay(int attemptNumber)
        {
            var delay = TimeSpan.FromTicks((long)(InitialRetryDelay.Ticks * Math.Pow(BackoffMultiplier, attemptNumber - 1)));

            if (MaxRetryDelay.HasValue && delay > MaxRetryDelay.Value)
            {
                delay = MaxRetryDelay.Value;
            }

            if (UseJitter && JitterFactor > 0)
            {
                var random = Random.Shared;
                var jitterRange = delay.TotalMilliseconds * JitterFactor;
                var jitter = (random.NextDouble() * 2 - 1) * jitterRange;
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        }

        /// <summary>
        /// Determines whether a specific exception should trigger a retry.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        public bool ShouldRetry(Exception exception, int attemptNumber)
        {
            if (attemptNumber >= MaxRetryAttempts)
                return false;

            if (ShouldRetryPredicate != null)
                return ShouldRetryPredicate(exception);

            if (RetryableExceptions == null || RetryableExceptions.Length == 0)
                return true;

            foreach (var type in RetryableExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }
    }
}

