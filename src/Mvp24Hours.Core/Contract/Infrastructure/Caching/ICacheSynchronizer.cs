//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for synchronizing cache invalidation across multiple instances via pub/sub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface enables cache synchronization between application instances by publishing
    /// invalidation events when cache entries are modified. Other instances subscribe to these
    /// events and invalidate their local L1 cache accordingly.
    /// </para>
    /// <para>
    /// <strong>Supported Backends:</strong>
    /// <list type="bullet">
    /// <item>Redis Pub/Sub (via StackExchange.Redis)</item>
    /// <item>RabbitMQ (via Mvp24Hours.Infrastructure.RabbitMQ)</item>
    /// <item>In-Memory (for single-instance scenarios or testing)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Redis implementation
    /// services.AddSingleton&lt;ICacheSynchronizer&gt;(sp =>
    ///     new RedisCacheSynchronizer(redisConnection, logger));
    /// 
    /// // RabbitMQ implementation
    /// services.AddSingleton&lt;ICacheSynchronizer&gt;(sp =>
    ///     new RabbitMqCacheSynchronizer(rabbitMqClient, logger));
    /// </code>
    /// </example>
    public interface ICacheSynchronizer
    {
        /// <summary>
        /// Publishes a cache invalidation event for the specified key.
        /// </summary>
        /// <param name="key">The cache key to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This method publishes an event that other instances will receive to invalidate
        /// their local L1 cache. The event includes the cache key and optionally metadata.
        /// </remarks>
        Task PublishInvalidationAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes cache invalidation events for multiple keys.
        /// </summary>
        /// <param name="keys">The cache keys to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishInvalidationManyAsync(string[] keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to cache invalidation events.
        /// </summary>
        /// <param name="onInvalidation">Callback invoked when an invalidation event is received.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when subscription is established.</returns>
        /// <remarks>
        /// The callback receives the cache key that should be invalidated. This is typically
        /// used to remove entries from the local L1 cache.
        /// </remarks>
        Task SubscribeAsync(Func<string, CancellationToken, Task> onInvalidation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from cache invalidation events.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UnsubscribeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache invalidation event payload.
    /// </summary>
    public class CacheInvalidationEvent
    {
        /// <summary>
        /// Gets or sets the cache key to invalidate.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the invalidation occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the instance ID that triggered the invalidation (optional).
        /// </summary>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata (optional).
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string>? Metadata { get; set; }
    }
}

