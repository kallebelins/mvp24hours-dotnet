//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Represents a cached item with expiration metadata for Refresh-Ahead pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    internal class CachedItemWithExpiration<T> where T : class
    {
        public T Value { get; }
        public DateTime CachedAt { get; }
        public TimeSpan Expiration { get; }
        public DateTime ExpiresAt => CachedAt.Add(Expiration);

        public CachedItemWithExpiration(T value, TimeSpan expiration)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            CachedAt = DateTime.UtcNow;
            Expiration = expiration;
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool ShouldRefresh(TimeSpan refreshThreshold) => DateTime.UtcNow >= ExpiresAt.Subtract(refreshThreshold);
    }

    /// <summary>
    /// Base implementation of the Refresh-Ahead caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a generic implementation of the Refresh-Ahead pattern.
    /// It proactively refreshes cached data before expiration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create refresh-ahead cache
    /// var refreshAheadCache = new RefreshAheadCache&lt;Product&gt;(
    ///     cacheProvider,
    ///     async (key, ct) => 
    ///     {
    ///         var id = ExtractIdFromKey(key);
    ///         return await _repository.GetByIdAsync(id, ct);
    ///     },
    ///     TimeSpan.FromMinutes(5), // Expiration
    ///     TimeSpan.FromMinutes(1)  // Refresh threshold (refresh when 1 min before expiration)
    /// );
    /// 
    /// // Use it
    /// var product = await refreshAheadCache.GetAsync("product:123");
    /// </code>
    /// </example>
    public class RefreshAheadCache<T> : IRefreshAheadCache<T> where T : class
    {
        private readonly ICacheProvider _cache;
        private readonly Func<string, CancellationToken, Task<T?>> _loadFromSource;
        private readonly TimeSpan _expiration;
        private readonly TimeSpan _refreshThreshold;
        private readonly Func<string, CacheEntryOptions>? _getCacheOptions;
        private readonly ILogger<RefreshAheadCache<T>>? _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks;

        /// <summary>
        /// Creates a new instance of RefreshAheadCache.
        /// </summary>
        /// <param name="cache">The cache provider.</param>
        /// <param name="loadFromSource">The factory function to load data from source.</param>
        /// <param name="expiration">The expiration duration for cached items.</param>
        /// <param name="refreshThreshold">The time before expiration when refresh should be triggered.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when cache or loadFromSource is null.</exception>
        public RefreshAheadCache(
            ICacheProvider cache,
            Func<string, CancellationToken, Task<T?>> loadFromSource,
            TimeSpan expiration,
            TimeSpan refreshThreshold,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ILogger<RefreshAheadCache<T>>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _loadFromSource = loadFromSource ?? throw new ArgumentNullException(nameof(loadFromSource));
            _expiration = expiration;
            _refreshThreshold = refreshThreshold;
            _getCacheOptions = getCacheOptions;
            _logger = logger;
            _refreshLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                // Try to get from cache
                var cachedWrapper = await _cache.GetAsync<CachedItemWithExpiration<T>>(key, cancellationToken);
                
                if (cachedWrapper != null)
                {
                    // Check if refresh is needed
                    if (cachedWrapper.ShouldRefresh(_refreshThreshold) && !cachedWrapper.IsExpired)
                    {
                        _logger?.LogDebug("Refresh-Ahead: Triggering background refresh for key: {Key}", key);
                        
                        // Trigger background refresh (fire and forget)
                        _ = RefreshInBackgroundAsync(key, cancellationToken);
                    }

                    // Return cached value immediately (even if expired, we return it and refresh in background)
                    if (!cachedWrapper.IsExpired)
                    {
                        _logger?.LogDebug("Refresh-Ahead: Cache HIT for key: {Key}", key);
                        return cachedWrapper.Value;
                    }
                }

                // Cache miss or expired - load immediately
                _logger?.LogDebug("Refresh-Ahead: Cache MISS for key: {Key}, loading from source", key);
                return await LoadAndCacheAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Refresh-Ahead cache for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            await RefreshInBackgroundAsync(key, cancellationToken);
        }

        private async Task<T?> LoadAndCacheAsync(string key, CancellationToken cancellationToken)
        {
            var value = await _loadFromSource(key, cancellationToken);
            if (value != null)
            {
                var wrapper = new CachedItemWithExpiration<T>(value, _expiration);
                var options = _getCacheOptions?.Invoke(key) ?? CacheEntryOptions.FromDuration(_expiration);
                await _cache.SetAsync(key, wrapper, options, cancellationToken);
                _logger?.LogDebug("Refresh-Ahead: Loaded and cached value for key: {Key}", key);
            }

            return value;
        }

        private async Task RefreshInBackgroundAsync(string key, CancellationToken cancellationToken)
        {
            // Get or create a semaphore for this key to prevent concurrent refreshes
            var semaphore = _refreshLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            if (!await semaphore.WaitAsync(0, cancellationToken))
            {
                // Another refresh is already in progress for this key
                _logger?.LogDebug("Refresh-Ahead: Refresh already in progress for key: {Key}", key);
                return;
            }

            try
            {
                _logger?.LogDebug("Refresh-Ahead: Refreshing key: {Key}", key);
                var value = await _loadFromSource(key, cancellationToken);
                
                if (value != null)
                {
                    var wrapper = new CachedItemWithExpiration<T>(value, _expiration);
                    var options = _getCacheOptions?.Invoke(key) ?? CacheEntryOptions.FromDuration(_expiration);
                    await _cache.SetAsync(key, wrapper, options, cancellationToken);
                    _logger?.LogDebug("Refresh-Ahead: Successfully refreshed key: {Key}", key);
                }
                else
                {
                    _logger?.LogWarning("Refresh-Ahead: No value found in source for key: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Refresh-Ahead: Error refreshing key: {Key}", key);
                // Don't throw - this is a background operation
            }
            finally
            {
                semaphore.Release();
                
                // Clean up semaphore if no longer needed (optional optimization)
                if (_refreshLocks.TryRemove(key, out var removedSemaphore))
                {
                    removedSemaphore.Dispose();
                }
            }
        }
    }
}

