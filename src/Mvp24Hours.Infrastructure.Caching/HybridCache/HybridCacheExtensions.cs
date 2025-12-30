//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Hybrid;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// Extension methods for HybridCache operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide convenient helper methods for common HybridCache patterns:
    /// <list type="bullet">
    /// <item><strong>GetOrCreateAsync:</strong> Get from cache or create using factory (recommended pattern)</item>
    /// <item><strong>GetOrDefaultAsync:</strong> Get from cache or return default value</item>
    /// <item><strong>SetWithTagsAsync:</strong> Set value with tags for group invalidation</item>
    /// <item><strong>InvalidateByPatternAsync:</strong> Invalidate keys matching a pattern</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Stampede Protection:</strong>
    /// The GetOrCreateAsync pattern is automatically protected against cache stampedes
    /// by HybridCache's native implementation. Only one factory call will be made even
    /// if multiple concurrent requests arrive for the same key.
    /// </para>
    /// </remarks>
    public static class HybridCacheExtensions
    {
        /// <summary>
        /// Gets a value from cache or creates it using the factory if not found.
        /// This is the recommended pattern for HybridCache usage.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="provider">The cache provider (must be HybridCacheProvider).</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not cached.</param>
        /// <param name="options">Cache entry options.</param>
        /// <param name="tags">Tags for this cache entry (for group invalidation).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if provider is not HybridCacheProvider.</exception>
        /// <example>
        /// <code>
        /// var product = await _cache.GetOrCreateAsync(
        ///     $"product:{id}",
        ///     async ct => await _repository.GetByIdAsync(id, ct),
        ///     new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
        ///     new[] { "products", $"product:{id}" });
        /// </code>
        /// </example>
        public static Task<T> GetOrCreateAsync<T>(
            this ICacheProvider provider,
            string key,
            Func<CancellationToken, ValueTask<T>> factory,
            CacheEntryOptions? options = null,
            string[]? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (provider is HybridCacheProvider hybridProvider)
            {
                return hybridProvider.GetOrCreateAsync(key, factory, options, tags, cancellationToken);
            }

            throw new InvalidOperationException(
                "GetOrCreateAsync with tags is only supported by HybridCacheProvider. " +
                "Either use HybridCacheProvider or use the standard GetAsync/SetAsync pattern.");
        }

        /// <summary>
        /// Gets a value from cache or creates it using an async factory.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="provider">The cache provider (must be HybridCacheProvider).</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Async factory function to create the value if not cached.</param>
        /// <param name="expirationMinutes">Cache expiration in minutes.</param>
        /// <param name="tags">Tags for this cache entry.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <example>
        /// <code>
        /// var product = await _cache.GetOrCreateAsync(
        ///     $"product:{id}",
        ///     () => _repository.GetByIdAsync(id),
        ///     expirationMinutes: 10,
        ///     tags: new[] { "products" });
        /// </code>
        /// </example>
        public static Task<T> GetOrCreateAsync<T>(
            this ICacheProvider provider,
            string key,
            Func<Task<T>> factory,
            int expirationMinutes = 5,
            string[]? tags = null,
            CancellationToken cancellationToken = default)
        {
            return provider.GetOrCreateAsync(
                key,
                async ct => await factory(),
                new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes) },
                tags,
                cancellationToken);
        }

        /// <summary>
        /// Gets a value from cache or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="provider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="defaultValue">The default value to return if not found.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value or the default value.</returns>
        /// <example>
        /// <code>
        /// var settings = await _cache.GetOrDefaultAsync("app:settings", new AppSettings());
        /// </code>
        /// </example>
        public static async Task<T> GetOrDefaultAsync<T>(
            this ICacheProvider provider,
            string key,
            T defaultValue,
            CancellationToken cancellationToken = default) where T : class
        {
            var value = await provider.GetAsync<T>(key, cancellationToken);
            return value ?? defaultValue;
        }

        /// <summary>
        /// Sets a value in cache with tags for group invalidation.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="provider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="tags">Tags for this cache entry.</param>
        /// <param name="expirationMinutes">Cache expiration in minutes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        /// <code>
        /// await _cache.SetWithTagsAsync(
        ///     $"product:{product.Id}",
        ///     product,
        ///     new[] { "products", $"category:{product.CategoryId}" },
        ///     expirationMinutes: 30);
        /// </code>
        /// </example>
        public static Task SetWithTagsAsync<T>(
            this ICacheProvider provider,
            string key,
            T value,
            string[] tags,
            int expirationMinutes = 5,
            CancellationToken cancellationToken = default) where T : class
        {
            var options = new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                Tags = tags
            };

            return provider.SetAsync(key, value, options, cancellationToken);
        }

        /// <summary>
        /// Invalidates all cache entries with the specified tag.
        /// Only works with HybridCacheProvider.
        /// </summary>
        /// <param name="provider">The cache provider (must be HybridCacheProvider).</param>
        /// <param name="tag">The tag to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown if provider is not HybridCacheProvider.</exception>
        /// <example>
        /// <code>
        /// // Invalidate all products when catalog changes
        /// await _cache.InvalidateByTagAsync("products");
        /// 
        /// // Invalidate all entries for a specific category
        /// await _cache.InvalidateByTagAsync($"category:{categoryId}");
        /// </code>
        /// </example>
        public static Task InvalidateByTagAsync(
            this ICacheProvider provider,
            string tag,
            CancellationToken cancellationToken = default)
        {
            if (provider is HybridCacheProvider hybridProvider)
            {
                return hybridProvider.InvalidateByTagAsync(tag, cancellationToken);
            }

            throw new InvalidOperationException(
                "InvalidateByTagAsync is only supported by HybridCacheProvider. " +
                "Use AddMvpHybridCache() to configure HybridCache.");
        }

        /// <summary>
        /// Invalidates all cache entries with any of the specified tags.
        /// Only works with HybridCacheProvider.
        /// </summary>
        /// <param name="provider">The cache provider (must be HybridCacheProvider).</param>
        /// <param name="tags">The tags to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown if provider is not HybridCacheProvider.</exception>
        /// <example>
        /// <code>
        /// // Invalidate multiple entity types at once
        /// await _cache.InvalidateByTagsAsync(new[] { "products", "categories", "inventory" });
        /// </code>
        /// </example>
        public static Task InvalidateByTagsAsync(
            this ICacheProvider provider,
            string[] tags,
            CancellationToken cancellationToken = default)
        {
            if (provider is HybridCacheProvider hybridProvider)
            {
                return hybridProvider.InvalidateByTagsAsync(tags, cancellationToken);
            }

            throw new InvalidOperationException(
                "InvalidateByTagsAsync is only supported by HybridCacheProvider. " +
                "Use AddMvpHybridCache() to configure HybridCache.");
        }

        /// <summary>
        /// Sets a value in cache with sliding expiration.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="provider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="slidingExpirationMinutes">Sliding expiration in minutes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        /// <code>
        /// // Cache user session with 30-minute sliding expiration
        /// await _cache.SetWithSlidingExpirationAsync($"session:{userId}", session, 30);
        /// </code>
        /// </example>
        public static Task SetWithSlidingExpirationAsync<T>(
            this ICacheProvider provider,
            string key,
            T value,
            int slidingExpirationMinutes,
            CancellationToken cancellationToken = default) where T : class
        {
            var options = new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(slidingExpirationMinutes)
            };

            return provider.SetAsync(key, value, options, cancellationToken);
        }

        /// <summary>
        /// Checks if a key exists in the cache without retrieving the value.
        /// </summary>
        /// <param name="provider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public static async Task<bool> ContainsKeyAsync(
            this ICacheProvider provider,
            string key,
            CancellationToken cancellationToken = default)
        {
            return await provider.ExistsAsync(key, cancellationToken);
        }

        /// <summary>
        /// Removes multiple keys from the cache by pattern (prefix matching).
        /// </summary>
        /// <param name="provider">The cache provider.</param>
        /// <param name="keyPrefix">The key prefix to match.</param>
        /// <param name="keys">Array of specific keys that match the prefix.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This method requires you to know the specific keys to remove.
        /// For tag-based invalidation, use InvalidateByTagAsync instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Remove all product cache entries for a specific category
        /// var productKeys = new[] { "product:1", "product:2", "product:3" };
        /// await _cache.RemoveByPrefixAsync("product:", productKeys);
        /// </code>
        /// </example>
        public static Task RemoveByPrefixAsync(
            this ICacheProvider provider,
            string keyPrefix,
            string[] keys,
            CancellationToken cancellationToken = default)
        {
            var keysToRemove = keys.Where(k => k.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            return provider.RemoveManyAsync(keysToRemove, cancellationToken);
        }
    }
}

