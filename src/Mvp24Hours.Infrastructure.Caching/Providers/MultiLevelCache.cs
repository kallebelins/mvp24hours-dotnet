//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
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
    /// Multi-level cache implementation combining L1 (memory) and L2 (distributed) caches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides a two-tier caching strategy:
    /// <list type="bullet">
    /// <item><strong>L1 (Memory Cache):</strong> Fast, local to each instance, limited by memory</item>
    /// <item><strong>L2 (Distributed Cache):</strong> Shared across instances, persistent, network latency</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Read Strategy:</strong>
    /// <code>
    /// 1. Check L1 → if found, return immediately (fastest)
    /// 2. Check L2 → if found, promote to L1 and return
    /// 3. Load from factory → store in both L1 and L2, return
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Write Strategy:</strong>
    /// <code>
    /// 1. Write to L2 first (ensures consistency)
    /// 2. Write to L1 (fast local access)
    /// 3. Publish invalidation event (synchronize other instances)
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Synchronization:</strong>
    /// When cache entries are invalidated, events are published via <see cref="ICacheSynchronizer"/>
    /// to notify other instances to invalidate their L1 cache, ensuring consistency.
    /// </para>
    /// </remarks>
    public class MultiLevelCache : IMultiLevelCache
    {
        private readonly ICacheProvider _l1Cache;
        private readonly ICacheProvider _l2Cache;
        private readonly ICacheSynchronizer? _synchronizer;
        private readonly ILogger<MultiLevelCache>? _logger;
        private readonly MultiLevelCacheStatistics _statistics;

        /// <summary>
        /// Creates a new instance of MultiLevelCache.
        /// </summary>
        /// <param name="l1Cache">The L1 cache provider (typically MemoryCacheProvider).</param>
        /// <param name="l2Cache">The L2 cache provider (typically DistributedCacheProvider).</param>
        /// <param name="synchronizer">Optional cache synchronizer for pub/sub invalidation.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when l1Cache or l2Cache is null.</exception>
        public MultiLevelCache(
            ICacheProvider l1Cache,
            ICacheProvider l2Cache,
            ICacheSynchronizer? synchronizer = null,
            ILogger<MultiLevelCache>? logger = null)
        {
            _l1Cache = l1Cache ?? throw new ArgumentNullException(nameof(l1Cache));
            _l2Cache = l2Cache ?? throw new ArgumentNullException(nameof(l2Cache));
            _synchronizer = synchronizer;
            _logger = logger;
            _statistics = new MultiLevelCacheStatistics();

            // Subscribe to invalidation events if synchronizer is available
            if (_synchronizer != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _synchronizer.SubscribeAsync(async (key, ct) =>
                        {
                            try
                            {
                                // Invalidate L1 cache when receiving invalidation event
                                await _l1Cache.RemoveAsync(key, ct);
                                _logger?.LogDebug("[MultiLevelCache] L1 invalidated via sync: {Key}", key);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "[MultiLevelCache] Error invalidating L1 via sync: {Key}", key);
                            }
                        }, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[MultiLevelCache] Error subscribing to invalidation events");
                    }
                });
            }
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _statistics.L1.Requests++;

            try
            {
                // Try L1 first (fastest)
                var l1Value = await _l1Cache.GetAsync<T>(key, cancellationToken);
                if (l1Value != null)
                {
                    _statistics.L1.Hits++;
                    _logger?.LogDebug("[MultiLevelCache] L1 HIT: {Key}", key);
                    return l1Value;
                }

                _statistics.L1.Misses++;
                _statistics.L2.Requests++;

                // Try L2 (distributed)
                var l2Value = await _l2Cache.GetAsync<T>(key, cancellationToken);
                if (l2Value != null)
                {
                    _statistics.L2.Hits++;
                    _logger?.LogDebug("[MultiLevelCache] L2 HIT: {Key}, promoting to L1", key);

                    // Promote to L1 (write-through)
                    try
                    {
                        await _l1Cache.SetAsync(key, l2Value, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error promoting {Key} to L1", key);
                        // Continue even if promotion fails
                    }

                    return l2Value;
                }

                _statistics.L2.Misses++;
                _logger?.LogDebug("[MultiLevelCache] MISS: {Key} (not found in L1 or L2)", key);
                return null;
            }
            catch (Exception ex)
            {
                _statistics.L1.Errors++;
                _logger?.LogError(ex, "[MultiLevelCache] Error getting {Key}", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Try cache first
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Load from factory
            _logger?.LogDebug("[MultiLevelCache] Loading {Key} from factory", key);
            var value = await factory(cancellationToken);

            if (value != null)
            {
                // Store in both levels
                await SetBothAsync(key, value, options, cancellationToken);
            }

            return value;
        }

        /// <inheritdoc />
        public async Task<T?> GetFromL1Async<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _statistics.L1.Requests++;
            try
            {
                var value = await _l1Cache.GetAsync<T>(key, cancellationToken);
                if (value != null)
                {
                    _statistics.L1.Hits++;
                }
                else
                {
                    _statistics.L1.Misses++;
                }
                return value;
            }
            catch (Exception ex)
            {
                _statistics.L1.Errors++;
                _logger?.LogError(ex, "[MultiLevelCache] Error getting {Key} from L1", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<T?> GetFromL2Async<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _statistics.L2.Requests++;
            try
            {
                var value = await _l2Cache.GetAsync<T>(key, cancellationToken);
                if (value != null)
                {
                    _statistics.L2.Hits++;
                }
                else
                {
                    _statistics.L2.Misses++;
                }
                return value;
            }
            catch (Exception ex)
            {
                _statistics.L2.Errors++;
                _logger?.LogError(ex, "[MultiLevelCache] Error getting {Key} from L2", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> PromoteToL1Async<T>(string key, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                var l2Value = await _l2Cache.GetAsync<T>(key, cancellationToken);
                if (l2Value == null)
                {
                    return false;
                }

                await _l1Cache.SetAsync(key, l2Value, options, cancellationToken);
                _logger?.LogDebug("[MultiLevelCache] Promoted {Key} from L2 to L1", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error promoting {Key} to L1", key);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DemoteFromL1Async(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                var exists = await _l1Cache.ExistsAsync(key, cancellationToken);
                if (!exists)
                {
                    return false;
                }

                await _l1Cache.RemoveAsync(key, cancellationToken);
                _logger?.LogDebug("[MultiLevelCache] Demoted {Key} from L1 (removed from L1, kept in L2)", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error demoting {Key} from L1", key);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            await SetBothAsync(key, value, options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SetBothAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                // Write to L2 first (ensures consistency)
                await _l2Cache.SetAsync(key, value, options, cancellationToken);
                _logger?.LogDebug("[MultiLevelCache] Set {Key} in L2", key);

                // Write to L1 (fast local access)
                await _l1Cache.SetAsync(key, value, options, cancellationToken);
                _logger?.LogDebug("[MultiLevelCache] Set {Key} in L1", key);

                // Publish invalidation event (synchronize other instances)
                if (_synchronizer != null)
                {
                    try
                    {
                        await _synchronizer.PublishInvalidationAsync(key, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error publishing invalidation for {Key}", key);
                        // Continue even if synchronization fails
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error setting {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _statistics.L1.Requests++;

            try
            {
                // Try L1 first
                var l1Value = await _l1Cache.GetStringAsync(key, cancellationToken);
                if (l1Value != null)
                {
                    _statistics.L1.Hits++;
                    return l1Value;
                }

                _statistics.L1.Misses++;
                _statistics.L2.Requests++;

                // Try L2
                var l2Value = await _l2Cache.GetStringAsync(key, cancellationToken);
                if (l2Value != null)
                {
                    _statistics.L2.Hits++;
                    // Promote to L1
                    try
                    {
                        await _l1Cache.SetStringAsync(key, l2Value, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error promoting string {Key} to L1", key);
                    }
                    return l2Value;
                }

                _statistics.L2.Misses++;
                return null;
            }
            catch (Exception ex)
            {
                _statistics.L1.Errors++;
                _logger?.LogError(ex, "[MultiLevelCache] Error getting string {Key}", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            await SetBothAsync(key, value, options, cancellationToken);
        }

        private async Task SetBothAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                await _l2Cache.SetStringAsync(key, value, options, cancellationToken);
                await _l1Cache.SetStringAsync(key, value, options, cancellationToken);

                if (_synchronizer != null)
                {
                    try
                    {
                        await _synchronizer.PublishInvalidationAsync(key, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error publishing invalidation for string {Key}", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error setting string {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await RemoveBothAsync(key, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveBothAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                // Remove from both levels
                await Task.WhenAll(
                    _l1Cache.RemoveAsync(key, cancellationToken),
                    _l2Cache.RemoveAsync(key, cancellationToken)
                );

                // Publish invalidation event
                if (_synchronizer != null)
                {
                    try
                    {
                        await _synchronizer.PublishInvalidationAsync(key, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error publishing invalidation for {Key}", key);
                    }
                }

                _logger?.LogDebug("[MultiLevelCache] Removed {Key} from both L1 and L2", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error removing {Key}", key);
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
                await Task.WhenAll(
                    _l1Cache.RemoveManyAsync(keys, cancellationToken),
                    _l2Cache.RemoveManyAsync(keys, cancellationToken)
                );

                if (_synchronizer != null)
                {
                    try
                    {
                        await _synchronizer.PublishInvalidationManyAsync(keys, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error publishing invalidation for {Count} keys", keys.Length);
                    }
                }

                _logger?.LogDebug("[MultiLevelCache] Removed {Count} keys from both L1 and L2", keys.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error removing {Count} keys", keys.Length);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Check L1 first (fastest)
            var existsInL1 = await _l1Cache.ExistsAsync(key, cancellationToken);
            if (existsInL1)
            {
                return true;
            }

            // Check L2
            return await _l2Cache.ExistsAsync(key, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null || keys.Length == 0)
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            // Try L1 first
            var l1Results = await _l1Cache.GetManyAsync<T>(keys, cancellationToken);
            foreach (var kvp in l1Results)
            {
                result[kvp.Key] = kvp.Value;
            }

            // Get missing keys from L2
            var missingKeys = keys.Where(k => !result.ContainsKey(k)).ToArray();
            if (missingKeys.Length > 0)
            {
                var l2Results = await _l2Cache.GetManyAsync<T>(missingKeys, cancellationToken);
                foreach (var kvp in l2Results)
                {
                    result[kvp.Key] = kvp.Value;
                    // Promote to L1
                    try
                    {
                        await _l1Cache.SetAsync(kvp.Key, kvp.Value, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error promoting {Key} to L1", kvp.Key);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (values == null || values.Count == 0)
                return;

            try
            {
                await Task.WhenAll(
                    _l2Cache.SetManyAsync(values, options, cancellationToken),
                    _l1Cache.SetManyAsync(values, options, cancellationToken)
                );

                if (_synchronizer != null)
                {
                    try
                    {
                        var keys = values.Keys.ToArray();
                        await _synchronizer.PublishInvalidationManyAsync(keys, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[MultiLevelCache] Error publishing invalidation for {Count} keys", values.Count);
                    }
                }

                _logger?.LogDebug("[MultiLevelCache] Set {Count} keys in both L1 and L2", values.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error setting {Count} keys", values.Count);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                await Task.WhenAll(
                    _l1Cache.RefreshAsync(key, cancellationToken),
                    _l2Cache.RefreshAsync(key, cancellationToken)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MultiLevelCache] Error refreshing {Key}", key);
            }
        }

        /// <inheritdoc />
        public MultiLevelCacheStatistics GetStatistics()
        {
            return _statistics;
        }
    }
}

