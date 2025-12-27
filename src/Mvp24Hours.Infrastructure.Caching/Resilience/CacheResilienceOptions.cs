//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Resilience
{
    /// <summary>
    /// Configuration options for cache resilience (circuit breaker, retry, fallback).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control how the cache provider handles failures and implements
    /// resilience patterns to ensure graceful degradation when cache is unavailable.
    /// </para>
    /// </remarks>
    public class CacheResilienceOptions
    {
        /// <summary>
        /// Gets or sets whether circuit breaker is enabled for remote cache operations.
        /// Default is true for distributed cache providers.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets the circuit breaker options for cache operations.
        /// </summary>
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5
        };

        /// <summary>
        /// Gets or sets whether retry policy is enabled for cache operations.
        /// Default is true.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts. Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retries. Default is 100ms.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries. Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum delay between retries. Default is 5 seconds.
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether graceful degradation is enabled.
        /// When enabled, cache failures don't throw exceptions but return null/default.
        /// Default is true.
        /// </summary>
        public bool EnableGracefulDegradation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log cache failures when graceful degradation is enabled.
        /// Default is true.
        /// </summary>
        public bool LogFailures { get; set; } = true;

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should trigger retry.
        /// If null, only transient exceptions (TimeoutException, IOException, etc.) trigger retry.
        /// </summary>
        public Func<Exception, bool>? ShouldRetry { get; set; }

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should be counted as a failure
        /// for circuit breaker purposes. If null, all exceptions are counted.
        /// </summary>
        public Func<Exception, bool>? ShouldCountAsFailure { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when cache operation fails and fallback is used.
        /// </summary>
        public Action<string, Exception>? OnFallback { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when circuit breaker opens for cache.
        /// </summary>
        public Action<string>? OnCircuitBreakerOpen { get; set; }
    }
}

