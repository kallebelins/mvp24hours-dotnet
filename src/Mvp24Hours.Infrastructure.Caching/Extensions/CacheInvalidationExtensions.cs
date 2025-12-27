//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for cache invalidation features (tags, dependencies, events).
    /// </summary>
    public static class CacheInvalidationExtensions
    {
        /// <summary>
        /// Sets a value in the cache with tags and dependencies support.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="cacheProvider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache entry options (may include tags and dependencies).</param>
        /// <param name="tagManager">Optional tag manager for tag support.</param>
        /// <param name="dependencyManager">Optional dependency manager for dependency support.</param>
        /// <param name="eventPublisher">Optional event publisher for event-based invalidation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SetWithInvalidationAsync<T>(
            this ICacheProvider cacheProvider,
            string key,
            T value,
            CacheEntryOptions? options = null,
            ICacheTagManager? tagManager = null,
            CacheDependencyManager? dependencyManager = null,
            ICacheInvalidationEventPublisher? eventPublisher = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cacheProvider == null)
                throw new ArgumentNullException(nameof(cacheProvider));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Set the value in cache
            await cacheProvider.SetAsync(key, value, options, cancellationToken);

            // Register tags if provided
            if (tagManager != null && options?.Tags != null && options.Tags.Count > 0)
            {
                await tagManager.TagKeyAsync(key, options.Tags, cancellationToken);
            }

            // Register dependencies if provided
            if (dependencyManager != null && options?.Dependencies != null && options.Dependencies.Count > 0)
            {
                await dependencyManager.RegisterDependenciesAsync(key, options.Dependencies, cancellationToken);
            }
        }

        /// <summary>
        /// Removes a value from the cache and cleans up tags/dependencies.
        /// </summary>
        /// <param name="cacheProvider">The cache provider.</param>
        /// <param name="key">The cache key to remove.</param>
        /// <param name="tagManager">Optional tag manager for tag cleanup.</param>
        /// <param name="dependencyManager">Optional dependency manager for dependency cleanup.</param>
        /// <param name="eventPublisher">Optional event publisher for event-based invalidation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task RemoveWithCleanupAsync(
            this ICacheProvider cacheProvider,
            string key,
            ICacheTagManager? tagManager = null,
            CacheDependencyManager? dependencyManager = null,
            ICacheInvalidationEventPublisher? eventPublisher = null,
            CancellationToken cancellationToken = default)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException(nameof(cacheProvider));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            // Remove from cache
            await cacheProvider.RemoveAsync(key, cancellationToken);

            // Clean up tags
            if (tagManager != null)
            {
                await tagManager.RemoveAllTagsAsync(key, cancellationToken);
            }

            // Publish invalidation event
            if (eventPublisher != null)
            {
                await eventPublisher.PublishKeyInvalidationAsync(key, cancellationToken);
            }
        }

        /// <summary>
        /// Gets a value from cache or sets it using a factory function with stampede prevention.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="cacheProvider">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory function to compute the value if not cached.</param>
        /// <param name="options">Cache entry options.</param>
        /// <param name="stampedePrevention">Optional stampede prevention.</param>
        /// <param name="tagManager">Optional tag manager for tag support.</param>
        /// <param name="dependencyManager">Optional dependency manager for dependency support.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or computed value.</returns>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cacheProvider,
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            ICacheStampedePrevention? stampedePrevention = null,
            ICacheTagManager? tagManager = null,
            CacheDependencyManager? dependencyManager = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cacheProvider == null)
                throw new ArgumentNullException(nameof(cacheProvider));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Try to get from cache
            var cached = await cacheProvider.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Use stampede prevention if available
            if (stampedePrevention != null)
            {
                return await stampedePrevention.ExecuteAsync(key, async (ct) =>
                {
                    // Double-check cache after acquiring lock
                    var doubleCheck = await cacheProvider.GetAsync<T>(key, ct);
                    if (doubleCheck != null)
                    {
                        return doubleCheck;
                    }

                    // Compute value
                    var value = await factory(ct);

                    // Set in cache with tags/dependencies
                    await SetWithInvalidationAsync(
                        cacheProvider,
                        key,
                        value,
                        options,
                        tagManager,
                        dependencyManager,
                        null,
                        ct);

                    return value;
                }, cancellationToken: cancellationToken);
            }

            // Without stampede prevention, just compute and set
            var computed = await factory(cancellationToken);
            await SetWithInvalidationAsync(
                cacheProvider,
                key,
                computed,
                options,
                tagManager,
                dependencyManager,
                null,
                cancellationToken);

            return computed;
        }
    }
}

