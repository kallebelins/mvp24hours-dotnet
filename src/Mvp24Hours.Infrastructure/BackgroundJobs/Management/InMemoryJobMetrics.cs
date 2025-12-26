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
    /// In-memory implementation of <see cref="IJobMetrics"/> for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores metrics in memory. Metrics are lost when the application
    /// restarts. Suitable for testing and development scenarios.
    /// </para>
    /// </remarks>
    public class InMemoryJobMetrics : IJobMetrics
    {
        private readonly List<JobMetric> _metrics = new();
        private readonly Dictionary<string, QueueStatistics> _queueStats = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task RecordMetricAsync(
            JobMetric metric,
            CancellationToken cancellationToken = default)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            lock (_lock)
            {
                _metrics.Add(metric);

                // Update queue statistics
                var queueName = metric.Queue ?? "default";
                if (!_queueStats.TryGetValue(queueName, out var queueStat))
                {
                    queueStat = new QueueStatistics { QueueName = queueName };
                    _queueStats[queueName] = queueStat;
                }

                queueStat.LastUpdated = DateTimeOffset.UtcNow;

                if (metric.Success)
                {
                    queueStat.CompletedJobs++;
                }
                else if (metric.Status == Contract.JobStatus.Failed)
                {
                    queueStat.FailedJobs++;
                }

                if (metric.Duration.HasValue)
                {
                    var durations = _metrics
                        .Where(m => m.Queue == queueName && m.Duration.HasValue)
                        .Select(m => m.Duration!.Value)
                        .ToList();

                    if (durations.Count > 0)
                    {
                        queueStat.AverageExecutionTime = TimeSpan.FromMilliseconds(
                            durations.Average(d => d.TotalMilliseconds));
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<JobMetricsAggregate> GetMetricsAsync(
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
                var metrics = _metrics.Where(m => m.JobType == jobType);

                if (startDate.HasValue)
                {
                    metrics = metrics.Where(m => m.RecordedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    metrics = metrics.Where(m => m.RecordedAt <= endDate.Value);
                }

                var metricsList = metrics.ToList();

                var aggregate = new JobMetricsAggregate
                {
                    JobType = jobType,
                    TotalExecutions = metricsList.Count,
                    SuccessfulExecutions = metricsList.Count(m => m.Success),
                    FailedExecutions = metricsList.Count(m => m.Status == Contract.JobStatus.Failed),
                    CancelledExecutions = metricsList.Count(m => m.Status == Contract.JobStatus.Cancelled),
                    StartDate = startDate,
                    EndDate = endDate
                };

                var durations = metricsList
                    .Where(m => m.Duration.HasValue)
                    .Select(m => m.Duration!.Value)
                    .OrderBy(d => d.TotalMilliseconds)
                    .ToList();

                if (durations.Count > 0)
                {
                    aggregate.AverageDuration = TimeSpan.FromMilliseconds(
                        durations.Average(d => d.TotalMilliseconds));
                    aggregate.MinDuration = durations.First();
                    aggregate.MaxDuration = durations.Last();
                    aggregate.P50Duration = durations[durations.Count / 2];
                    aggregate.P95Duration = durations[(int)(durations.Count * 0.95)];
                    aggregate.P99Duration = durations[(int)(durations.Count * 0.99)];
                }

                // Calculate throughput
                if (startDate.HasValue && endDate.HasValue)
                {
                    var period = endDate.Value - startDate.Value;
                    if (period.TotalSeconds > 0)
                    {
                        aggregate.Throughput = aggregate.TotalExecutions / period.TotalSeconds;
                    }
                }

                return Task.FromResult(aggregate);
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, JobMetricsAggregate>> GetAllMetricsAsync(
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var jobTypes = _metrics
                    .Select(m => m.JobType)
                    .Distinct()
                    .ToList();

                var result = new Dictionary<string, JobMetricsAggregate>();

                foreach (var jobType in jobTypes)
                {
                    var metrics = GetMetricsAsync(jobType, startDate, endDate, cancellationToken).Result;
                    result[jobType] = metrics;
                }

                return Task.FromResult<IReadOnlyDictionary<string, JobMetricsAggregate>>(result);
            }
        }

        /// <inheritdoc />
        public Task<QueueStatistics> GetQueueStatisticsAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));
            }

            lock (_lock)
            {
                if (_queueStats.TryGetValue(queueName, out var stats))
                {
                    return Task.FromResult(stats);
                }

                // Return empty statistics if queue doesn't exist
                return Task.FromResult(new QueueStatistics { QueueName = queueName });
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, QueueStatistics>> GetAllQueueStatisticsAsync(
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult<IReadOnlyDictionary<string, QueueStatistics>>(
                    new Dictionary<string, QueueStatistics>(_queueStats));
            }
        }

        /// <inheritdoc />
        public Task ResetMetricsAsync(
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _metrics.Clear();
                _queueStats.Clear();
            }

            return Task.CompletedTask;
        }
    }
}

