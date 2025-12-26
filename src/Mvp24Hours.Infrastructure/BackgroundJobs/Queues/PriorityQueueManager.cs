//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Queues
{
    /// <summary>
    /// Manages priority queues for background jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides priority-based job queue management. Jobs are organized by
    /// priority level, with higher priority jobs being executed before lower priority jobs.
    /// </para>
    /// <para>
    /// Priority levels (from highest to lowest):
    /// - Critical (3)
    /// - High (2)
    /// - Normal (1)
    /// - Low (0)
    /// </para>
    /// </remarks>
    internal class PriorityQueueManager
    {
        private readonly Dictionary<string, Dictionary<JobPriority, Queue<QueuedJob>>> _queues = new();
        private readonly object _lock = new();

        /// <summary>
        /// Enqueues a job into the appropriate priority queue.
        /// </summary>
        /// <param name="queueName">The queue name (or "default" if not specified).</param>
        /// <param name="priority">The job priority.</param>
        /// <param name="job">The job to enqueue.</param>
        public void Enqueue(string queueName, JobPriority priority, QueuedJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            var normalizedQueueName = NormalizeQueueName(queueName);

            lock (_lock)
            {
                if (!_queues.TryGetValue(normalizedQueueName, out var priorityQueues))
                {
                    priorityQueues = new Dictionary<JobPriority, Queue<QueuedJob>>();
                    _queues[normalizedQueueName] = priorityQueues;
                }

                if (!priorityQueues.TryGetValue(priority, out var queue))
                {
                    queue = new Queue<QueuedJob>();
                    priorityQueues[priority] = queue;
                }

                queue.Enqueue(job);
            }
        }

        /// <summary>
        /// Dequeues the next job from the highest priority queue that has jobs available.
        /// </summary>
        /// <param name="queueName">The queue name (or "default" if not specified).</param>
        /// <returns>The next job to execute, or <c>null</c> if no jobs are available.</returns>
        public QueuedJob? Dequeue(string queueName)
        {
            var normalizedQueueName = NormalizeQueueName(queueName);

            lock (_lock)
            {
                if (!_queues.TryGetValue(normalizedQueueName, out var priorityQueues))
                {
                    return null;
                }

                // Try to dequeue from highest priority first
                var priorities = new[] { JobPriority.Critical, JobPriority.High, JobPriority.Normal, JobPriority.Low };

                foreach (var priority in priorities)
                {
                    if (priorityQueues.TryGetValue(priority, out var queue) && queue.Count > 0)
                    {
                        return queue.Dequeue();
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the count of jobs in a specific queue and priority.
        /// </summary>
        /// <param name="queueName">The queue name (or "default" if not specified).</param>
        /// <param name="priority">The priority level.</param>
        /// <returns>The number of jobs in the queue.</returns>
        public int GetCount(string queueName, JobPriority? priority = null)
        {
            var normalizedQueueName = NormalizeQueueName(queueName);

            lock (_lock)
            {
                if (!_queues.TryGetValue(normalizedQueueName, out var priorityQueues))
                {
                    return 0;
                }

                if (priority.HasValue)
                {
                    if (priorityQueues.TryGetValue(priority.Value, out var queue))
                    {
                        return queue.Count;
                    }

                    return 0;
                }

                // Return total count across all priorities
                return priorityQueues.Values.Sum(q => q.Count);
            }
        }

        /// <summary>
        /// Gets the total count of jobs across all queues.
        /// </summary>
        /// <returns>The total number of jobs.</returns>
        public int GetTotalCount()
        {
            lock (_lock)
            {
                return _queues.Values
                    .SelectMany(pq => pq.Values)
                    .Sum(q => q.Count);
            }
        }

        /// <summary>
        /// Clears all jobs from a specific queue.
        /// </summary>
        /// <param name="queueName">The queue name (or "default" if not specified).</param>
        public void Clear(string queueName)
        {
            var normalizedQueueName = NormalizeQueueName(queueName);

            lock (_lock)
            {
                if (_queues.TryGetValue(normalizedQueueName, out var priorityQueues))
                {
                    foreach (var queue in priorityQueues.Values)
                    {
                        queue.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Clears all jobs from all queues.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _queues.Clear();
            }
        }

        /// <summary>
        /// Gets statistics for all queues.
        /// </summary>
        /// <returns>A dictionary mapping queue names to their statistics.</returns>
        public Dictionary<string, QueueStats> GetQueueStatistics()
        {
            lock (_lock)
            {
                var stats = new Dictionary<string, QueueStats>();

                foreach (var kvp in _queues)
                {
                    var queueStats = new QueueStats
                    {
                        QueueName = kvp.Key,
                        CriticalCount = kvp.Value.TryGetValue(JobPriority.Critical, out var cq) ? cq.Count : 0,
                        HighCount = kvp.Value.TryGetValue(JobPriority.High, out var hq) ? hq.Count : 0,
                        NormalCount = kvp.Value.TryGetValue(JobPriority.Normal, out var nq) ? nq.Count : 0,
                        LowCount = kvp.Value.TryGetValue(JobPriority.Low, out var lq) ? lq.Count : 0
                    };

                    queueStats.TotalCount = queueStats.CriticalCount + queueStats.HighCount + 
                                          queueStats.NormalCount + queueStats.LowCount;

                    stats[kvp.Key] = queueStats;
                }

                return stats;
            }
        }

        private static string NormalizeQueueName(string? queueName)
        {
            return string.IsNullOrWhiteSpace(queueName) ? "default" : queueName;
        }

        /// <summary>
        /// Represents a job queued for execution.
        /// </summary>
        internal class QueuedJob
        {
            public string JobId { get; set; } = string.Empty;
            public string JobType { get; set; } = string.Empty;
            public string SerializedArgs { get; set; } = string.Empty;
            public JobOptions Options { get; set; } = JobOptions.Default;
            public DateTimeOffset ScheduledFor { get; set; }
        }

        /// <summary>
        /// Represents statistics for a queue.
        /// </summary>
        internal class QueueStats
        {
            public string QueueName { get; set; } = string.Empty;
            public int CriticalCount { get; set; }
            public int HighCount { get; set; }
            public int NormalCount { get; set; }
            public int LowCount { get; set; }
            public int TotalCount { get; set; }
        }
    }
}

