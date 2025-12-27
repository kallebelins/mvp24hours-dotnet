//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Mvp24Hours.Infrastructure.Caching.Helpers;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for IDistributedCache with expiration support.
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Sets a string value in the cache with expiration in minutes.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="minutes">Expiration time in minutes.</param>
        public static void SetString(this IDistributedCache cache, string key, string value, int minutes)
        {
            if (cache == null || !key.HasValue() || !value.HasValue())
            {
                return;
            }
            cache.SetString(key, value, DateTimeOffset.Now.AddMinutes(minutes));
        }

        /// <summary>
        /// Sets a string value in the cache with absolute expiration time.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="time">Absolute expiration time.</param>
        public static void SetString(this IDistributedCache cache, string key, string value, DateTimeOffset time)
        {
            if (cache == null || !key.HasValue() || !value.HasValue())
            {
                return;
            }
            cache.SetString(key, value, CacheConfigHelper.GetCacheOptions(time));
        }
    }
}
