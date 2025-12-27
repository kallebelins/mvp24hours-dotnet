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

namespace Mvp24Hours.Infrastructure.Caching.Invalidation
{
    /// <summary>
    /// In-memory implementation of cache invalidation event publisher for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores events in memory and is useful for testing or
    /// single-instance scenarios. Events are not shared across application instances.
    /// </para>
    /// </remarks>
    public class InMemoryCacheInvalidationEventPublisher : ICacheInvalidationEventPublisher
    {
        private readonly ConcurrentQueue<CacheInvalidationEvent> _events = new();
        private readonly ILogger<InMemoryCacheInvalidationEventPublisher>? _logger;

        /// <summary>
        /// Creates a new instance of InMemoryCacheInvalidationEventPublisher.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public InMemoryCacheInvalidationEventPublisher(ILogger<InMemoryCacheInvalidationEventPublisher>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task PublishKeyInvalidationAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var evt = new CacheInvalidationEvent
            {
                Type = CacheInvalidationEventType.Key,
                Key = key,
                Timestamp = DateTimeOffset.UtcNow
            };

            _events.Enqueue(evt);
            _logger?.LogDebug("Published key invalidation event for {Key} (in-memory)", key);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PublishTagInvalidationAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

            var evt = new CacheInvalidationEvent
            {
                Type = CacheInvalidationEventType.Tag,
                Tag = tag,
                Timestamp = DateTimeOffset.UtcNow
            };

            _events.Enqueue(evt);
            _logger?.LogDebug("Published tag invalidation event for {Tag} (in-memory)", tag);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PublishTagsInvalidationAsync(string[] tags, CancellationToken cancellationToken = default)
        {
            if (tags == null || tags.Length == 0)
                return Task.CompletedTask;

            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    var evt = new CacheInvalidationEvent
                    {
                        Type = CacheInvalidationEventType.Tags,
                        Tags = tags,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    _events.Enqueue(evt);
                }
            }

            _logger?.LogDebug("Published tags invalidation event for {TagCount} tags (in-memory)", tags.Length);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets all published events (for testing).
        /// </summary>
        /// <returns>Collection of published events.</returns>
        public CacheInvalidationEvent[] GetPublishedEvents()
        {
            return _events.ToArray();
        }

        /// <summary>
        /// Clears all published events (for testing).
        /// </summary>
        public void ClearEvents()
        {
            while (_events.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// Represents a cache invalidation event.
    /// </summary>
    internal class CacheInvalidationEvent
    {
        public CacheInvalidationEventType Type { get; set; }
        public string? Key { get; set; }
        public string? Tag { get; set; }
        public string[]? Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Type of cache invalidation event.
    /// </summary>
    internal enum CacheInvalidationEventType
    {
        Key,
        Tag,
        Tags
    }
}

