//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// Redis-based implementation of the scheduled message store using IDistributedCache.
    /// Suitable for distributed/multi-instance scenarios.
    /// Note: This is a simplified implementation. For production, consider using
    /// Redis Sorted Sets for better performance on time-based queries.
    /// </summary>
    public class RedisScheduledMessageStore : IScheduledMessageStore
    {
        private readonly IDistributedCache _cache;
        private readonly string _keyPrefix;
        private readonly TimeSpan _defaultExpiration;
        private readonly JsonSerializerOptions _jsonOptions;

        // Local index for message IDs (for iteration - in production, use Redis Sets)
        private readonly ConcurrentDictionary<Guid, bool> _messageIndex = new();

        private const string INDEX_KEY = "mvp:scheduled:index";

        /// <summary>
        /// Initializes a new instance of the RedisScheduledMessageStore.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="keyPrefix">Optional key prefix. Default is "mvp:scheduled:".</param>
        /// <param name="defaultExpiration">Default expiration for completed messages. Default is 7 days.</param>
        public RedisScheduledMessageStore(
            IDistributedCache cache,
            string keyPrefix = "mvp:scheduled:",
            TimeSpan? defaultExpiration = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keyPrefix = keyPrefix;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromDays(7);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        private string GetKey(Guid id) => $"{_keyPrefix}{id}";

        /// <inheritdoc />
        public async Task AddAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var key = GetKey(message.Id);
            var json = JsonSerializer.Serialize(message, _jsonOptions);

            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                // Keep pending messages for a long time
                AbsoluteExpirationRelativeToNow = message.IsRecurring ? null : _defaultExpiration
            }, cancellationToken);

            // Add to local index
            _messageIndex.TryAdd(message.Id, true);

            // Also store in a global index (for iteration)
            await UpdateIndexAsync(message.Id, true, cancellationToken);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var key = GetKey(message.Id);
            var json = JsonSerializer.Serialize(message, _jsonOptions);

            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = message.Status == ScheduledMessageStatus.Completed ||
                                                   message.Status == ScheduledMessageStatus.Failed
                    ? _defaultExpiration
                    : null
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ScheduledMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var key = GetKey(id);
            var json = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ScheduledMessage>(json, _jsonOptions);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduledMessage>> GetDueMessagesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var result = new List<ScheduledMessage>();

            foreach (var id in _messageIndex.Keys.Take(batchSize * 10)) // Check more than batch size
            {
                if (result.Count >= batchSize)
                {
                    break;
                }

                var message = await GetByIdAsync(id, cancellationToken);
                if (message == null)
                {
                    _messageIndex.TryRemove(id, out _);
                    continue;
                }

                var isDue = (message.Status == ScheduledMessageStatus.Pending || message.Status == ScheduledMessageStatus.Active)
                    && (message.IsRecurring ? message.NextExecutionTime <= now : message.ScheduledTime <= now);

                if (isDue)
                {
                    result.Add(message);
                }
            }

            return result.OrderBy(m => m.IsRecurring ? m.NextExecutionTime : m.ScheduledTime).Take(batchSize);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduledMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<ScheduledMessage>();

            foreach (var id in _messageIndex.Keys)
            {
                var message = await GetByIdAsync(id, cancellationToken);
                if (message?.Status == ScheduledMessageStatus.Pending)
                {
                    result.Add(message);
                }
            }

            return result.OrderBy(m => m.ScheduledTime);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduledMessage>> GetActiveRecurringMessagesAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<ScheduledMessage>();

            foreach (var id in _messageIndex.Keys)
            {
                var message = await GetByIdAsync(id, cancellationToken);
                if (message?.IsRecurring == true && message.Status == ScheduledMessageStatus.Active)
                {
                    result.Add(message);
                }
            }

            return result.OrderBy(m => m.NextExecutionTime);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduledMessage>> GetByStatusAsync(
            ScheduledMessageStatus status,
            CancellationToken cancellationToken = default)
        {
            var result = new List<ScheduledMessage>();

            foreach (var id in _messageIndex.Keys)
            {
                var message = await GetByIdAsync(id, cancellationToken);
                if (message?.Status == status)
                {
                    result.Add(message);
                }
            }

            return result.OrderBy(m => m.ScheduledTime);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var key = GetKey(id);
            await _cache.RemoveAsync(key, cancellationToken);
            _messageIndex.TryRemove(id, out _);
            await UpdateIndexAsync(id, false, cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsProcessingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var message = await GetByIdAsync(id, cancellationToken);
            if (message == null || message.Status == ScheduledMessageStatus.Processing)
            {
                return false;
            }

            message.Status = ScheduledMessageStatus.Processing;
            await UpdateAsync(message, cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task MarkAsCompletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var message = await GetByIdAsync(id, cancellationToken);
            if (message != null)
            {
                message.Status = ScheduledMessageStatus.Completed;
                message.ProcessedAt = DateTimeOffset.UtcNow;
                await UpdateAsync(message, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            var message = await GetByIdAsync(id, cancellationToken);
            if (message != null)
            {
                message.Status = ScheduledMessageStatus.Failed;
                message.LastError = error;
                message.ProcessedAt = DateTimeOffset.UtcNow;
                await UpdateAsync(message, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<int> CleanupOldMessagesAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default)
        {
            var removed = 0;

            foreach (var id in _messageIndex.Keys.ToList())
            {
                var message = await GetByIdAsync(id, cancellationToken);
                if (message == null)
                {
                    _messageIndex.TryRemove(id, out _);
                    continue;
                }

                if ((message.Status == ScheduledMessageStatus.Completed || message.Status == ScheduledMessageStatus.Failed)
                    && message.ProcessedAt.HasValue && message.ProcessedAt.Value < olderThan)
                {
                    await RemoveAsync(id, cancellationToken);
                    removed++;
                }
            }

            return removed;
        }

        /// <inheritdoc />
        public async Task<Dictionary<ScheduledMessageStatus, int>> GetStatusCountsAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<ScheduledMessageStatus, int>();

            foreach (ScheduledMessageStatus status in Enum.GetValues<ScheduledMessageStatus>())
            {
                result[status] = 0;
            }

            foreach (var id in _messageIndex.Keys)
            {
                var message = await GetByIdAsync(id, cancellationToken);
                if (message != null)
                {
                    result[message.Status]++;
                }
            }

            return result;
        }

        private async Task UpdateIndexAsync(Guid id, bool add, CancellationToken cancellationToken)
        {
            // Get current index
            var indexJson = await _cache.GetStringAsync(INDEX_KEY, cancellationToken);
            var index = string.IsNullOrEmpty(indexJson)
                ? new HashSet<Guid>()
                : JsonSerializer.Deserialize<HashSet<Guid>>(indexJson, _jsonOptions) ?? new HashSet<Guid>();

            if (add)
            {
                index.Add(id);
            }
            else
            {
                index.Remove(id);
            }

            // Save updated index
            await _cache.SetStringAsync(INDEX_KEY, JsonSerializer.Serialize(index, _jsonOptions), cancellationToken);
        }

        /// <summary>
        /// Loads the message index from Redis on startup.
        /// </summary>
        public async Task LoadIndexAsync(CancellationToken cancellationToken = default)
        {
            var indexJson = await _cache.GetStringAsync(INDEX_KEY, cancellationToken);
            if (!string.IsNullOrEmpty(indexJson))
            {
                var index = JsonSerializer.Deserialize<HashSet<Guid>>(indexJson, _jsonOptions);
                if (index != null)
                {
                    foreach (var id in index)
                    {
                        _messageIndex.TryAdd(id, true);
                    }
                }
            }
        }
    }
}

