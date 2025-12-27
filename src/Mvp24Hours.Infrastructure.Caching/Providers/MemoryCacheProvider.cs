//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Providers
{
    /// <summary>
    /// Cache provider implementation using IMemoryCache (in-memory cache).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider wraps Microsoft.Extensions.Caching.Memory.IMemoryCache to provide
    /// a unified ICacheProvider interface. It's ideal for single-instance applications
    /// or development/testing scenarios.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    /// <item>Not shared across multiple application instances</item>
    /// <item>Lost on application restart</item>
    /// <item>Limited by available memory</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ICacheSerializer _serializer;
        private readonly ILogger<MemoryCacheProvider>? _logger;

        /// <summary>
        /// Creates a new instance of MemoryCacheProvider.
        /// </summary>
        /// <param name="cache">The memory cache instance.</param>
        /// <param name="serializer">The cache serializer (defaults to JsonCacheSerializer).</param>
        /// <param name="logger">Optional logger.</param>
        public MemoryCacheProvider(
            IMemoryCache cache,
            ICacheSerializer? serializer = null,
            ILogger<MemoryCacheProvider>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _serializer = serializer ?? new JsonCacheSerializer();
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    if (value is T typedValue)
                    {
                        _logger?.LogDebug("Cache HIT for key: {Key}", key);
                        return Task.FromResult<T?>(typedValue);
                    }

                    if (value is byte[] bytes)
                    {
                        return DeserializeFromBytes<T>(bytes);
                    }

                    if (value is string str)
                    {
                        return _serializer.DeserializeFromStringAsync<T>(str, cancellationToken);
                    }
                }

                _logger?.LogDebug("Cache MISS for key: {Key}", key);
                return Task.FromResult<T?>(null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting value from cache for key: {Key}", key);
                return Task.FromResult<T?>(null);
            }
        }

        /// <inheritdoc />
        public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    _logger?.LogDebug("Cache HIT for key: {Key}", key);
                    return Task.FromResult(value?.ToString());
                }

                _logger?.LogDebug("Cache MISS for key: {Key}", key);
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting string from cache for key: {Key}", key);
                return Task.FromResult<string?>(null);
            }
        }

        /// <inheritdoc />
        public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var cacheOptions = ConvertToMemoryCacheEntryOptions(options);
                _cache.Set(key, value, cacheOptions);
                _logger?.LogDebug("Cache SET for key: {Key}", key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting value in cache for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var cacheOptions = ConvertToMemoryCacheEntryOptions(options);
                _cache.Set(key, value, cacheOptions);
                _logger?.LogDebug("Cache SET for key: {Key}", key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting string in cache for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                _cache.Remove(key);
                _logger?.LogDebug("Cache REMOVE for key: {Key}", key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing key from cache: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
                return Task.CompletedTask;

            try
            {
                foreach (var key in keys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        _cache.Remove(key);
                    }
                }

                _logger?.LogDebug("Cache REMOVE for {Count} keys", keys.Length);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing keys from cache");
                throw;
            }
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult(false);

            try
            {
                var exists = _cache.TryGetValue(key, out _);
                return Task.FromResult(exists);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking existence for key: {Key}", key);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null || keys.Length == 0)
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var value = await GetAsync<T>(key, cancellationToken);
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (values == null || values.Count == 0)
                return Task.CompletedTask;

            var tasks = values.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, cancellationToken));
            return Task.WhenAll(tasks);
        }

        /// <inheritdoc />
        public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            // MemoryCache doesn't support refresh, so we get and set again
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            try
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    // Re-set with same value to refresh sliding expiration
                    var entry = _cache.CreateEntry(key);
                    entry.Value = value;
                    entry.Dispose();
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing key: {Key}", key);
                return Task.CompletedTask;
            }
        }

        private MemoryCacheEntryOptions ConvertToMemoryCacheEntryOptions(CacheEntryOptions? options)
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            if (options != null)
            {
                if (options.AbsoluteExpiration.HasValue)
                {
                    cacheOptions.AbsoluteExpiration = options.AbsoluteExpiration;
                }
                else if (options.AbsoluteExpirationRelativeToNow.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
                }

                if (options.SlidingExpiration.HasValue)
                {
                    cacheOptions.SlidingExpiration = options.SlidingExpiration;
                }

                cacheOptions.Priority = ConvertPriority(options.Priority);
            }

            return cacheOptions;
        }

        private static CacheItemPriority ConvertPriority(CacheEntryPriority priority)
        {
            return priority switch
            {
                CacheEntryPriority.Low => CacheItemPriority.Low,
                CacheEntryPriority.Normal => CacheItemPriority.Normal,
                CacheEntryPriority.High => CacheItemPriority.High,
                CacheEntryPriority.NeverRemove => CacheItemPriority.NeverRemove,
                _ => CacheItemPriority.Normal
            };
        }

        private async Task<T?> DeserializeFromBytes<T>(byte[] bytes) where T : class
        {
            try
            {
                return await _serializer.DeserializeAsync<T>(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing bytes to {Type}", typeof(T).Name);
                return null;
            }
        }
    }
}

