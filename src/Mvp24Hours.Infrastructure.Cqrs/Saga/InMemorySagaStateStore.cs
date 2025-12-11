//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// In-memory implementation of saga state store.
/// Suitable for development, testing, and single-instance deployments.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This store does not persist state across application restarts.
/// Use a database-backed implementation for production scenarios.
/// </para>
/// </remarks>
public class InMemorySagaStateStore : ISagaStateStore
{
    private readonly ConcurrentDictionary<Guid, SagaState> _states = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <inheritdoc />
    public Task SaveAsync(SagaState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        state.LastUpdatedAt = DateTime.UtcNow;
        _states[state.SagaId] = Clone(state);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync<TData>(SagaState<TData> state, CancellationToken cancellationToken = default) 
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(state);

        var nonGenericState = new SagaState
        {
            SagaId = state.SagaId,
            SagaType = state.SagaType,
            Status = state.Status,
            CurrentStepIndex = state.CurrentStepIndex,
            CurrentStepName = state.CurrentStepName,
            DataJson = JsonSerializer.Serialize(state.Data, _jsonOptions),
            DataType = typeof(TData).AssemblyQualifiedName ?? typeof(TData).FullName ?? typeof(TData).Name,
            StartedAt = state.StartedAt,
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = state.CompletedAt,
            TimeoutSeconds = state.Timeout.HasValue ? (int)state.Timeout.Value.TotalSeconds : null,
            ExpiresAt = state.ExpiresAt,
            ExecutedSteps = new List<string>(state.ExecutedSteps),
            Errors = new List<string>(state.Errors),
            CompensationErrors = new List<string>(state.CompensationErrors),
            CorrelationId = state.CorrelationId,
            RetryCount = state.RetryCount,
            MaxRetries = state.MaxRetries,
            NextRetryAt = state.NextRetryAt,
            Metadata = new Dictionary<string, string>(state.Metadata)
        };

        _states[state.SagaId] = nonGenericState;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_states.TryGetValue(sagaId, out var state) ? Clone(state) : null);
    }

    /// <inheritdoc />
    public Task<SagaState<TData>?> GetAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default) 
        where TData : class
    {
        if (!_states.TryGetValue(sagaId, out var state))
            return Task.FromResult<SagaState<TData>?>(null);

        var data = JsonSerializer.Deserialize<TData>(state.DataJson, _jsonOptions);
        if (data == null)
            return Task.FromResult<SagaState<TData>?>(null);

        var typedState = new SagaState<TData>
        {
            SagaId = state.SagaId,
            SagaType = state.SagaType,
            Status = state.Status,
            CurrentStepIndex = state.CurrentStepIndex,
            CurrentStepName = state.CurrentStepName,
            Data = data,
            StartedAt = state.StartedAt,
            LastUpdatedAt = state.LastUpdatedAt,
            CompletedAt = state.CompletedAt,
            Timeout = state.TimeoutSeconds.HasValue ? TimeSpan.FromSeconds(state.TimeoutSeconds.Value) : null,
            ExpiresAt = state.ExpiresAt,
            ExecutedSteps = new List<string>(state.ExecutedSteps),
            Errors = new List<string>(state.Errors),
            CompensationErrors = new List<string>(state.CompensationErrors),
            CorrelationId = state.CorrelationId,
            RetryCount = state.RetryCount,
            MaxRetries = state.MaxRetries,
            NextRetryAt = state.NextRetryAt,
            Metadata = new Dictionary<string, string>(state.Metadata)
        };

        return Task.FromResult<SagaState<TData>?>(typedState);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Guid sagaId, Action<SagaState> update, CancellationToken cancellationToken = default)
    {
        if (_states.TryGetValue(sagaId, out var state))
        {
            update(state);
            state.LastUpdatedAt = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync<TData>(Guid sagaId, Action<SagaState<TData>> update, CancellationToken cancellationToken = default) 
        where TData : class
    {
        if (!_states.TryGetValue(sagaId, out var state))
            return Task.CompletedTask;

        var data = JsonSerializer.Deserialize<TData>(state.DataJson, _jsonOptions);
        if (data == null)
            return Task.CompletedTask;

        var typedState = new SagaState<TData>
        {
            SagaId = state.SagaId,
            SagaType = state.SagaType,
            Status = state.Status,
            CurrentStepIndex = state.CurrentStepIndex,
            CurrentStepName = state.CurrentStepName,
            Data = data,
            StartedAt = state.StartedAt,
            LastUpdatedAt = state.LastUpdatedAt,
            CompletedAt = state.CompletedAt,
            ExecutedSteps = state.ExecutedSteps,
            Errors = state.Errors,
            CompensationErrors = state.CompensationErrors,
            CorrelationId = state.CorrelationId,
            RetryCount = state.RetryCount,
            MaxRetries = state.MaxRetries,
            NextRetryAt = state.NextRetryAt,
            Metadata = state.Metadata
        };

        update(typedState);

        // Update back
        state.Status = typedState.Status;
        state.CurrentStepIndex = typedState.CurrentStepIndex;
        state.CurrentStepName = typedState.CurrentStepName;
        state.DataJson = JsonSerializer.Serialize(typedState.Data, _jsonOptions);
        state.CompletedAt = typedState.CompletedAt;
        state.ExecutedSteps = typedState.ExecutedSteps;
        state.Errors = typedState.Errors;
        state.CompensationErrors = typedState.CompensationErrors;
        state.RetryCount = typedState.RetryCount;
        state.NextRetryAt = typedState.NextRetryAt;
        state.LastUpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(sagaId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SagaState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default)
    {
        var result = _states.Values
            .Where(s => s.Status == status)
            .Select(Clone)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<SagaState>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SagaState>> GetPendingCompensationsAsync(CancellationToken cancellationToken = default)
    {
        var result = _states.Values
            .Where(s => s.Status == SagaStatus.Failed)
            .Select(Clone)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<SagaState>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SagaState>> GetTimedOutSagasAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var result = _states.Values
            .Where(s => s.Status == SagaStatus.Running && 
                        s.TimeoutSeconds.HasValue && 
                        now > s.StartedAt.AddSeconds(s.TimeoutSeconds.Value))
            .Select(Clone)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<SagaState>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SagaState>> GetReadyForRetryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var result = _states.Values
            .Where(s => s.Status == SagaStatus.Suspended && 
                        s.NextRetryAt.HasValue && 
                        now >= s.NextRetryAt.Value &&
                        s.RetryCount < s.MaxRetries)
            .Select(Clone)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<SagaState>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SagaState>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var result = _states.Values
            .Where(s => s.ExpiresAt.HasValue && now > s.ExpiresAt.Value)
            .Select(Clone)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<SagaState>>(result);
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var toRemove = _states.Values
            .Where(s => (s.Status == SagaStatus.Completed || s.Status == SagaStatus.Compensated) &&
                        s.CompletedAt.HasValue && s.CompletedAt.Value < olderThan)
            .Select(s => s.SagaId)
            .ToList();

        foreach (var id in toRemove)
        {
            _states.TryRemove(id, out _);
        }

        return Task.FromResult(toRemove.Count);
    }

    private SagaState Clone(SagaState state)
    {
        return new SagaState
        {
            SagaId = state.SagaId,
            SagaType = state.SagaType,
            Status = state.Status,
            CurrentStepIndex = state.CurrentStepIndex,
            CurrentStepName = state.CurrentStepName,
            DataJson = state.DataJson,
            DataType = state.DataType,
            StartedAt = state.StartedAt,
            LastUpdatedAt = state.LastUpdatedAt,
            CompletedAt = state.CompletedAt,
            TimeoutSeconds = state.TimeoutSeconds,
            ExpiresAt = state.ExpiresAt,
            ExecutedSteps = new List<string>(state.ExecutedSteps),
            Errors = new List<string>(state.Errors),
            CompensationErrors = new List<string>(state.CompensationErrors),
            CorrelationId = state.CorrelationId,
            RetryCount = state.RetryCount,
            MaxRetries = state.MaxRetries,
            NextRetryAt = state.NextRetryAt,
            Metadata = new Dictionary<string, string>(state.Metadata)
        };
    }
}

