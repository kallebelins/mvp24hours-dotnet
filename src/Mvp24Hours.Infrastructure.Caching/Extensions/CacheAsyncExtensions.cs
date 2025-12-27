//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Mvp24Hours.Infrastructure.Caching.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Async extension methods for IDistributedCache with expiration support.
    /// </summary>
    public static class CacheAsyncExtensions
    {
        /// <summary>
        /// Sets a string value in the cache asynchronously with expiration in minutes.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="minutes">Expiration time in minutes.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task SetStringAsync(this IDistributedCache cache, string key, string value, int minutes, CancellationToken token = default)
        {
            if (cache == null || !key.HasValue() || !value.HasValue())
            {
                return;
            }
            await cache.SetStringAsync(key, value, DateTimeOffset.Now.AddMinutes(minutes), token);
        }

        /// <summary>
        /// Sets a string value in the cache asynchronously with absolute expiration time.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="time">Absolute expiration time.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task SetStringAsync(this IDistributedCache cache, string key, string value, DateTimeOffset time, CancellationToken token = default)
        {
            if (cache == null || !key.HasValue() || !value.HasValue())
            {
                return;
            }
            await cache.SetStringAsync(key, value, CacheConfigHelper.GetCacheOptions(time), token);
        }
    }
}
