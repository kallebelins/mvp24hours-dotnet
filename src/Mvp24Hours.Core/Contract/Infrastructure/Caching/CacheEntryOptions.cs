//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Options for configuring cache entry behavior (expiration, priority, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a unified way to configure cache entries across different
    /// cache providers. It supports both absolute and sliding expiration strategies.
    /// </para>
    /// <para>
    /// <strong>Expiration Strategies:</strong>
    /// <list type="bullet">
    /// <item><see cref="AbsoluteExpiration"/> - Entry expires at a specific point in time</item>
    /// <item><see cref="AbsoluteExpirationRelativeToNow"/> - Entry expires after a fixed duration</item>
    /// <item><see cref="SlidingExpiration"/> - Entry expiration resets on each access</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Absolute expiration (expires in 5 minutes)
    /// var options = new CacheEntryOptions
    /// {
    ///     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    /// };
    /// 
    /// // Sliding expiration (expires 2 minutes after last access)
    /// var slidingOptions = new CacheEntryOptions
    /// {
    ///     SlidingExpiration = TimeSpan.FromMinutes(2)
    /// };
    /// 
    /// // Combined (expires in 10 minutes, but resets on access)
    /// var combinedOptions = new CacheEntryOptions
    /// {
    ///     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    ///     SlidingExpiration = TimeSpan.FromMinutes(2)
    /// };
    /// </code>
    /// </example>
    public class CacheEntryOptions
    {
        /// <summary>
        /// Gets or sets the absolute expiration date and time for the cache entry.
        /// </summary>
        /// <remarks>
        /// If set, the entry will expire at this specific point in time, regardless of access.
        /// </remarks>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the absolute expiration time relative to now.
        /// </summary>
        /// <remarks>
        /// If set, the entry will expire after this duration from the time it was cached.
        /// This is more convenient than <see cref="AbsoluteExpiration"/> for relative durations.
        /// </remarks>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time for the cache entry.
        /// </summary>
        /// <remarks>
        /// If set, the entry expiration time resets each time it is accessed.
        /// The entry will expire if not accessed within this duration.
        /// </remarks>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the priority of the cache entry.
        /// </summary>
        /// <remarks>
        /// Higher priority entries are less likely to be evicted when the cache is full.
        /// </remarks>
        public CacheEntryPriority Priority { get; set; } = CacheEntryPriority.Normal;

        /// <summary>
        /// Gets or sets the tags associated with this cache entry.
        /// </summary>
        /// <remarks>
        /// Tags allow grouping related cache entries for bulk invalidation.
        /// When a tag is invalidated, all entries with that tag are removed.
        /// </remarks>
        /// <example>
        /// <code>
        /// var options = new CacheEntryOptions
        /// {
        ///     Tags = new[] { "products", "category:electronics", "active" }
        /// };
        /// </code>
        /// </example>
        public IList<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the dependencies for this cache entry.
        /// </summary>
        /// <remarks>
        /// Dependencies allow invalidating this entry when other cache keys change.
        /// When a dependency key is invalidated, this entry is also invalidated.
        /// </remarks>
        /// <example>
        /// <code>
        /// var options = new CacheEntryOptions
        /// {
        ///     Dependencies = new[] { "config:global", "user:123:permissions" }
        /// };
        /// </code>
        /// </example>
        public IList<string>? Dependencies { get; set; }

        /// <summary>
        /// Creates a new instance with absolute expiration relative to now.
        /// </summary>
        /// <param name="expiration">The expiration duration.</param>
        /// <returns>A new CacheEntryOptions instance.</returns>
        public static CacheEntryOptions FromDuration(TimeSpan expiration)
        {
            return new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
        }

        /// <summary>
        /// Creates a new instance with sliding expiration.
        /// </summary>
        /// <param name="slidingExpiration">The sliding expiration duration.</param>
        /// <returns>A new CacheEntryOptions instance.</returns>
        public static CacheEntryOptions WithSlidingExpiration(TimeSpan slidingExpiration)
        {
            return new CacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };
        }

        /// <summary>
        /// Creates a new instance with both absolute and sliding expiration.
        /// </summary>
        /// <param name="absoluteExpiration">The absolute expiration duration.</param>
        /// <param name="slidingExpiration">The sliding expiration duration.</param>
        /// <returns>A new CacheEntryOptions instance.</returns>
        public static CacheEntryOptions WithBothExpirations(TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
        {
            return new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };
        }
    }

    /// <summary>
    /// Priority levels for cache entries.
    /// </summary>
    public enum CacheEntryPriority
    {
        /// <summary>
        /// Low priority - most likely to be evicted.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - default priority.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - less likely to be evicted.
        /// </summary>
        High = 2,

        /// <summary>
        /// Never remove - should not be evicted (use with caution).
        /// </summary>
        NeverRemove = 3
    }
}

