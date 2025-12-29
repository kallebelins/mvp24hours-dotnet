//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Options for configuring native HTTP resilience using Microsoft.Extensions.Http.Resilience.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options map to <see cref="Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions"/>.
    /// Use this class for simplified configuration in the Mvp24Hours ecosystem.
    /// </para>
    /// </remarks>
    public class NativeResilienceOptions
    {
        /// <summary>
        /// Gets or sets the total request timeout. Default is 30 seconds.
        /// </summary>
        /// <remarks>
        /// This is the overall timeout for the entire request including all retry attempts.
        /// </remarks>
        public TimeSpan TotalRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the timeout for each individual attempt. Default is 10 seconds.
        /// </summary>
        public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the maximum number of retry attempts. Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retries. Default is 2 seconds.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets whether to use jitter for retry delays. Default is true.
        /// </summary>
        /// <remarks>
        /// Jitter adds randomness to retry delays to prevent thundering herd problems.
        /// </remarks>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the failure ratio threshold for the circuit breaker. Default is 0.1 (10%).
        /// </summary>
        public double CircuitBreakerFailureRatio { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the sampling duration for the circuit breaker. Default is 30 seconds.
        /// </summary>
        public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the minimum throughput before the circuit breaker can trip. Default is 10.
        /// </summary>
        public int CircuitBreakerMinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Gets or sets the duration the circuit stays open. Default is 30 seconds.
        /// </summary>
        public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable the retry strategy. Default is true.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable the circuit breaker strategy. Default is true.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable the attempt timeout strategy. Default is true.
        /// </summary>
        public bool EnableAttemptTimeout { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable the total request timeout strategy. Default is true.
        /// </summary>
        public bool EnableTotalTimeout { get; set; } = true;

        /// <summary>
        /// Creates a preset for high-availability scenarios with more aggressive retries.
        /// </summary>
        public static NativeResilienceOptions HighAvailability => new()
        {
            TotalRequestTimeout = TimeSpan.FromMinutes(2),
            AttemptTimeout = TimeSpan.FromSeconds(15),
            MaxRetryAttempts = 5,
            RetryDelay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            CircuitBreakerFailureRatio = 0.25,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(60),
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Creates a preset for low-latency scenarios with fewer retries.
        /// </summary>
        public static NativeResilienceOptions LowLatency => new()
        {
            TotalRequestTimeout = TimeSpan.FromSeconds(10),
            AttemptTimeout = TimeSpan.FromSeconds(3),
            MaxRetryAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            UseJitter = true,
            CircuitBreakerFailureRatio = 0.1,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(15),
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Creates a preset for batch/background processing with more tolerance for failures.
        /// </summary>
        public static NativeResilienceOptions BatchProcessing => new()
        {
            TotalRequestTimeout = TimeSpan.FromMinutes(5),
            AttemptTimeout = TimeSpan.FromMinutes(1),
            MaxRetryAttempts = 10,
            RetryDelay = TimeSpan.FromSeconds(5),
            UseJitter = true,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerSamplingDuration = TimeSpan.FromMinutes(2),
            CircuitBreakerMinimumThroughput = 50,
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// Creates a preset that disables all resilience strategies (for testing).
        /// </summary>
        public static NativeResilienceOptions Disabled => new()
        {
            EnableRetry = false,
            EnableCircuitBreaker = false,
            EnableAttemptTimeout = false,
            EnableTotalTimeout = false
        };
    }
}

