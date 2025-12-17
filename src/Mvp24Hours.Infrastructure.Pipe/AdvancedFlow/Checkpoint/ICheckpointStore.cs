//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint
{
    /// <summary>
    /// Represents a checkpoint in a long-running pipeline.
    /// </summary>
    public sealed class PipelineCheckpoint
    {
        /// <summary>
        /// Unique identifier for this checkpoint.
        /// </summary>
        public required string CheckpointId { get; init; }

        /// <summary>
        /// Identifier of the pipeline execution this checkpoint belongs to.
        /// </summary>
        public required string PipelineExecutionId { get; init; }

        /// <summary>
        /// Name of the pipeline.
        /// </summary>
        public string? PipelineName { get; init; }

        /// <summary>
        /// Index of the step that created this checkpoint.
        /// </summary>
        public int StepIndex { get; init; }

        /// <summary>
        /// ID of the step that created this checkpoint.
        /// </summary>
        public string? StepId { get; init; }

        /// <summary>
        /// Name of the step that created this checkpoint.
        /// </summary>
        public string? StepName { get; init; }

        /// <summary>
        /// Serialized state data at this checkpoint.
        /// </summary>
        public string? StateData { get; init; }

        /// <summary>
        /// Type name of the state for deserialization.
        /// </summary>
        public string? StateTypeName { get; init; }

        /// <summary>
        /// When the checkpoint was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Additional metadata associated with the checkpoint.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Metadata { get; init; }

        /// <summary>
        /// Status of the checkpoint.
        /// </summary>
        public CheckpointStatus Status { get; init; }

        /// <summary>
        /// Error message if the checkpoint represents a failure.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Correlation ID for tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Status of a checkpoint.
    /// </summary>
    public enum CheckpointStatus
    {
        /// <summary>
        /// Checkpoint was created successfully.
        /// </summary>
        Created,

        /// <summary>
        /// Pipeline is actively running from this checkpoint.
        /// </summary>
        Running,

        /// <summary>
        /// Pipeline was paused at this checkpoint.
        /// </summary>
        Paused,

        /// <summary>
        /// Pipeline completed successfully after this checkpoint.
        /// </summary>
        Completed,

        /// <summary>
        /// Pipeline failed after this checkpoint.
        /// </summary>
        Failed,

        /// <summary>
        /// Pipeline was resumed from this checkpoint.
        /// </summary>
        Resumed,

        /// <summary>
        /// Checkpoint has expired.
        /// </summary>
        Expired
    }

    /// <summary>
    /// Interface for storing and retrieving pipeline checkpoints.
    /// </summary>
    public interface ICheckpointStore
    {
        /// <summary>
        /// Saves a checkpoint.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveCheckpointAsync(PipelineCheckpoint checkpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a checkpoint by ID.
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The checkpoint, or null if not found.</returns>
        Task<PipelineCheckpoint?> GetCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest checkpoint for a pipeline execution.
        /// </summary>
        /// <param name="pipelineExecutionId">The pipeline execution ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The latest checkpoint, or null if none exist.</returns>
        Task<PipelineCheckpoint?> GetLatestCheckpointAsync(string pipelineExecutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all checkpoints for a pipeline execution.
        /// </summary>
        /// <param name="pipelineExecutionId">The pipeline execution ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>All checkpoints for the execution.</returns>
        Task<IReadOnlyList<PipelineCheckpoint>> GetCheckpointsAsync(string pipelineExecutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of a checkpoint.
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="errorMessage">Optional error message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateCheckpointStatusAsync(string checkpointId, CheckpointStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a checkpoint.
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all checkpoints for a pipeline execution.
        /// </summary>
        /// <param name="pipelineExecutionId">The pipeline execution ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteCheckpointsAsync(string pipelineExecutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets checkpoints that are in a resumable state (Paused or Failed).
        /// </summary>
        /// <param name="pipelineName">Optional filter by pipeline name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resumable checkpoints.</returns>
        Task<IReadOnlyList<PipelineCheckpoint>> GetResumableCheckpointsAsync(string? pipelineName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up expired checkpoints.
        /// </summary>
        /// <param name="expirationTime">Maximum age of checkpoints to keep.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of deleted checkpoints.</returns>
        Task<int> CleanupExpiredCheckpointsAsync(TimeSpan expirationTime, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for checkpoint behavior.
    /// </summary>
    public sealed class CheckpointOptions
    {
        /// <summary>
        /// Whether checkpointing is enabled. Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Interval between automatic checkpoints (by number of operations).
        /// </summary>
        public int CheckpointInterval { get; set; } = 1;

        /// <summary>
        /// Whether to checkpoint on errors. Default: true.
        /// </summary>
        public bool CheckpointOnError { get; set; } = true;

        /// <summary>
        /// Whether to automatically resume from the last checkpoint. Default: false.
        /// </summary>
        public bool AutoResume { get; set; }

        /// <summary>
        /// How long checkpoints are valid for resume. Null means no expiration.
        /// </summary>
        public TimeSpan? CheckpointExpiration { get; set; }

        /// <summary>
        /// Whether to delete checkpoints on successful completion. Default: true.
        /// </summary>
        public bool CleanupOnSuccess { get; set; } = true;

        /// <summary>
        /// State serializer to use. Default: JSON.
        /// </summary>
        public IStateSerializer? StateSerializer { get; set; }
    }

    /// <summary>
    /// Interface for serializing checkpoint state.
    /// </summary>
    public interface IStateSerializer
    {
        /// <summary>
        /// Serializes state to a string.
        /// </summary>
        string Serialize<T>(T state);

        /// <summary>
        /// Deserializes state from a string.
        /// </summary>
        T? Deserialize<T>(string data);

        /// <summary>
        /// Deserializes state from a string using a type name.
        /// </summary>
        object? Deserialize(string data, string typeName);
    }
}

