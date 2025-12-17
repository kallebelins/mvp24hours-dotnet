//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence
{
    /// <summary>
    /// MongoDB-based implementation of saga repository.
    /// Provides flexible, document-based saga state storage.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Requirements:</strong>
    /// Register IMongoSagaCollection in DI to use this repository.
    /// The collection interface provides MongoDB operations.
    /// </para>
    /// </remarks>
    public class MongoDbSagaRepository<TData> : ISagaRepository<TData> where TData : class, new()
    {
        private readonly IMongoSagaCollection<TData> _collection;
        private readonly ILogger<MongoDbSagaRepository<TData>>? _logger;

        /// <summary>
        /// Creates a new MongoDB saga repository.
        /// </summary>
        public MongoDbSagaRepository(
            IMongoSagaCollection<TData> collection,
            ILogger<MongoDbSagaRepository<TData>>? logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<SagaInstance<TData>?> FindAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            return _collection.FindByCorrelationIdAsync(correlationId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<SagaInstance<TData>> CreateAsync(
            Guid correlationId,
            string initialState,
            TData? initialData = null,
            CancellationToken cancellationToken = default)
        {
            var existing = await _collection.FindByCorrelationIdAsync(correlationId, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Saga instance with correlation ID {correlationId} already exists.");
            }

            var instance = new SagaInstance<TData>
            {
                CorrelationId = correlationId,
                CurrentState = initialState,
                Data = initialData ?? new TData(),
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            instance.StateHistory.Add(new SagaStateTransition
            {
                FromState = string.Empty,
                ToState = initialState,
                Timestamp = DateTime.UtcNow,
                Reason = "Saga created"
            });

            await _collection.InsertAsync(instance, cancellationToken);

            _logger?.LogDebug("Created saga instance {SagaId} in MongoDB", correlationId);

            return instance;
        }

        /// <inheritdoc />
        public async Task SaveAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);

            instance.LastUpdatedAt = DateTime.UtcNow;
            instance.Version++;

            await _collection.ReplaceAsync(instance.CorrelationId, instance, cancellationToken);

            _logger?.LogDebug("Saved saga instance {SagaId} to MongoDB", instance.CorrelationId);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            var deleted = await _collection.DeleteAsync(correlationId, cancellationToken);
            if (deleted)
            {
                _logger?.LogDebug("Deleted saga instance {SagaId} from MongoDB", correlationId);
            }
            return deleted;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(
            string state,
            CancellationToken cancellationToken = default)
        {
            return _collection.FindByStateAsync(state, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(
            TimeSpan timeoutThreshold,
            CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(timeoutThreshold);
            return _collection.FindTimedOutAsync(threshold, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<SagaInstance<TData>>> FindFaultedAsync(
            CancellationToken cancellationToken = default)
        {
            return FindByStateAsync("Faulted", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(olderThan);
            var cleaned = await _collection.DeleteCompletedOlderThanAsync(threshold, cancellationToken);

            _logger?.LogInformation("Cleaned up {Count} old saga instances from MongoDB", cleaned);

            return cleaned;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(
            Guid correlationId,
            int expectedVersion,
            Action<SagaInstance<TData>> update,
            CancellationToken cancellationToken = default)
        {
            var instance = await FindAsync(correlationId, cancellationToken);
            if (instance == null)
            {
                return false;
            }

            if (instance.Version != expectedVersion)
            {
                return false;
            }

            update(instance);
            await SaveAsync(instance, cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Interface for MongoDB saga collection operations.
    /// Implement this interface to provide MongoDB operations for saga storage.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    public interface IMongoSagaCollection<TData> where TData : class, new()
    {
        Task<SagaInstance<TData>?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
        Task InsertAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default);
        Task ReplaceAsync(Guid correlationId, SagaInstance<TData> instance, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(string state, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(DateTime threshold, CancellationToken cancellationToken = default);
        Task<int> DeleteCompletedOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default);
    }
}

