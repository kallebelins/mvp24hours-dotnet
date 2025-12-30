//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// In-memory implementation of IHybridCacheTagManager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores tag-key associations in memory using concurrent dictionaries.
    /// It's suitable for single-instance applications or as a local cache in distributed scenarios.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    /// <item>Data is lost on application restart</item>
    /// <item>Not shared across application instances</item>
    /// <item>Memory usage grows with number of keys/tags</item>
    /// </list>
    /// </para>
    /// <para>
    /// For distributed scenarios, consider implementing a Redis-based tag manager
    /// using Redis Sets for tag-key associations.
    /// </para>
    /// </remarks>
    public class InMemoryHybridCacheTagManager : IHybridCacheTagManager
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _tagToKeys = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _keyToTags = new();
        private readonly object _lock = new();
        private readonly ILogger<InMemoryHybridCacheTagManager>? _logger;
        private long _tagInvalidations;

        /// <summary>
        /// Creates a new instance of InMemoryHybridCacheTagManager.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public InMemoryHybridCacheTagManager(ILogger<InMemoryHybridCacheTagManager>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task TrackKeyWithTagsAsync(string key, string[] tags, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (tags == null || tags.Length == 0)
                return Task.CompletedTask;

            lock (_lock)
            {
                // Add key to each tag's set
                foreach (var tag in tags)
                {
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;

                    var keysSet = _tagToKeys.GetOrAdd(tag, _ => new HashSet<string>());
                    keysSet.Add(key);
                }

                // Add tags to key's set
                var tagsSet = _keyToTags.GetOrAdd(key, _ => new HashSet<string>());
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                        tagsSet.Add(tag);
                }
            }

            _logger?.LogDebug("Tracked key {Key} with {TagCount} tags", key, tags.Length);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveKeyFromTagsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            lock (_lock)
            {
                // Get tags for this key
                if (_keyToTags.TryRemove(key, out var tags))
                {
                    // Remove key from each tag's set
                    foreach (var tag in tags)
                    {
                        if (_tagToKeys.TryGetValue(tag, out var keysSet))
                        {
                            keysSet.Remove(key);

                            // Clean up empty tag sets
                            if (keysSet.Count == 0)
                            {
                                _tagToKeys.TryRemove(tag, out _);
                            }
                        }
                    }
                }
            }

            _logger?.LogDebug("Removed key {Key} from tag tracking", key);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

            lock (_lock)
            {
                if (_tagToKeys.TryGetValue(tag, out var keysSet))
                {
                    return Task.FromResult<IEnumerable<string>>(keysSet.ToArray());
                }
            }

            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

            lock (_lock)
            {
                if (_keyToTags.TryGetValue(key, out var tagsSet))
                {
                    return Task.FromResult<IEnumerable<string>>(tagsSet.ToArray());
                }
            }

            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        /// <inheritdoc />
        public Task InvalidateTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Task.CompletedTask;

            lock (_lock)
            {
                if (_tagToKeys.TryRemove(tag, out var keysSet))
                {
                    // Remove tag from each key's tag set
                    foreach (var key in keysSet)
                    {
                        if (_keyToTags.TryGetValue(key, out var tags))
                        {
                            tags.Remove(tag);

                            // Clean up empty key-tag sets
                            if (tags.Count == 0)
                            {
                                _keyToTags.TryRemove(key, out _);
                            }
                        }
                    }

                    Interlocked.Increment(ref _tagInvalidations);
                    _logger?.LogDebug("Invalidated tag {Tag} affecting {KeyCount} keys", tag, keysSet.Count);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public HybridCacheTagStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new HybridCacheTagStatistics
                {
                    TotalTags = _tagToKeys.Count,
                    TotalAssociations = _keyToTags.Values.Sum(s => s.Count),
                    TagInvalidations = Interlocked.Read(ref _tagInvalidations),
                    KeysPerTag = _tagToKeys.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
                };

                return stats;
            }
        }

        /// <inheritdoc />
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _tagToKeys.Clear();
                _keyToTags.Clear();
            }

            _logger?.LogDebug("Cleared all tag tracking data");
            return Task.CompletedTask;
        }
    }
}

