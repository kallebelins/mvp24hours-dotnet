//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.RateLimiting
{
    /// <summary>
    /// Rate limiter for email sending operations to prevent exceeding provider limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rate limiter helps prevent hitting email provider rate limits by controlling
    /// the rate at which emails are sent. It supports multiple rate limiting strategies:
    /// - Fixed window: Maximum X emails per time window
    /// - Sliding window: Maximum X emails per rolling time window
    /// - Token bucket: Burst-friendly rate limiting
    /// </para>
    /// <para>
    /// <strong>Rate Limiting Strategies:</strong>
    /// <list type="bullet">
    /// <item>
    /// <description><strong>Fixed Window:</strong> Allows up to N emails per fixed time window (e.g., 100 emails per minute)</description>
    /// </item>
    /// <item>
    /// <description><strong>Sliding Window:</strong> Allows up to N emails per rolling time window (more accurate but more memory-intensive)</description>
    /// </item>
    /// <item>
    /// <description><strong>Token Bucket:</strong> Allows bursts up to bucket size, refills at fixed rate</description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class EmailRateLimiter
    {
        private readonly RateLimitOptions _options;
        private readonly Queue<DateTimeOffset> _requestTimestamps;
        private readonly SemaphoreSlim _semaphore;
        private DateTimeOffset _lastRefillTime;
        private int _tokens;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailRateLimiter"/> class.
        /// </summary>
        /// <param name="options">Rate limiting options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public EmailRateLimiter(RateLimitOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _requestTimestamps = new Queue<DateTimeOffset>();
            _semaphore = new SemaphoreSlim(1, 1);
            _lastRefillTime = DateTimeOffset.UtcNow;
            _tokens = options.MaxRequestsPerWindow;
        }

        /// <summary>
        /// Waits until rate limit allows sending an email.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Task that completes when rate limit allows sending.</returns>
        /// <remarks>
        /// This method will block until the rate limit allows sending another email.
        /// It automatically handles rate limit windows and token refills.
        /// </remarks>
        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var now = DateTimeOffset.UtcNow;

                switch (_options.Strategy)
                {
                    case RateLimitStrategy.FixedWindow:
                        await WaitForFixedWindowAsync(now, cancellationToken);
                        break;
                    case RateLimitStrategy.SlidingWindow:
                        await WaitForSlidingWindowAsync(now, cancellationToken);
                        break;
                    case RateLimitStrategy.TokenBucket:
                        await WaitForTokenBucketAsync(now, cancellationToken);
                        break;
                }

                _requestTimestamps.Enqueue(now);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Attempts to acquire permission to send an email without waiting.
        /// </summary>
        /// <returns>True if permission is granted; otherwise, false.</returns>
        public bool TryAcquire()
        {
            if (!_semaphore.Wait(0))
            {
                return false;
            }

            try
            {
                var now = DateTimeOffset.UtcNow;

                switch (_options.Strategy)
                {
                    case RateLimitStrategy.FixedWindow:
                        return TryAcquireFixedWindow(now);
                    case RateLimitStrategy.SlidingWindow:
                        return TryAcquireSlidingWindow(now);
                    case RateLimitStrategy.TokenBucket:
                        return TryAcquireTokenBucket(now);
                    default:
                        return false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the number of requests remaining in the current window.
        /// </summary>
        public int GetRemainingRequests()
        {
            _semaphore.Wait();
            try
            {
                var now = DateTimeOffset.UtcNow;
                CleanupOldTimestamps(now);

                return Math.Max(0, _options.MaxRequestsPerWindow - _requestTimestamps.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the time until the next request can be sent.
        /// </summary>
        public TimeSpan? GetTimeUntilNextRequest()
        {
            _semaphore.Wait();
            try
            {
                if (_requestTimestamps.Count < _options.MaxRequestsPerWindow)
                {
                    return TimeSpan.Zero;
                }

                var oldestTimestamp = _requestTimestamps.Peek();
                var windowEnd = oldestTimestamp.Add(_options.WindowSize);
                var now = DateTimeOffset.UtcNow;

                if (windowEnd > now)
                {
                    return windowEnd - now;
                }

                return TimeSpan.Zero;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task WaitForFixedWindowAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            CleanupOldTimestamps(now);

            while (_requestTimestamps.Count >= _options.MaxRequestsPerWindow)
            {
                var oldestTimestamp = _requestTimestamps.Peek();
                var waitTime = oldestTimestamp.Add(_options.WindowSize) - now;

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken);
                    now = DateTimeOffset.UtcNow;
                    CleanupOldTimestamps(now);
                }
                else
                {
                    break;
                }
            }
        }

        private async Task WaitForSlidingWindowAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            CleanupOldTimestamps(now);

            while (_requestTimestamps.Count >= _options.MaxRequestsPerWindow)
            {
                var oldestTimestamp = _requestTimestamps.Peek();
                var waitTime = oldestTimestamp.Add(_options.WindowSize) - now;

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken);
                    now = DateTimeOffset.UtcNow;
                    CleanupOldTimestamps(now);
                }
                else
                {
                    break;
                }
            }
        }

        private async Task WaitForTokenBucketAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            RefillTokens(now);

            while (_tokens <= 0)
            {
                var refillInterval = _options.WindowSize.TotalMilliseconds / _options.MaxRequestsPerWindow;
                var waitTime = TimeSpan.FromMilliseconds(refillInterval);
                await Task.Delay(waitTime, cancellationToken);
                now = DateTimeOffset.UtcNow;
                RefillTokens(now);
            }

            _tokens--;
        }

        private bool TryAcquireFixedWindow(DateTimeOffset now)
        {
            CleanupOldTimestamps(now);
            if (_requestTimestamps.Count < _options.MaxRequestsPerWindow)
            {
                return true;
            }
            return false;
        }

        private bool TryAcquireSlidingWindow(DateTimeOffset now)
        {
            CleanupOldTimestamps(now);
            if (_requestTimestamps.Count < _options.MaxRequestsPerWindow)
            {
                return true;
            }
            return false;
        }

        private bool TryAcquireTokenBucket(DateTimeOffset now)
        {
            RefillTokens(now);
            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }
            return false;
        }

        private void CleanupOldTimestamps(DateTimeOffset now)
        {
            var windowStart = now.Subtract(_options.WindowSize);
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }
        }

        private void RefillTokens(DateTimeOffset now)
        {
            var elapsed = now - _lastRefillTime;
            var tokensToAdd = (int)(elapsed.TotalMilliseconds / (_options.WindowSize.TotalMilliseconds / _options.MaxRequestsPerWindow));
            
            if (tokensToAdd > 0)
            {
                _tokens = Math.Min(_options.MaxRequestsPerWindow, _tokens + tokensToAdd);
                _lastRefillTime = now;
            }
        }
    }

    /// <summary>
    /// Options for email rate limiting.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of requests allowed per window.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time window size.
        /// </summary>
        public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the rate limiting strategy.
        /// </summary>
        public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.FixedWindow;
    }

    /// <summary>
    /// Rate limiting strategy.
    /// </summary>
    public enum RateLimitStrategy
    {
        /// <summary>
        /// Fixed window: Maximum X emails per fixed time window.
        /// </summary>
        FixedWindow = 0,

        /// <summary>
        /// Sliding window: Maximum X emails per rolling time window.
        /// </summary>
        SlidingWindow = 1,

        /// <summary>
        /// Token bucket: Allows bursts up to bucket size, refills at fixed rate.
        /// </summary>
        TokenBucket = 2
    }
}

