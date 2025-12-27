//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Extension methods for implementing the Cache-Aside pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Cache-Aside pattern (also known as Lazy Loading) is the most common caching pattern.
    /// The application is responsible for loading data into the cache on demand. When data is requested:
    /// <list type="number">
    /// <item>Check the cache first</item>
    /// <item>If found (cache hit), return cached data</item>
    /// <item>If not found (cache miss), load from data source</item>
    /// <item>Store loaded data in cache for future requests</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Simple to implement</item>
    /// <item>Cache failures don't affect application availability</item>
    /// <item>Works well with any cache provider</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Considerations:</strong>
    /// <list type="bullet">
    /// <item>Cache misses result in additional latency</item>
    /// <item>Application must handle cache invalidation</item>
    /// <item>Potential for stale data if not properly invalidated</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple usage
    /// var product = await _cache.GetOrSetAsync(
    ///     "product:123",
    ///     async () => await _repository.GetByIdAsync(123),
    ///     TimeSpan.FromMinutes(5)
    /// );
    /// 
    /// // With custom options
    /// var user = await _cache.GetOrSetAsync(
    ///     "user:456",
    ///     async () => await _userService.GetUserAsync(456),
    ///     new CacheEntryOptions
    ///     {
    ///         AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
    ///         SlidingExpiration = TimeSpan.FromMinutes(15)
    ///     }
    /// );
    /// </code>
    /// </example>
    public static class CacheAsideExtensions
    {
        /// <summary>
        /// Gets a value from the cache, or sets it using the provided factory function if not found.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory function to load data if cache miss.</param>
        /// <param name="expiration">The expiration duration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly loaded value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when cache, key, or factory is null.</exception>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan expiration,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var options = CacheEntryOptions.FromDuration(expiration);
            return await GetOrSetAsync(cache, key, factory, options, cancellationToken);
        }

        /// <summary>
        /// Gets a value from the cache, or sets it using the provided factory function if not found.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory function to load data if cache miss.</param>
        /// <param name="options">The cache entry options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly loaded value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when cache, key, or factory is null.</exception>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Try to get from cache first
            var cached = await cache.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Cache miss - load from source
            var value = await factory(cancellationToken);
            if (value != null)
            {
                await cache.SetAsync(key, value, options, cancellationToken);
            }

            return value;
        }

        /// <summary>
        /// Gets a value from the cache, or sets it using the provided factory function if not found.
        /// This overload accepts a factory that doesn't require CancellationToken.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory function to load data if cache miss.</param>
        /// <param name="expiration">The expiration duration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly loaded value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when cache, key, or factory is null.</exception>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan expiration,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var options = CacheEntryOptions.FromDuration(expiration);
            return await GetOrSetAsync(cache, key, factory, options, cancellationToken);
        }

        /// <summary>
        /// Gets a value from the cache, or sets it using the provided factory function if not found.
        /// This overload accepts a factory that doesn't require CancellationToken.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory function to load data if cache miss.</param>
        /// <param name="options">The cache entry options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly loaded value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when cache, key, or factory is null.</exception>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Try to get from cache first
            var cached = await cache.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Cache miss - load from source
            var value = await factory();
            if (value != null)
            {
                await cache.SetAsync(key, value, options, cancellationToken);
            }

            return value;
        }
    }
}

