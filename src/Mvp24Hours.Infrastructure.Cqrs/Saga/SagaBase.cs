//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Base class for implementing sagas with orchestration pattern.
/// </summary>
/// <typeparam name="TData">The type of data used by the saga.</typeparam>
/// <remarks>
/// <para>
/// <strong>Usage:</strong>
/// <list type="number">
/// <item>Inherit from this class</item>
/// <item>Configure steps in constructor using ConfigureSteps</item>
/// <item>Optionally override OnStepCompleted, OnSagaCompleted, etc.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderSaga : SagaBase&lt;OrderSagaData&gt;
/// {
///     public OrderSaga(IServiceProvider serviceProvider) : base(serviceProvider)
///     {
///         ConfigureSteps(steps =>
///         {
///             steps.Add&lt;ReserveStockStep&gt;();
///             steps.Add&lt;ProcessPaymentStep&gt;();
///             steps.Add&lt;ShipOrderStep&gt;();
///         });
///         
///         WithTimeout(TimeSpan.FromMinutes(5));
///         WithMaxRetries(3);
///     }
/// }
/// </code>
/// </example>
public abstract class SagaBase<TData> : ISaga<TData>, ISaga where TData : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly List<ISagaStep<TData>> _steps = [];
    private readonly Stack<ISagaStep<TData>> _executedSteps = new();

    /// <summary>
    /// Initializes a new instance of the saga.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    protected SagaBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());
    }

    #region Properties

    /// <inheritdoc />
    public Guid SagaId { get; private set; }

    /// <inheritdoc />
    public TData Data { get; private set; } = default!;

    /// <inheritdoc />
    public SagaStatus Status { get; private set; } = SagaStatus.NotStarted;

    /// <inheritdoc />
    public int CurrentStepIndex { get; private set; }

    /// <inheritdoc />
    public string? CurrentStepName { get; private set; }

    /// <inheritdoc />
    public DateTime? StartedAt { get; private set; }

    /// <inheritdoc />
    public DateTime? CompletedAt { get; private set; }

    /// <inheritdoc />
    public Exception? Error { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ISagaStep<TData>> Steps => _steps.AsReadOnly();

    /// <inheritdoc />
    public Type DataType => typeof(TData);

    /// <summary>
    /// Gets the timeout for the saga.
    /// </summary>
    public TimeSpan? Timeout { get; private set; }

    /// <summary>
    /// Gets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; private set; } = 3;

    /// <summary>
    /// Gets the current retry count.
    /// </summary>
    public int RetryCount { get; private set; }

    #endregion

    #region Configuration

    /// <summary>
    /// Configures the steps for this saga.
    /// </summary>
    /// <param name="configure">Action to configure steps.</param>
    protected void ConfigureSteps(Action<SagaStepBuilder<TData>> configure)
    {
        var builder = new SagaStepBuilder<TData>(_serviceProvider);
        configure(builder);
        _steps.AddRange(builder.Build());
    }

    /// <summary>
    /// Sets the timeout for the saga.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    protected void WithTimeout(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Sets the maximum number of retries for failed steps.
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    protected void WithMaxRetries(int maxRetries)
    {
        MaxRetries = maxRetries;
    }

    #endregion

    #region Execution

    /// <inheritdoc />
    public virtual async Task<SagaResult> StartAsync(TData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (Status != SagaStatus.NotStarted)
        {
            throw new SagaInvalidStateException(SagaId, Status, SagaStatus.NotStarted);
        }

        SagaId = Guid.NewGuid();
        Data = data;
        Status = SagaStatus.Running;
        StartedAt = DateTime.UtcNow;
        CurrentStepIndex = 0;

        _logger.LogInformation("Saga {SagaId} started", SagaId);

        try
        {
            await ExecuteStepsAsync(cancellationToken);

            Status = SagaStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Saga {SagaId} completed successfully", SagaId);
            await OnSagaCompletedAsync(cancellationToken);

            return SagaResult.Success(SagaId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Status = SagaStatus.Cancelled;
            _logger.LogWarning("Saga {SagaId} was cancelled", SagaId);
            return SagaResult.Cancelled(SagaId);
        }
        catch (Exception ex)
        {
            Error = ex;
            Status = SagaStatus.Failed;

            _logger.LogError(ex, "Saga {SagaId} failed at step {Step}", SagaId, CurrentStepName);

            await OnSagaFailedAsync(ex, cancellationToken);
            await CompensateAsync(cancellationToken);

            return Status == SagaStatus.Compensated
                ? SagaResult.Compensated(SagaId, ex.Message)
                : SagaResult.PartiallyCompensated(SagaId, ex.Message);
        }
    }

    private async Task ExecuteStepsAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = Timeout.HasValue
            ? new CancellationTokenSource(Timeout.Value)
            : new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();

        foreach (ISagaStep<TData>? step in orderedSteps)
        {
            if (linkedCts.Token.IsCancellationRequested)
            {
                if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new SagaTimeoutException(SagaId, Timeout!.Value, step.Name);
                }

                linkedCts.Token.ThrowIfCancellationRequested();
            }

            CurrentStepName = step.Name;
            _logger.LogDebug("Saga {SagaId}: Executing step {Step}", SagaId, step.Name);

            try
            {
                await ExecuteStepWithRetryAsync(step, linkedCts.Token);
                _executedSteps.Push(step);
                CurrentStepIndex++;

                await OnStepCompletedAsync(step, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SagaStepException(SagaId, step.Name, CurrentStepIndex, ex.Message, ex);
            }
        }
    }

    private async Task ExecuteStepWithRetryAsync(ISagaStep<TData> step, CancellationToken cancellationToken)
    {
        int attempts = 0;
        Exception? lastException = null;

        while (attempts <= MaxRetries)
        {
            try
            {
                await step.ExecuteAsync(Data, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempts < MaxRetries && ShouldRetry(ex))
            {
                lastException = ex;
                attempts++;
                RetryCount++;

                TimeSpan delay = CalculateRetryDelay(attempts);
                _logger.LogWarning(ex,
                    "Saga {SagaId}: Step {Step} failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    SagaId, step.Name, delay.TotalMilliseconds, attempts, MaxRetries);

                await OnStepRetryAsync(step, attempts, ex, cancellationToken);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new SagaMaxRetriesExceededException(SagaId, MaxRetries, step.Name, lastException);
    }

    /// <summary>
    /// Determines if the exception should trigger a retry.
    /// Override to customize retry behavior.
    /// </summary>
    protected virtual bool ShouldRetry(Exception exception)
    {
        // By default, retry on transient exceptions
        return exception is TimeoutException or
               System.Net.Http.HttpRequestException or
               System.IO.IOException;
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// Uses exponential backoff by default.
    /// </summary>
    protected virtual TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        // Exponential backoff: 100ms, 200ms, 400ms, 800ms, etc.
        return TimeSpan.FromMilliseconds(100 * Math.Pow(2, attemptNumber - 1));
    }

    #endregion

    #region Compensation

    /// <inheritdoc />
    public virtual async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not SagaStatus.Failed and not SagaStatus.Running)
        {
            throw new SagaInvalidStateException(SagaId, Status,
                "Saga can only be compensated when in Failed or Running state");
        }

        Status = SagaStatus.Compensating;
        _logger.LogInformation("Saga {SagaId}: Starting compensation", SagaId);

        var compensationErrors = new List<Exception>();
        var failedSteps = new List<string>();

        while (_executedSteps.TryPop(out ISagaStep<TData>? step))
        {
            if (!step.CanCompensate)
            {
                _logger.LogWarning("Saga {SagaId}: Step {Step} cannot be compensated", SagaId, step.Name);
                continue;
            }

            try
            {
                _logger.LogDebug("Saga {SagaId}: Compensating step {Step}", SagaId, step.Name);
                await step.CompensateAsync(Data, cancellationToken);
                await OnStepCompensatedAsync(step, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga {SagaId}: Compensation failed for step {Step}", SagaId, step.Name);
                compensationErrors.Add(ex);
                failedSteps.Add(step.Name);
            }
        }

        if (compensationErrors.Count > 0)
        {
            Status = SagaStatus.PartiallyCompensated;
            await OnCompensationFailedAsync(failedSteps, compensationErrors, cancellationToken);
        }
        else
        {
            Status = SagaStatus.Compensated;
            await OnSagaCompensatedAsync(cancellationToken);
        }

        CompletedAt = DateTime.UtcNow;
        _logger.LogInformation("Saga {SagaId}: Compensation completed with status {Status}", SagaId, Status);
    }

    #endregion

    #region Event Handling

    /// <inheritdoc />
    public virtual Task HandleEventAsync(IMediatorDomainEvent @event, CancellationToken cancellationToken = default)
    {
        // Override in derived classes to handle events for choreography-style sagas
        return Task.CompletedTask;
    }

    #endregion

    #region Resume

    /// <inheritdoc />
    public virtual async Task<SagaResult> ResumeAsync(SagaState<TData> state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Status is not SagaStatus.Running and not SagaStatus.Suspended)
        {
            throw new SagaInvalidStateException(state.SagaId, state.Status,
                "Saga can only be resumed when in Running or Suspended state");
        }

        SagaId = state.SagaId;
        Data = state.Data;
        Status = SagaStatus.Running;
        StartedAt = state.StartedAt;
        CurrentStepIndex = state.CurrentStepIndex;
        CurrentStepName = state.CurrentStepName;
        RetryCount = state.RetryCount;

        // Rebuild executed steps
        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();
        for (int i = 0; i < CurrentStepIndex && i < orderedSteps.Count; i++)
        {
            _executedSteps.Push(orderedSteps[i]);
        }

        _logger.LogInformation("Saga {SagaId} resumed at step {Step}", SagaId, CurrentStepName);

        try
        {
            // Continue from current step
            var remainingSteps = orderedSteps.Skip(CurrentStepIndex).ToList();

            using CancellationTokenSource timeoutCts = Timeout.HasValue
                ? new CancellationTokenSource(Timeout.Value)
                : new CancellationTokenSource();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            foreach (ISagaStep<TData>? step in remainingSteps)
            {
                if (linkedCts.Token.IsCancellationRequested)
                {
                    if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        throw new SagaTimeoutException(SagaId, Timeout!.Value, step.Name);
                    }

                    linkedCts.Token.ThrowIfCancellationRequested();
                }

                CurrentStepName = step.Name;
                await ExecuteStepWithRetryAsync(step, linkedCts.Token);
                _executedSteps.Push(step);
                CurrentStepIndex++;

                await OnStepCompletedAsync(step, cancellationToken);
            }

            Status = SagaStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Saga {SagaId} completed successfully after resume", SagaId);
            await OnSagaCompletedAsync(cancellationToken);

            return SagaResult.Success(SagaId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Status = SagaStatus.Cancelled;
            return SagaResult.Cancelled(SagaId);
        }
        catch (Exception ex)
        {
            Error = ex;
            Status = SagaStatus.Failed;

            await OnSagaFailedAsync(ex, cancellationToken);
            await CompensateAsync(cancellationToken);

            return Status == SagaStatus.Compensated
                ? SagaResult.Compensated(SagaId, ex.Message)
                : SagaResult.PartiallyCompensated(SagaId, ex.Message);
        }
    }

    #endregion

    #region Hooks

    /// <summary>
    /// Called when a step completes successfully.
    /// </summary>
    protected virtual Task OnStepCompletedAsync(ISagaStep<TData> step, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called before a step is retried.
    /// </summary>
    protected virtual Task OnStepRetryAsync(ISagaStep<TData> step, int attemptNumber, Exception error, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a step's compensation completes.
    /// </summary>
    protected virtual Task OnStepCompensatedAsync(ISagaStep<TData> step, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the saga completes successfully.
    /// </summary>
    protected virtual Task OnSagaCompletedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the saga fails (before compensation).
    /// </summary>
    protected virtual Task OnSagaFailedAsync(Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when all compensation steps complete successfully.
    /// </summary>
    protected virtual Task OnSagaCompensatedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when one or more compensation steps fail.
    /// </summary>
    protected virtual Task OnCompensationFailedAsync(IReadOnlyList<string> failedSteps, IReadOnlyList<Exception> errors, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region State

    /// <summary>
    /// Gets the current state of the saga for persistence.
    /// </summary>
    public SagaState<TData> GetState()
    {
        return new SagaState<TData>
        {
            SagaId = SagaId,
            SagaType = GetType().FullName ?? GetType().Name,
            Status = Status,
            CurrentStepIndex = CurrentStepIndex,
            CurrentStepName = CurrentStepName,
            Data = Data,
            StartedAt = StartedAt ?? DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = CompletedAt,
            Timeout = Timeout,
            ExecutedSteps = [.. _executedSteps.Select(s => s.Name).Reverse()],
            Errors = Error != null ? [Error.Message] : [],
            RetryCount = RetryCount,
            MaxRetries = MaxRetries
        };
    }

    #endregion
}

/// <summary>
/// Builder for configuring saga steps.
/// </summary>
/// <typeparam name="TData">The type of saga data.</typeparam>
public sealed class SagaStepBuilder<TData> where TData : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ISagaStep<TData>> _steps = [];

    internal SagaStepBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    /// <typeparam name="TStep">The step type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public SagaStepBuilder<TData> Add<TStep>() where TStep : ISagaStep<TData>
    {
        TStep step = ActivatorUtilities.CreateInstance<TStep>(_serviceProvider);
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a step instance to the saga.
    /// </summary>
    /// <param name="step">The step instance.</param>
    /// <returns>The builder for chaining.</returns>
    public SagaStepBuilder<TData> Add(ISagaStep<TData> step)
    {
        _steps.Add(step);
        return this;
    }

    internal IReadOnlyList<ISagaStep<TData>> Build()
    {
        return _steps.AsReadOnly();
    }
}

