//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.RateLimiting
{
    /// <summary>
    /// Interface for distributed rate limiting operations.
    /// </summary>
    public interface IDistributedRateLimiter
    {
        /// <summary>
        /// Attempts to acquire a permit for the specified key.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="permitLimit">The maximum number of permits allowed.</param>
        /// <param name="window">The time window for the rate limit.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating whether the permit was acquired.</returns>
        Task<DistributedRateLimitResult> TryAcquireAsync(
            string key,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current count for the specified key.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current count, or -1 if the key doesn't exist.</returns>
        Task<long> GetCurrentCountAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the remaining permits for the specified key.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="permitLimit">The maximum number of permits allowed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of remaining permits.</returns>
        Task<long> GetRemainingPermitsAsync(string key, int permitLimit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the time until the key expires/resets.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The time until reset, or null if the key doesn't exist.</returns>
        Task<TimeSpan?> GetTimeToResetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the rate limit for the specified key.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ResetAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a distributed rate limit acquisition attempt.
    /// </summary>
    public sealed class DistributedRateLimitResult
    {
        /// <summary>
        /// Gets whether the permit was acquired.
        /// </summary>
        public bool IsAcquired { get; init; }

        /// <summary>
        /// Gets the current count after the acquisition attempt.
        /// </summary>
        public long CurrentCount { get; init; }

        /// <summary>
        /// Gets the limit for this rate limiter.
        /// </summary>
        public int Limit { get; init; }

        /// <summary>
        /// Gets the remaining permits.
        /// </summary>
        public long Remaining => Math.Max(0, Limit - CurrentCount);

        /// <summary>
        /// Gets the time until the rate limit resets.
        /// </summary>
        public TimeSpan? RetryAfter { get; init; }

        /// <summary>
        /// Gets the timestamp when the rate limit will reset.
        /// </summary>
        public DateTimeOffset? ResetAt { get; init; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static DistributedRateLimitResult Success(long currentCount, int limit, TimeSpan? retryAfter = null)
        {
            return new DistributedRateLimitResult
            {
                IsAcquired = true,
                CurrentCount = currentCount,
                Limit = limit,
                RetryAfter = retryAfter,
                ResetAt = retryAfter.HasValue ? DateTimeOffset.UtcNow.Add(retryAfter.Value) : null
            };
        }

        /// <summary>
        /// Creates a failure result (rate limit exceeded).
        /// </summary>
        public static DistributedRateLimitResult Failure(long currentCount, int limit, TimeSpan retryAfter)
        {
            return new DistributedRateLimitResult
            {
                IsAcquired = false,
                CurrentCount = currentCount,
                Limit = limit,
                RetryAfter = retryAfter,
                ResetAt = DateTimeOffset.UtcNow.Add(retryAfter)
            };
        }
    }
}

