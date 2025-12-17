//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// In-memory implementation of <see cref="IDeadLetterStore"/>.
    /// Useful for testing and single-instance scenarios.
    /// </summary>
    public class InMemoryDeadLetterStore : IDeadLetterStore
    {
        private readonly ConcurrentDictionary<Guid, DeadLetterOperation> _store = new();
        private readonly int _maxItems;

        /// <summary>
        /// Creates a new instance with default settings.
        /// </summary>
        public InMemoryDeadLetterStore()
            : this(10000)
        {
        }

        /// <summary>
        /// Creates a new instance with a maximum item limit.
        /// </summary>
        /// <param name="maxItems">Maximum number of items to store.</param>
        public InMemoryDeadLetterStore(int maxItems)
        {
            _maxItems = maxItems > 0 ? maxItems : throw new ArgumentOutOfRangeException(nameof(maxItems));
        }

        /// <inheritdoc />
        public Task StoreAsync(DeadLetterOperation deadLetter, CancellationToken cancellationToken = default)
        {
            if (deadLetter == null)
                throw new ArgumentNullException(nameof(deadLetter));

            cancellationToken.ThrowIfCancellationRequested();

            // Enforce max items limit by removing oldest acknowledged items
            while (_store.Count >= _maxItems)
            {
                var oldestAcknowledged = _store.Values
                    .Where(x => x.IsAcknowledged)
                    .OrderBy(x => x.AcknowledgedAt ?? x.FailedAt)
                    .FirstOrDefault();

                if (oldestAcknowledged != null)
                {
                    _store.TryRemove(oldestAcknowledged.Id, out _);
                }
                else
                {
                    // Remove oldest unacknowledged if no acknowledged items
                    var oldest = _store.Values
                        .OrderBy(x => x.FailedAt)
                        .FirstOrDefault();

                    if (oldest != null)
                    {
                        _store.TryRemove(oldest.Id, out _);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _store[deadLetter.Id] = deadLetter;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<DeadLetterOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _store.TryGetValue(id, out var result);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<DeadLetterOperation>> GetAllAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            DateTimeOffset? fromDate = null,
            DateTimeOffset? toDate = null,
            bool includeAcknowledged = false,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = _store.Values.AsEnumerable();

            if (!includeAcknowledged)
            {
                query = query.Where(x => !x.IsAcknowledged);
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                query = query.Where(x => x.OperationName == operationName);
            }

            if (reason.HasValue)
            {
                query = query.Where(x => x.Reason == reason.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.FailedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.FailedAt <= toDate.Value);
            }

            var result = query
                .OrderByDescending(x => x.FailedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<DeadLetterOperation>>(result);
        }

        /// <inheritdoc />
        public Task<long> GetCountAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            bool includeAcknowledged = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = _store.Values.AsEnumerable();

            if (!includeAcknowledged)
            {
                query = query.Where(x => !x.IsAcknowledged);
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                query = query.Where(x => x.OperationName == operationName);
            }

            if (reason.HasValue)
            {
                query = query.Where(x => x.Reason == reason.Value);
            }

            return Task.FromResult((long)query.Count());
        }

        /// <inheritdoc />
        public Task<bool> AcknowledgeAsync(Guid id, string? acknowledgedBy = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_store.TryGetValue(id, out var deadLetter))
            {
                deadLetter.IsAcknowledged = true;
                deadLetter.AcknowledgedAt = DateTimeOffset.UtcNow;
                deadLetter.AcknowledgedBy = acknowledgedBy;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> MarkReprocessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_store.TryGetValue(id, out var deadLetter))
            {
                deadLetter.ReprocessCount++;
                deadLetter.LastReprocessAt = DateTimeOffset.UtcNow;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_store.TryRemove(id, out _));
        }

        /// <inheritdoc />
        public Task<int> PurgeAcknowledgedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cutoff = DateTimeOffset.UtcNow.Subtract(olderThan);
            var toRemove = _store.Values
                .Where(x => x.IsAcknowledged && x.AcknowledgedAt.HasValue && x.AcknowledgedAt.Value < cutoff)
                .Select(x => x.Id)
                .ToList();

            var count = 0;
            foreach (var id in toRemove)
            {
                if (_store.TryRemove(id, out _))
                {
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<DeadLetterOperation>> GetForReprocessingAsync(
            int maxReprocessAttempts = 3,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _store.Values
                .Where(x => !x.IsAcknowledged && x.ReprocessCount < maxReprocessAttempts)
                .OrderBy(x => x.ReprocessCount)
                .ThenBy(x => x.FailedAt)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<DeadLetterOperation>>(result);
        }

        /// <summary>
        /// Clears all dead letters from the store.
        /// </summary>
        public void Clear()
        {
            _store.Clear();
        }

        /// <summary>
        /// Gets the current count of all items in the store.
        /// </summary>
        public int Count => _store.Count;

        /// <summary>
        /// Gets all dead letters as a list (for testing/debugging).
        /// </summary>
        public IReadOnlyList<DeadLetterOperation> GetAllSync()
        {
            return _store.Values.OrderByDescending(x => x.FailedAt).ToList();
        }
    }
}

