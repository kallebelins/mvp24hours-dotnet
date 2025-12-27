//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint
{
    /// <summary>
    /// A pipeline that supports checkpointing for long-running operations,
    /// enabling pause/resume functionality.
    /// </summary>
    /// <typeparam name="TState">The type of state being processed.</typeparam>
    /// <example>
    /// <code>
    /// var pipeline = new CheckpointablePipeline&lt;ProcessingState&gt;("data-processing", checkpointStore);
    /// 
    /// pipeline
    ///     .AddStep("load", state => LoadData(state))
    ///     .AddStep("transform", state => TransformData(state))
    ///     .AddStep("validate", state => ValidateData(state))
    ///     .AddStep("save", state => SaveData(state));
    /// 
    /// // Execute with checkpointing
    /// var result = await pipeline.ExecuteAsync(new ProcessingState { BatchId = "batch-001" });
    /// 
    /// // Later, resume from checkpoint if needed
    /// var resumeResult = await pipeline.ResumeAsync(executionId);
    /// </code>
    /// </example>
    public class CheckpointablePipeline<TState> where TState : class, new()
    {
        private readonly List<CheckpointableStep<TState>> _steps = [];
        private readonly string _pipelineName;
        private readonly ICheckpointStore _checkpointStore;
        private readonly CheckpointOptions _options;
        private readonly IStateSerializer _serializer;
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new checkpointable pipeline.
        /// </summary>
        /// <param name="pipelineName">Name of the pipeline.</param>
        /// <param name="checkpointStore">Store for persisting checkpoints.</param>
        /// <param name="options">Checkpoint options.</param>
        public CheckpointablePipeline(
            string pipelineName,
            ICheckpointStore checkpointStore,
            CheckpointOptions? options = null,
            ILogger<CheckpointablePipeline<TState>>? logger = null)
        {
            _pipelineName = pipelineName ?? throw new ArgumentNullException(nameof(pipelineName));
            _checkpointStore = checkpointStore ?? throw new ArgumentNullException(nameof(checkpointStore));
            _options = options ?? new CheckpointOptions();
            _serializer = _options.StateSerializer ?? new JsonStateSerializer();
            _logger = logger;
        }

        /// <summary>
        /// Adds a synchronous step to the pipeline.
        /// </summary>
        public CheckpointablePipeline<TState> AddStep(string stepId, Func<TState, IOperationResult<TState>> execute, string? name = null)
        {
            _steps.Add(new CheckpointableStep<TState>
            {
                StepId = stepId,
                Name = name ?? stepId,
                ExecuteSync = execute
            });
            return this;
        }

        /// <summary>
        /// Adds an async step to the pipeline.
        /// </summary>
        public CheckpointablePipeline<TState> AddStep(
            string stepId,
            Func<TState, CancellationToken, Task<IOperationResult<TState>>> executeAsync,
            string? name = null)
        {
            _steps.Add(new CheckpointableStep<TState>
            {
                StepId = stepId,
                Name = name ?? stepId,
                ExecuteAsync = executeAsync
            });
            return this;
        }

        /// <summary>
        /// Executes the pipeline from the beginning.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The execution result.</returns>
        public Task<CheckpointableResult<TState>> ExecuteAsync(TState initialState, CancellationToken cancellationToken = default)
        {
            var executionId = Guid.NewGuid().ToString("N");
            return ExecuteFromStepAsync(executionId, 0, initialState, cancellationToken);
        }

        /// <summary>
        /// Resumes execution from the last checkpoint.
        /// </summary>
        /// <param name="pipelineExecutionId">The pipeline execution ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The execution result.</returns>
        public async Task<CheckpointableResult<TState>> ResumeAsync(string pipelineExecutionId, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("CheckpointablePipeline: Resume started. ExecutionId: {ExecutionId}", pipelineExecutionId);

            var checkpoint = await _checkpointStore.GetLatestCheckpointAsync(pipelineExecutionId, cancellationToken);

            if (checkpoint == null)
            {
                return new CheckpointableResult<TState>
                {
                    IsSuccess = false,
                    ErrorMessage = $"No checkpoint found for execution ID '{pipelineExecutionId}'",
                    PipelineExecutionId = pipelineExecutionId
                };
            }

            if (checkpoint.Status != CheckpointStatus.Paused && checkpoint.Status != CheckpointStatus.Failed)
            {
                return new CheckpointableResult<TState>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Checkpoint is not in a resumable state. Current status: {checkpoint.Status}",
                    PipelineExecutionId = pipelineExecutionId
                };
            }

            // Check expiration
            if (_options.CheckpointExpiration.HasValue)
            {
                var age = DateTime.UtcNow - checkpoint.CreatedAt;
                if (age > _options.CheckpointExpiration.Value)
                {
                    await _checkpointStore.UpdateCheckpointStatusAsync(checkpoint.CheckpointId, CheckpointStatus.Expired, cancellationToken: cancellationToken);
                    return new CheckpointableResult<TState>
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Checkpoint has expired (age: {age.TotalHours:F1} hours)",
                        PipelineExecutionId = pipelineExecutionId
                    };
                }
            }

            // Deserialize state
            TState? state = null;
            if (!string.IsNullOrEmpty(checkpoint.StateData))
            {
                state = _serializer.Deserialize<TState>(checkpoint.StateData);
            }

            if (state == null)
            {
                return new CheckpointableResult<TState>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to deserialize checkpoint state",
                    PipelineExecutionId = pipelineExecutionId
                };
            }

            // Mark checkpoint as resumed
            await _checkpointStore.UpdateCheckpointStatusAsync(checkpoint.CheckpointId, CheckpointStatus.Resumed, cancellationToken: cancellationToken);

            // Resume from next step
            var resumeStepIndex = checkpoint.StepIndex + 1;
            return await ExecuteFromStepAsync(pipelineExecutionId, resumeStepIndex, state, cancellationToken);
        }

        /// <summary>
        /// Pauses the pipeline at the current step.
        /// </summary>
        public async Task<bool> PauseAsync(string pipelineExecutionId, CancellationToken cancellationToken = default)
        {
            var checkpoint = await _checkpointStore.GetLatestCheckpointAsync(pipelineExecutionId, cancellationToken);
            if (checkpoint != null && checkpoint.Status == CheckpointStatus.Running)
            {
                await _checkpointStore.UpdateCheckpointStatusAsync(checkpoint.CheckpointId, CheckpointStatus.Paused, cancellationToken: cancellationToken);
                return true;
            }
            return false;
        }

        private async Task<CheckpointableResult<TState>> ExecuteFromStepAsync(
            string executionId,
            int startIndex,
            TState state,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var stepResults = new List<CheckpointableStepResult>();
            string? failedStepId = null;
            string? errorMessage = null;
            var currentState = state;
            string? correlationId = null;

            _logger?.LogDebug("CheckpointablePipeline: Execute started. ExecutionId: {ExecutionId}, StartIndex: {StartIndex}, TotalSteps: {TotalSteps}",
                executionId, startIndex, _steps.Count);

            try
            {
                for (int i = startIndex; i < _steps.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var step = _steps[i];
                    var stepStartedAt = DateTime.UtcNow;
                    var stepStopwatch = Stopwatch.StartNew();

                    // Create checkpoint before step (if enabled)
                    if (_options.Enabled && (i == startIndex || i % _options.CheckpointInterval == 0))
                    {
                        var checkpointId = $"{executionId}-step-{i}";
                        await _checkpointStore.SaveCheckpointAsync(new PipelineCheckpoint
                        {
                            CheckpointId = checkpointId,
                            PipelineExecutionId = executionId,
                            PipelineName = _pipelineName,
                            StepIndex = i,
                            StepId = step.StepId,
                            StepName = step.Name,
                            StateData = _serializer.Serialize(currentState),
                            StateTypeName = typeof(TState).AssemblyQualifiedName,
                            CreatedAt = DateTime.UtcNow,
                            Status = CheckpointStatus.Running,
                            CorrelationId = correlationId
                        }, cancellationToken);
                    }

                    _logger?.LogDebug("CheckpointablePipeline: Step started. ExecutionId: {ExecutionId}, Step: {StepId}",
                        executionId, step.StepId);

                    IOperationResult<TState> result;
                    try
                    {
                        if (step.ExecuteAsync != null)
                        {
                            result = await step.ExecuteAsync(currentState, cancellationToken);
                        }
                        else if (step.ExecuteSync != null)
                        {
                            result = step.ExecuteSync(currentState);
                        }
                        else
                        {
                            result = OperationResult<TState>.Success(currentState);
                        }
                    }
                    catch (Exception ex)
                    {
                        result = OperationResult<TState>.Failure(ex);
                    }

                    stepStopwatch.Stop();

                    stepResults.Add(new CheckpointableStepResult
                    {
                        StepId = step.StepId,
                        StepName = step.Name,
                        IsSuccess = result.IsSuccess,
                        ErrorMessage = result.ErrorMessage,
                        StartedAt = stepStartedAt,
                        CompletedAt = DateTime.UtcNow,
                        Duration = stepStopwatch.Elapsed
                    });

                    if (result.IsSuccess)
                    {
                        currentState = result.Value ?? currentState;
                        _logger?.LogDebug("CheckpointablePipeline: Step succeeded. ExecutionId: {ExecutionId}, Step: {StepId}",
                            executionId, step.StepId);
                    }
                    else
                    {
                        failedStepId = step.StepId;
                        errorMessage = result.ErrorMessage;

                        _logger?.LogWarning("CheckpointablePipeline: Step failed. ExecutionId: {ExecutionId}, Step: {StepId}, Error: {ErrorMessage}",
                            executionId, step.StepId, errorMessage);

                        // Save error checkpoint
                        if (_options.Enabled && _options.CheckpointOnError)
                        {
                            var errorCheckpointId = $"{executionId}-error-{i}";
                            await _checkpointStore.SaveCheckpointAsync(new PipelineCheckpoint
                            {
                                CheckpointId = errorCheckpointId,
                                PipelineExecutionId = executionId,
                                PipelineName = _pipelineName,
                                StepIndex = i,
                                StepId = step.StepId,
                                StepName = step.Name,
                                StateData = _serializer.Serialize(currentState),
                                StateTypeName = typeof(TState).AssemblyQualifiedName,
                                CreatedAt = DateTime.UtcNow,
                                Status = CheckpointStatus.Failed,
                                ErrorMessage = errorMessage,
                                CorrelationId = correlationId
                            }, cancellationToken);
                        }

                        break;
                    }
                }

                stopwatch.Stop();

                var isSuccess = failedStepId == null;

                // Cleanup checkpoints on success
                if (isSuccess && _options.Enabled && _options.CleanupOnSuccess)
                {
                    await _checkpointStore.DeleteCheckpointsAsync(executionId, cancellationToken);
                }

                // Save final checkpoint
                if (_options.Enabled && !_options.CleanupOnSuccess)
                {
                    var finalCheckpointId = $"{executionId}-final";
                    await _checkpointStore.SaveCheckpointAsync(new PipelineCheckpoint
                    {
                        CheckpointId = finalCheckpointId,
                        PipelineExecutionId = executionId,
                        PipelineName = _pipelineName,
                        StepIndex = _steps.Count - 1,
                        StateData = _serializer.Serialize(currentState),
                        StateTypeName = typeof(TState).AssemblyQualifiedName,
                        CreatedAt = DateTime.UtcNow,
                        Status = isSuccess ? CheckpointStatus.Completed : CheckpointStatus.Failed,
                        ErrorMessage = errorMessage,
                        CorrelationId = correlationId
                    }, cancellationToken);
                }

                _logger?.LogDebug("CheckpointablePipeline: Execute completed. ExecutionId: {ExecutionId}, Success: {IsSuccess}, Duration: {Duration}ms",
                    executionId, isSuccess, stopwatch.ElapsedMilliseconds);

                return new CheckpointableResult<TState>
                {
                    IsSuccess = isSuccess,
                    State = currentState,
                    PipelineExecutionId = executionId,
                    StepResults = stepResults,
                    FailedStepId = failedStepId,
                    ErrorMessage = errorMessage,
                    TotalDuration = stopwatch.Elapsed
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();

                // Save paused checkpoint
                if (_options.Enabled)
                {
                    var pausedCheckpointId = $"{executionId}-paused";
                    await _checkpointStore.SaveCheckpointAsync(new PipelineCheckpoint
                    {
                        CheckpointId = pausedCheckpointId,
                        PipelineExecutionId = executionId,
                        PipelineName = _pipelineName,
                        StepIndex = stepResults.Count - 1,
                        StateData = _serializer.Serialize(currentState),
                        StateTypeName = typeof(TState).AssemblyQualifiedName,
                        CreatedAt = DateTime.UtcNow,
                        Status = CheckpointStatus.Paused,
                        CorrelationId = correlationId
                    }, CancellationToken.None);
                }

                return new CheckpointableResult<TState>
                {
                    IsSuccess = false,
                    State = currentState,
                    PipelineExecutionId = executionId,
                    StepResults = stepResults,
                    ErrorMessage = "Pipeline was cancelled",
                    TotalDuration = stopwatch.Elapsed,
                    WasCancelled = true
                };
            }
        }
    }

    /// <summary>
    /// Result of a checkpointable pipeline execution.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    public sealed class CheckpointableResult<TState>
    {
        /// <summary>
        /// Whether the pipeline completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// The final state.
        /// </summary>
        public TState? State { get; init; }

        /// <summary>
        /// The pipeline execution ID.
        /// </summary>
        public string? PipelineExecutionId { get; init; }

        /// <summary>
        /// Results from each step.
        /// </summary>
        public IReadOnlyList<CheckpointableStepResult> StepResults { get; init; } = [];

        /// <summary>
        /// The step that failed, if any.
        /// </summary>
        public string? FailedStepId { get; init; }

        /// <summary>
        /// Error message if failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Total execution duration.
        /// </summary>
        public TimeSpan TotalDuration { get; init; }

        /// <summary>
        /// Whether the pipeline was cancelled.
        /// </summary>
        public bool WasCancelled { get; init; }
    }

    /// <summary>
    /// Result of a single step execution.
    /// </summary>
    public sealed class CheckpointableStepResult
    {
        public required string StepId { get; init; }
        public string? StepName { get; init; }
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime CompletedAt { get; init; }
        public TimeSpan Duration { get; init; }
    }

    /// <summary>
    /// Internal representation of a checkpointable step.
    /// </summary>
    internal sealed class CheckpointableStep<TState>
    {
        public required string StepId { get; init; }
        public string? Name { get; init; }
        public Func<TState, IOperationResult<TState>>? ExecuteSync { get; init; }
        public Func<TState, CancellationToken, Task<IOperationResult<TState>>>? ExecuteAsync { get; init; }
    }

    /// <summary>
    /// Default JSON state serializer.
    /// </summary>
    public sealed class JsonStateSerializer : IStateSerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonStateSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public string Serialize<T>(T state)
        {
            return JsonSerializer.Serialize(state, _options);
        }

        public T? Deserialize<T>(string data)
        {
            return JsonSerializer.Deserialize<T>(data, _options);
        }

        public object? Deserialize(string data, string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null) return null;
            return JsonSerializer.Deserialize(data, type, _options);
        }
    }
}

