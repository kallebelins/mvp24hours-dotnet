//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Manages cache tags for invalidation by group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cache tags allow grouping related cache entries together. When a tag is invalidated,
    /// all entries associated with that tag are removed from the cache.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Invalidate all product-related cache when a product is updated</item>
    /// <item>Invalidate all user-related cache when user permissions change</item>
    /// <item>Invalidate all category-related cache when category structure changes</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set cache with tags
    /// await cache.SetAsync("product:123", product, new CacheEntryOptions
    /// {
    ///     Tags = new[] { "products", "category:electronics", "active" }
    /// });
    /// 
    /// // Invalidate all entries with tag "products"
    /// await tagManager.InvalidateByTagAsync("products");
    /// </code>
    /// </example>
    public interface ICacheTagManager
    {
        /// <summary>
        /// Associates a cache key with one or more tags.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="tags">The tags to associate with the key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TagKeyAsync(string key, IEnumerable<string> tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all keys associated with a specific tag.
        /// </summary>
        /// <param name="tag">The tag to query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of cache keys associated with the tag.</returns>
        Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cache entries associated with a specific tag.
        /// </summary>
        /// <param name="tag">The tag to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of keys invalidated.</returns>
        Task<int> InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cache entries associated with multiple tags.
        /// </summary>
        /// <param name="tags">The tags to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of keys invalidated.</returns>
        Task<int> InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a tag association from a cache key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="tags">The tags to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveTagsAsync(string key, IEnumerable<string> tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all tag associations for a cache key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAllTagsAsync(string key, CancellationToken cancellationToken = default);
    }
}

