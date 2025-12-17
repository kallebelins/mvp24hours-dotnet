//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Default implementation of <see cref="IPipelineMetrics"/> providing thread-safe
    /// metrics collection for pipeline and operation execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides:
    /// <list type="bullet">
    /// <item>Thread-safe metrics collection using concurrent dictionaries</item>
    /// <item>Automatic percentile calculation for latency tracking</item>
    /// <item>Memory tracking when enabled</item>
    /// <item>Real-time success rate calculations</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class PipelineMetrics : IPipelineMetrics
    {
        #region [ Fields ]

        private readonly ConcurrentDictionary<string, PipelineExecutionMetrics> _pipelineMetrics = new();
        private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
        private readonly ConcurrentDictionary<string, PipelineExecutionContext> _activeExecutions = new();
        private readonly ConcurrentDictionary<string, List<double>> _operationDurations = new();

        private long _totalPipelineExecutions;
        private long _successfulPipelineExecutions;
        private long _failedPipelineExecutions;
        private long _totalOperationsExecuted;
        private long _successfulOperations;
        private long _failedOperations;
        private long _totalMemoryAllocated;

        private readonly object _percentileLock = new();
        private readonly int _maxDurationSamples;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new instance of PipelineMetrics.
        /// </summary>
        /// <param name="maxDurationSamples">Maximum number of duration samples to keep for percentile calculations (default: 1000).</param>
        public PipelineMetrics(int maxDurationSamples = 1000)
        {
            _maxDurationSamples = maxDurationSamples;
        }

        #endregion

        #region [ IPipelineMetrics Implementation ]

        /// <inheritdoc />
        public void RecordPipelineStart(string pipelineId, string pipelineName)
        {
            if (string.IsNullOrEmpty(pipelineId))
                throw new ArgumentException("Pipeline ID cannot be null or empty.", nameof(pipelineId));

            if (string.IsNullOrEmpty(pipelineName))
                throw new ArgumentException("Pipeline name cannot be null or empty.", nameof(pipelineName));

            var context = new PipelineExecutionContext
            {
                PipelineId = pipelineId,
                PipelineName = pipelineName,
                StartedAt = DateTime.UtcNow,
                Stopwatch = Stopwatch.StartNew(),
                StartMemory = GC.GetTotalMemory(false)
            };

            _activeExecutions[pipelineId] = context;

            var metrics = _pipelineMetrics.GetOrAdd(pipelineName, _ => new PipelineExecutionMetrics { PipelineName = pipelineName });
            lock (metrics)
            {
                metrics.ActiveExecutions++;
            }
        }

        /// <inheritdoc />
        public void RecordPipelineEnd(string pipelineId, bool success)
        {
            if (string.IsNullOrEmpty(pipelineId))
                throw new ArgumentException("Pipeline ID cannot be null or empty.", nameof(pipelineId));

            if (!_activeExecutions.TryRemove(pipelineId, out var context))
                return;

            context.Stopwatch.Stop();
            var duration = context.Stopwatch.Elapsed.TotalMilliseconds;
            var memoryDelta = GC.GetTotalMemory(false) - context.StartMemory;

            Interlocked.Increment(ref _totalPipelineExecutions);

            if (success)
            {
                Interlocked.Increment(ref _successfulPipelineExecutions);
            }
            else
            {
                Interlocked.Increment(ref _failedPipelineExecutions);
            }

            if (memoryDelta > 0)
            {
                Interlocked.Add(ref _totalMemoryAllocated, memoryDelta);
            }

            var metrics = _pipelineMetrics.GetOrAdd(context.PipelineName, _ => new PipelineExecutionMetrics { PipelineName = context.PipelineName });

            lock (metrics)
            {
                metrics.TotalExecutions++;
                metrics.TotalDurationMs += duration;
                metrics.LastExecutionAt = DateTime.UtcNow;

                if (duration < metrics.MinDurationMs)
                    metrics.MinDurationMs = duration;

                if (duration > metrics.MaxDurationMs)
                    metrics.MaxDurationMs = duration;

                if (success)
                    metrics.SuccessfulExecutions++;
                else
                    metrics.FailedExecutions++;

                if (memoryDelta > 0)
                    metrics.TotalMemoryAllocated += memoryDelta;

                if (metrics.ActiveExecutions > 0)
                    metrics.ActiveExecutions--;
            }
        }

        /// <inheritdoc />
        public void RecordOperationStart(string pipelineId, string operationName, int operationIndex)
        {
            if (string.IsNullOrEmpty(pipelineId))
                throw new ArgumentException("Pipeline ID cannot be null or empty.", nameof(pipelineId));

            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));

            if (_activeExecutions.TryGetValue(pipelineId, out var context))
            {
                var opContext = new OperationExecutionContext
                {
                    OperationName = operationName,
                    OperationIndex = operationIndex,
                    StartedAt = DateTime.UtcNow,
                    Stopwatch = Stopwatch.StartNew(),
                    StartMemory = GC.GetTotalMemory(false)
                };

                context.CurrentOperation = opContext;
            }
        }

        /// <inheritdoc />
        public void RecordOperationEnd(string pipelineId, string operationName, TimeSpan duration, bool success, long? memoryDelta = null)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));

            Interlocked.Increment(ref _totalOperationsExecuted);

            if (success)
            {
                Interlocked.Increment(ref _successfulOperations);
            }
            else
            {
                Interlocked.Increment(ref _failedOperations);
            }

            var durationMs = duration.TotalMilliseconds;

            var metrics = _operationMetrics.GetOrAdd(operationName, _ => new OperationMetrics { OperationName = operationName });

            lock (metrics)
            {
                metrics.TotalExecutions++;
                metrics.TotalDurationMs += durationMs;
                metrics.LastExecutionAt = DateTime.UtcNow;

                if (durationMs < metrics.MinDurationMs)
                    metrics.MinDurationMs = durationMs;

                if (durationMs > metrics.MaxDurationMs)
                    metrics.MaxDurationMs = durationMs;

                if (success)
                    metrics.SuccessfulExecutions++;
                else
                    metrics.FailedExecutions++;

                if (memoryDelta.HasValue && memoryDelta.Value > 0)
                    metrics.TotalMemoryDelta += memoryDelta.Value;
            }

            // Track duration samples for percentile calculations
            TrackDurationSample(operationName, durationMs);

            // Clear current operation from pipeline context
            if (_activeExecutions.TryGetValue(pipelineId, out var context))
            {
                context.CurrentOperation = null;
            }
        }

        /// <inheritdoc />
        public void RecordOperationFailure(string pipelineId, string operationName, Exception exception)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));

            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var metrics = _operationMetrics.GetOrAdd(operationName, _ => new OperationMetrics { OperationName = operationName });

            lock (metrics)
            {
                metrics.LastExceptionType = exception.GetType().FullName;
                metrics.LastExceptionMessage = exception.Message;
            }
        }

        /// <inheritdoc />
        public PipelineMetricsSnapshot GetSnapshot()
        {
            // Calculate percentiles for all operations
            foreach (var kvp in _operationMetrics)
            {
                CalculatePercentiles(kvp.Key, kvp.Value);
            }

            var totalPipeline = Interlocked.Read(ref _totalPipelineExecutions);
            var successPipeline = Interlocked.Read(ref _successfulPipelineExecutions);
            var totalOps = Interlocked.Read(ref _totalOperationsExecuted);
            var successOps = Interlocked.Read(ref _successfulOperations);

            return new PipelineMetricsSnapshot
            {
                CapturedAt = DateTime.UtcNow,
                TotalPipelineExecutions = totalPipeline,
                SuccessfulPipelineExecutions = successPipeline,
                FailedPipelineExecutions = Interlocked.Read(ref _failedPipelineExecutions),
                TotalOperationsExecuted = totalOps,
                AveragePipelineDurationMs = CalculateAveragePipelineDuration(),
                AverageOperationDurationMs = CalculateAverageOperationDuration(),
                PipelineSuccessRate = totalPipeline > 0 ? (double)successPipeline / totalPipeline : 0,
                OperationSuccessRate = totalOps > 0 ? (double)successOps / totalOps : 0,
                TotalMemoryAllocated = Interlocked.Read(ref _totalMemoryAllocated),
                PipelineMetrics = _pipelineMetrics.ToDictionary(kvp => kvp.Key, kvp => ClonePipelineMetrics(kvp.Value)),
                OperationMetrics = _operationMetrics.ToDictionary(kvp => kvp.Key, kvp => CloneOperationMetrics(kvp.Value))
            };
        }

        /// <inheritdoc />
        public PipelineExecutionMetrics? GetPipelineMetrics(string pipelineName)
        {
            if (string.IsNullOrEmpty(pipelineName))
                return null;

            return _pipelineMetrics.TryGetValue(pipelineName, out var metrics)
                ? ClonePipelineMetrics(metrics)
                : null;
        }

        /// <inheritdoc />
        public OperationMetrics? GetOperationMetrics(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
                return null;

            if (!_operationMetrics.TryGetValue(operationName, out var metrics))
                return null;

            var clone = CloneOperationMetrics(metrics);
            CalculatePercentiles(operationName, clone);
            return clone;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _pipelineMetrics.Clear();
            _operationMetrics.Clear();
            _activeExecutions.Clear();
            _operationDurations.Clear();

            Interlocked.Exchange(ref _totalPipelineExecutions, 0);
            Interlocked.Exchange(ref _successfulPipelineExecutions, 0);
            Interlocked.Exchange(ref _failedPipelineExecutions, 0);
            Interlocked.Exchange(ref _totalOperationsExecuted, 0);
            Interlocked.Exchange(ref _successfulOperations, 0);
            Interlocked.Exchange(ref _failedOperations, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);
        }

        #endregion

        #region [ Private Methods ]

        private void TrackDurationSample(string operationName, double durationMs)
        {
            lock (_percentileLock)
            {
                if (!_operationDurations.TryGetValue(operationName, out var samples))
                {
                    samples = new List<double>();
                    _operationDurations[operationName] = samples;
                }

                samples.Add(durationMs);

                // Keep only the most recent samples
                if (samples.Count > _maxDurationSamples)
                {
                    samples.RemoveAt(0);
                }
            }
        }

        private void CalculatePercentiles(string operationName, OperationMetrics metrics)
        {
            lock (_percentileLock)
            {
                if (!_operationDurations.TryGetValue(operationName, out var samples) || samples.Count == 0)
                    return;

                var sorted = samples.OrderBy(x => x).ToList();
                metrics.P95DurationMs = GetPercentile(sorted, 0.95);
                metrics.P99DurationMs = GetPercentile(sorted, 0.99);
            }
        }

        private static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0)
                return 0;

            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }

        private double CalculateAveragePipelineDuration()
        {
            var total = 0.0;
            var count = 0L;

            foreach (var metrics in _pipelineMetrics.Values)
            {
                total += metrics.TotalDurationMs;
                count += metrics.TotalExecutions;
            }

            return count > 0 ? total / count : 0;
        }

        private double CalculateAverageOperationDuration()
        {
            var total = 0.0;
            var count = 0L;

            foreach (var metrics in _operationMetrics.Values)
            {
                total += metrics.TotalDurationMs;
                count += metrics.TotalExecutions;
            }

            return count > 0 ? total / count : 0;
        }

        private static PipelineExecutionMetrics ClonePipelineMetrics(PipelineExecutionMetrics source)
        {
            lock (source)
            {
                return new PipelineExecutionMetrics
                {
                    PipelineName = source.PipelineName,
                    TotalExecutions = source.TotalExecutions,
                    SuccessfulExecutions = source.SuccessfulExecutions,
                    FailedExecutions = source.FailedExecutions,
                    TotalDurationMs = source.TotalDurationMs,
                    MinDurationMs = source.MinDurationMs,
                    MaxDurationMs = source.MaxDurationMs,
                    LastExecutionAt = source.LastExecutionAt,
                    TotalMemoryAllocated = source.TotalMemoryAllocated,
                    ActiveExecutions = source.ActiveExecutions
                };
            }
        }

        private static OperationMetrics CloneOperationMetrics(OperationMetrics source)
        {
            lock (source)
            {
                return new OperationMetrics
                {
                    OperationName = source.OperationName,
                    TotalExecutions = source.TotalExecutions,
                    SuccessfulExecutions = source.SuccessfulExecutions,
                    FailedExecutions = source.FailedExecutions,
                    TotalDurationMs = source.TotalDurationMs,
                    MinDurationMs = source.MinDurationMs,
                    MaxDurationMs = source.MaxDurationMs,
                    LastExecutionAt = source.LastExecutionAt,
                    TotalMemoryDelta = source.TotalMemoryDelta,
                    LastExceptionType = source.LastExceptionType,
                    LastExceptionMessage = source.LastExceptionMessage,
                    P95DurationMs = source.P95DurationMs,
                    P99DurationMs = source.P99DurationMs
                };
            }
        }

        #endregion

        #region [ Nested Types ]

        private sealed class PipelineExecutionContext
        {
            public required string PipelineId { get; init; }
            public required string PipelineName { get; init; }
            public DateTime StartedAt { get; init; }
            public required Stopwatch Stopwatch { get; init; }
            public long StartMemory { get; init; }
            public OperationExecutionContext? CurrentOperation { get; set; }
        }

        private sealed class OperationExecutionContext
        {
            public required string OperationName { get; init; }
            public int OperationIndex { get; init; }
            public DateTime StartedAt { get; init; }
            public required Stopwatch Stopwatch { get; init; }
            public long StartMemory { get; init; }
        }

        #endregion
    }
}

