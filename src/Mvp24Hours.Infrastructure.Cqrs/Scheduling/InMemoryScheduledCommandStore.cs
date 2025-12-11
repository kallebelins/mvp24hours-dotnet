//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// In-memory implementation of the scheduled command store.
    /// Suitable for development and testing. Use a persistent store (SQL, Redis) in production.
    /// </summary>
    public class InMemoryScheduledCommandStore : IScheduledCommandStore
    {
        private readonly ConcurrentDictionary<string, ScheduledCommandEntry> _entries = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task SaveAsync(ScheduledCommandEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            _entries[entry.Id] = entry;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ScheduledCommandEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id)) return Task.FromResult<ScheduledCommandEntry?>(null);

            _entries.TryGetValue(id, out var entry);
            return Task.FromResult(entry);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ScheduledCommandEntry>> GetReadyForExecutionAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var ready = _entries.Values
                .Where(e => e.Status == ScheduledCommandStatus.Pending &&
                           e.ScheduledAt <= now &&
                           (!e.ExpiresAt.HasValue || e.ExpiresAt.Value > now))
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.ScheduledAt)
                .Take(batchSize)
                .ToList();

            return Task.FromResult<IReadOnlyList<ScheduledCommandEntry>>(ready);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ScheduledCommandEntry>> GetReadyForRetryAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var ready = _entries.Values
                .Where(e => e.Status == ScheduledCommandStatus.Failed &&
                           e.RetryCount < e.MaxRetries &&
                           e.NextRetryAt.HasValue &&
                           e.NextRetryAt.Value <= now &&
                           (!e.ExpiresAt.HasValue || e.ExpiresAt.Value > now))
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.NextRetryAt)
                .Take(batchSize)
                .ToList();

            return Task.FromResult<IReadOnlyList<ScheduledCommandEntry>>(ready);
        }

        /// <inheritdoc />
        public Task UpdateAsync(ScheduledCommandEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (_entries.ContainsKey(entry.Id))
            {
                _entries[entry.Id] = entry;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id)) return Task.FromResult(false);

            return Task.FromResult(_entries.TryRemove(id, out _));
        }

        /// <inheritdoc />
        public Task<int> PurgeCompletedAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var toRemove = _entries.Values
                .Where(e => e.Status == ScheduledCommandStatus.Completed &&
                           e.CompletedAt.HasValue &&
                           e.CompletedAt.Value < olderThan)
                .Select(e => e.Id)
                .ToList();

            int count = 0;
            foreach (var id in toRemove)
            {
                if (_entries.TryRemove(id, out _))
                {
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ScheduledCommandEntry>> GetByStatusAsync(
            ScheduledCommandStatus status,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            var entries = _entries.Values
                .Where(e => e.Status == status)
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<ScheduledCommandEntry>>(entries);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ScheduledCommandEntry>> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(correlationId))
                return Task.FromResult<IReadOnlyList<ScheduledCommandEntry>>([]);

            var entries = _entries.Values
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<ScheduledCommandEntry>>(entries);
        }

        /// <inheritdoc />
        public Task<int> MarkExpiredAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            int count = 0;

            foreach (var entry in _entries.Values.Where(e =>
                e.Status == ScheduledCommandStatus.Pending &&
                e.ExpiresAt.HasValue &&
                e.ExpiresAt.Value <= now))
            {
                entry.Status = ScheduledCommandStatus.Expired;
                count++;
            }

            return Task.FromResult(count);
        }

        /// <inheritdoc />
        public Task<ScheduledCommandStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var entries = _entries.Values.ToList();

            var stats = new ScheduledCommandStatistics
            {
                PendingCount = entries.Count(e => e.Status == ScheduledCommandStatus.Pending),
                ProcessingCount = entries.Count(e => e.Status == ScheduledCommandStatus.Processing),
                CompletedCount = entries.Count(e => e.Status == ScheduledCommandStatus.Completed),
                FailedCount = entries.Count(e => e.Status == ScheduledCommandStatus.Failed),
                CancelledCount = entries.Count(e => e.Status == ScheduledCommandStatus.Cancelled),
                ExpiredCount = entries.Count(e => e.Status == ScheduledCommandStatus.Expired),
                OldestPendingAt = entries
                    .Where(e => e.Status == ScheduledCommandStatus.Pending)
                    .OrderBy(e => e.ScheduledAt)
                    .FirstOrDefault()?.ScheduledAt,
                RetryPendingCount = entries.Count(e =>
                    e.Status == ScheduledCommandStatus.Failed &&
                    e.RetryCount < e.MaxRetries &&
                    e.NextRetryAt.HasValue &&
                    e.NextRetryAt.Value <= now)
            };

            return Task.FromResult(stats);
        }

        /// <summary>
        /// Clears all entries. For testing purposes.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}

