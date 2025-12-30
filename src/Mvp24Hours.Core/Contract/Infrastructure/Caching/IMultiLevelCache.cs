//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for multi-level cache implementations that combine L1 (local memory) and L2 (distributed) caches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>⚠️ DEPRECATED:</strong> This interface is deprecated in favor of .NET 9 HybridCache.
    /// Use <see cref="ICacheProvider"/> with HybridCacheProvider instead.
    /// </para>
    /// <para>
    /// <strong>Migration Guide:</strong>
    /// <code>
    /// // Before (IMultiLevelCache):
    /// services.AddMultiLevelCache();
    /// var cache = sp.GetRequiredService&lt;IMultiLevelCache&gt;();
    /// 
    /// // After (HybridCache - recommended):
    /// services.AddMvpHybridCache();
    /// var cache = sp.GetRequiredService&lt;ICacheProvider&gt;();
    /// </code>
    /// </para>
    /// <para>
    /// Multi-level caching improves performance by using a fast local cache (L1) for frequently accessed data
    /// and a distributed cache (L2) for shared data across instances. This pattern provides:
    /// <list type="bullet">
    /// <item><strong>Low Latency:</strong> L1 cache provides sub-millisecond access times</item>
    /// <item><strong>Scalability:</strong> L2 cache is shared across multiple application instances</item>
    /// <item><strong>Resilience:</strong> Automatic fallback from L2 → L1 → data source</item>
    /// <item><strong>Consistency:</strong> Synchronization between levels via pub/sub</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Cache Flow:</strong>
    /// <code>
    /// 1. Check L1 (Memory) → if found, return immediately
    /// 2. Check L2 (Distributed) → if found, promote to L1 and return
    /// 3. Load from data source → store in both L1 and L2, then return
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Write Flow:</strong>
    /// <code>
    /// 1. Write to L2 (Distributed) → ensures consistency across instances
    /// 2. Write to L1 (Memory) → fast local access
    /// 3. Publish invalidation event → other instances invalidate their L1
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register multi-level cache
    /// services.AddMultiLevelCache(options =>
    /// {
    ///     options.L1Cache = new MemoryCacheProvider(memoryCache);
    ///     options.L2Cache = new DistributedCacheProvider(distributedCache);
    ///     options.EnableSynchronization = true;
    /// });
    /// 
    /// // Use in service
    /// public class ProductService
    /// {
    ///     private readonly IMultiLevelCache _cache;
    ///     
    ///     public async Task&lt;Product&gt; GetProductAsync(int id)
    ///     {
    ///         return await _cache.GetOrSetAsync(
    ///             $"product:{id}",
    ///             async () => await _repository.GetByIdAsync(id),
    ///             CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(10))
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
    [Obsolete("Use ICacheProvider with HybridCacheProvider from Mvp24Hours.Infrastructure.Caching.HybridCache namespace instead. " +
              "HybridCache (.NET 9) provides native multi-level caching with built-in stampede protection. " +
              "See migration guide in documentation: docs/en-us/modernization/hybrid-cache.md")]
    public interface IMultiLevelCache : ICacheProvider
    {
        /// <summary>
        /// Gets a value from cache with automatic fallback: L1 → L2 → data source.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to load data if not found in cache.</param>
        /// <param name="options">Cache entry options (expiration, priority, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or loaded value.</returns>
        /// <remarks>
        /// <para>
        /// This method implements the cache-aside pattern with multi-level fallback:
        /// <list type="number">
        /// <item>Check L1 (memory cache) - fastest, local to instance</item>
        /// <item>Check L2 (distributed cache) - shared across instances</item>
        /// <item>Load from factory - data source (database, API, etc.)</item>
        /// <item>Store in both L1 and L2 for future requests</item>
        /// </list>
        /// </para>
        /// <para>
        /// If data is found in L2 but not L1, it will be automatically promoted to L1 (write-through).
        /// </para>
        /// </remarks>
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Gets a value from L1 cache only (fastest, no network call).
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value from L1, or null if not found.</returns>
        Task<T?> GetFromL1Async<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Gets a value from L2 cache only (distributed, shared across instances).
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value from L2, or null if not found.</returns>
        Task<T?> GetFromL2Async<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Promotes a value from L2 to L1 (writes to L1 if found in L2).
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="options">Cache entry options for L1 (if not found, uses L2 options).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if promotion was successful, false if key not found in L2.</returns>
        Task<bool> PromoteToL1Async<T>(string key, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Demotes a value from L1 to L2 (removes from L1, keeps in L2).
        /// Useful for memory pressure scenarios.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if demotion was successful, false if key not found in L1.</returns>
        Task<bool> DemoteFromL1Async(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in both L1 and L2 caches (write-through).
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache entry options (applied to both levels).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This method writes to both levels synchronously. If L2 write fails, L1 write is still performed
        /// to ensure local availability. Invalidation events are published to synchronize other instances.
        /// </remarks>
        Task SetBothAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Removes a value from both L1 and L2 caches.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This method removes from both levels and publishes an invalidation event to synchronize other instances.
        /// </remarks>
        Task RemoveBothAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about cache performance (hit rates, sizes, etc.).
        /// </summary>
        /// <returns>Cache statistics for both L1 and L2.</returns>
        MultiLevelCacheStatistics GetStatistics();
    }

    /// <summary>
    /// Statistics for multi-level cache performance monitoring.
    /// </summary>
    public class MultiLevelCacheStatistics
    {
        /// <summary>
        /// Gets or sets L1 cache statistics.
        /// </summary>
        public CacheLevelStatistics L1 { get; set; } = new CacheLevelStatistics();

        /// <summary>
        /// Gets or sets L2 cache statistics.
        /// </summary>
        public CacheLevelStatistics L2 { get; set; } = new CacheLevelStatistics();

        /// <summary>
        /// Gets the overall hit rate (L1 hits + L2 hits) / total requests.
        /// </summary>
        public double OverallHitRate => TotalRequests > 0 ? (double)(L1.Hits + L2.Hits) / TotalRequests : 0;

        /// <summary>
        /// Gets the total number of requests.
        /// </summary>
        public long TotalRequests => L1.Requests + L2.Requests;

        /// <summary>
        /// Gets the total number of misses (not found in either level).
        /// </summary>
        public long TotalMisses => L1.Misses + L2.Misses;
    }

    /// <summary>
    /// Statistics for a single cache level.
    /// </summary>
    public class CacheLevelStatistics
    {
        /// <summary>
        /// Gets or sets the total number of requests to this level.
        /// </summary>
        public long Requests { get; set; }

        /// <summary>
        /// Gets or sets the number of cache hits.
        /// </summary>
        public long Hits { get; set; }

        /// <summary>
        /// Gets or sets the number of cache misses.
        /// </summary>
        public long Misses { get; set; }

        /// <summary>
        /// Gets the hit rate (hits / requests).
        /// </summary>
        public double HitRate => Requests > 0 ? (double)Hits / Requests : 0;

        /// <summary>
        /// Gets or sets the number of errors encountered.
        /// </summary>
        public long Errors { get; set; }
    }
}

