//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for implementing the Refresh-Ahead caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// The Refresh-Ahead pattern proactively refreshes cached data before it expires.
    /// When data is accessed and is close to expiration, a background refresh is triggered
    /// to ensure fresh data is always available.
    /// </para>
    /// <para>
    /// <strong>How it works:</strong>
    /// <list type="number">
    /// <item>Application requests data from cache</item>
    /// <item>Cache checks expiration time</item>
    /// <item>If data is close to expiration (within refresh threshold), return cached data immediately</item>
    /// <item>Trigger background refresh to load fresh data</item>
    /// <item>Update cache with fresh data when ready</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Low latency (returns cached data immediately)</item>
    /// <item>Fresh data available before expiration</item>
    /// <item>Reduced cache misses</item>
    /// <item>Better user experience</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Considerations:</strong>
    /// <list type="bullet">
    /// <item>Additional load on data source</item>
    /// <item>More complex implementation</item>
    /// <item>Requires tracking expiration times</item>
    /// <item>May refresh data that's never accessed again</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use cases:</strong>
    /// <list type="bullet">
    /// <item>When data freshness is important</item>
    /// <item>When you want to minimize cache misses</item>
    /// <item>When background refresh is acceptable</item>
    /// <item>When data source can handle refresh load</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implementation example
    /// public class ProductRefreshAheadCache : IRefreshAheadCache&lt;Product&gt;
    /// {
    ///     public async Task&lt;Product&gt; GetAsync(string key, CancellationToken cancellationToken = default)
    ///     {
    ///         var cached = await _cache.GetAsync&lt;CachedItem&lt;Product&gt;&gt;(key, cancellationToken);
    ///         
    ///         if (cached != null)
    ///         {
    ///             // Check if refresh is needed
    ///             if (ShouldRefresh(cached))
    ///             {
    ///                 // Trigger background refresh
    ///                 _ = RefreshInBackgroundAsync(key, cancellationToken);
    ///             }
    ///             
    ///             return cached.Value;
    ///         }
    ///         
    ///         // Cache miss - load immediately
    ///         return await LoadAndCacheAsync(key, cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IRefreshAheadCache<T> where T : class
    {
        /// <summary>
        /// Gets a value from the cache, triggering a background refresh if the data is close to expiration.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value, or null if not found.</returns>
        Task<T?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually triggers a refresh for a specific key.
        /// </summary>
        /// <param name="key">The cache key to refresh.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RefreshAsync(string key, CancellationToken cancellationToken = default);
    }
}

