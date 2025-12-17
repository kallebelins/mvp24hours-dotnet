//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.RateLimiting
{
    /// <summary>
    /// Redis-based distributed rate limiter using IDistributedCache.
    /// Implements sliding window algorithm for distributed rate limiting.
    /// </summary>
    public class RedisDistributedRateLimiter : IDistributedRateLimiter
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedRateLimitingOptions _options;
        private readonly ILogger<RedisDistributedRateLimiter> _logger;
        private readonly InMemoryRateLimiter? _fallbackLimiter;

        /// <summary>
        /// Initializes a new instance of <see cref="RedisDistributedRateLimiter"/>.
        /// </summary>
        public RedisDistributedRateLimiter(
            IDistributedCache cache,
            IOptions<DistributedRateLimitingOptions> options,
            ILogger<RedisDistributedRateLimiter> logger,
            InMemoryRateLimiter? fallbackLimiter = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fallbackLimiter = fallbackLimiter;
        }

        /// <inheritdoc />
        public async Task<DistributedRateLimitResult> TryAcquireAsync(
            string key,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            var fullKey = GetFullKey(key);

            try
            {
                // Get current state from cache
                var stateBytes = await _cache.GetAsync(fullKey, cancellationToken);
                var state = DeserializeState(stateBytes);

                var now = DateTimeOffset.UtcNow;

                // Check if window has expired and reset if necessary
                if (state.WindowStart.Add(window) <= now)
                {
                    state = new RateLimitState
                    {
                        Count = 0,
                        WindowStart = now
                    };
                }

                // Check if limit exceeded
                if (state.Count >= permitLimit)
                {
                    var retryAfter = state.WindowStart.Add(window) - now;
                    if (retryAfter < TimeSpan.Zero)
                        retryAfter = TimeSpan.Zero;

                    _logger.LogDebug(
                        "Rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}, RetryAfter: {RetryAfter}",
                        key, state.Count, permitLimit, retryAfter);

                    return DistributedRateLimitResult.Failure(state.Count, permitLimit, retryAfter);
                }

                // Increment count
                state.Count++;

                // Calculate expiry
                var expiry = state.WindowStart.Add(window) - now;
                if (expiry <= TimeSpan.Zero)
                    expiry = window;

                // Save state back to cache
                await _cache.SetAsync(
                    fullKey,
                    SerializeState(state),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry
                    },
                    cancellationToken);

                var timeToReset = state.WindowStart.Add(window) - now;
                return DistributedRateLimitResult.Success(state.Count, permitLimit, timeToReset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing distributed cache for rate limiting. Key: {Key}", key);

                // Fallback to in-memory if configured
                if (_options.FallbackToInMemory && _fallbackLimiter != null)
                {
                    _logger.LogWarning("Falling back to in-memory rate limiting for key: {Key}", key);
                    return await _fallbackLimiter.TryAcquireAsync(key, permitLimit, window, cancellationToken);
                }

                // If no fallback, allow the request (fail open)
                return DistributedRateLimitResult.Success(0, permitLimit);
            }
        }

        /// <inheritdoc />
        public async Task<long> GetCurrentCountAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            try
            {
                var fullKey = GetFullKey(key);
                var stateBytes = await _cache.GetAsync(fullKey, cancellationToken);
                
                if (stateBytes == null)
                    return 0;

                var state = DeserializeState(stateBytes);
                return state.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current count from distributed cache. Key: {Key}", key);
                return -1;
            }
        }

        /// <inheritdoc />
        public async Task<long> GetRemainingPermitsAsync(string key, int permitLimit, CancellationToken cancellationToken = default)
        {
            var currentCount = await GetCurrentCountAsync(key, cancellationToken);
            if (currentCount < 0)
                return permitLimit; // Unknown state, assume full permits

            return Math.Max(0, permitLimit - currentCount);
        }

        /// <inheritdoc />
        public async Task<TimeSpan?> GetTimeToResetAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            try
            {
                var fullKey = GetFullKey(key);
                var stateBytes = await _cache.GetAsync(fullKey, cancellationToken);
                
                if (stateBytes == null)
                    return null;

                var state = DeserializeState(stateBytes);
                // Note: Without knowing the window duration, we can only estimate
                // This would require storing the window duration in the state
                return _options.DefaultExpiry - (DateTimeOffset.UtcNow - state.WindowStart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time to reset from distributed cache. Key: {Key}", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            try
            {
                var fullKey = GetFullKey(key);
                await _cache.RemoveAsync(fullKey, cancellationToken);
                
                _logger.LogDebug("Rate limit reset for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limit in distributed cache. Key: {Key}", key);
            }
        }

        private string GetFullKey(string key)
        {
            return $"{_options.InstanceName}{key}";
        }

        private static byte[] SerializeState(RateLimitState state)
        {
            // Simple binary serialization: count (8 bytes) + windowStart (8 bytes)
            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), state.Count);
            BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), state.WindowStart.ToUnixTimeMilliseconds());
            return bytes;
        }

        private static RateLimitState DeserializeState(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 16)
            {
                return new RateLimitState
                {
                    Count = 0,
                    WindowStart = DateTimeOffset.UtcNow
                };
            }

            return new RateLimitState
            {
                Count = BitConverter.ToInt64(bytes, 0),
                WindowStart = DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.ToInt64(bytes, 8))
            };
        }

        private struct RateLimitState
        {
            public long Count;
            public DateTimeOffset WindowStart;
        }
    }

    /// <summary>
    /// In-memory fallback rate limiter for when distributed cache is unavailable.
    /// </summary>
    public class InMemoryRateLimiter : IDistributedRateLimiter
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, RateLimitEntry> _entries = new();
        private readonly ILogger<InMemoryRateLimiter> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryRateLimiter"/>.
        /// </summary>
        public InMemoryRateLimiter(ILogger<InMemoryRateLimiter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<DistributedRateLimitResult> TryAcquireAsync(
            string key,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            var entry = _entries.AddOrUpdate(
                key,
                _ => new RateLimitEntry { Count = 1, WindowStart = now },
                (_, existing) =>
                {
                    if (existing.WindowStart.Add(window) <= now)
                    {
                        // Window expired, reset
                        return new RateLimitEntry { Count = 1, WindowStart = now };
                    }

                    if (existing.Count >= permitLimit)
                    {
                        // Limit exceeded, don't increment
                        return existing;
                    }

                    // Increment
                    return new RateLimitEntry { Count = existing.Count + 1, WindowStart = existing.WindowStart };
                });

            var timeToReset = entry.WindowStart.Add(window) - now;
            if (timeToReset < TimeSpan.Zero)
                timeToReset = TimeSpan.Zero;

            if (entry.Count > permitLimit)
            {
                return Task.FromResult(DistributedRateLimitResult.Failure(entry.Count, permitLimit, timeToReset));
            }

            return Task.FromResult(DistributedRateLimitResult.Success(entry.Count, permitLimit, timeToReset));
        }

        /// <inheritdoc />
        public Task<long> GetCurrentCountAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                return Task.FromResult(entry.Count);
            }
            return Task.FromResult(0L);
        }

        /// <inheritdoc />
        public async Task<long> GetRemainingPermitsAsync(string key, int permitLimit, CancellationToken cancellationToken = default)
        {
            var current = await GetCurrentCountAsync(key, cancellationToken);
            return Math.Max(0, permitLimit - current);
        }

        /// <inheritdoc />
        public Task<TimeSpan?> GetTimeToResetAsync(string key, CancellationToken cancellationToken = default)
        {
            // In-memory doesn't track expiry well without the window duration
            return Task.FromResult<TimeSpan?>(null);
        }

        /// <inheritdoc />
        public Task ResetAsync(string key, CancellationToken cancellationToken = default)
        {
            _entries.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        private class RateLimitEntry
        {
            public long Count { get; init; }
            public DateTimeOffset WindowStart { get; init; }
        }
    }
}

