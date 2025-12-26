//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Management
{
    /// <summary>
    /// In-memory implementation of <see cref="IDeadLetterQueue"/> for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores failed jobs in memory. Failed jobs are lost when
    /// the application restarts. Suitable for testing and development scenarios.
    /// </para>
    /// </remarks>
    public class InMemoryDeadLetterQueue : IDeadLetterQueue
    {
        private readonly Dictionary<string, FailedJob> _failedJobs = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task AddFailedJobAsync(
            FailedJob failedJob,
            CancellationToken cancellationToken = default)
        {
            if (failedJob == null)
            {
                throw new ArgumentNullException(nameof(failedJob));
            }

            if (string.IsNullOrWhiteSpace(failedJob.JobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(failedJob));
            }

            lock (_lock)
            {
                failedJob.AddedToDlqAt = DateTimeOffset.UtcNow;
                _failedJobs[failedJob.JobId] = failedJob;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<FailedJob>> GetFailedJobsAsync(
            DeadLetterQueueFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _failedJobs.Values.AsQueryable();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.JobType))
                    {
                        query = query.Where(j => j.JobType == filter.JobType);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Queue))
                    {
                        query = query.Where(j => j.Queue == filter.Queue);
                    }

                    if (filter.StartDate.HasValue)
                    {
                        query = query.Where(j => j.AddedToDlqAt >= filter.StartDate.Value);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        query = query.Where(j => j.AddedToDlqAt <= filter.EndDate.Value);
                    }
                }

                var results = query
                    .OrderByDescending(j => j.AddedToDlqAt)
                    .AsEnumerable();

                if (filter != null)
                {
                    if (filter.Skip.HasValue)
                    {
                        results = results.Skip(filter.Skip.Value);
                    }

                    if (filter.MaxRecords.HasValue)
                    {
                        results = results.Take(filter.MaxRecords.Value);
                    }
                }

                return Task.FromResult<IReadOnlyList<FailedJob>>(results.ToList().AsReadOnly());
            }
        }

        /// <inheritdoc />
        public Task<FailedJob?> GetFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            lock (_lock)
            {
                _failedJobs.TryGetValue(jobId, out var failedJob);
                return Task.FromResult(failedJob);
            }
        }

        /// <inheritdoc />
        public Task<bool> RemoveFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            lock (_lock)
            {
                return Task.FromResult(_failedJobs.Remove(jobId));
            }
        }

        /// <inheritdoc />
        public Task<string?> RetryFailedJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            // Note: This method would need access to IJobScheduler to reschedule the job.
            // For now, it just removes the job from DLQ and returns the job ID.
            // The actual rescheduling should be handled by the caller or a higher-level service.
            
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            lock (_lock)
            {
                if (_failedJobs.Remove(jobId))
                {
                    return Task.FromResult<string?>(jobId);
                }

                return Task.FromResult<string?>(null);
            }
        }

        /// <inheritdoc />
        public Task<int> GetFailedJobCountAsync(
            DeadLetterQueueFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _failedJobs.Values.AsQueryable();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.JobType))
                    {
                        query = query.Where(j => j.JobType == filter.JobType);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Queue))
                    {
                        query = query.Where(j => j.Queue == filter.Queue);
                    }

                    if (filter.StartDate.HasValue)
                    {
                        query = query.Where(j => j.AddedToDlqAt >= filter.StartDate.Value);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        query = query.Where(j => j.AddedToDlqAt <= filter.EndDate.Value);
                    }
                }

                return Task.FromResult(query.Count());
            }
        }

        /// <inheritdoc />
        public Task<int> ClearOldFailedJobsAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var keysToRemove = _failedJobs
                    .Where(kvp => kvp.Value.AddedToDlqAt < olderThan)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _failedJobs.Remove(key);
                }

                return Task.FromResult(keysToRemove.Count);
            }
        }
    }
}

