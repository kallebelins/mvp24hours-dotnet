//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// Interface for managing cache tags in HybridCache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tags allow grouping related cache entries for bulk invalidation.
    /// This is useful for scenarios like:
    /// <list type="bullet">
    /// <item><strong>Entity updates:</strong> Invalidate all "Product" entries when catalog changes</item>
    /// <item><strong>User sessions:</strong> Invalidate all entries for a specific user</item>
    /// <item><strong>Tenant isolation:</strong> Invalidate all entries for a tenant</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>HybridCache Native Tags:</strong>
    /// .NET 9 HybridCache has native tag support via RemoveByTagAsync().
    /// This manager provides additional tracking and statistics.
    /// </para>
    /// </remarks>
    public interface IHybridCacheTagManager
    {
        /// <summary>
        /// Tracks a cache key with its associated tags.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="tags">The tags associated with this key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrackKeyWithTagsAsync(string key, string[] tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a key from tag tracking (called when key is removed from cache).
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveKeyFromTagsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all keys associated with a tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of keys associated with the tag.</returns>
        Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tags for a key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of tags associated with the key.</returns>
        Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates a tag (removes all tracking for that tag).
        /// </summary>
        /// <param name="tag">The tag to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about tag usage.
        /// </summary>
        /// <returns>Tag statistics.</returns>
        HybridCacheTagStatistics GetStatistics();

        /// <summary>
        /// Clears all tag tracking data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about tag usage in HybridCache.
    /// </summary>
    public class HybridCacheTagStatistics
    {
        /// <summary>
        /// Gets or sets the total number of unique tags being tracked.
        /// </summary>
        public int TotalTags { get; set; }

        /// <summary>
        /// Gets or sets the total number of key-tag associations.
        /// </summary>
        public int TotalAssociations { get; set; }

        /// <summary>
        /// Gets or sets the number of tag invalidations performed.
        /// </summary>
        public long TagInvalidations { get; set; }

        /// <summary>
        /// Gets or sets detailed statistics per tag.
        /// </summary>
        public Dictionary<string, int> KeysPerTag { get; set; } = new();
    }
}

