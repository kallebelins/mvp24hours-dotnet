//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// In-memory implementation of the scheduled message store.
    /// Suitable for development and single-instance scenarios.
    /// For production with multiple instances, use a distributed store (Redis, SQL, etc.).
    /// </summary>
    public class InMemoryScheduledMessageStore : IScheduledMessageStore
    {
        private readonly ConcurrentDictionary<Guid, ScheduledMessage> _messages = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <inheritdoc />
        public Task AddAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            _messages.TryAdd(message.Id, message);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdateAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            _messages[message.Id] = message;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ScheduledMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _messages.TryGetValue(id, out var message);
            return Task.FromResult(message);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduledMessage>> GetDueMessagesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var now = DateTimeOffset.UtcNow;
                return _messages.Values
                    .Where(m => (m.Status == ScheduledMessageStatus.Pending || m.Status == ScheduledMessageStatus.Active)
                        && (m.IsRecurring ? m.NextExecutionTime <= now : m.ScheduledTime <= now))
                    .OrderBy(m => m.IsRecurring ? m.NextExecutionTime : m.ScheduledTime)
                    .Take(batchSize)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            var result = _messages.Values
                .Where(m => m.Status == ScheduledMessageStatus.Pending)
                .OrderBy(m => m.ScheduledTime)
                .ToList();
            return Task.FromResult<IEnumerable<ScheduledMessage>>(result);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetActiveRecurringMessagesAsync(CancellationToken cancellationToken = default)
        {
            var result = _messages.Values
                .Where(m => m.IsRecurring && m.Status == ScheduledMessageStatus.Active)
                .OrderBy(m => m.NextExecutionTime)
                .ToList();
            return Task.FromResult<IEnumerable<ScheduledMessage>>(result);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetByStatusAsync(
            ScheduledMessageStatus status,
            CancellationToken cancellationToken = default)
        {
            var result = _messages.Values
                .Where(m => m.Status == status)
                .OrderBy(m => m.ScheduledTime)
                .ToList();
            return Task.FromResult<IEnumerable<ScheduledMessage>>(result);
        }

        /// <inheritdoc />
        public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_messages.TryRemove(id, out _));
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsProcessingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_messages.TryGetValue(id, out var message))
                {
                    if (message.Status == ScheduledMessageStatus.Processing)
                    {
                        return false; // Already being processed
                    }

                    message.Status = ScheduledMessageStatus.Processing;
                    return true;
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Task MarkAsCompletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var message))
            {
                message.Status = ScheduledMessageStatus.Completed;
                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var message))
            {
                message.Status = ScheduledMessageStatus.Failed;
                message.LastError = error;
                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<int> CleanupOldMessagesAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default)
        {
            var toRemove = _messages.Values
                .Where(m => (m.Status == ScheduledMessageStatus.Completed || m.Status == ScheduledMessageStatus.Failed)
                    && m.ProcessedAt.HasValue && m.ProcessedAt.Value < olderThan)
                .Select(m => m.Id)
                .ToList();

            var removed = 0;
            foreach (var id in toRemove)
            {
                if (_messages.TryRemove(id, out _))
                {
                    removed++;
                }
            }

            return Task.FromResult(removed);
        }

        /// <inheritdoc />
        public Task<Dictionary<ScheduledMessageStatus, int>> GetStatusCountsAsync(
            CancellationToken cancellationToken = default)
        {
            var result = _messages.Values
                .GroupBy(m => m.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            // Ensure all statuses are present
            foreach (ScheduledMessageStatus status in Enum.GetValues<ScheduledMessageStatus>())
            {
                result.TryAdd(status, 0);
            }

            return Task.FromResult(result);
        }
    }
}

