//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

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
    private readonly List<ISagaStep<TData>> _steps = new();
    private readonly Stack<ISagaStep<TData>> _executedSteps = new();
    
    private TData _data = default!;
    private Guid _sagaId;
    private SagaStatus _status = SagaStatus.NotStarted;
    private int _currentStepIndex;
    private string? _currentStepName;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private Exception? _error;
    private TimeSpan? _timeout;
    private int _maxRetries = 3;
    private int _retryCount;

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
    public Guid SagaId => _sagaId;

    /// <inheritdoc />
    public TData Data => _data;

    /// <inheritdoc />
    public SagaStatus Status => _status;

    /// <inheritdoc />
    public int CurrentStepIndex => _currentStepIndex;

    /// <inheritdoc />
    public string? CurrentStepName => _currentStepName;

    /// <inheritdoc />
    public DateTime? StartedAt => _startedAt;

    /// <inheritdoc />
    public DateTime? CompletedAt => _completedAt;

    /// <inheritdoc />
    public Exception? Error => _error;

    /// <inheritdoc />
    public IReadOnlyList<ISagaStep<TData>> Steps => _steps.AsReadOnly();

    /// <inheritdoc />
    public Type DataType => typeof(TData);

    /// <summary>
    /// Gets the timeout for the saga.
    /// </summary>
    public TimeSpan? Timeout => _timeout;

    /// <summary>
    /// Gets the maximum number of retries.
    /// </summary>
    public int MaxRetries => _maxRetries;

    /// <summary>
    /// Gets the current retry count.
    /// </summary>
    public int RetryCount => _retryCount;

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
        _timeout = timeout;
    }

    /// <summary>
    /// Sets the maximum number of retries for failed steps.
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    protected void WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
    }

    #endregion

    #region Execution

    /// <inheritdoc />
    public virtual async Task<SagaResult> StartAsync(TData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (_status != SagaStatus.NotStarted)
        {
            throw new SagaInvalidStateException(_sagaId, _status, SagaStatus.NotStarted);
        }

        _sagaId = Guid.NewGuid();
        _data = data;
        _status = SagaStatus.Running;
        _startedAt = DateTime.UtcNow;
        _currentStepIndex = 0;

        _logger.LogInformation("Saga {SagaId} started", _sagaId);

        try
        {
            await ExecuteStepsAsync(cancellationToken);
            
            _status = SagaStatus.Completed;
            _completedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Saga {SagaId} completed successfully", _sagaId);
            await OnSagaCompletedAsync(cancellationToken);
            
            return SagaResult.Success(_sagaId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _status = SagaStatus.Cancelled;
            _logger.LogWarning("Saga {SagaId} was cancelled", _sagaId);
            return SagaResult.Cancelled(_sagaId);
        }
        catch (Exception ex)
        {
            _error = ex;
            _status = SagaStatus.Failed;
            
            _logger.LogError(ex, "Saga {SagaId} failed at step {Step}", _sagaId, _currentStepName);
            
            await OnSagaFailedAsync(ex, cancellationToken);
            await CompensateAsync(cancellationToken);
            
            return _status == SagaStatus.Compensated
                ? SagaResult.Compensated(_sagaId, ex.Message)
                : SagaResult.PartiallyCompensated(_sagaId, ex.Message);
        }
    }

    private async Task ExecuteStepsAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = _timeout.HasValue
            ? new CancellationTokenSource(_timeout.Value)
            : new CancellationTokenSource();
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();

        foreach (var step in orderedSteps)
        {
            if (linkedCts.Token.IsCancellationRequested)
            {
                if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new SagaTimeoutException(_sagaId, _timeout!.Value, step.Name);
                }
                
                linkedCts.Token.ThrowIfCancellationRequested();
            }

            _currentStepName = step.Name;
            _logger.LogDebug("Saga {SagaId}: Executing step {Step}", _sagaId, step.Name);

            try
            {
                await ExecuteStepWithRetryAsync(step, linkedCts.Token);
                _executedSteps.Push(step);
                _currentStepIndex++;
                
                await OnStepCompletedAsync(step, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SagaStepException(_sagaId, step.Name, _currentStepIndex, ex.Message, ex);
            }
        }
    }

    private async Task ExecuteStepWithRetryAsync(ISagaStep<TData> step, CancellationToken cancellationToken)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= _maxRetries)
        {
            try
            {
                await step.ExecuteAsync(_data, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempts < _maxRetries && ShouldRetry(ex))
            {
                lastException = ex;
                attempts++;
                _retryCount++;
                
                var delay = CalculateRetryDelay(attempts);
                _logger.LogWarning(ex, 
                    "Saga {SagaId}: Step {Step} failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    _sagaId, step.Name, delay.TotalMilliseconds, attempts, _maxRetries);
                
                await OnStepRetryAsync(step, attempts, ex, cancellationToken);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new SagaMaxRetriesExceededException(_sagaId, _maxRetries, step.Name, lastException);
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
        if (_status != SagaStatus.Failed && _status != SagaStatus.Running)
        {
            throw new SagaInvalidStateException(_sagaId, _status,
                "Saga can only be compensated when in Failed or Running state");
        }

        _status = SagaStatus.Compensating;
        _logger.LogInformation("Saga {SagaId}: Starting compensation", _sagaId);

        var compensationErrors = new List<Exception>();
        var failedSteps = new List<string>();

        while (_executedSteps.TryPop(out var step))
        {
            if (!step.CanCompensate)
            {
                _logger.LogWarning("Saga {SagaId}: Step {Step} cannot be compensated", _sagaId, step.Name);
                continue;
            }

            try
            {
                _logger.LogDebug("Saga {SagaId}: Compensating step {Step}", _sagaId, step.Name);
                await step.CompensateAsync(_data, cancellationToken);
                await OnStepCompensatedAsync(step, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga {SagaId}: Compensation failed for step {Step}", _sagaId, step.Name);
                compensationErrors.Add(ex);
                failedSteps.Add(step.Name);
            }
        }

        if (compensationErrors.Count > 0)
        {
            _status = SagaStatus.PartiallyCompensated;
            await OnCompensationFailedAsync(failedSteps, compensationErrors, cancellationToken);
        }
        else
        {
            _status = SagaStatus.Compensated;
            await OnSagaCompensatedAsync(cancellationToken);
        }

        _completedAt = DateTime.UtcNow;
        _logger.LogInformation("Saga {SagaId}: Compensation completed with status {Status}", _sagaId, _status);
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

        if (state.Status != SagaStatus.Running && state.Status != SagaStatus.Suspended)
        {
            throw new SagaInvalidStateException(state.SagaId, state.Status,
                "Saga can only be resumed when in Running or Suspended state");
        }

        _sagaId = state.SagaId;
        _data = state.Data;
        _status = SagaStatus.Running;
        _startedAt = state.StartedAt;
        _currentStepIndex = state.CurrentStepIndex;
        _currentStepName = state.CurrentStepName;
        _retryCount = state.RetryCount;

        // Rebuild executed steps
        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();
        for (var i = 0; i < _currentStepIndex && i < orderedSteps.Count; i++)
        {
            _executedSteps.Push(orderedSteps[i]);
        }

        _logger.LogInformation("Saga {SagaId} resumed at step {Step}", _sagaId, _currentStepName);

        try
        {
            // Continue from current step
            var remainingSteps = orderedSteps.Skip(_currentStepIndex).ToList();
            
            using var timeoutCts = _timeout.HasValue
                ? new CancellationTokenSource(_timeout.Value)
                : new CancellationTokenSource();
            
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            foreach (var step in remainingSteps)
            {
                if (linkedCts.Token.IsCancellationRequested)
                {
                    if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        throw new SagaTimeoutException(_sagaId, _timeout!.Value, step.Name);
                    }
                    
                    linkedCts.Token.ThrowIfCancellationRequested();
                }

                _currentStepName = step.Name;
                await ExecuteStepWithRetryAsync(step, linkedCts.Token);
                _executedSteps.Push(step);
                _currentStepIndex++;
                
                await OnStepCompletedAsync(step, cancellationToken);
            }

            _status = SagaStatus.Completed;
            _completedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Saga {SagaId} completed successfully after resume", _sagaId);
            await OnSagaCompletedAsync(cancellationToken);
            
            return SagaResult.Success(_sagaId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _status = SagaStatus.Cancelled;
            return SagaResult.Cancelled(_sagaId);
        }
        catch (Exception ex)
        {
            _error = ex;
            _status = SagaStatus.Failed;
            
            await OnSagaFailedAsync(ex, cancellationToken);
            await CompensateAsync(cancellationToken);
            
            return _status == SagaStatus.Compensated
                ? SagaResult.Compensated(_sagaId, ex.Message)
                : SagaResult.PartiallyCompensated(_sagaId, ex.Message);
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
            SagaId = _sagaId,
            SagaType = GetType().FullName ?? GetType().Name,
            Status = _status,
            CurrentStepIndex = _currentStepIndex,
            CurrentStepName = _currentStepName,
            Data = _data,
            StartedAt = _startedAt ?? DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = _completedAt,
            Timeout = _timeout,
            ExecutedSteps = _executedSteps.Select(s => s.Name).Reverse().ToList(),
            Errors = _error != null ? new List<string> { _error.Message } : new List<string>(),
            RetryCount = _retryCount,
            MaxRetries = _maxRetries
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
    private readonly List<ISagaStep<TData>> _steps = new();

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
        var step = ActivatorUtilities.CreateInstance<TStep>(_serviceProvider);
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

    internal IReadOnlyList<ISagaStep<TData>> Build() => _steps.AsReadOnly();
}

