//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Provides second-level caching capabilities for query results.
    /// This interface abstracts the underlying cache implementation (memory, distributed, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The query cache provider is responsible for:
    /// <list type="bullet">
    /// <item>Storing query results with configurable expiration</item>
    /// <item>Retrieving cached results</item>
    /// <item>Invalidating cache entries by key or region</item>
    /// <item>Supporting cache stampede prevention</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class ProductQueryService
    /// {
    ///     private readonly IQueryCacheProvider _cacheProvider;
    ///     
    ///     public async Task&lt;IList&lt;Product&gt;&gt; GetProductsAsync(GetProductsQuery query)
    ///     {
    ///         return await _cacheProvider.GetOrSetAsync(
    ///             query.GetCacheKey(),
    ///             () => _repository.GetByAsync(p => p.IsActive),
    ///             new CacheEntryOptions { Duration = query.CacheDuration }
    ///         );
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IQueryCacheProvider
    {
        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value, or default if not found.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache entry options (duration, sliding expiration, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync<T>(string key, T value, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cached value or creates and caches a new value if not found.
        /// Implements cache-aside pattern with stampede prevention.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not cached.</param>
        /// <param name="options">Cache entry options (duration, sliding expiration, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a cached entry by key.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cached entries in a specific region/group.
        /// </summary>
        /// <param name="region">The cache region to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cached entries matching a pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match (supports wildcards).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}

