//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Invalidation
{
    /// <summary>
    /// Publishes cache invalidation events via Redis pub/sub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation requires StackExchange.Redis. It publishes invalidation events
    /// to Redis channels that can be subscribed by other application instances.
    /// </para>
    /// </remarks>
    public class RedisCacheInvalidationEventPublisher : ICacheInvalidationEventPublisher
    {
        private const string KeyInvalidationChannel = "mvp24hours:cache:invalidate:key";
        private const string TagInvalidationChannel = "mvp24hours:cache:invalidate:tag";

        private readonly object _redisConnection;
        private readonly ILogger<RedisCacheInvalidationEventPublisher>? _logger;
        private readonly bool _isRedisAvailable;

        /// <summary>
        /// Creates a new instance of RedisCacheInvalidationEventPublisher.
        /// </summary>
        /// <param name="redisConnection">The Redis connection (StackExchange.Redis.IConnectionMultiplexer).</param>
        /// <param name="logger">Optional logger.</param>
        public RedisCacheInvalidationEventPublisher(object redisConnection, ILogger<RedisCacheInvalidationEventPublisher>? logger = null)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            _logger = logger;

            // Check if Redis connection is available
            _isRedisAvailable = CheckRedisAvailability();
        }

        /// <inheritdoc />
        public async Task PublishKeyInvalidationAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (!_isRedisAvailable)
            {
                _logger?.LogWarning("Redis not available, skipping key invalidation event for {Key}", key);
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(new { Key = key, Timestamp = DateTimeOffset.UtcNow });
                await PublishToRedisAsync(KeyInvalidationChannel, message, cancellationToken);
                _logger?.LogDebug("Published key invalidation event for {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error publishing key invalidation event for {Key}", key);
                // Don't throw - invalidation events are best-effort
            }
        }

        /// <inheritdoc />
        public async Task PublishTagInvalidationAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

            if (!_isRedisAvailable)
            {
                _logger?.LogWarning("Redis not available, skipping tag invalidation event for {Tag}", tag);
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(new { Tag = tag, Timestamp = DateTimeOffset.UtcNow });
                await PublishToRedisAsync(TagInvalidationChannel, message, cancellationToken);
                _logger?.LogDebug("Published tag invalidation event for {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error publishing tag invalidation event for {Tag}", tag);
                // Don't throw - invalidation events are best-effort
            }
        }

        /// <inheritdoc />
        public async Task PublishTagsInvalidationAsync(string[] tags, CancellationToken cancellationToken = default)
        {
            if (tags == null || tags.Length == 0)
                return;

            if (!_isRedisAvailable)
            {
                _logger?.LogWarning("Redis not available, skipping tags invalidation event");
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(new { Tags = tags, Timestamp = DateTimeOffset.UtcNow });
                await PublishToRedisAsync(TagInvalidationChannel, message, cancellationToken);
                _logger?.LogDebug("Published tags invalidation event for {TagCount} tags", tags.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error publishing tags invalidation event");
                // Don't throw - invalidation events are best-effort
            }
        }

        private async Task PublishToRedisAsync(string channel, string message, CancellationToken cancellationToken)
        {
            try
            {
                // Use reflection to call Redis methods without direct dependency
                // In a real implementation, you'd use StackExchange.Redis directly
                var connectionType = _redisConnection.GetType();
                var getDatabaseMethod = connectionType.GetMethod("GetDatabase", new[] { typeof(int) });
                if (getDatabaseMethod == null)
                {
                    getDatabaseMethod = connectionType.GetMethod("GetDatabase");
                }

                if (getDatabaseMethod == null)
                {
                    _logger?.LogWarning("Redis connection does not support GetDatabase method");
                    return;
                }

                object? database;
                if (getDatabaseMethod.GetParameters().Length > 0)
                {
                    database = getDatabaseMethod.Invoke(_redisConnection, new object[] { -1 });
                }
                else
                {
                    database = getDatabaseMethod.Invoke(_redisConnection, null);
                }

                if (database == null)
                {
                    _logger?.LogWarning("Failed to get Redis database");
                    return;
                }

                // Try string-based publish first (most common)
                var publishMethod = database.GetType().GetMethod("PublishAsync", new[] { typeof(string), typeof(string) });
                if (publishMethod != null)
                {
                    var task = publishMethod.Invoke(database, new object[] { channel, message }) as Task<long>;
                    if (task != null)
                    {
                        await task;
                        return;
                    }
                }

                // Try RedisChannel-based publish
                publishMethod = database.GetType().GetMethod("PublishAsync");
                if (publishMethod != null)
                {
                    var parameters = publishMethod.GetParameters();
                    if (parameters.Length == 2)
                    {
                        var channelParam = channel; // Use string directly
                        var task = publishMethod.Invoke(database, new object[] { channelParam, message }) as Task;
                        if (task != null)
                        {
                            await task;
                            return;
                        }
                    }
                }

                _logger?.LogWarning("Redis database does not support PublishAsync method with expected signature");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error publishing to Redis channel {Channel}", channel);
                // Don't throw - invalidation events are best-effort
            }
        }

        private bool CheckRedisAvailability()
        {
            try
            {
                // Check if Redis connection is available
                var connectionType = _redisConnection.GetType();
                var isConnectedProperty = connectionType.GetProperty("IsConnected");
                if (isConnectedProperty != null)
                {
                    var isConnected = isConnectedProperty.GetValue(_redisConnection);
                    return isConnected is bool connected && connected;
                }
                return true; // Assume available if we can't check
            }
            catch
            {
                return false;
            }
        }
    }

}

