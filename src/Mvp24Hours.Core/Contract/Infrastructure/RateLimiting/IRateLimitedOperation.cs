//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading.RateLimiting;

namespace Mvp24Hours.Core.Contract.Infrastructure.RateLimiting
{
    /// <summary>
    /// Interface for operations that support rate limiting configuration.
    /// </summary>
    public interface IRateLimitedOperation
    {
        /// <summary>
        /// Gets the rate limiter key used to identify this operation's rate limit partition.
        /// Operations with the same key share the same rate limit.
        /// </summary>
        string RateLimiterKey { get; }

        /// <summary>
        /// Gets the rate limiting algorithm to use.
        /// </summary>
        RateLimitingAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the maximum number of permits allowed in the time window.
        /// </summary>
        int PermitLimit { get; }

        /// <summary>
        /// Gets the time window for rate limiting.
        /// </summary>
        TimeSpan Window { get; }

        /// <summary>
        /// Gets the number of segments per window (for sliding window algorithm).
        /// </summary>
        int SegmentsPerWindow { get; }

        /// <summary>
        /// Gets the token replenishment period (for token bucket algorithm).
        /// </summary>
        TimeSpan ReplenishmentPeriod { get; }

        /// <summary>
        /// Gets the number of tokens to add per replenishment period (for token bucket algorithm).
        /// </summary>
        int TokensPerPeriod { get; }

        /// <summary>
        /// Gets whether to auto-replenish tokens (for token bucket algorithm).
        /// </summary>
        bool AutoReplenishment { get; }

        /// <summary>
        /// Gets the queue limit for requests waiting for permits.
        /// </summary>
        int QueueLimit { get; }

        /// <summary>
        /// Gets the queue processing order.
        /// </summary>
        QueueProcessingOrder QueueProcessingOrder { get; }

        /// <summary>
        /// Gets the timeout for waiting in queue.
        /// </summary>
        TimeSpan? QueueTimeout { get; }

        /// <summary>
        /// Called when the operation is rate limited (rejected).
        /// </summary>
        /// <param name="retryAfter">Time to wait before retrying.</param>
        void OnRateLimited(TimeSpan? retryAfter);
    }

    /// <summary>
    /// Rate limiting algorithm types.
    /// </summary>
    public enum RateLimitingAlgorithm
    {
        /// <summary>
        /// Fixed window rate limiting. Counts requests in fixed time windows.
        /// Simple but can allow bursts at window boundaries.
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Sliding window rate limiting. Smooths the fixed window boundaries.
        /// More accurate but slightly more complex.
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Token bucket rate limiting. Allows controlled bursts with smooth refill.
        /// Good for APIs that allow occasional bursts.
        /// </summary>
        TokenBucket,

        /// <summary>
        /// Concurrency limiter. Limits concurrent requests instead of rate.
        /// Useful for resource-intensive operations.
        /// </summary>
        Concurrency
    }
}

