//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Provides detailed metrics for pipeline and operation execution tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides comprehensive metrics for monitoring pipeline performance:
    /// <list type="bullet">
    /// <item>Duration tracking per operation and total pipeline</item>
    /// <item>Memory allocation tracking</item>
    /// <item>Success/failure rate calculations</item>
    /// <item>Execution counts and timing statistics</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IPipelineMetrics
    {
        /// <summary>
        /// Records the start of a pipeline execution.
        /// </summary>
        /// <param name="pipelineId">Unique identifier for this pipeline execution.</param>
        /// <param name="pipelineName">Name of the pipeline.</param>
        void RecordPipelineStart(string pipelineId, string pipelineName);

        /// <summary>
        /// Records the completion of a pipeline execution.
        /// </summary>
        /// <param name="pipelineId">The pipeline execution identifier.</param>
        /// <param name="success">Whether the pipeline completed successfully.</param>
        void RecordPipelineEnd(string pipelineId, bool success);

        /// <summary>
        /// Records the start of an operation within a pipeline.
        /// </summary>
        /// <param name="pipelineId">The pipeline execution identifier.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="operationIndex">Index of the operation in the pipeline.</param>
        void RecordOperationStart(string pipelineId, string operationName, int operationIndex);

        /// <summary>
        /// Records the completion of an operation.
        /// </summary>
        /// <param name="pipelineId">The pipeline execution identifier.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="duration">Duration of the operation.</param>
        /// <param name="success">Whether the operation completed successfully.</param>
        /// <param name="memoryDelta">Optional memory allocation delta in bytes.</param>
        void RecordOperationEnd(string pipelineId, string operationName, TimeSpan duration, bool success, long? memoryDelta = null);

        /// <summary>
        /// Records an operation failure with exception details.
        /// </summary>
        /// <param name="pipelineId">The pipeline execution identifier.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="exception">The exception that occurred.</param>
        void RecordOperationFailure(string pipelineId, string operationName, Exception exception);

        /// <summary>
        /// Gets the current metrics snapshot for all pipelines.
        /// </summary>
        /// <returns>A metrics snapshot.</returns>
        PipelineMetricsSnapshot GetSnapshot();

        /// <summary>
        /// Gets the metrics for a specific pipeline.
        /// </summary>
        /// <param name="pipelineName">Name of the pipeline.</param>
        /// <returns>Pipeline-specific metrics, or null if not found.</returns>
        PipelineExecutionMetrics? GetPipelineMetrics(string pipelineName);

        /// <summary>
        /// Gets the metrics for a specific operation across all pipelines.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>Operation-specific metrics, or null if not found.</returns>
        OperationMetrics? GetOperationMetrics(string operationName);

        /// <summary>
        /// Resets all collected metrics.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Represents a complete snapshot of pipeline metrics at a point in time.
    /// </summary>
    public sealed class PipelineMetricsSnapshot
    {
        /// <summary>
        /// Gets or sets the timestamp when this snapshot was created.
        /// </summary>
        public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total number of pipeline executions.
        /// </summary>
        public long TotalPipelineExecutions { get; init; }

        /// <summary>
        /// Gets or sets the total number of successful pipeline executions.
        /// </summary>
        public long SuccessfulPipelineExecutions { get; init; }

        /// <summary>
        /// Gets or sets the total number of failed pipeline executions.
        /// </summary>
        public long FailedPipelineExecutions { get; init; }

        /// <summary>
        /// Gets or sets the total number of operations executed.
        /// </summary>
        public long TotalOperationsExecuted { get; init; }

        /// <summary>
        /// Gets or sets the average pipeline duration in milliseconds.
        /// </summary>
        public double AveragePipelineDurationMs { get; init; }

        /// <summary>
        /// Gets or sets the average operation duration in milliseconds.
        /// </summary>
        public double AverageOperationDurationMs { get; init; }

        /// <summary>
        /// Gets or sets the pipeline success rate (0-1).
        /// </summary>
        public double PipelineSuccessRate { get; init; }

        /// <summary>
        /// Gets or sets the operation success rate (0-1).
        /// </summary>
        public double OperationSuccessRate { get; init; }

        /// <summary>
        /// Gets or sets the total memory allocated in bytes (if tracked).
        /// </summary>
        public long TotalMemoryAllocated { get; init; }

        /// <summary>
        /// Gets or sets the metrics per pipeline.
        /// </summary>
        public IReadOnlyDictionary<string, PipelineExecutionMetrics> PipelineMetrics { get; init; } = new Dictionary<string, PipelineExecutionMetrics>();

        /// <summary>
        /// Gets or sets the metrics per operation.
        /// </summary>
        public IReadOnlyDictionary<string, OperationMetrics> OperationMetrics { get; init; } = new Dictionary<string, OperationMetrics>();
    }

    /// <summary>
    /// Represents execution metrics for a specific pipeline.
    /// </summary>
    public sealed class PipelineExecutionMetrics
    {
        /// <summary>
        /// Gets or sets the pipeline name.
        /// </summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public long TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public long SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public long FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the total duration in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution duration in milliseconds.
        /// </summary>
        public double MinDurationMs { get; set; } = double.MaxValue;

        /// <summary>
        /// Gets or sets the maximum execution duration in milliseconds.
        /// </summary>
        public double MaxDurationMs { get; set; }

        /// <summary>
        /// Gets the average execution duration in milliseconds.
        /// </summary>
        public double AverageDurationMs => TotalExecutions > 0 ? TotalDurationMs / TotalExecutions : 0;

        /// <summary>
        /// Gets the success rate (0-1).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;

        /// <summary>
        /// Gets or sets the last execution timestamp.
        /// </summary>
        public DateTime? LastExecutionAt { get; set; }

        /// <summary>
        /// Gets or sets the total memory allocated in bytes.
        /// </summary>
        public long TotalMemoryAllocated { get; set; }

        /// <summary>
        /// Gets or sets the currently active executions.
        /// </summary>
        public int ActiveExecutions { get; set; }
    }

    /// <summary>
    /// Represents execution metrics for a specific operation.
    /// </summary>
    public sealed class OperationMetrics
    {
        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string OperationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public long TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public long SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public long FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the total duration in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution duration in milliseconds.
        /// </summary>
        public double MinDurationMs { get; set; } = double.MaxValue;

        /// <summary>
        /// Gets or sets the maximum execution duration in milliseconds.
        /// </summary>
        public double MaxDurationMs { get; set; }

        /// <summary>
        /// Gets the average execution duration in milliseconds.
        /// </summary>
        public double AverageDurationMs => TotalExecutions > 0 ? TotalDurationMs / TotalExecutions : 0;

        /// <summary>
        /// Gets the success rate (0-1).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;

        /// <summary>
        /// Gets or sets the last execution timestamp.
        /// </summary>
        public DateTime? LastExecutionAt { get; set; }

        /// <summary>
        /// Gets or sets the total memory delta in bytes.
        /// </summary>
        public long TotalMemoryDelta { get; set; }

        /// <summary>
        /// Gets or sets the last exception type that occurred.
        /// </summary>
        public string? LastExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the last exception message.
        /// </summary>
        public string? LastExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile duration in milliseconds.
        /// </summary>
        public double P95DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the 99th percentile duration in milliseconds.
        /// </summary>
        public double P99DurationMs { get; set; }
    }
}

