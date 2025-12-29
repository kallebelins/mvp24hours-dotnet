//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.RateLimiting
{
    /// <summary>
    /// Provides rate limiters for different keys/partitions.
    /// </summary>
    public interface IRateLimiterProvider : IDisposable
    {
        /// <summary>
        /// Gets or creates a rate limiter for the specified key.
        /// </summary>
        /// <param name="key">The partition key for the rate limiter.</param>
        /// <param name="options">The rate limiter options to use if creating a new limiter.</param>
        /// <returns>The rate limiter for the specified key.</returns>
        RateLimiter GetRateLimiter(string key, NativeRateLimiterOptions options);

        /// <summary>
        /// Acquires a permit from the rate limiter.
        /// </summary>
        /// <param name="key">The partition key for the rate limiter.</param>
        /// <param name="options">The rate limiter options to use if creating a new limiter.</param>
        /// <param name="permitCount">The number of permits to acquire.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A lease representing the acquired permit, or a rejected lease if rate limited.</returns>
        ValueTask<RateLimitLease> AcquireAsync(
            string key,
            NativeRateLimiterOptions options,
            int permitCount = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a rate limiter from the provider.
        /// </summary>
        /// <param name="key">The partition key of the rate limiter to remove.</param>
        /// <returns>True if the rate limiter was removed; otherwise, false.</returns>
        bool TryRemoveRateLimiter(string key);
    }

    /// <summary>
    /// Options for configuring a native rate limiter.
    /// </summary>
    public class NativeRateLimiterOptions
    {
        /// <summary>
        /// Gets or sets the rate limiting algorithm to use.
        /// Default: SlidingWindow.
        /// </summary>
        public RateLimitingAlgorithm Algorithm { get; set; } = RateLimitingAlgorithm.SlidingWindow;

        /// <summary>
        /// Gets or sets the maximum number of permits allowed in the time window.
        /// Default: 100.
        /// </summary>
        public int PermitLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time window for rate limiting.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the number of segments per window (for sliding window).
        /// Default: 4.
        /// </summary>
        public int SegmentsPerWindow { get; set; } = 4;

        /// <summary>
        /// Gets or sets the token replenishment period (for token bucket).
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the number of tokens to add per period (for token bucket).
        /// Default: 10.
        /// </summary>
        public int TokensPerPeriod { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to auto-replenish tokens.
        /// Default: true.
        /// </summary>
        public bool AutoReplenishment { get; set; } = true;

        /// <summary>
        /// Gets or sets the queue limit for requests waiting for permits.
        /// Default: 0 (no queuing).
        /// </summary>
        public int QueueLimit { get; set; } = 0;

        /// <summary>
        /// Gets or sets the queue processing order.
        /// Default: OldestFirst.
        /// </summary>
        public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

        /// <summary>
        /// Creates default options for fixed window rate limiting.
        /// </summary>
        public static NativeRateLimiterOptions FixedWindow(int permitLimit = 100, TimeSpan? window = null)
        {
            return new NativeRateLimiterOptions
            {
                Algorithm = RateLimitingAlgorithm.FixedWindow,
                PermitLimit = permitLimit,
                Window = window ?? TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Creates default options for sliding window rate limiting.
        /// </summary>
        public static NativeRateLimiterOptions SlidingWindow(
            int permitLimit = 100,
            TimeSpan? window = null,
            int segmentsPerWindow = 4)
        {
            return new NativeRateLimiterOptions
            {
                Algorithm = RateLimitingAlgorithm.SlidingWindow,
                PermitLimit = permitLimit,
                Window = window ?? TimeSpan.FromMinutes(1),
                SegmentsPerWindow = segmentsPerWindow
            };
        }

        /// <summary>
        /// Creates default options for token bucket rate limiting.
        /// </summary>
        public static NativeRateLimiterOptions TokenBucket(
            int tokenLimit = 100,
            TimeSpan? replenishmentPeriod = null,
            int tokensPerPeriod = 10)
        {
            return new NativeRateLimiterOptions
            {
                Algorithm = RateLimitingAlgorithm.TokenBucket,
                PermitLimit = tokenLimit,
                ReplenishmentPeriod = replenishmentPeriod ?? TimeSpan.FromSeconds(10),
                TokensPerPeriod = tokensPerPeriod
            };
        }

        /// <summary>
        /// Creates default options for concurrency limiting.
        /// </summary>
        public static NativeRateLimiterOptions Concurrency(int permitLimit = 10, int queueLimit = 0)
        {
            return new NativeRateLimiterOptions
            {
                Algorithm = RateLimitingAlgorithm.Concurrency,
                PermitLimit = permitLimit,
                QueueLimit = queueLimit
            };
        }
    }
}

