//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// Redis-based implementation of IHybridCacheTagManager for distributed scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores tag-key associations in Redis using Sets.
    /// It's suitable for distributed applications where multiple instances share the same cache.
    /// </para>
    /// <para>
    /// <strong>Redis Data Structures:</strong>
    /// <list type="bullet">
    /// <item><c>tag:{tagName}</c> - Redis SET containing all keys associated with this tag</item>
    /// <item><c>key:{keyName}:tags</c> - Redis SET containing all tags for this key</item>
    /// <item><c>tagstats:invalidations</c> - Counter for total tag invalidations</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Shared across all application instances</item>
    /// <item>Persists across application restarts</item>
    /// <item>Atomic operations using Redis transactions</item>
    /// <item>Efficient set operations for tag management</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddSingleton&lt;IConnectionMultiplexer&gt;(
    ///     ConnectionMultiplexer.Connect("localhost:6379"));
    /// services.AddSingleton&lt;IHybridCacheTagManager, RedisHybridCacheTagManager&gt;();
    /// 
    /// // Or use the extension method
    /// services.AddMvpHybridCache(options =>
    /// {
    ///     options.UseRedisAsL2 = true;
    ///     options.RedisConnectionString = "localhost:6379";
    /// });
    /// services.AddHybridCacheTagManager&lt;RedisHybridCacheTagManager&gt;();
    /// </code>
    /// </example>
    public class RedisHybridCacheTagManager : IHybridCacheTagManager
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly RedisHybridCacheTagManagerOptions _options;
        private readonly ILogger<RedisHybridCacheTagManager>? _logger;

        private const string TagPrefix = "tag:";
        private const string KeyTagsPrefix = "key:";
        private const string KeyTagsSuffix = ":tags";
        private const string InvalidationsKey = "tagstats:invalidations";

        /// <summary>
        /// Creates a new instance of RedisHybridCacheTagManager.
        /// </summary>
        /// <param name="redis">The Redis connection multiplexer.</param>
        /// <param name="options">Configuration options.</param>
        /// <param name="logger">Optional logger.</param>
        public RedisHybridCacheTagManager(
            IConnectionMultiplexer redis,
            IOptions<RedisHybridCacheTagManagerOptions>? options = null,
            ILogger<RedisHybridCacheTagManager>? logger = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _database = _redis.GetDatabase(options?.Value.DatabaseId ?? 0);
            _options = options?.Value ?? new RedisHybridCacheTagManagerOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task TrackKeyWithTagsAsync(string key, string[] tags, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (tags == null || tags.Length == 0)
                return;

            var fullKey = GetFullKey(key);
            var keyTagsKey = GetKeyTagsKey(fullKey);

            try
            {
                var transaction = _database.CreateTransaction();

                foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var tagKey = GetTagKey(tag);
                    // Add key to tag's set
                    _ = transaction.SetAddAsync(tagKey, fullKey);
                    // Add tag to key's tag set
                    _ = transaction.SetAddAsync(keyTagsKey, tag);

                    // Set expiration on tag key if configured
                    if (_options.TagExpiration.HasValue)
                    {
                        _ = transaction.KeyExpireAsync(tagKey, _options.TagExpiration.Value);
                    }
                }

                // Set expiration on key-tags mapping if configured
                if (_options.KeyTagsMappingExpiration.HasValue)
                {
                    _ = transaction.KeyExpireAsync(keyTagsKey, _options.KeyTagsMappingExpiration.Value);
                }

                await transaction.ExecuteAsync();

                _logger?.LogDebug("Tracked key {Key} with {TagCount} tags in Redis", key, tags.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error tracking key {Key} with tags in Redis", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveKeyFromTagsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var fullKey = GetFullKey(key);
            var keyTagsKey = GetKeyTagsKey(fullKey);

            try
            {
                // Get all tags for this key
                var tags = await _database.SetMembersAsync(keyTagsKey);

                if (tags.Length > 0)
                {
                    var transaction = _database.CreateTransaction();

                    // Remove key from each tag's set
                    foreach (var tag in tags)
                    {
                        var tagKey = GetTagKey(tag.ToString());
                        _ = transaction.SetRemoveAsync(tagKey, fullKey);
                    }

                    // Remove the key's tag mapping
                    _ = transaction.KeyDeleteAsync(keyTagsKey);

                    await transaction.ExecuteAsync();
                }

                _logger?.LogDebug("Removed key {Key} from tag tracking in Redis", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing key {Key} from tag tracking in Redis", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Array.Empty<string>();

            try
            {
                var tagKey = GetTagKey(tag);
                var members = await _database.SetMembersAsync(tagKey);

                var keys = members
                    .Where(m => m.HasValue)
                    .Select(m => RemoveKeyPrefix(m.ToString()))
                    .ToArray();

                return keys;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting keys by tag {Tag} from Redis", tag);
                return Array.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Array.Empty<string>();

            var fullKey = GetFullKey(key);
            var keyTagsKey = GetKeyTagsKey(fullKey);

            try
            {
                var members = await _database.SetMembersAsync(keyTagsKey);

                var tags = members
                    .Where(m => m.HasValue)
                    .Select(m => m.ToString())
                    .ToArray();

                return tags;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tags by key {Key} from Redis", key);
                return Array.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task InvalidateTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            try
            {
                var tagKey = GetTagKey(tag);

                // Get all keys for this tag before deleting
                var keys = await _database.SetMembersAsync(tagKey);

                if (keys.Length > 0)
                {
                    var transaction = _database.CreateTransaction();

                    // Remove tag from each key's tag set
                    foreach (var keyValue in keys)
                    {
                        if (!keyValue.HasValue) continue;

                        var keyTagsKey = GetKeyTagsKey(keyValue.ToString());
                        _ = transaction.SetRemoveAsync(keyTagsKey, tag);

                        // Clean up empty key-tag sets
                        _ = transaction.ScriptEvaluateAsync(
                            "if redis.call('SCARD', KEYS[1]) == 0 then redis.call('DEL', KEYS[1]) end",
                            new RedisKey[] { keyTagsKey });
                    }

                    // Delete the tag's key set
                    _ = transaction.KeyDeleteAsync(tagKey);

                    // Increment invalidation counter
                    _ = transaction.StringIncrementAsync(InvalidationsKey);

                    await transaction.ExecuteAsync();
                }
                else
                {
                    // Just delete the tag key if it exists
                    await _database.KeyDeleteAsync(tagKey);
                    await _database.StringIncrementAsync(InvalidationsKey);
                }

                _logger?.LogDebug("Invalidated tag {Tag} affecting {KeyCount} keys in Redis", tag, keys.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error invalidating tag {Tag} in Redis", tag);
                throw;
            }
        }

        /// <inheritdoc />
        public HybridCacheTagStatistics GetStatistics()
        {
            try
            {
                var stats = new HybridCacheTagStatistics();

                // Get invalidation count
                var invalidations = _database.StringGet(InvalidationsKey);
                if (invalidations.HasValue && long.TryParse(invalidations.ToString(), out var count))
                {
                    stats.TagInvalidations = count;
                }

                // Scan for all tag keys to count tags and associations
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var tagKeys = server.Keys(pattern: $"{_options.KeyPrefix}{TagPrefix}*", pageSize: 1000).ToList();

                stats.TotalTags = tagKeys.Count;

                // Get keys per tag (sampling for performance)
                var keysPerTag = new Dictionary<string, int>();
                var totalAssociations = 0;

                foreach (var tagKey in tagKeys.Take(100)) // Sample first 100 tags
                {
                    var keyCount = (int)_database.SetLength(tagKey);
                    var tagName = tagKey.ToString().Replace($"{_options.KeyPrefix}{TagPrefix}", "");
                    keysPerTag[tagName] = keyCount;
                    totalAssociations += keyCount;
                }

                stats.KeysPerTag = keysPerTag;
                stats.TotalAssociations = totalAssociations;

                return stats;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tag statistics from Redis");
                return new HybridCacheTagStatistics();
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());

                // Get all tag-related keys
                var tagKeys = server.Keys(pattern: $"{_options.KeyPrefix}{TagPrefix}*", pageSize: 1000).ToArray();
                var keyTagsKeys = server.Keys(pattern: $"{_options.KeyPrefix}{KeyTagsPrefix}*", pageSize: 1000).ToArray();

                if (tagKeys.Length > 0)
                {
                    await _database.KeyDeleteAsync(tagKeys);
                }

                if (keyTagsKeys.Length > 0)
                {
                    await _database.KeyDeleteAsync(keyTagsKeys);
                }

                // Reset invalidation counter
                await _database.KeyDeleteAsync($"{_options.KeyPrefix}{InvalidationsKey}");

                _logger?.LogDebug("Cleared all tag tracking data from Redis");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing tag tracking data from Redis");
                throw;
            }
        }

        #region Private Helpers

        private string GetFullKey(string key)
        {
            if (string.IsNullOrEmpty(_options.KeyPrefix))
                return key;
            return $"{_options.KeyPrefix}{key}";
        }

        private string RemoveKeyPrefix(string fullKey)
        {
            if (string.IsNullOrEmpty(_options.KeyPrefix))
                return fullKey;
            return fullKey.StartsWith(_options.KeyPrefix)
                ? fullKey.Substring(_options.KeyPrefix.Length)
                : fullKey;
        }

        private string GetTagKey(string tag)
        {
            return $"{_options.KeyPrefix}{TagPrefix}{tag}";
        }

        private string GetKeyTagsKey(string fullKey)
        {
            return $"{KeyTagsPrefix}{fullKey}{KeyTagsSuffix}";
        }

        #endregion
    }

    /// <summary>
    /// Configuration options for RedisHybridCacheTagManager.
    /// </summary>
    public class RedisHybridCacheTagManagerOptions
    {
        /// <summary>
        /// Gets or sets the Redis database ID to use.
        /// Default: 0.
        /// </summary>
        public int DatabaseId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the key prefix for all tag-related keys.
        /// Default: "mvp24h:tags:".
        /// </summary>
        public string KeyPrefix { get; set; } = "mvp24h:tags:";

        /// <summary>
        /// Gets or sets the expiration time for tag keys.
        /// If null, tags don't expire automatically.
        /// </summary>
        public TimeSpan? TagExpiration { get; set; }

        /// <summary>
        /// Gets or sets the expiration time for key-to-tags mapping keys.
        /// If null, mappings don't expire automatically.
        /// </summary>
        public TimeSpan? KeyTagsMappingExpiration { get; set; }
    }
}

