//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Default implementation of the saga orchestrator.
/// </summary>
public class SagaOrchestrator : ISagaOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<SagaOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaOrchestrator"/> class.
    /// </summary>
    public SagaOrchestrator(
        IServiceProvider serviceProvider,
        ISagaStateStore stateStore,
        ILogger<SagaOrchestrator> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SagaResult<TData>> ExecuteAsync<TSaga, TData>(
        TData data,
        SagaExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        options ??= new SagaExecutionOptions();
        var saga = ActivatorUtilities.CreateInstance<TSaga>(_serviceProvider);

        _logger.LogInformation("Starting saga {SagaType}", typeof(TSaga).Name);

        try
        {
            var result = await saga.StartAsync(data, cancellationToken);

            if (options.PersistState)
            {
                var state = CreateState<TSaga, TData>(saga, options);
                await _stateStore.SaveAsync(state, cancellationToken);
            }

            return result.IsSuccess
                ? SagaResult<TData>.Success(result.SagaId, saga.Data)
                : SagaResult<TData>.Failed(result.SagaId, result.ErrorMessage ?? "Unknown error", saga.Data, result.Exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga {SagaType} execution failed", typeof(TSaga).Name);
            
            if (options.PersistState)
            {
                var state = CreateState<TSaga, TData>(saga, options);
                state.Status = SagaStatus.Failed;
                state.Errors.Add(ex.Message);
                await _stateStore.SaveAsync(state, cancellationToken);
            }

            return SagaResult<TData>.Failed(saga.SagaId, ex.Message, saga.Data, ex);
        }
    }

    /// <inheritdoc />
    public async Task<SagaResult<TData>> ResumeAsync<TSaga, TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class
    {
        var state = await _stateStore.GetAsync<TData>(sagaId, cancellationToken);
        if (state == null)
        {
            throw new SagaNotFoundException(sagaId);
        }

        var saga = ActivatorUtilities.CreateInstance<TSaga>(_serviceProvider);

        _logger.LogInformation("Resuming saga {SagaId} of type {SagaType}", sagaId, typeof(TSaga).Name);

        try
        {
            var result = await saga.ResumeAsync(state, cancellationToken);

            await _stateStore.UpdateAsync<TData>(sagaId, s =>
            {
                s.Status = result.Status;
                s.CurrentStepIndex = saga.CurrentStepIndex;
                s.CurrentStepName = saga.CurrentStepName;
                s.Data = saga.Data;
                s.CompletedAt = DateTime.UtcNow;
            }, cancellationToken);

            return result.IsSuccess
                ? SagaResult<TData>.Success(result.SagaId, saga.Data)
                : SagaResult<TData>.Failed(result.SagaId, result.ErrorMessage ?? "Unknown error", saga.Data, result.Exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume saga {SagaId}", sagaId);

            await _stateStore.UpdateAsync<TData>(sagaId, s =>
            {
                s.Status = SagaStatus.Failed;
                s.Errors.Add(ex.Message);
            }, cancellationToken);

            return SagaResult<TData>.Failed(sagaId, ex.Message, exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<SagaResult> CompensateAsync<TSaga, TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class
    {
        var state = await _stateStore.GetAsync<TData>(sagaId, cancellationToken);
        if (state == null)
        {
            throw new SagaNotFoundException(sagaId);
        }

        if (state.Status != SagaStatus.Failed && state.Status != SagaStatus.Running)
        {
            throw new SagaInvalidStateException(sagaId, state.Status, SagaStatus.Failed);
        }

        var saga = ActivatorUtilities.CreateInstance<TSaga>(_serviceProvider);

        _logger.LogInformation("Compensating saga {SagaId}", sagaId);

        try
        {
            // Restore saga state
            await saga.ResumeAsync(state, cancellationToken);
            await saga.CompensateAsync(cancellationToken);

            await _stateStore.UpdateAsync<TData>(sagaId, s =>
            {
                s.Status = saga.Status;
                s.CompletedAt = DateTime.UtcNow;
            }, cancellationToken);

            return saga.Status == SagaStatus.Compensated
                ? SagaResult.Compensated(sagaId, "Saga compensated successfully")
                : SagaResult.PartiallyCompensated(sagaId, "Some compensation steps failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compensate saga {SagaId}", sagaId);

            await _stateStore.UpdateAsync<TData>(sagaId, s =>
            {
                s.Status = SagaStatus.PartiallyCompensated;
                s.CompensationErrors.Add(ex.Message);
            }, cancellationToken);

            return SagaResult.PartiallyCompensated(sagaId, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<SagaState?> GetStatusAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return _stateStore.GetAsync(sagaId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SagaResult> CancelAsync(Guid sagaId, bool compensate = true, CancellationToken cancellationToken = default)
    {
        var state = await _stateStore.GetAsync(sagaId, cancellationToken);
        if (state == null)
        {
            throw new SagaNotFoundException(sagaId);
        }

        if (state.Status != SagaStatus.Running && state.Status != SagaStatus.Suspended)
        {
            throw new SagaInvalidStateException(sagaId, state.Status, 
                "Saga can only be cancelled when Running or Suspended");
        }

        _logger.LogInformation("Cancelling saga {SagaId}", sagaId);

        await _stateStore.UpdateAsync(sagaId, s =>
        {
            s.Status = SagaStatus.Cancelled;
            s.CompletedAt = DateTime.UtcNow;
        }, cancellationToken);

        // Note: Compensation would require knowing the saga type
        // This is a simplified implementation
        
        return SagaResult.Cancelled(sagaId);
    }

    /// <inheritdoc />
    public async Task<int> ProcessRetryQueueAsync(CancellationToken cancellationToken = default)
    {
        var readyForRetry = await _stateStore.GetReadyForRetryAsync(cancellationToken);
        var processed = 0;

        foreach (var state in readyForRetry)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Mark as running to prevent duplicate processing
                await _stateStore.UpdateAsync(state.SagaId, s =>
                {
                    s.Status = SagaStatus.Running;
                    s.RetryCount++;
                    s.NextRetryAt = null;
                }, cancellationToken);

                _logger.LogInformation("Retrying saga {SagaId} (attempt {Attempt})", 
                    state.SagaId, state.RetryCount + 1);

                // Note: Actual retry would require instantiating the correct saga type
                // This would typically be done through a saga registry
                
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry saga {SagaId}", state.SagaId);
                
                await _stateStore.UpdateAsync(state.SagaId, s =>
                {
                    s.Status = SagaStatus.Suspended;
                    s.Errors.Add(ex.Message);
                    s.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, s.RetryCount));
                }, cancellationToken);
            }
        }

        return processed;
    }

    /// <inheritdoc />
    public async Task<int> ProcessTimeoutsAsync(CancellationToken cancellationToken = default)
    {
        var timedOut = await _stateStore.GetTimedOutSagasAsync(cancellationToken);
        var processed = 0;

        foreach (var state in timedOut)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await _stateStore.UpdateAsync(state.SagaId, s =>
                {
                    s.Status = SagaStatus.TimedOut;
                    s.Errors.Add($"Saga timed out after {s.TimeoutSeconds} seconds");
                }, cancellationToken);

                _logger.LogWarning("Saga {SagaId} timed out", state.SagaId);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process timeout for saga {SagaId}", state.SagaId);
            }
        }

        return processed;
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return _stateStore.CleanupAsync(olderThan, cancellationToken);
    }

    private static SagaState<TData> CreateState<TSaga, TData>(TSaga saga, SagaExecutionOptions options)
        where TSaga : ISaga<TData>
        where TData : class
    {
        return new SagaState<TData>
        {
            SagaId = saga.SagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Status = saga.Status,
            CurrentStepIndex = saga.CurrentStepIndex,
            CurrentStepName = saga.CurrentStepName,
            Data = saga.Data,
            StartedAt = saga.StartedAt ?? DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = saga.CompletedAt,
            Timeout = options.Timeout,
            ExpiresAt = options.ExpiresAt,
            CorrelationId = options.CorrelationId,
            MaxRetries = options.MaxRetries ?? 3,
            Metadata = options.Metadata ?? new Dictionary<string, string>()
        };
    }
}

