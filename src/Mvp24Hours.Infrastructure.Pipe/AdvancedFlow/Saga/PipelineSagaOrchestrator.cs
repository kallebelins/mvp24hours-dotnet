//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga
{
    /// <summary>
    /// Orchestrates the execution of pipeline sagas with compensation support.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    /// <example>
    /// <code>
    /// var saga = new PipelineSagaOrchestrator&lt;OrderContext&gt;()
    ///     .AddStep(new ReserveInventoryStep())
    ///     .AddStep(new ProcessPaymentStep())
    ///     .AddStep(new CreateShipmentStep())
    ///     .AddStep(new SendConfirmationStep());
    /// 
    /// var result = await saga.ExecuteAsync(new OrderContext { OrderId = "123" });
    /// 
    /// if (!result.IsSuccess)
    /// {
    ///     Console.WriteLine($"Saga failed: {result.ErrorMessage}");
    ///     Console.WriteLine($"Compensated steps: {result.CompensationResults.Count}");
    /// }
    /// </code>
    /// </example>
    public class PipelineSagaOrchestrator<TContext>
    {
        private readonly List<IPipelineSagaStep<TContext>> _steps = [];
        private readonly PipelineSagaOptions _options;
        private readonly IPipelineSagaStateStore<TContext>? _stateStore;
        private readonly ILogger? _logger;
        private string _sagaId;

        /// <summary>
        /// Creates a new saga orchestrator.
        /// </summary>
        /// <param name="options">Saga execution options.</param>
        /// <param name="stateStore">Optional state store for persistence.</param>
        public PipelineSagaOrchestrator(
            PipelineSagaOptions? options = null,
            IPipelineSagaStateStore<TContext>? stateStore = null,
            ILogger<PipelineSagaOrchestrator<TContext>>? logger = null)
        {
            _options = options ?? new PipelineSagaOptions();
            _stateStore = stateStore;
            _logger = logger;
            _sagaId = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Gets the saga instance ID.
        /// </summary>
        public string SagaId => _sagaId;

        /// <summary>
        /// Sets a specific saga ID (useful for resuming).
        /// </summary>
        public PipelineSagaOrchestrator<TContext> WithSagaId(string sagaId)
        {
            _sagaId = sagaId;
            return this;
        }

        /// <summary>
        /// Adds a step to the saga.
        /// </summary>
        /// <param name="step">The step to add.</param>
        /// <returns>This orchestrator for chaining.</returns>
        public PipelineSagaOrchestrator<TContext> AddStep(IPipelineSagaStep<TContext> step)
        {
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        /// <summary>
        /// Adds a simple step using lambda functions.
        /// </summary>
        /// <param name="stepId">Unique step identifier.</param>
        /// <param name="execute">The execute function.</param>
        /// <param name="compensate">The compensation function.</param>
        /// <param name="name">Optional display name.</param>
        /// <returns>This orchestrator for chaining.</returns>
        public PipelineSagaOrchestrator<TContext> AddStep(
            string stepId,
            Func<TContext, CancellationToken, Task<SagaStepResult>> execute,
            Func<TContext, CancellationToken, Task<SagaStepResult>>? compensate = null,
            string? name = null)
        {
            return AddStep(new LambdaSagaStep<TContext>(stepId, execute, compensate, name));
        }

        /// <summary>
        /// Executes the saga.
        /// </summary>
        /// <param name="context">The initial context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saga execution result.</returns>
        public async Task<PipelineSagaResult<TContext>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var stepResults = new List<StepExecutionRecord>();
            var compensationResults = new List<StepExecutionRecord>();
            var completedSteps = new Stack<IPipelineSagaStep<TContext>>();
            string? failedStepId = null;
            string? errorMessage = null;
            var state = SagaState.Running;

            _logger?.LogDebug("PipelineSagaOrchestrator: Saga '{SagaId}' started with {StepCount} steps", _sagaId, _steps.Count);

            using var sagaCts = _options.SagaTimeout.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;
            sagaCts?.CancelAfter(_options.SagaTimeout!.Value);
            var effectiveToken = sagaCts?.Token ?? cancellationToken;

            try
            {
                // Save initial state if persistence is enabled
                if (_options.EnableStatePersistence && _stateStore != null)
                {
                    await _stateStore.SaveStateAsync(_sagaId, new SagaPersistedState<TContext>
                    {
                        SagaId = _sagaId,
                        State = SagaState.Running,
                        Context = context,
                        CurrentStepIndex = 0,
                        CompletedSteps = [],
                        CompensatedSteps = [],
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, effectiveToken);
                }

                // Execute steps
                for (int i = 0; i < _steps.Count; i++)
                {
                    effectiveToken.ThrowIfCancellationRequested();

                    var step = _steps[i];
                    var stepStartedAt = DateTime.UtcNow;
                    var stepStopwatch = Stopwatch.StartNew();

                    _logger?.LogDebug("PipelineSagaOrchestrator: Step '{StepId}' started for saga '{SagaId}'", step.StepId, _sagaId);

                    var result = await ExecuteStepWithRetryAsync(step, context, effectiveToken);

                    stepStopwatch.Stop();

                    stepResults.Add(new StepExecutionRecord
                    {
                        StepId = step.StepId,
                        StepName = step.Name,
                        IsSuccess = result.IsSuccess,
                        ErrorMessage = result.ErrorMessage,
                        StartedAt = stepStartedAt,
                        CompletedAt = DateTime.UtcNow,
                        Duration = stepStopwatch.Elapsed,
                        RetryCount = 0, // TODO: track retries
                        IsCompensation = false
                    });

                    if (result.IsSuccess)
                    {
                        if (step.RequiresCompensation)
                        {
                            completedSteps.Push(step);
                        }

                        _logger?.LogDebug("PipelineSagaOrchestrator: Step '{StepId}' succeeded for saga '{SagaId}'", step.StepId, _sagaId);

                        // Apply step delay if configured
                        if (_options.StepDelay.HasValue && i < _steps.Count - 1)
                        {
                            await Task.Delay(_options.StepDelay.Value, effectiveToken);
                        }
                    }
                    else
                    {
                        failedStepId = step.StepId;
                        errorMessage = result.ErrorMessage;
                        state = SagaState.Failed;

                        _logger?.LogWarning("PipelineSagaOrchestrator: Step '{StepId}' failed for saga '{SagaId}'. Error: {ErrorMessage}", step.StepId, _sagaId, result.ErrorMessage);
                        break;
                    }

                    // Update persisted state
                    if (_options.EnableStatePersistence && _stateStore != null)
                    {
                        var completedStepIds = new List<string>();
                        foreach (var s in completedSteps)
                        {
                            completedStepIds.Add(s.StepId);
                        }
                        completedStepIds.Reverse();

                        await _stateStore.SaveStateAsync(_sagaId, new SagaPersistedState<TContext>
                        {
                            SagaId = _sagaId,
                            State = SagaState.Running,
                            Context = context,
                            CurrentStepIndex = i + 1,
                            CompletedSteps = completedStepIds,
                            CompensatedSteps = [],
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }, effectiveToken);
                    }
                }

                // If saga failed and auto-compensate is enabled
                if (state == SagaState.Failed && _options.AutoCompensateOnFailure && completedSteps.Count > 0)
                {
                    state = SagaState.Compensating;
                    _logger?.LogDebug("PipelineSagaOrchestrator: Compensation started for saga '{SagaId}'. Steps to compensate: {StepsToCompensate}", _sagaId, completedSteps.Count);

                    var compensationSuccess = true;

                    while (completedSteps.TryPop(out var stepToCompensate))
                    {
                        var compStartedAt = DateTime.UtcNow;
                        var compStopwatch = Stopwatch.StartNew();

                        try
                        {
                            using var compCts = _options.CompensationTimeout.HasValue
                                ? CancellationTokenSource.CreateLinkedTokenSource(effectiveToken)
                                : null;
                            compCts?.CancelAfter(_options.CompensationTimeout!.Value);

                            var compResult = await stepToCompensate.CompensateAsync(
                                context,
                                compCts?.Token ?? effectiveToken);

                            compStopwatch.Stop();

                            compensationResults.Add(new StepExecutionRecord
                            {
                                StepId = stepToCompensate.StepId,
                                StepName = stepToCompensate.Name,
                                IsSuccess = compResult.IsSuccess,
                                ErrorMessage = compResult.ErrorMessage,
                                StartedAt = compStartedAt,
                                CompletedAt = DateTime.UtcNow,
                                Duration = compStopwatch.Elapsed,
                                RetryCount = 0,
                                IsCompensation = true
                            });

                            if (!compResult.IsSuccess)
                            {
                                compensationSuccess = false;
                                _logger?.LogError("PipelineSagaOrchestrator: Compensation step '{StepId}' failed for saga '{SagaId}'. Error: {ErrorMessage}", stepToCompensate.StepId, _sagaId, compResult.ErrorMessage);

                                if (!_options.ContinueCompensationOnError)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                _logger?.LogDebug("PipelineSagaOrchestrator: Compensation step '{StepId}' succeeded for saga '{SagaId}'", stepToCompensate.StepId, _sagaId);
                            }
                        }
                        catch (Exception ex)
                        {
                            compStopwatch.Stop();
                            compensationSuccess = false;

                            compensationResults.Add(new StepExecutionRecord
                            {
                                StepId = stepToCompensate.StepId,
                                StepName = stepToCompensate.Name,
                                IsSuccess = false,
                                ErrorMessage = ex.Message,
                                StartedAt = compStartedAt,
                                CompletedAt = DateTime.UtcNow,
                                Duration = compStopwatch.Elapsed,
                                RetryCount = 0,
                                IsCompensation = true
                            });

                            _logger?.LogError(ex, "PipelineSagaOrchestrator: Compensation step '{StepId}' threw an exception for saga '{SagaId}'", stepToCompensate.StepId, _sagaId);

                            if (!_options.ContinueCompensationOnError)
                            {
                                break;
                            }
                        }
                    }

                    state = compensationSuccess ? SagaState.CompensationCompleted : SagaState.CompensationFailed;
                    _logger?.LogDebug("PipelineSagaOrchestrator: Compensation finished for saga '{SagaId}'. Success: {CompensationSuccess}", _sagaId, compensationSuccess);
                }

                // Mark as completed if all steps succeeded
                if (failedStepId == null)
                {
                    state = SagaState.Completed;
                }

                stopwatch.Stop();
                _logger?.LogDebug("PipelineSagaOrchestrator: Saga '{SagaId}' finished. State: {SagaState}, Duration: {DurationMs}ms", _sagaId, state, stopwatch.ElapsedMilliseconds);

                // Clean up persisted state on completion
                if (_options.EnableStatePersistence && _stateStore != null && state == SagaState.Completed)
                {
                    await _stateStore.DeleteStateAsync(_sagaId, effectiveToken);
                }

                return new PipelineSagaResult<TContext>
                {
                    IsSuccess = state == SagaState.Completed,
                    Context = context,
                    StepResults = stepResults,
                    CompensationResults = compensationResults,
                    FailedStepId = failedStepId,
                    ErrorMessage = errorMessage,
                    TotalDuration = stopwatch.Elapsed,
                    State = state
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger?.LogWarning("PipelineSagaOrchestrator: Saga '{SagaId}' was cancelled", _sagaId);

                return new PipelineSagaResult<TContext>
                {
                    IsSuccess = false,
                    Context = context,
                    StepResults = stepResults,
                    CompensationResults = compensationResults,
                    FailedStepId = failedStepId,
                    ErrorMessage = "Saga was cancelled or timed out",
                    TotalDuration = stopwatch.Elapsed,
                    State = SagaState.Failed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex, "PipelineSagaOrchestrator: Saga '{SagaId}' encountered an unhandled exception", _sagaId);

                return new PipelineSagaResult<TContext>
                {
                    IsSuccess = false,
                    Context = context,
                    StepResults = stepResults,
                    CompensationResults = compensationResults,
                    FailedStepId = failedStepId,
                    ErrorMessage = ex.Message,
                    TotalDuration = stopwatch.Elapsed,
                    State = SagaState.Failed
                };
            }
        }

        private async Task<SagaStepResult> ExecuteStepWithRetryAsync(
            IPipelineSagaStep<TContext> step,
            TContext context,
            CancellationToken cancellationToken)
        {
            var retryCount = 0;
            var maxRetries = step.MaxRetries;
            var retryDelay = step.RetryDelay;

            while (true)
            {
                try
                {
                    using var stepCts = _options.StepTimeout.HasValue
                        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                        : null;
                    stepCts?.CancelAfter(_options.StepTimeout!.Value);

                    return await step.ExecuteAsync(context, stepCts?.Token ?? cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // Don't retry on explicit cancellation
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        return SagaStepResult.Failure(ex);
                    }

                    _logger?.LogWarning("PipelineSagaOrchestrator: Retrying step '{StepId}' for saga '{SagaId}'. Attempt: {AttemptCount}/{MaxRetries}", step.StepId, _sagaId, retryCount, maxRetries);

                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Lambda-based saga step implementation.
    /// </summary>
    internal sealed class LambdaSagaStep<TContext> : IPipelineSagaStep<TContext>
    {
        private readonly Func<TContext, CancellationToken, Task<SagaStepResult>> _execute;
        private readonly Func<TContext, CancellationToken, Task<SagaStepResult>>? _compensate;

        public LambdaSagaStep(
            string stepId,
            Func<TContext, CancellationToken, Task<SagaStepResult>> execute,
            Func<TContext, CancellationToken, Task<SagaStepResult>>? compensate = null,
            string? name = null,
            int maxRetries = 0,
            TimeSpan? retryDelay = null)
        {
            StepId = stepId ?? throw new ArgumentNullException(nameof(stepId));
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _compensate = compensate;
            Name = name;
            MaxRetries = maxRetries;
            RetryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
        }

        public string StepId { get; }
        public string? Name { get; }
        public bool RequiresCompensation => _compensate != null;
        public int MaxRetries { get; }
        public TimeSpan RetryDelay { get; }

        public Task<SagaStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken)
            => _execute(context, cancellationToken);

        public Task<SagaStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken)
            => _compensate?.Invoke(context, cancellationToken) ?? Task.FromResult(SagaStepResult.Success());
    }

    /// <summary>
    /// Base class for saga steps with boilerplate handled.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public abstract class PipelineSagaStepBase<TContext> : IPipelineSagaStep<TContext>
    {
        protected PipelineSagaStepBase(string stepId, string? name = null)
        {
            StepId = stepId ?? throw new ArgumentNullException(nameof(stepId));
            Name = name ?? stepId;
        }

        public string StepId { get; }
        public string? Name { get; }
        public virtual bool RequiresCompensation => true;
        public virtual int MaxRetries => 0;
        public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(1);

        public abstract Task<SagaStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken);

        public virtual Task<SagaStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken)
            => Task.FromResult(SagaStepResult.Success());
    }
}

