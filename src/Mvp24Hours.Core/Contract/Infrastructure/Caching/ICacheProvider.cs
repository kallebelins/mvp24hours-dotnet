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
    /// Unified abstraction for cache providers supporting different backends (Memory, Redis, SQL, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a unified API for caching operations regardless of the underlying
    /// cache implementation. It abstracts away differences between in-memory caches, distributed
    /// caches, and other cache providers.
    /// </para>
    /// <para>
    /// <strong>Supported Operations:</strong>
    /// <list type="bullet">
    /// <item>Get/Set values with expiration</item>
    /// <item>Remove keys</item>
    /// <item>Check existence</item>
    /// <item>Batch operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register provider
    /// services.AddSingleton&lt;ICacheProvider&gt;(sp => 
    ///     new MemoryCacheProvider(sp.GetRequiredService&lt;IMemoryCache&gt;()));
    /// 
    /// // Use in service
    /// public class MyService
    /// {
    ///     private readonly ICacheProvider _cache;
    ///     
    ///     public MyService(ICacheProvider cache) => _cache = cache;
    ///     
    ///     public async Task&lt;T&gt; GetCachedDataAsync&lt;T&gt;(string key)
    ///     {
    ///         var cached = await _cache.GetAsync&lt;T&gt;(key);
    ///         if (cached != null) return cached;
    ///         
    ///         var data = await LoadDataAsync();
    ///         await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5));
    ///         return data;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ICacheProvider
    {
        /// <summary>
        /// Gets a value from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value, or null if not found.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Gets a string value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached string, or null if not found.</returns>
        Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in the cache with expiration options.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache entry options (expiration, priority, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sets a string value in the cache with expiration options.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The string value to cache.</param>
        /// <param name="options">Cache entry options (expiration, priority, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple keys from the cache.
        /// </summary>
        /// <param name="keys">The cache keys to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple values from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached values.</typeparam>
        /// <param name="keys">The cache keys.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary of key-value pairs (only includes found keys).</returns>
        Task<System.Collections.Generic.Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sets multiple values in the cache with the same expiration options.
        /// </summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="values">Dictionary of key-value pairs to cache.</param>
        /// <param name="options">Cache entry options (expiration, priority, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetManyAsync<T>(System.Collections.Generic.Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Refreshes a cached value, resetting its sliding expiration.
        /// </summary>
        /// <param name="key">The cache key to refresh.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RefreshAsync(string key, CancellationToken cancellationToken = default);
    }
}

