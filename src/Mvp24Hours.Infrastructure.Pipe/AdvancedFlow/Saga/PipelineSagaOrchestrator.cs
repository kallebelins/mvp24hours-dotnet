//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga;

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
/// <remarks>
/// Creates a new saga orchestrator.
/// </remarks>
/// <param name="options">Saga execution options.</param>
/// <param name="stateStore">Optional state store for persistence.</param>
/// <param name="logger"></param>
public class PipelineSagaOrchestrator<TContext>(
    PipelineSagaOptions? options = null,
    IPipelineSagaStateStore<TContext>? stateStore = null,
    ILogger<PipelineSagaOrchestrator<TContext>>? logger = null)
{
    private readonly List<IPipelineSagaStep<TContext>> _steps = [];
    private readonly PipelineSagaOptions _options = options ?? new PipelineSagaOptions();
    private readonly IPipelineSagaStateStore<TContext>? _stateStore = stateStore;
    private readonly ILogger? _logger = logger;

    /// <summary>
    /// Gets the saga instance ID.
    /// </summary>
    public string SagaId { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Sets a specific saga ID (useful for resuming).
    /// </summary>
    public PipelineSagaOrchestrator<TContext> WithSagaId(string sagaId)
    {
        SagaId = sagaId;
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
        SagaState state = SagaState.Running;

        _logger?.LogDebug("PipelineSagaOrchestrator: Saga '{SagaId}' started with {StepCount} steps", SagaId, _steps.Count);

        using CancellationTokenSource? sagaCts = _options.SagaTimeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        sagaCts?.CancelAfter(_options.SagaTimeout!.Value);
        CancellationToken effectiveToken = sagaCts?.Token ?? cancellationToken;

        try
        {
            // Save initial state if persistence is enabled
            if (_options.EnableStatePersistence && _stateStore != null)
            {
                await _stateStore.SaveStateAsync(SagaId, new SagaPersistedState<TContext>
                {
                    SagaId = SagaId,
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

                IPipelineSagaStep<TContext> step = _steps[i];
                DateTime stepStartedAt = DateTime.UtcNow;
                var stepStopwatch = Stopwatch.StartNew();

                _logger?.LogDebug("PipelineSagaOrchestrator: Step '{StepId}' started for saga '{SagaId}'", step.StepId, SagaId);

                SagaStepResult result = await ExecuteStepWithRetryAsync(step, context, effectiveToken);

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

                    _logger?.LogDebug("PipelineSagaOrchestrator: Step '{StepId}' succeeded for saga '{SagaId}'", step.StepId, SagaId);

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

                    _logger?.LogWarning("PipelineSagaOrchestrator: Step '{StepId}' failed for saga '{SagaId}'. Error: {ErrorMessage}", step.StepId, SagaId, result.ErrorMessage);
                    break;
                }

                // Update persisted state
                if (_options.EnableStatePersistence && _stateStore != null)
                {
                    var completedStepIds = new List<string>();
                    foreach (IPipelineSagaStep<TContext> s in completedSteps)
                    {
                        completedStepIds.Add(s.StepId);
                    }
                    completedStepIds.Reverse();

                    await _stateStore.SaveStateAsync(SagaId, new SagaPersistedState<TContext>
                    {
                        SagaId = SagaId,
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
                _logger?.LogDebug("PipelineSagaOrchestrator: Compensation started for saga '{SagaId}'. Steps to compensate: {StepsToCompensate}", SagaId, completedSteps.Count);

                bool compensationSuccess = true;

                while (completedSteps.TryPop(out IPipelineSagaStep<TContext>? stepToCompensate))
                {
                    DateTime compStartedAt = DateTime.UtcNow;
                    var compStopwatch = Stopwatch.StartNew();

                    try
                    {
                        using CancellationTokenSource? compCts = _options.CompensationTimeout.HasValue
                            ? CancellationTokenSource.CreateLinkedTokenSource(effectiveToken)
                            : null;
                        compCts?.CancelAfter(_options.CompensationTimeout!.Value);

                        SagaStepResult compResult = await stepToCompensate.CompensateAsync(
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
                            _logger?.LogError("PipelineSagaOrchestrator: Compensation step '{StepId}' failed for saga '{SagaId}'. Error: {ErrorMessage}", stepToCompensate.StepId, SagaId, compResult.ErrorMessage);

                            if (!_options.ContinueCompensationOnError)
                            {
                                break;
                            }
                        }
                        else
                        {
                            _logger?.LogDebug("PipelineSagaOrchestrator: Compensation step '{StepId}' succeeded for saga '{SagaId}'", stepToCompensate.StepId, SagaId);
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

                        _logger?.LogError(ex, "PipelineSagaOrchestrator: Compensation step '{StepId}' threw an exception for saga '{SagaId}'", stepToCompensate.StepId, SagaId);

                        if (!_options.ContinueCompensationOnError)
                        {
                            break;
                        }
                    }
                }

                state = compensationSuccess ? SagaState.CompensationCompleted : SagaState.CompensationFailed;
                _logger?.LogDebug("PipelineSagaOrchestrator: Compensation finished for saga '{SagaId}'. Success: {CompensationSuccess}", SagaId, compensationSuccess);
            }

            // Mark as completed if all steps succeeded
            if (failedStepId == null)
            {
                state = SagaState.Completed;
            }

            stopwatch.Stop();
            _logger?.LogDebug("PipelineSagaOrchestrator: Saga '{SagaId}' finished. State: {SagaState}, Duration: {DurationMs}ms", SagaId, state, stopwatch.ElapsedMilliseconds);

            // Clean up persisted state on completion
            if (_options.EnableStatePersistence && _stateStore != null && state == SagaState.Completed)
            {
                await _stateStore.DeleteStateAsync(SagaId, effectiveToken);
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
            _logger?.LogWarning("PipelineSagaOrchestrator: Saga '{SagaId}' was cancelled", SagaId);

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
            _logger?.LogError(ex, "PipelineSagaOrchestrator: Saga '{SagaId}' encountered an unhandled exception", SagaId);

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
        int retryCount = 0;
        int maxRetries = step.MaxRetries;
        TimeSpan retryDelay = step.RetryDelay;

        while (true)
        {
            try
            {
                using CancellationTokenSource? stepCts = _options.StepTimeout.HasValue
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

                _logger?.LogWarning("PipelineSagaOrchestrator: Retrying step '{StepId}' for saga '{SagaId}'. Attempt: {AttemptCount}/{MaxRetries}", step.StepId, SagaId, retryCount, maxRetries);

                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }
}

/// <summary>
/// Lambda-based saga step implementation.
/// </summary>
internal sealed class LambdaSagaStep<TContext>(
    string stepId,
    Func<TContext, CancellationToken, Task<SagaStepResult>> execute,
    Func<TContext, CancellationToken, Task<SagaStepResult>>? compensate = null,
    string? name = null,
    int maxRetries = 0,
    TimeSpan? retryDelay = null) : IPipelineSagaStep<TContext>
{
    private readonly Func<TContext, CancellationToken, Task<SagaStepResult>> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<TContext, CancellationToken, Task<SagaStepResult>>? _compensate = compensate;

    public string StepId { get; } = stepId ?? throw new ArgumentNullException(nameof(stepId));
    public string? Name { get; } = name;
    public bool RequiresCompensation => _compensate != null;
    public int MaxRetries { get; } = maxRetries;
    public TimeSpan RetryDelay { get; } = retryDelay ?? TimeSpan.FromSeconds(1);

    public Task<SagaStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        return _execute(context, cancellationToken);
    }

    public Task<SagaStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken)
    {
        return _compensate?.Invoke(context, cancellationToken) ?? Task.FromResult(SagaStepResult.Success());
    }
}

/// <summary>
/// Base class for saga steps with boilerplate handled.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public abstract class PipelineSagaStepBase<TContext>(string stepId, string? name = null) : IPipelineSagaStep<TContext>
{
    public string StepId { get; } = stepId ?? throw new ArgumentNullException(nameof(stepId));
    public string? Name { get; } = name ?? stepId;
    public virtual bool RequiresCompensation => true;
    public virtual int MaxRetries => 0;
    public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(1);

    public abstract Task<SagaStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken);

    public virtual Task<SagaStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(SagaStepResult.Success());
    }
}

