//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Metrics
{
    /// <summary>
    /// Metrics for distributed lock operations.
    /// Tracks contention, wait times, and acquisition success rates.
    /// </summary>
    /// <remarks>
    /// This class provides thread-safe metrics collection for distributed locking.
    /// Metrics can be used for monitoring, alerting, and performance analysis.
    /// </remarks>
    public class DistributedLockMetrics
    {
        private readonly ConcurrentDictionary<string, LockMetrics> _resourceMetrics = new();

        /// <summary>
        /// Records a lock acquisition attempt.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="success">Whether the acquisition was successful.</param>
        /// <param name="waitTime">The time spent waiting for the lock.</param>
        public void RecordAcquisition(string resource, bool success, TimeSpan waitTime)
        {
            var metrics = _resourceMetrics.GetOrAdd(resource, _ => new LockMetrics());
            metrics.RecordAcquisition(success, waitTime);
        }

        /// <summary>
        /// Records a lock release.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        public void RecordRelease(string resource)
        {
            if (_resourceMetrics.TryGetValue(resource, out var metrics))
            {
                metrics.RecordRelease();
            }
        }

        /// <summary>
        /// Gets metrics for a specific resource.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <returns>Lock metrics for the resource, or null if no metrics exist.</returns>
        public LockResourceMetrics? GetMetrics(string resource)
        {
            if (_resourceMetrics.TryGetValue(resource, out var metrics))
            {
                return metrics.GetSnapshot();
            }

            return null;
        }

        /// <summary>
        /// Gets all resource metrics.
        /// </summary>
        /// <returns>Dictionary of resource metrics.</returns>
        public System.Collections.Generic.Dictionary<string, LockResourceMetrics> GetAllMetrics()
        {
            var result = new System.Collections.Generic.Dictionary<string, LockResourceMetrics>();

            foreach (var (resource, metrics) in _resourceMetrics)
            {
                result[resource] = metrics.GetSnapshot();
            }

            return result;
        }

        /// <summary>
        /// Resets metrics for a specific resource.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        public void ResetMetrics(string resource)
        {
            _resourceMetrics.TryRemove(resource, out _);
        }

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public void ResetAllMetrics()
        {
            _resourceMetrics.Clear();
        }

        /// <summary>
        /// Thread-safe metrics for a single resource.
        /// </summary>
        private class LockMetrics
        {
            private long _totalAttempts;
            private long _successfulAttempts;
            private long _failedAttempts;
            private long _timeouts;
            private long _totalWaitTimeTicks;
            private long _maxWaitTimeTicks;
            private long _releases;
            private readonly object _lock = new();

            public void RecordAcquisition(bool success, TimeSpan waitTime)
            {
                lock (_lock)
                {
                    _totalAttempts++;
                    var waitTicks = waitTime.Ticks;

                    if (success)
                    {
                        _successfulAttempts++;
                    }
                    else
                    {
                        _failedAttempts++;
                        // Assume timeout if wait time is close to timeout duration
                        // This is approximate, actual timeout detection should be done by caller
                        if (waitTicks > 0)
                        {
                            _timeouts++;
                        }
                    }

                    _totalWaitTimeTicks += waitTicks;
                    if (waitTicks > _maxWaitTimeTicks)
                    {
                        _maxWaitTimeTicks = waitTicks;
                    }
                }
            }

            public void RecordRelease()
            {
                lock (_lock)
                {
                    _releases++;
                }
            }

            public LockResourceMetrics GetSnapshot()
            {
                lock (_lock)
                {
                    var avgWaitTime = _totalAttempts > 0
                        ? TimeSpan.FromTicks(_totalWaitTimeTicks / _totalAttempts)
                        : TimeSpan.Zero;

                    var maxWaitTime = TimeSpan.FromTicks(_maxWaitTimeTicks);

                    var contentionRate = _totalAttempts > 0
                        ? (double)_failedAttempts / _totalAttempts
                        : 0.0;

                    var successRate = _totalAttempts > 0
                        ? (double)_successfulAttempts / _totalAttempts
                        : 0.0;

                    return new LockResourceMetrics
                    {
                        Resource = string.Empty, // Will be set by caller
                        TotalAttempts = _totalAttempts,
                        SuccessfulAttempts = _successfulAttempts,
                        FailedAttempts = _failedAttempts,
                        Timeouts = _timeouts,
                        Releases = _releases,
                        AverageWaitTime = avgWaitTime,
                        MaxWaitTime = maxWaitTime,
                        ContentionRate = contentionRate,
                        SuccessRate = successRate
                    };
                }
            }
        }
    }

    /// <summary>
    /// Snapshot of metrics for a specific resource.
    /// </summary>
    public class LockResourceMetrics
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string Resource { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of lock acquisition attempts.
        /// </summary>
        public long TotalAttempts { get; set; }

        /// <summary>
        /// Gets or sets the number of successful acquisitions.
        /// </summary>
        public long SuccessfulAttempts { get; set; }

        /// <summary>
        /// Gets or sets the number of failed acquisitions.
        /// </summary>
        public long FailedAttempts { get; set; }

        /// <summary>
        /// Gets or sets the number of timeout failures.
        /// </summary>
        public long Timeouts { get; set; }

        /// <summary>
        /// Gets or sets the number of lock releases.
        /// </summary>
        public long Releases { get; set; }

        /// <summary>
        /// Gets or sets the average wait time for lock acquisition.
        /// </summary>
        public TimeSpan AverageWaitTime { get; set; }

        /// <summary>
        /// Gets or sets the maximum wait time observed.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; }

        /// <summary>
        /// Gets or sets the contention rate (failed attempts / total attempts).
        /// </summary>
        public double ContentionRate { get; set; }

        /// <summary>
        /// Gets or sets the success rate (successful attempts / total attempts).
        /// </summary>
        public double SuccessRate { get; set; }
    }
}

