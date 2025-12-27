//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for prefetching cache values asynchronously.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prefetching allows loading cache values in the background before they are needed,
    /// reducing latency for frequently accessed data. This is especially useful for:
    /// <list type="bullet">
    /// <item>Data that is accessed predictably (e.g., user profiles after login)</item>
    /// <item>Data that takes time to load (e.g., database queries, API calls)</item>
    /// <item>Data that is accessed in batches (e.g., product catalogs)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICachePrefetcher
    {
        /// <summary>
        /// Prefetches a single cache value asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value to prefetch.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">Factory function to generate the value if not cached.</param>
        /// <param name="options">Cache entry options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the prefetch operation.</returns>
        Task PrefetchAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> valueFactory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Prefetches multiple cache values asynchronously in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the values to prefetch.</typeparam>
        /// <param name="prefetchRequests">Collection of prefetch requests (key + value factory).</param>
        /// <param name="options">Cache entry options (applied to all entries).</param>
        /// <param name="maxConcurrency">Maximum number of concurrent prefetch operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the prefetch operation.</returns>
        Task PrefetchManyAsync<T>(
            IEnumerable<PrefetchRequest<T>> prefetchRequests,
            CacheEntryOptions? options = null,
            int maxConcurrency = 10,
            CancellationToken cancellationToken = default) where T : class;
    }

    /// <summary>
    /// Represents a prefetch request with key and value factory.
    /// </summary>
    /// <typeparam name="T">The type of the value to prefetch.</typeparam>
    public class PrefetchRequest<T> where T : class
    {
        /// <summary>
        /// Gets or sets the cache key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the factory function to generate the value.
        /// </summary>
        public Func<CancellationToken, Task<T>> ValueFactory { get; set; } = null!;

        /// <summary>
        /// Gets or sets optional cache entry options (overrides default options).
        /// </summary>
        public CacheEntryOptions? Options { get; set; }
    }
}

