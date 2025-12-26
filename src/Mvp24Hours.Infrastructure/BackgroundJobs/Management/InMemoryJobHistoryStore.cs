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
    /// In-memory implementation of <see cref="IJobHistoryStore"/> for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores job execution history in memory. History is lost when
    /// the application restarts. Suitable for testing and development scenarios.
    /// </para>
    /// </remarks>
    public class InMemoryJobHistoryStore : IJobHistoryStore
    {
        private readonly List<JobExecutionRecord> _records = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task RecordExecutionAsync(
            JobExecutionRecord record,
            CancellationToken cancellationToken = default)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            lock (_lock)
            {
                _records.Add(record);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<JobExecutionRecord>> GetJobHistoryAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            lock (_lock)
            {
                var history = _records
                    .Where(r => r.JobId == jobId)
                    .OrderByDescending(r => r.StartedAt)
                    .ToList()
                    .AsReadOnly();

                return Task.FromResult<IReadOnlyList<JobExecutionRecord>>(history);
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<JobExecutionRecord>> QueryHistoryAsync(
            JobHistoryFilter filter,
            CancellationToken cancellationToken = default)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            lock (_lock)
            {
                var query = _records.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.JobId))
                {
                    query = query.Where(r => r.JobId == filter.JobId);
                }

                if (!string.IsNullOrWhiteSpace(filter.JobType))
                {
                    query = query.Where(r => r.JobType == filter.JobType);
                }

                if (filter.Status.HasValue)
                {
                    query = query.Where(r => r.Status == filter.Status.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Queue))
                {
                    query = query.Where(r => r.Queue == filter.Queue);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(r => r.StartedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(r => r.StartedAt <= filter.EndDate.Value);
                }

                var results = query
                    .OrderByDescending(r => r.StartedAt)
                    .AsEnumerable();

                if (filter.Skip.HasValue)
                {
                    results = results.Skip(filter.Skip.Value);
                }

                if (filter.MaxRecords.HasValue)
                {
                    results = results.Take(filter.MaxRecords.Value);
                }

                return Task.FromResult<IReadOnlyList<JobExecutionRecord>>(results.ToList().AsReadOnly());
            }
        }

        /// <inheritdoc />
        public Task<JobExecutionStatistics> GetStatisticsAsync(
            string jobType,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobType))
            {
                throw new ArgumentException("Job type cannot be null or empty.", nameof(jobType));
            }

            lock (_lock)
            {
                var records = _records.Where(r => r.JobType == jobType);

                if (startDate.HasValue)
                {
                    records = records.Where(r => r.StartedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    records = records.Where(r => r.StartedAt <= endDate.Value);
                }

                var recordsList = records.ToList();

                var statistics = new JobExecutionStatistics
                {
                    JobType = jobType,
                    TotalExecutions = recordsList.Count,
                    SuccessfulExecutions = recordsList.Count(r => r.Status == Contract.JobStatus.Completed),
                    FailedExecutions = recordsList.Count(r => r.Status == Contract.JobStatus.Failed),
                    CancelledExecutions = recordsList.Count(r => r.Status == Contract.JobStatus.Cancelled)
                };

                var durations = recordsList
                    .Where(r => r.Duration.HasValue)
                    .Select(r => r.Duration!.Value)
                    .ToList();

                if (durations.Count > 0)
                {
                    statistics.AverageDuration = TimeSpan.FromMilliseconds(
                        durations.Average(d => d.TotalMilliseconds));
                    statistics.MinDuration = durations.Min();
                    statistics.MaxDuration = durations.Max();
                }

                return Task.FromResult(statistics);
            }
        }

        /// <inheritdoc />
        public Task<int> CleanupOldRecordsAsync(
            int retentionDays,
            CancellationToken cancellationToken = default)
        {
            if (retentionDays < 0)
            {
                throw new ArgumentException("Retention days must be greater than or equal to zero.", nameof(retentionDays));
            }

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);

            lock (_lock)
            {
                var removedCount = _records.RemoveAll(r => r.StartedAt < cutoffDate);
                return Task.FromResult(removedCount);
            }
        }
    }
}

