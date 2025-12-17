//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga
{
    /// <summary>
    /// Represents a step in a pipeline saga with compensation support.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public interface IPipelineSagaStep<TContext>
    {
        /// <summary>
        /// Unique identifier for this step.
        /// </summary>
        string StepId { get; }

        /// <summary>
        /// Display name for this step.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Executes the step's forward action.
        /// </summary>
        /// <param name="context">The saga context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The step execution result.</returns>
        Task<SagaStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the step's compensation (rollback) action.
        /// </summary>
        /// <param name="context">The saga context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The compensation result.</returns>
        Task<SagaStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this step requires compensation on saga failure.
        /// </summary>
        bool RequiresCompensation { get; }

        /// <summary>
        /// Maximum number of retries for this step.
        /// </summary>
        int MaxRetries { get; }

        /// <summary>
        /// Delay between retries.
        /// </summary>
        TimeSpan RetryDelay { get; }
    }

    /// <summary>
    /// Result of a saga step execution.
    /// </summary>
    public sealed class SagaStepResult
    {
        /// <summary>
        /// Whether the step succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error message if the step failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Exception that caused the failure, if any.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Additional data from the step execution.
        /// </summary>
        public object? Data { get; init; }

        /// <summary>
        /// Whether to skip compensation for this step even on failure.
        /// </summary>
        public bool SkipCompensation { get; init; }

        /// <summary>
        /// Creates a successful step result.
        /// </summary>
        public static SagaStepResult Success(object? data = null) => new()
        {
            IsSuccess = true,
            Data = data
        };

        /// <summary>
        /// Creates a failed step result.
        /// </summary>
        public static SagaStepResult Failure(string errorMessage, Exception? exception = null, bool skipCompensation = false) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            SkipCompensation = skipCompensation
        };

        /// <summary>
        /// Creates a failed step result from an exception.
        /// </summary>
        public static SagaStepResult Failure(Exception exception, bool skipCompensation = false) => new()
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            Exception = exception,
            SkipCompensation = skipCompensation
        };
    }

    /// <summary>
    /// Configuration options for pipeline saga execution.
    /// </summary>
    public sealed class PipelineSagaOptions
    {
        /// <summary>
        /// Whether to automatically compensate on failure. Default: true.
        /// </summary>
        public bool AutoCompensateOnFailure { get; set; } = true;

        /// <summary>
        /// Maximum time to wait for the entire saga. Null means no timeout.
        /// </summary>
        public TimeSpan? SagaTimeout { get; set; }

        /// <summary>
        /// Maximum time to wait for each step. Null means no timeout.
        /// </summary>
        public TimeSpan? StepTimeout { get; set; }

        /// <summary>
        /// Maximum time to wait for compensation. Null means no timeout.
        /// </summary>
        public TimeSpan? CompensationTimeout { get; set; }

        /// <summary>
        /// Whether to continue compensation even if a compensation step fails.
        /// </summary>
        public bool ContinueCompensationOnError { get; set; } = true;

        /// <summary>
        /// Whether to persist saga state for recovery. Default: false.
        /// </summary>
        public bool EnableStatePersistence { get; set; }

        /// <summary>
        /// Delay between steps (for rate limiting, etc). Default: none.
        /// </summary>
        public TimeSpan? StepDelay { get; set; }
    }

    /// <summary>
    /// Result of a complete pipeline saga execution.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public sealed class PipelineSagaResult<TContext>
    {
        /// <summary>
        /// Whether the saga completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// The final context after saga execution.
        /// </summary>
        public TContext? Context { get; init; }

        /// <summary>
        /// Results from each step execution.
        /// </summary>
        public IReadOnlyList<StepExecutionRecord> StepResults { get; init; } = [];

        /// <summary>
        /// Steps that were compensated.
        /// </summary>
        public IReadOnlyList<StepExecutionRecord> CompensationResults { get; init; } = [];

        /// <summary>
        /// The step that caused the saga to fail, if any.
        /// </summary>
        public string? FailedStepId { get; init; }

        /// <summary>
        /// Overall error message.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Total saga execution time.
        /// </summary>
        public TimeSpan TotalDuration { get; init; }

        /// <summary>
        /// Current state of the saga.
        /// </summary>
        public SagaState State { get; init; }
    }

    /// <summary>
    /// Record of a single step execution.
    /// </summary>
    public sealed class StepExecutionRecord
    {
        /// <summary>
        /// ID of the step.
        /// </summary>
        public required string StepId { get; init; }

        /// <summary>
        /// Name of the step.
        /// </summary>
        public string? StepName { get; init; }

        /// <summary>
        /// Whether the step succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error message if failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// When the step started.
        /// </summary>
        public DateTime StartedAt { get; init; }

        /// <summary>
        /// When the step completed.
        /// </summary>
        public DateTime CompletedAt { get; init; }

        /// <summary>
        /// Duration of the step.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Number of retries attempted.
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// Whether this was a compensation execution.
        /// </summary>
        public bool IsCompensation { get; init; }
    }

    /// <summary>
    /// State of a saga.
    /// </summary>
    public enum SagaState
    {
        /// <summary>
        /// Saga has not started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Saga is currently executing.
        /// </summary>
        Running,

        /// <summary>
        /// Saga completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Saga failed and is compensating.
        /// </summary>
        Compensating,

        /// <summary>
        /// Saga failed and compensation completed.
        /// </summary>
        CompensationCompleted,

        /// <summary>
        /// Saga failed and compensation also failed.
        /// </summary>
        CompensationFailed,

        /// <summary>
        /// Saga failed without compensation.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Interface for saga state persistence.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public interface IPipelineSagaStateStore<TContext>
    {
        /// <summary>
        /// Saves the current saga state.
        /// </summary>
        Task SaveStateAsync(string sagaId, SagaPersistedState<TContext> state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the saga state.
        /// </summary>
        Task<SagaPersistedState<TContext>?> LoadStateAsync(string sagaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the saga state.
        /// </summary>
        Task DeleteStateAsync(string sagaId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Persisted state for saga recovery.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public sealed class SagaPersistedState<TContext>
    {
        /// <summary>
        /// Unique saga instance ID.
        /// </summary>
        public required string SagaId { get; init; }

        /// <summary>
        /// Current state of the saga.
        /// </summary>
        public SagaState State { get; init; }

        /// <summary>
        /// The saga context.
        /// </summary>
        public TContext? Context { get; init; }

        /// <summary>
        /// Index of the current step (or last completed step).
        /// </summary>
        public int CurrentStepIndex { get; init; }

        /// <summary>
        /// IDs of completed steps.
        /// </summary>
        public IReadOnlyList<string> CompletedSteps { get; init; } = [];

        /// <summary>
        /// IDs of compensated steps.
        /// </summary>
        public IReadOnlyList<string> CompensatedSteps { get; init; } = [];

        /// <summary>
        /// When the saga was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// When the saga was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; init; }
    }
}

