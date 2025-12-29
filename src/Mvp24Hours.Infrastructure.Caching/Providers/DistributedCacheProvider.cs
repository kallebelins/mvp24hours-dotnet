//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Providers
{
    /// <summary>
    /// Cache provider implementation using IDistributedCache (Redis, SQL Server, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider wraps Microsoft.Extensions.Caching.Distributed.IDistributedCache to provide
    /// a unified ICacheProvider interface. It supports various distributed cache backends:
    /// <list type="bullet">
    /// <item>Redis (via StackExchange.Redis)</item>
    /// <item>SQL Server (via Microsoft.Extensions.Caching.SqlServer)</item>
    /// <item>NCache</item>
    /// <item>Other IDistributedCache implementations</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Shared across multiple application instances</item>
    /// <item>Persists across application restarts (depending on backend)</item>
    /// <item>Scalable and distributed</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DistributedCacheProvider : ICacheProvider
    {
        private readonly IDistributedCache _cache;
        private readonly ICacheSerializer _serializer;
        private readonly ILogger<DistributedCacheProvider>? _logger;

        /// <summary>
        /// Creates a new instance of DistributedCacheProvider.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="serializer">The cache serializer (defaults to JsonCacheSerializer).</param>
        /// <param name="logger">Optional logger.</param>
        public DistributedCacheProvider(
            IDistributedCache cache,
            ICacheSerializer? serializer = null,
            ILogger<DistributedCacheProvider>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _serializer = serializer ?? new JsonCacheSerializer();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                var bytes = await _cache.GetAsync(key, cancellationToken);
                if (bytes == null || bytes.Length == 0)
                {
                    _logger?.LogDebug("Cache MISS for key: {Key}", key);
                    return null;
                }

                _logger?.LogDebug("Cache HIT for key: {Key}", key);
                return await _serializer.DeserializeAsync<T>(bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting value from cache for key: {Key}", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                var value = await _cache.GetStringAsync(key, cancellationToken);
                if (value == null)
                {
                    _logger?.LogDebug("Cache MISS for key: {Key}", key);
                }
                else
                {
                    _logger?.LogDebug("Cache HIT for key: {Key}", key);
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting string from cache for key: {Key}", key);
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

            try
            {
                var bytes = await _serializer.SerializeAsync(value, cancellationToken);
                var distributedOptions = ConvertToDistributedCacheEntryOptions(options);
                await _cache.SetAsync(key, bytes, distributedOptions, cancellationToken);
                _logger?.LogDebug("Cache SET for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting value in cache for key: {Key}", key);
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

            try
            {
                var distributedOptions = ConvertToDistributedCacheEntryOptions(options);
                await _cache.SetStringAsync(key, value, distributedOptions, cancellationToken);
                _logger?.LogDebug("Cache SET for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting string in cache for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _logger?.LogDebug("Cache REMOVE for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing key from cache: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
                return;

            var tasks = keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(key => RemoveAsync(key, cancellationToken));

            await Task.WhenAll(tasks);
            _logger?.LogDebug("Cache REMOVE for {Count} keys", keys.Length);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var bytes = await _cache.GetAsync(key, cancellationToken);
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking existence for key: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null || keys.Length == 0)
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            // Note: IDistributedCache doesn't support batch get, so we do parallel gets
            var tasks = keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(async key =>
                {
                    var value = await GetAsync<T>(key, cancellationToken);
                    return new { Key = key, Value = value };
                });

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                if (item.Value != null)
                {
                    result[item.Key] = item.Value;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (values == null || values.Count == 0)
                return;

            var tasks = values.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, cancellationToken));
            await Task.WhenAll(tasks);
            _logger?.LogDebug("Cache SET for {Count} keys", values.Count);
        }

        /// <inheritdoc />
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                await _cache.RefreshAsync(key, cancellationToken);
                _logger?.LogDebug("Cache REFRESH for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing key: {Key}", key);
            }
        }

        private DistributedCacheEntryOptions ConvertToDistributedCacheEntryOptions(CacheEntryOptions? options)
        {
            var distributedOptions = new DistributedCacheEntryOptions();

            if (options != null)
            {
                if (options.AbsoluteExpiration.HasValue)
                {
                    distributedOptions.AbsoluteExpiration = options.AbsoluteExpiration;
                }
                else if (options.AbsoluteExpirationRelativeToNow.HasValue)
                {
                    distributedOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
                }

                if (options.SlidingExpiration.HasValue)
                {
                    distributedOptions.SlidingExpiration = options.SlidingExpiration;
                }

                // Note: IDistributedCache doesn't support priority, so we ignore it
            }

            return distributedOptions;
        }
    }
}

