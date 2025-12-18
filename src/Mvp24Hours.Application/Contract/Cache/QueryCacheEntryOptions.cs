//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Configuration options for a cache entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Expiration Types:</strong>
    /// <list type="bullet">
    /// <item><strong>Absolute:</strong> Entry expires after a fixed duration from creation.</item>
    /// <item><strong>Sliding:</strong> Entry expires if not accessed within the duration (resets on each access).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// var options = new QueryCacheEntryOptions
    /// {
    ///     Duration = TimeSpan.FromMinutes(10),
    ///     UseSlidingExpiration = true,
    ///     Region = "Products",
    ///     Tags = ["category:electronics", "active"]
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public class QueryCacheEntryOptions
    {
        /// <summary>
        /// Gets or sets the cache duration.
        /// </summary>
        /// <value>The duration for which the entry should be cached. Default is 5 minutes.</value>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets a value indicating whether to use sliding expiration.
        /// </summary>
        /// <value>True to use sliding expiration; false for absolute expiration.</value>
        public bool UseSlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the cache region for grouping related entries.
        /// </summary>
        /// <value>The cache region name.</value>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets tags for the cache entry.
        /// Tags enable invalidation of multiple entries at once.
        /// </summary>
        /// <value>A collection of tags associated with this cache entry.</value>
        public IReadOnlyCollection<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets whether to enable cache stampede prevention.
        /// When enabled, only one request will execute the factory while others wait.
        /// </summary>
        /// <value>True to enable stampede prevention; default is true.</value>
        public bool EnableStampedePrevention { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum time to wait for cache stampede prevention lock.
        /// </summary>
        /// <value>The maximum wait time. Default is 30 seconds.</value>
        public TimeSpan StampedePreventionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Creates cache entry options from an <see cref="ICacheableQuery"/>.
        /// </summary>
        /// <param name="query">The cacheable query.</param>
        /// <returns>Cache entry options configured from the query.</returns>
        public static QueryCacheEntryOptions FromQuery(ICacheableQuery query)
        {
            return new QueryCacheEntryOptions
            {
                Duration = query.CacheDuration,
                UseSlidingExpiration = query.UseSlidingExpiration,
                Region = query.CacheRegion
            };
        }

        /// <summary>
        /// Creates cache entry options with absolute expiration.
        /// </summary>
        /// <param name="duration">The duration until expiration.</param>
        /// <param name="region">Optional cache region.</param>
        /// <returns>Cache entry options with absolute expiration.</returns>
        public static QueryCacheEntryOptions Absolute(TimeSpan duration, string? region = null)
        {
            return new QueryCacheEntryOptions
            {
                Duration = duration,
                UseSlidingExpiration = false,
                Region = region
            };
        }

        /// <summary>
        /// Creates cache entry options with sliding expiration.
        /// </summary>
        /// <param name="duration">The sliding window duration.</param>
        /// <param name="region">Optional cache region.</param>
        /// <returns>Cache entry options with sliding expiration.</returns>
        public static QueryCacheEntryOptions Sliding(TimeSpan duration, string? region = null)
        {
            return new QueryCacheEntryOptions
            {
                Duration = duration,
                UseSlidingExpiration = true,
                Region = region
            };
        }
    }
}

