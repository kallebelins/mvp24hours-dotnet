//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Resilience
{
    /// <summary>
    /// Extension methods for cache resilience features.
    /// </summary>
    public static class CacheResilienceExtensions
    {
        /// <summary>
        /// Wraps an existing cache provider with resilience features (circuit breaker, retry, graceful degradation).
        /// </summary>
        /// <param name="provider">The cache provider to wrap.</param>
        /// <param name="options">Resilience options (null uses defaults).</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A resilient cache provider wrapper.</returns>
        /// <example>
        /// <code>
        /// var baseProvider = new DistributedCacheProvider(...);
        /// var resilientProvider = baseProvider.WithResilience(
        ///     new CacheResilienceOptions
        ///     {
        ///         EnableCircuitBreaker = true,
        ///         EnableRetry = true,
        ///         EnableGracefulDegradation = true
        ///     });
        /// </code>
        /// </example>
        public static ICacheProvider WithResilience(
            this ICacheProvider provider,
            CacheResilienceOptions? options = null,
            ILogger<ResilientCacheProvider>? logger = null)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return new ResilientCacheProvider(provider, options, logger);
        }

        /// <summary>
        /// Gets a value from cache, or falls back to a source function if not found or cache fails.
        /// Implements the cache-aside pattern with fallback strategy.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="sourceFactory">Factory function to load data from source when cache misses or fails.</param>
        /// <param name="options">Cache entry options for storing the value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or source value.</returns>
        /// <remarks>
        /// <para>
        /// This method implements a fallback strategy:
        /// 1. Try to get from cache
        /// 2. If cache miss or failure, load from source
        /// 3. Store in cache for future requests
        /// 4. Return the value
        /// </para>
        /// <para>
        /// If cache fails and graceful degradation is enabled, the method will still
        /// return the source value without throwing exceptions.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var value = await cache.GetOrSetAsync("user:123",
        ///     async () => await userRepository.GetByIdAsync(123),
        ///     new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        /// </code>
        /// </example>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<CancellationToken, Task<T>> sourceFactory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (sourceFactory == null)
                throw new ArgumentNullException(nameof(sourceFactory));

            // Try to get from cache
            var cached = await cache.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Cache miss or failure - load from source
            var value = await sourceFactory(cancellationToken);
            if (value != null)
            {
                // Try to store in cache (don't fail if this fails)
                try
                {
                    await cache.SetAsync(key, value, options, cancellationToken);
                }
                catch
                {
                    // Ignore cache set failures - we still have the value from source
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a value from cache, or falls back to a default value if not found or cache fails.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="defaultValue">Default value to return if cache misses or fails.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value or default value.</returns>
        /// <remarks>
        /// <para>
        /// This method implements a fallback strategy:
        /// 1. Try to get from cache
        /// 2. If cache miss or failure, return default value
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var value = await cache.GetOrDefaultAsync("config:setting", new MyConfig { DefaultValue = 42 });
        /// </code>
        /// </example>
        public static async Task<T> GetOrDefaultAsync<T>(
            this ICacheProvider cache,
            string key,
            T defaultValue,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            try
            {
                var cached = await cache.GetAsync<T>(key, cancellationToken);
                return cached ?? defaultValue;
            }
            catch
            {
                // On failure, return default value
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets a value from cache with fallback strategy: cache → source → default.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="cache">The cache provider.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="sourceFactory">Factory function to load data from source.</param>
        /// <param name="defaultValue">Default value if both cache and source fail.</param>
        /// <param name="options">Cache entry options for storing the value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached, source, or default value.</returns>
        /// <remarks>
        /// <para>
        /// This method implements a complete fallback strategy:
        /// 1. Try to get from cache
        /// 2. If cache miss, try to load from source
        /// 3. If source fails, return default value
        /// 4. Store source value in cache for future requests
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var value = await cache.GetWithFallbackAsync("user:123",
        ///     async () => await userRepository.GetByIdAsync(123),
        ///     new User { Id = 123, Name = "Guest" },
        ///     new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        /// </code>
        /// </example>
        public static async Task<T> GetWithFallbackAsync<T>(
            this ICacheProvider cache,
            string key,
            Func<CancellationToken, Task<T>> sourceFactory,
            T defaultValue,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (sourceFactory == null)
                throw new ArgumentNullException(nameof(sourceFactory));
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            // Try to get from cache
            var cached = await cache.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Cache miss - try to load from source
            try
            {
                var value = await sourceFactory(cancellationToken);
                if (value != null)
                {
                    // Try to store in cache
                    try
                    {
                        await cache.SetAsync(key, value, options, cancellationToken);
                    }
                    catch
                    {
                        // Ignore cache set failures
                    }
                    return value;
                }
            }
            catch
            {
                // Source failed - fall back to default
            }

            // Both cache and source failed - return default
            return defaultValue;
        }

        /// <summary>
        /// Adds a resilient cache provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddResilientCacheProvider(options =>
        /// {
        ///     options.EnableCircuitBreaker = true;
        ///     options.EnableRetry = true;
        ///     options.EnableGracefulDegradation = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddResilientCacheProvider(
            this IServiceCollection services,
            Action<CacheResilienceOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new CacheResilienceOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(Options.Create(options));
            services.AddSingleton<ResilientCacheProvider>(sp =>
            {
                var baseProvider = sp.GetRequiredService<ICacheProvider>();
                var opts = sp.GetService<IOptions<CacheResilienceOptions>>()?.Value ?? new CacheResilienceOptions();
                var logger = sp.GetService<ILogger<ResilientCacheProvider>>();
                return new ResilientCacheProvider(baseProvider, opts, logger);
            });

            return services;
        }
    }
}

