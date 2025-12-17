//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence
{
    /// <summary>
    /// In-memory implementation of saga repository.
    /// Suitable for testing and development scenarios.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> Data is lost when the application restarts.
    /// Use Redis, SQL, or MongoDB implementations for production.
    /// </para>
    /// </remarks>
    public class InMemorySagaRepository<TData> : ISagaRepository<TData> where TData : class, new()
    {
        private readonly ConcurrentDictionary<Guid, SagaInstance<TData>> _sagas = new();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <inheritdoc />
        public Task<SagaInstance<TData>?> FindAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            _sagas.TryGetValue(correlationId, out var instance);
            
            // Return a clone to prevent external modification
            if (instance != null)
            {
                instance = Clone(instance);
            }
            
            return Task.FromResult(instance);
        }

        /// <inheritdoc />
        public Task<SagaInstance<TData>> CreateAsync(
            Guid correlationId,
            string initialState,
            TData? initialData = null,
            CancellationToken cancellationToken = default)
        {
            var instance = new SagaInstance<TData>
            {
                CorrelationId = correlationId,
                CurrentState = initialState,
                Data = initialData ?? new TData(),
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            // Add initial state to history
            instance.StateHistory.Add(new SagaStateTransition
            {
                FromState = string.Empty,
                ToState = initialState,
                Timestamp = DateTime.UtcNow,
                Reason = "Saga created"
            });

            if (!_sagas.TryAdd(correlationId, instance))
            {
                throw new InvalidOperationException(
                    $"Saga instance with correlation ID {correlationId} already exists.");
            }

            return Task.FromResult(Clone(instance));
        }

        /// <inheritdoc />
        public Task SaveAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);

            instance.LastUpdatedAt = DateTime.UtcNow;
            instance.Version++;
            
            _sagas[instance.CorrelationId] = Clone(instance);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sagas.TryRemove(correlationId, out _));
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(
            string state,
            CancellationToken cancellationToken = default)
        {
            var result = _sagas.Values
                .Where(s => s.CurrentState == state)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<SagaInstance<TData>>>(result);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(
            TimeSpan timeoutThreshold,
            CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(timeoutThreshold);
            
            var result = _sagas.Values
                .Where(s => s.IsActive && s.LastUpdatedAt < threshold)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<SagaInstance<TData>>>(result);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindFaultedAsync(
            CancellationToken cancellationToken = default)
        {
            var result = _sagas.Values
                .Where(s => s.IsFaulted)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<SagaInstance<TData>>>(result);
        }

        /// <inheritdoc />
        public Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(olderThan);
            var toRemove = _sagas.Values
                .Where(s => (s.IsCompleted || s.IsFaulted) && s.LastUpdatedAt < threshold)
                .Select(s => s.CorrelationId)
                .ToList();

            var removed = 0;
            foreach (var id in toRemove)
            {
                if (_sagas.TryRemove(id, out _))
                {
                    removed++;
                }
            }

            return Task.FromResult(removed);
        }

        /// <inheritdoc />
        public Task<bool> UpdateAsync(
            Guid correlationId,
            int expectedVersion,
            Action<SagaInstance<TData>> update,
            CancellationToken cancellationToken = default)
        {
            if (!_sagas.TryGetValue(correlationId, out var instance))
            {
                return Task.FromResult(false);
            }

            if (instance.Version != expectedVersion)
            {
                return Task.FromResult(false);
            }

            var updated = Clone(instance);
            update(updated);
            updated.LastUpdatedAt = DateTime.UtcNow;
            updated.Version++;

            var success = _sagas.TryUpdate(correlationId, updated, instance);
            return Task.FromResult(success);
        }

        /// <summary>
        /// Clears all sagas. For testing purposes only.
        /// </summary>
        public void Clear()
        {
            _sagas.Clear();
        }

        /// <summary>
        /// Gets the count of sagas. For testing purposes.
        /// </summary>
        public int Count => _sagas.Count;

        private SagaInstance<TData> Clone(SagaInstance<TData> instance)
        {
            var json = JsonSerializer.Serialize(instance, _jsonOptions);
            return JsonSerializer.Deserialize<SagaInstance<TData>>(json, _jsonOptions)!;
        }
    }
}

