//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Base implementation of the Read-Through caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a generic implementation of the Read-Through pattern.
    /// It requires a factory function to load data from the data source when a cache miss occurs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create read-through cache
    /// var readThroughCache = new ReadThroughCache&lt;Product&gt;(
    ///     cacheProvider,
    ///     async (key, ct) => 
    ///     {
    ///         var id = ExtractIdFromKey(key);
    ///         return await _repository.GetByIdAsync(id, ct);
    ///     },
    ///     options => CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5))
    /// );
    /// 
    /// // Use it
    /// var product = await readThroughCache.GetAsync("product:123");
    /// </code>
    /// </example>
    public class ReadThroughCache<T> : IReadThroughCache<T> where T : class
    {
        private readonly ICacheProvider _cache;
        private readonly Func<string, CancellationToken, Task<T?>> _loadFromSource;
        private readonly Func<string, CacheEntryOptions>? _getCacheOptions;
        private readonly ILogger<ReadThroughCache<T>>? _logger;

        /// <summary>
        /// Creates a new instance of ReadThroughCache.
        /// </summary>
        /// <param name="cache">The cache provider.</param>
        /// <param name="loadFromSource">The factory function to load data from source on cache miss.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when cache or loadFromSource is null.</exception>
        public ReadThroughCache(
            ICacheProvider cache,
            Func<string, CancellationToken, Task<T?>> loadFromSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ILogger<ReadThroughCache<T>>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _loadFromSource = loadFromSource ?? throw new ArgumentNullException(nameof(loadFromSource));
            _getCacheOptions = getCacheOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                // Try to get from cache first
                var cached = await _cache.GetAsync<T>(key, cancellationToken);
                if (cached != null)
                {
                    _logger?.LogDebug("Cache HIT for key: {Key}", key);
                    return cached;
                }

                _logger?.LogDebug("Cache MISS for key: {Key}, loading from source", key);

                // Cache miss - load from source
                var value = await _loadFromSource(key, cancellationToken);
                if (value != null)
                {
                    // Determine cache options
                    var options = _getCacheOptions?.Invoke(key) ?? CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5));
                    
                    // Store in cache
                    await _cache.SetAsync(key, value, options, cancellationToken);
                    _logger?.LogDebug("Loaded and cached value for key: {Key}", key);
                }
                else
                {
                    _logger?.LogDebug("No value found in source for key: {Key}", key);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Read-Through cache for key: {Key}", key);
                throw;
            }
        }
    }
}

