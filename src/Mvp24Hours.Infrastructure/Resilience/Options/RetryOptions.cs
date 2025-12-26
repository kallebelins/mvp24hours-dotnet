//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Resilience.Options
{
    /// <summary>
    /// Configuration options for retry policies (generic, non-HTTP specific).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options are used for generic retry operations across different types
    /// of operations (database, messaging, file operations, etc.), not just HTTP.
    /// </para>
    /// </remarks>
    public class RetryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts. Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retries. Default is 1 second.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum delay between retries. Default is 30 seconds.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the backoff type. Default is Exponential.
        /// </summary>
        public RetryBackoffType BackoffType { get; set; } = RetryBackoffType.Exponential;

        /// <summary>
        /// Gets or sets the jitter factor for delay randomization (0.0 to 1.0). Default is 0.1.
        /// </summary>
        /// <remarks>
        /// Jitter helps prevent thundering herd problems by randomizing retry delays.
        /// A value of 0.1 means delays can vary by up to 10%.
        /// </remarks>
        public double JitterFactor { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets whether to use exponential backoff. Default is true.
        /// </summary>
        /// <remarks>
        /// When true, delay doubles with each retry attempt (exponential backoff).
        /// When false, uses constant delay.
        /// </remarks>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should trigger a retry.
        /// If null, defaults to retrying on common transient exceptions.
        /// </summary>
        public Func<Exception, bool>? ShouldRetryOnException { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked before each retry attempt.
        /// </summary>
        public Action<RetryAttemptInfo>? OnRetry { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when all retry attempts are exhausted.
        /// </summary>
        public Action<RetryExhaustedInfo>? OnRetryExhausted { get; set; }
    }

    /// <summary>
    /// Defines the backoff strategy type for retry policies.
    /// </summary>
    public enum RetryBackoffType
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        Constant,

        /// <summary>
        /// Linear increase in delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay between retries (2^n).
        /// </summary>
        Exponential,

        /// <summary>
        /// Exponential with decorrelated jitter for better distribution.
        /// </summary>
        DecorrelatedJitter
    }

    /// <summary>
    /// Information about a retry attempt.
    /// </summary>
    public class RetryAttemptInfo
    {
        /// <summary>
        /// Gets or sets the current retry attempt number (1-based).
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxAttempts { get; set; }

        /// <summary>
        /// Gets or sets the delay before the next retry attempt.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Gets or sets the exception that triggered the retry.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the retry attempt.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Information about exhausted retry attempts.
    /// </summary>
    public class RetryExhaustedInfo
    {
        /// <summary>
        /// Gets or sets the total number of attempts made.
        /// </summary>
        public int TotalAttempts { get; set; }

        /// <summary>
        /// Gets or sets the final exception that caused failure.
        /// </summary>
        public Exception? FinalException { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when retries were exhausted.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

