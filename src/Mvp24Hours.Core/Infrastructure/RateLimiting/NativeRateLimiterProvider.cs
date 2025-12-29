//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Infrastructure.RateLimiting
{
    /// <summary>
    /// Default implementation of <see cref="IRateLimiterProvider"/> using .NET native RateLimiter.
    /// Manages rate limiters per partition key.
    /// </summary>
    public class NativeRateLimiterProvider : IRateLimiterProvider
    {
        private readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new();
        private readonly ILogger<NativeRateLimiterProvider>? _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeRateLimiterProvider"/>.
        /// </summary>
        public NativeRateLimiterProvider(ILogger<NativeRateLimiterProvider>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public RateLimiter GetRateLimiter(string key, NativeRateLimiterOptions options)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(options);
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _rateLimiters.GetOrAdd(key, _ => CreateRateLimiter(key, options));
        }

        /// <inheritdoc />
        public async ValueTask<RateLimitLease> AcquireAsync(
            string key,
            NativeRateLimiterOptions options,
            int permitCount = 1,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(options);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var limiter = GetRateLimiter(key, options);
            var lease = await limiter.AcquireAsync(permitCount, cancellationToken);

            if (!lease.IsAcquired)
            {
                var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? retry : TimeSpan.Zero;
                _logger?.LogDebug(
                    "Rate limit acquired={Acquired} for key={Key}. RetryAfter={RetryAfter}",
                    lease.IsAcquired,
                    key,
                    retryAfter);
            }

            return lease;
        }

        /// <inheritdoc />
        public bool TryRemoveRateLimiter(string key)
        {
            if (_rateLimiters.TryRemove(key, out var limiter))
            {
                limiter.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a rate limiter based on the specified options.
        /// </summary>
        private RateLimiter CreateRateLimiter(string key, NativeRateLimiterOptions options)
        {
            _logger?.LogDebug(
                "Creating rate limiter for key={Key} with algorithm={Algorithm}, permitLimit={PermitLimit}, window={Window}",
                key,
                options.Algorithm,
                options.PermitLimit,
                options.Window);

            return options.Algorithm switch
            {
                RateLimitingAlgorithm.FixedWindow => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    QueueLimit = options.QueueLimit,
                    QueueProcessingOrder = options.QueueProcessingOrder,
                    AutoReplenishment = options.AutoReplenishment
                }),
                RateLimitingAlgorithm.SlidingWindow => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    SegmentsPerWindow = options.SegmentsPerWindow,
                    QueueLimit = options.QueueLimit,
                    QueueProcessingOrder = options.QueueProcessingOrder,
                    AutoReplenishment = options.AutoReplenishment
                }),
                RateLimitingAlgorithm.TokenBucket => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = options.PermitLimit,
                    ReplenishmentPeriod = options.ReplenishmentPeriod,
                    TokensPerPeriod = options.TokensPerPeriod,
                    QueueLimit = options.QueueLimit,
                    QueueProcessingOrder = options.QueueProcessingOrder,
                    AutoReplenishment = options.AutoReplenishment
                }),
                RateLimitingAlgorithm.Concurrency => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    QueueLimit = options.QueueLimit,
                    QueueProcessingOrder = options.QueueProcessingOrder
                }),
                _ => throw new ArgumentOutOfRangeException(nameof(options.Algorithm), options.Algorithm, "Unknown rate limiting algorithm")
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var limiter in _rateLimiters.Values)
                {
                    limiter.Dispose();
                }
                _rateLimiters.Clear();
            }
            GC.SuppressFinalize(this);
        }
    }
}

