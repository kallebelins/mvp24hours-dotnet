//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// ICacheProvider implementation using .NET 9 HybridCache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider wraps the native .NET 9 HybridCache to provide a unified
    /// ICacheProvider interface while leveraging all HybridCache benefits:
    /// <list type="bullet">
    /// <item><strong>Multi-level caching:</strong> Automatic L1 (memory) + L2 (distributed)</item>
    /// <item><strong>Stampede protection:</strong> Built-in prevention of cache stampedes</item>
    /// <item><strong>Efficient serialization:</strong> Optimized for performance</item>
    /// <item><strong>Tag-based invalidation:</strong> Invalidate groups of entries</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Migration from MultiLevelCache:</strong>
    /// HybridCacheProvider is the recommended replacement for the custom MultiLevelCache.
    /// It provides the same functionality with better performance and less code.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register via DI
    /// services.AddMvpHybridCache();
    /// 
    /// // Inject and use
    /// public class MyService
    /// {
    ///     private readonly ICacheProvider _cache;
    ///     
    ///     public MyService(ICacheProvider cache) => _cache = cache;
    ///     
    ///     public async Task&lt;Product&gt; GetProductAsync(int id)
    ///     {
    ///         var key = $"product:{id}";
    ///         var product = await _cache.GetAsync&lt;Product&gt;(key);
    ///         if (product == null)
    ///         {
    ///             product = await LoadFromDatabaseAsync(id);
    ///             await _cache.SetAsync(key, product, new CacheEntryOptions
    ///             {
    ///                 AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    ///             });
    ///         }
    ///         return product;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HybridCacheProvider : ICacheProvider
    {
        private readonly Microsoft.Extensions.Caching.Hybrid.HybridCache _hybridCache;
        private readonly MvpHybridCacheOptions _options;
        private readonly ILogger<HybridCacheProvider>? _logger;
        private readonly IHybridCacheTagManager? _tagManager;

        /// <summary>
        /// Creates a new instance of HybridCacheProvider.
        /// </summary>
        /// <param name="hybridCache">The .NET 9 HybridCache instance.</param>
        /// <param name="options">Configuration options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="tagManager">Optional tag manager for tag-based invalidation.</param>
        public HybridCacheProvider(
            Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache,
            IOptions<MvpHybridCacheOptions> options,
            ILogger<HybridCacheProvider>? logger = null,
            IHybridCacheTagManager? tagManager = null)
        {
            _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
            _options = options?.Value ?? new MvpHybridCacheOptions();
            _logger = logger;
            _tagManager = tagManager;
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var fullKey = GetFullKey(key);

            try
            {
                // HybridCache doesn't have a direct GetAsync without factory
                // We use GetOrCreateAsync with a factory that returns null-ish sentinel
                var result = await _hybridCache.GetOrCreateAsync<CacheWrapper<T>>(
                    fullKey,
                    ct => ValueTask.FromResult<CacheWrapper<T>>(new CacheWrapper<T> { Value = default, HasValue = false }),
                    cancellationToken: cancellationToken);

                if (result?.HasValue == true)
                {
                    LogDebug("HybridCache HIT: {Key}", key);
                    return result.Value;
                }

                LogDebug("HybridCache MISS: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting {Key} from HybridCache", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var fullKey = GetFullKey(key);

            try
            {
                var result = await _hybridCache.GetOrCreateAsync<StringWrapper>(
                    fullKey,
                    ct => ValueTask.FromResult(new StringWrapper { Value = null, HasValue = false }),
                    cancellationToken: cancellationToken);

                if (result?.HasValue == true)
                {
                    LogDebug("HybridCache HIT (string): {Key}", key);
                    return result.Value;
                }

                LogDebug("HybridCache MISS (string): {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting string {Key} from HybridCache", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var fullKey = GetFullKey(key);
            var entryOptions = ConvertToHybridCacheEntryOptions(options);
            var tags = GetTags(options);

            try
            {
                await _hybridCache.SetAsync(
                    fullKey,
                    new CacheWrapper<T> { Value = value, HasValue = true },
                    entryOptions,
                    tags,
                    cancellationToken);

                // Track tags if tag manager is available
                if (_tagManager != null && tags?.Length > 0)
                {
                    await _tagManager.TrackKeyWithTagsAsync(key, tags, cancellationToken);
                }

                LogDebug("HybridCache SET: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting {Key} in HybridCache", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var fullKey = GetFullKey(key);
            var entryOptions = ConvertToHybridCacheEntryOptions(options);
            var tags = GetTags(options);

            try
            {
                await _hybridCache.SetAsync(
                    fullKey,
                    new StringWrapper { Value = value, HasValue = true },
                    entryOptions,
                    tags,
                    cancellationToken);

                if (_tagManager != null && tags?.Length > 0)
                {
                    await _tagManager.TrackKeyWithTagsAsync(key, tags, cancellationToken);
                }

                LogDebug("HybridCache SET (string): {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting string {Key} in HybridCache", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var fullKey = GetFullKey(key);

            try
            {
                await _hybridCache.RemoveAsync(fullKey, cancellationToken);

                // Remove from tag tracking
                if (_tagManager != null)
                {
                    await _tagManager.RemoveKeyFromTagsAsync(key, cancellationToken);
                }

                LogDebug("HybridCache REMOVE: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing {Key} from HybridCache", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
                return;

            try
            {
                var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
                await Task.WhenAll(tasks);

                LogDebug("HybridCache REMOVE_MANY: {Count} keys", keys.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing {Count} keys from HybridCache", keys.Length);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // HybridCache doesn't have a direct Exists method
            // We need to try to get the value
            var result = await GetAsync<object>(key, cancellationToken);
            return result != null;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null || keys.Length == 0)
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            // HybridCache doesn't have native batch get, so we parallelize
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return (key, value);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (key, value) in results)
            {
                if (value != null)
                {
                    result[key] = value;
                }
            }

            LogDebug("HybridCache GET_MANY: {Found}/{Total} keys found", result.Count, keys.Length);
            return result;
        }

        /// <inheritdoc />
        public async Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (values == null || values.Count == 0)
                return;

            var tasks = values.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, cancellationToken));
            await Task.WhenAll(tasks);

            LogDebug("HybridCache SET_MANY: {Count} keys", values.Count);
        }

        /// <inheritdoc />
        public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            // HybridCache doesn't have a refresh method
            // This is a no-op as HybridCache handles expiration automatically
            LogDebug("HybridCache REFRESH (no-op): {Key}", key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets or creates a value in the cache using a factory function.
        /// This is the recommended pattern for HybridCache usage.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not cached.</param>
        /// <param name="options">Cache entry options.</param>
        /// <param name="tags">Tags for this cache entry.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <remarks>
        /// This method leverages HybridCache's native GetOrCreateAsync which includes:
        /// <list type="bullet">
        /// <item>Automatic stampede protection</item>
        /// <item>L1/L2 coordination</item>
        /// <item>Efficient serialization</item>
        /// </list>
        /// </remarks>
        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, ValueTask<T>> factory,
            CacheEntryOptions? options = null,
            string[]? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var fullKey = GetFullKey(key);
            var entryOptions = ConvertToHybridCacheEntryOptions(options);
            var allTags = GetTags(options, tags);

            try
            {
                var result = await _hybridCache.GetOrCreateAsync(
                    fullKey,
                    factory,
                    entryOptions,
                    allTags,
                    cancellationToken);

                // Track tags if tag manager is available
                if (_tagManager != null && allTags?.Length > 0)
                {
                    await _tagManager.TrackKeyWithTagsAsync(key, allTags, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetOrCreateAsync for {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Invalidates all cache entries with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

            try
            {
                await _hybridCache.RemoveByTagAsync(tag, cancellationToken);

                // Also clean up tag tracking
                if (_tagManager != null)
                {
                    await _tagManager.InvalidateTagAsync(tag, cancellationToken);
                }

                LogDebug("HybridCache INVALIDATE_TAG: {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error invalidating tag {Tag}", tag);
                throw;
            }
        }

        /// <summary>
        /// Invalidates all cache entries with any of the specified tags.
        /// </summary>
        /// <param name="tags">The tags to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
        {
            if (tags == null || tags.Length == 0)
                return;

            var tasks = tags.Select(tag => InvalidateByTagAsync(tag, cancellationToken));
            await Task.WhenAll(tasks);

            LogDebug("HybridCache INVALIDATE_TAGS: {Count} tags", tags.Length);
        }

        #region Private Helpers

        private string GetFullKey(string key)
        {
            if (string.IsNullOrEmpty(_options.KeyPrefix))
                return key;

            return $"{_options.KeyPrefix}{key}";
        }

        private HybridCacheEntryOptions? ConvertToHybridCacheEntryOptions(CacheEntryOptions? options)
        {
            if (options == null)
            {
                return new HybridCacheEntryOptions
                {
                    Expiration = _options.DefaultExpiration,
                    LocalCacheExpiration = _options.DefaultLocalCacheExpiration ?? _options.DefaultExpiration
                };
            }

            return new HybridCacheEntryOptions
            {
                Expiration = options.AbsoluteExpirationRelativeToNow ?? _options.DefaultExpiration,
                LocalCacheExpiration = options.SlidingExpiration ?? _options.DefaultLocalCacheExpiration ?? _options.DefaultExpiration
            };
        }

        private string[]? GetTags(CacheEntryOptions? options, string[]? additionalTags = null)
        {
            var tags = new List<string>();

            // Add default tags from options
            if (_options.DefaultTags?.Count > 0)
            {
                tags.AddRange(_options.DefaultTags);
            }

            // Add tags from CacheEntryOptions if available
            if (options?.Tags != null)
            {
                tags.AddRange(options.Tags);
            }

            // Add additional tags
            if (additionalTags?.Length > 0)
            {
                tags.AddRange(additionalTags);
            }

            return tags.Count > 0 ? tags.Distinct().ToArray() : null;
        }

        private void LogDebug(string message, params object[] args)
        {
            if (_options.EnableDetailedLogging)
            {
                _logger?.LogDebug(message, args);
            }
        }

        #endregion

        #region Cache Wrappers

        /// <summary>
        /// Wrapper to distinguish between "no value" and "null value" in cache.
        /// </summary>
        private class CacheWrapper<T>
        {
            public T? Value { get; set; }
            public bool HasValue { get; set; }
        }

        /// <summary>
        /// Wrapper for string values.
        /// </summary>
        private class StringWrapper
        {
            public string? Value { get; set; }
            public bool HasValue { get; set; }
        }

        #endregion
    }
}

