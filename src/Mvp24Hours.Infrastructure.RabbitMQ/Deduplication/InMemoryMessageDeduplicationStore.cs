//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;

/// <summary>
/// In-memory implementation of message deduplication store.
/// Suitable for single-instance deployments or testing.
/// For distributed scenarios, use Redis or database-backed implementations.
/// </summary>
/// <remarks>
/// Creates a new instance of InMemoryMessageDeduplicationStore.
/// </remarks>
/// <param name="defaultExpirationMinutes">Default expiration time in minutes for entries. Default is 60 minutes.</param>
/// <param name="maxEntries">Maximum number of entries to keep. Default is 100,000.</param>
public class InMemoryMessageDeduplicationStore(int defaultExpirationMinutes = 60, int maxEntries = 100_000) : IMessageDeduplicationStore
{
    private readonly ConcurrentDictionary<string, DeduplicationEntry> _processedMessages = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(defaultExpirationMinutes);
    private readonly int _maxEntries = maxEntries;
    private readonly object _cleanupLock = new();

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        if (_processedMessages.TryGetValue(messageId, out DeduplicationEntry? entry))
        {
            // Check if entry has expired
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                // Entry expired, remove it
                _processedMessages.TryRemove(messageId, out _);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(string messageId, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        var entry = new DeduplicationEntry
        {
            MessageId = messageId,
            ProcessedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.Add(_defaultExpiration)
        };

        _processedMessages.AddOrUpdate(messageId, entry, (_, _) => entry);

        // Trigger cleanup if we've exceeded max entries
        if (_processedMessages.Count > _maxEntries)
        {
            _ = Task.Run(() => CleanupExpiredAsync(cancellationToken), cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _processedMessages.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        if (!Monitor.TryEnter(_cleanupLock))
        {
            // Cleanup already in progress
            return Task.CompletedTask;
        }

        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var expiredKeys = _processedMessages
                .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string? key in expiredKeys)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _processedMessages.TryRemove(key, out _);
            }

            // If still over max, remove oldest entries
            if (_processedMessages.Count > _maxEntries)
            {
                var toRemove = _processedMessages
                    .OrderBy(kvp => kvp.Value.ProcessedAt)
                    .Take(_processedMessages.Count - _maxEntries)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (string? key in toRemove)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _processedMessages.TryRemove(key, out _);
                }
            }
        }
        finally
        {
            Monitor.Exit(_cleanupLock);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current number of entries in the store.
    /// </summary>
    public int Count => _processedMessages.Count;

    private class DeduplicationEntry
    {
        public string MessageId { get; init; } = string.Empty;
        public DateTimeOffset ProcessedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }
}

