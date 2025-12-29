//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for Pipeline operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// pipeline execution performance and health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>executions_total</c> - Counter for total pipeline executions</item>
/// <item><c>executions_failed_total</c> - Counter for failed executions</item>
/// <item><c>execution_duration_ms</c> - Histogram for execution duration</item>
/// <item><c>operations_total</c> - Counter for operation executions</item>
/// <item><c>operations_failed_total</c> - Counter for failed operations</item>
/// <item><c>operation_duration_ms</c> - Histogram for operation duration</item>
/// <item><c>active_count</c> - Gauge for active pipelines</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Inject via DI
/// public class MyPipeline
/// {
///     private readonly PipelineMetrics _metrics;
///     
///     public MyPipeline(PipelineMetrics metrics) => _metrics = metrics;
///     
///     public async Task ExecuteAsync()
///     {
///         using var scope = _metrics.BeginExecution("MyPipeline");
///         try
///         {
///             // Execute pipeline
///             scope.Complete();
///         }
///         catch
///         {
///             scope.Fail();
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
public sealed class PipelineMetrics
{
    private readonly Counter<long> _executionsTotal;
    private readonly Counter<long> _executionsFailedTotal;
    private readonly Histogram<double> _executionDuration;
    private readonly Counter<long> _operationsTotal;
    private readonly Counter<long> _operationsFailedTotal;
    private readonly Histogram<double> _operationDuration;
    private readonly UpDownCounter<int> _activeCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMetrics"/> class.
    /// </summary>
    public PipelineMetrics()
    {
        var meter = Mvp24HoursMeters.Pipe.Meter;

        _executionsTotal = meter.CreateCounter<long>(
            MetricNames.PipelineExecutionsTotal,
            unit: "{executions}",
            description: "Total number of pipeline executions");

        _executionsFailedTotal = meter.CreateCounter<long>(
            MetricNames.PipelineExecutionsFailedTotal,
            unit: "{executions}",
            description: "Total number of failed pipeline executions");

        _executionDuration = meter.CreateHistogram<double>(
            MetricNames.PipelineExecutionDuration,
            unit: "ms",
            description: "Duration of pipeline executions in milliseconds");

        _operationsTotal = meter.CreateCounter<long>(
            MetricNames.PipelineOperationsTotal,
            unit: "{operations}",
            description: "Total number of operation executions within pipelines");

        _operationsFailedTotal = meter.CreateCounter<long>(
            MetricNames.PipelineOperationsFailedTotal,
            unit: "{operations}",
            description: "Total number of failed operations within pipelines");

        _operationDuration = meter.CreateHistogram<double>(
            MetricNames.PipelineOperationDuration,
            unit: "ms",
            description: "Duration of individual operation executions in milliseconds");

        _activeCount = meter.CreateUpDownCounter<int>(
            MetricNames.PipelineActiveCount,
            unit: "{pipelines}",
            description: "Number of currently active pipelines");
    }

    /// <summary>
    /// Records the start of a pipeline execution.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <returns>A scope that should be disposed when execution completes.</returns>
    public PipelineExecutionScope BeginExecution(string pipelineName)
    {
        return new PipelineExecutionScope(this, pipelineName);
    }

    /// <summary>
    /// Records a pipeline execution.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="additionalTags">Additional tags to include.</param>
    public void RecordExecution(
        string pipelineName,
        double durationMs,
        bool success,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        var tags = CreateTags(pipelineName, success, additionalTags);

        _executionsTotal.Add(1, tags);

        if (!success)
        {
            _executionsFailedTotal.Add(1, tags);
        }

        _executionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records the start of an operation execution.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <returns>A scope that should be disposed when the operation completes.</returns>
    public OperationExecutionScope BeginOperation(string pipelineName, string operationName)
    {
        return new OperationExecutionScope(this, pipelineName, operationName);
    }

    /// <summary>
    /// Records an operation execution.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="additionalTags">Additional tags to include.</param>
    public void RecordOperation(
        string pipelineName,
        string operationName,
        double durationMs,
        bool success,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
    {
        var tags = CreateOperationTags(pipelineName, operationName, success, additionalTags);

        _operationsTotal.Add(1, tags);

        if (!success)
        {
            _operationsFailedTotal.Add(1, tags);
        }

        _operationDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Increments the active pipeline count.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    internal void IncrementActive(string pipelineName)
    {
        _activeCount.Add(1, new KeyValuePair<string, object?>(MetricTags.PipelineName, pipelineName));
    }

    /// <summary>
    /// Decrements the active pipeline count.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    internal void DecrementActive(string pipelineName)
    {
        _activeCount.Add(-1, new KeyValuePair<string, object?>(MetricTags.PipelineName, pipelineName));
    }

    private static TagList CreateTags(
        string pipelineName,
        bool success,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags)
    {
        var tags = new TagList
        {
            { MetricTags.PipelineName, pipelineName },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private static TagList CreateOperationTags(
        string pipelineName,
        string operationName,
        bool success,
        IEnumerable<KeyValuePair<string, object?>>? additionalTags)
    {
        var tags = new TagList
        {
            { MetricTags.PipelineName, pipelineName },
            { MetricTags.OperationName, operationName },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    /// <summary>
    /// Represents a scope for tracking pipeline execution duration.
    /// </summary>
    public struct PipelineExecutionScope : IDisposable
    {
        private readonly PipelineMetrics _metrics;
        private readonly string _pipelineName;
        private readonly long _startTimestamp;
        private readonly bool _trackActive;

        /// <summary>
        /// Gets or sets whether the execution succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal PipelineExecutionScope(PipelineMetrics metrics, string pipelineName, bool trackActive = true)
        {
            _metrics = metrics;
            _pipelineName = pipelineName;
            _startTimestamp = Stopwatch.GetTimestamp();
            _trackActive = trackActive;
            Succeeded = false;

            if (_trackActive)
            {
                _metrics.IncrementActive(_pipelineName);
            }
        }

        /// <summary>
        /// Marks the execution as completed successfully.
        /// </summary>
        public void Complete()
        {
            Succeeded = true;
        }

        /// <summary>
        /// Marks the execution as failed.
        /// </summary>
        public void Fail()
        {
            Succeeded = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordExecution(_pipelineName, elapsed.TotalMilliseconds, Succeeded);

            if (_trackActive)
            {
                _metrics.DecrementActive(_pipelineName);
            }
        }
    }

    /// <summary>
    /// Represents a scope for tracking operation execution duration.
    /// </summary>
    public struct OperationExecutionScope : IDisposable
    {
        private readonly PipelineMetrics _metrics;
        private readonly string _pipelineName;
        private readonly string _operationName;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal OperationExecutionScope(PipelineMetrics metrics, string pipelineName, string operationName)
        {
            _metrics = metrics;
            _pipelineName = pipelineName;
            _operationName = operationName;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the operation as completed successfully.
        /// </summary>
        public void Complete()
        {
            Succeeded = true;
        }

        /// <summary>
        /// Marks the operation as failed.
        /// </summary>
        public void Fail()
        {
            Succeeded = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordOperation(_pipelineName, _operationName, elapsed.TotalMilliseconds, Succeeded);
        }
    }
}

