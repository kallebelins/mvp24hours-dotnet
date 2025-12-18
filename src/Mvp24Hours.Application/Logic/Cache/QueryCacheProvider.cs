//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Cache
{
    /// <summary>
    /// Default implementation of <see cref="IQueryCacheProvider"/> that provides second-level
    /// caching for query results with support for both memory and distributed cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation supports:
    /// <list type="bullet">
    /// <item>Hybrid caching (L1 memory + L2 distributed)</item>
    /// <item>Cache stampede prevention via SemaphoreSlim</item>
    /// <item>Region-based invalidation via key tracking</item>
    /// <item>Pattern-based invalidation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class QueryCacheProvider : IQueryCacheProvider
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache? _memoryCache;
        private readonly ILogger<QueryCacheProvider> _logger;
        private readonly QueryCacheOptions _options;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _regionKeys = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryCacheProvider"/> class.
        /// </summary>
        public QueryCacheProvider(
            IDistributedCache distributedCache,
            ILogger<QueryCacheProvider> logger,
            IOptions<QueryCacheOptions> options,
            IMemoryCache? memoryCache = null)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new QueryCacheOptions();
            _memoryCache = memoryCache;
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-get-start", key);

            try
            {
                // Try L1 (memory) cache first
                if (_options.EnableL1Cache && _memoryCache != null)
                {
                    if (_memoryCache.TryGetValue(key, out T? memoryCachedValue))
                    {
                        _logger.LogDebug("Cache hit (L1) for key: {CacheKey}", key);
                        return memoryCachedValue;
                    }
                }

                // Try L2 (distributed) cache
                var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
                if (cachedBytes == null || cachedBytes.Length == 0)
                {
                    _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                    return default;
                }

                var value = JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
                _logger.LogDebug("Cache hit (L2) for key: {CacheKey}", key);

                // Populate L1 cache if enabled
                if (_options.EnableL1Cache && _memoryCache != null && value != null)
                {
                    _memoryCache.Set(key, value, _options.L1CacheDuration);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached value for key: {CacheKey}", key);
                return default;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-get-end", key);
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-set-start", key);

            try
            {
                options ??= new QueryCacheEntryOptions();

                var distributedCacheOptions = new DistributedCacheEntryOptions();
                if (options.UseSlidingExpiration)
                {
                    distributedCacheOptions.SlidingExpiration = options.Duration;
                }
                else
                {
                    distributedCacheOptions.AbsoluteExpirationRelativeToNow = options.Duration;
                }

                var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
                await _distributedCache.SetAsync(key, serializedValue, distributedCacheOptions, cancellationToken);

                // Set in L1 cache if enabled
                if (_options.EnableL1Cache && _memoryCache != null)
                {
                    var memoryCacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(
                            Math.Min(options.Duration.Ticks, _options.L1CacheDuration.Ticks))
                    };
                    _memoryCache.Set(key, value, memoryCacheOptions);
                }

                // Track key in region if specified
                if (!string.IsNullOrEmpty(options.Region))
                {
                    TrackKeyInRegion(key, options.Region);
                }

                _logger.LogDebug("Cache set for key: {CacheKey}, Duration: {Duration}", key, options.Duration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting cached value for key: {CacheKey}", key);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-set-end", key);
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            options ??= new QueryCacheEntryOptions();

            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Cache stampede prevention
            if (options.EnableStampedePrevention)
            {
                var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

                try
                {
                    var acquired = await semaphore.WaitAsync(options.StampedePreventionTimeout, cancellationToken);
                    if (!acquired)
                    {
                        _logger.LogWarning("Timeout waiting for cache lock on key: {CacheKey}", key);
                        // Fall through to execute factory anyway
                    }

                    // Double-check after acquiring lock
                    cachedValue = await GetAsync<T>(key, cancellationToken);
                    if (cachedValue != null)
                    {
                        return cachedValue;
                    }

                    // Execute factory and cache result
                    var value = await factory();
                    if (value != null)
                    {
                        await SetAsync(key, value, options, cancellationToken);
                    }
                    return value;
                }
                finally
                {
                    semaphore.Release();

                    // Clean up lock if no longer needed
                    if (semaphore.CurrentCount == 1)
                    {
                        _locks.TryRemove(key, out _);
                    }
                }
            }
            else
            {
                // No stampede prevention - execute factory directly
                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, options, cancellationToken);
                }
                return value;
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-remove-start", key);

            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);

                // Remove from L1 cache if enabled
                if (_options.EnableL1Cache && _memoryCache != null)
                {
                    _memoryCache.Remove(key);
                }

                _logger.LogDebug("Cache removed for key: {CacheKey}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error removing cached value for key: {CacheKey}", key);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-remove-end", key);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-invalidateregion-start", region);

            try
            {
                if (_regionKeys.TryGetValue(region, out var keys))
                {
                    foreach (var key in keys)
                    {
                        await RemoveAsync(key, cancellationToken);
                    }
                    _regionKeys.TryRemove(region, out _);
                }

                _logger.LogDebug("Cache region invalidated: {Region}", region);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache region: {Region}", region);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-invalidateregion-end", region);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-invalidatepattern-start", pattern);

            try
            {
                // For in-memory tracking, we can match patterns against tracked keys
                foreach (var regionKvp in _regionKeys)
                {
                    foreach (var key in regionKvp.Value)
                    {
                        if (MatchesPattern(key, pattern))
                        {
                            await RemoveAsync(key, cancellationToken);
                        }
                    }
                }

                _logger.LogDebug("Cache invalidated by pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache by pattern: {Pattern}", pattern);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-cache-invalidatepattern-end", pattern);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            try
            {
                // Check L1 first
                if (_options.EnableL1Cache && _memoryCache != null)
                {
                    if (_memoryCache.TryGetValue(key, out _))
                    {
                        return true;
                    }
                }

                // Check L2
                var value = await _distributedCache.GetAsync(key, cancellationToken);
                return value != null && value.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking cache existence for key: {CacheKey}", key);
                return false;
            }
        }

        /// <summary>
        /// Tracks a cache key in a region for later invalidation.
        /// </summary>
        private void TrackKeyInRegion(string key, string region)
        {
            var keys = _regionKeys.GetOrAdd(region, _ => new ConcurrentBag<string>());
            keys.Add(key);
        }

        /// <summary>
        /// Checks if a key matches a wildcard pattern.
        /// </summary>
        private static bool MatchesPattern(string key, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            // Simple wildcard matching (* at end)
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern[..^1];
                return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            // Simple wildcard matching (* at start)
            if (pattern.StartsWith('*'))
            {
                var suffix = pattern[1..];
                return key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            // Exact match
            return key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Configuration options for the query cache provider.
    /// </summary>
    public class QueryCacheOptions
    {
        /// <summary>
        /// Gets or sets whether to enable L1 (memory) cache as a fast local cache.
        /// </summary>
        /// <value>True to enable L1 cache; default is true.</value>
        public bool EnableL1Cache { get; set; } = true;

        /// <summary>
        /// Gets or sets the duration for L1 cache entries.
        /// </summary>
        /// <value>The L1 cache duration. Default is 1 minute.</value>
        public TimeSpan L1CacheDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the default cache duration when not specified.
        /// </summary>
        /// <value>The default cache duration. Default is 5 minutes.</value>
        public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the key prefix for all cache entries.
        /// </summary>
        /// <value>The key prefix. Default is "query:".</value>
        public string KeyPrefix { get; set; } = "query:";
    }
}

