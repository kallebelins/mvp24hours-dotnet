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
    /// Base implementation of the Write-Through caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a generic implementation of the Write-Through pattern.
    /// It requires a function to persist data to the data source.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create write-through cache
    /// var writeThroughCache = new WriteThroughCache&lt;Product&gt;(
    ///     cacheProvider,
    ///     async (key, value, ct) => 
    ///     {
    ///         await _repository.SaveAsync(value, ct);
    ///     },
    ///     options => CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5))
    /// );
    /// 
    /// // Use it
    /// await writeThroughCache.SetAsync("product:123", product);
    /// </code>
    /// </example>
    public class WriteThroughCache<T> : IWriteThroughCache<T> where T : class
    {
        private readonly ICacheProvider _cache;
        private readonly Func<string, T, CancellationToken, Task> _saveToSource;
        private readonly Func<string, CacheEntryOptions>? _getCacheOptions;
        private readonly ILogger<WriteThroughCache<T>>? _logger;

        /// <summary>
        /// Creates a new instance of WriteThroughCache.
        /// </summary>
        /// <param name="cache">The cache provider.</param>
        /// <param name="saveToSource">The function to persist data to the data source.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when cache or saveToSource is null.</exception>
        public WriteThroughCache(
            ICacheProvider cache,
            Func<string, T, CancellationToken, Task> saveToSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ILogger<WriteThroughCache<T>>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _saveToSource = saveToSource ?? throw new ArgumentNullException(nameof(saveToSource));
            _getCacheOptions = getCacheOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                _logger?.LogDebug("Write-Through: Saving to source for key: {Key}", key);

                // Write to data source first
                await _saveToSource(key, value, cancellationToken);

                _logger?.LogDebug("Write-Through: Successfully saved to source for key: {Key}", key);

                // Then write to cache
                var options = _getCacheOptions?.Invoke(key) ?? CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5));
                await _cache.SetAsync(key, value, options, cancellationToken);

                _logger?.LogDebug("Write-Through: Successfully cached value for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Write-Through cache for key: {Key}", key);
                throw;
            }
        }
    }
}

