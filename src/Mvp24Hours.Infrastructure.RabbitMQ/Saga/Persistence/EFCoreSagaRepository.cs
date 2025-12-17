//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence
{
    /// <summary>
    /// Entity Framework Core-based implementation of saga repository.
    /// Provides durable, transactional saga state storage.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Requirements:</strong>
    /// Register ISagaDbContext in DI to use this repository.
    /// The context should have a DbSet for SagaStateEntity.
    /// </para>
    /// </remarks>
    public class EFCoreSagaRepository<TData> : ISagaRepository<TData> where TData : class, new()
    {
        private readonly ISagaDbContext _dbContext;
        private readonly ILogger<EFCoreSagaRepository<TData>>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _sagaTypeName;

        /// <summary>
        /// Creates a new EF Core saga repository.
        /// </summary>
        public EFCoreSagaRepository(
            ISagaDbContext dbContext,
            ILogger<EFCoreSagaRepository<TData>>? logger = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger;
            _sagaTypeName = typeof(TData).FullName ?? typeof(TData).Name;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public async Task<SagaInstance<TData>?> FindAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GetSagaStateAsync(correlationId, _sagaTypeName, cancellationToken);
            return entity != null ? EntityToInstance(entity) : null;
        }

        /// <inheritdoc />
        public async Task<SagaInstance<TData>> CreateAsync(
            Guid correlationId,
            string initialState,
            TData? initialData = null,
            CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.GetSagaStateAsync(correlationId, _sagaTypeName, cancellationToken);
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

            var entity = InstanceToEntity(instance);
            await _dbContext.AddSagaStateAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogDebug("Created saga instance {SagaId} in database", correlationId);

            return instance;
        }

        /// <inheritdoc />
        public async Task SaveAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);

            instance.LastUpdatedAt = DateTime.UtcNow;
            instance.Version++;

            var entity = InstanceToEntity(instance);
            await _dbContext.UpdateSagaStateAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogDebug("Saved saga instance {SagaId} to database", instance.CorrelationId);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            var deleted = await _dbContext.DeleteSagaStateAsync(correlationId, _sagaTypeName, cancellationToken);
            if (deleted)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger?.LogDebug("Deleted saga instance {SagaId} from database", correlationId);
            }
            return deleted;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(
            string state,
            CancellationToken cancellationToken = default)
        {
            var entities = await _dbContext.GetSagasByStateAsync(_sagaTypeName, state, cancellationToken);
            return entities.Select(EntityToInstance).ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(
            TimeSpan timeoutThreshold,
            CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(timeoutThreshold);
            var entities = await _dbContext.GetTimedOutSagasAsync(_sagaTypeName, threshold, cancellationToken);
            return entities.Select(EntityToInstance).ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SagaInstance<TData>>> FindFaultedAsync(
            CancellationToken cancellationToken = default)
        {
            return await FindByStateAsync("Faulted", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.Subtract(olderThan);
            var cleaned = await _dbContext.CleanupOldSagasAsync(_sagaTypeName, threshold, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Cleaned up {Count} old saga instances from database", cleaned);

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

        private SagaInstance<TData> EntityToInstance(SagaStateEntity entity)
        {
            var data = JsonSerializer.Deserialize<TData>(entity.DataJson, _jsonOptions) ?? new TData();
            var metadata = string.IsNullOrEmpty(entity.MetadataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.MetadataJson, _jsonOptions) ?? new Dictionary<string, string>();
            var errors = string.IsNullOrEmpty(entity.ErrorsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entity.ErrorsJson, _jsonOptions) ?? new List<string>();
            var stateHistory = string.IsNullOrEmpty(entity.StateHistoryJson)
                ? new List<SagaStateTransition>()
                : JsonSerializer.Deserialize<List<SagaStateTransition>>(entity.StateHistoryJson, _jsonOptions) ?? new List<SagaStateTransition>();
            var scheduledTimeouts = string.IsNullOrEmpty(entity.ScheduledTimeoutsJson)
                ? new List<Guid>()
                : JsonSerializer.Deserialize<List<Guid>>(entity.ScheduledTimeoutsJson, _jsonOptions) ?? new List<Guid>();

            return new SagaInstance<TData>
            {
                CorrelationId = entity.CorrelationId,
                CurrentState = entity.CurrentState,
                Data = data,
                Version = entity.Version,
                CreatedAt = entity.CreatedAt,
                LastUpdatedAt = entity.LastUpdatedAt,
                CompletedAt = entity.CompletedAt,
                FaultedAt = entity.FaultedAt,
                ErrorMessage = entity.ErrorMessage,
                Errors = errors,
                Metadata = metadata,
                ScheduledTimeouts = scheduledTimeouts,
                StateHistory = stateHistory
            };
        }

        private SagaStateEntity InstanceToEntity(SagaInstance<TData> instance)
        {
            return new SagaStateEntity
            {
                CorrelationId = instance.CorrelationId,
                SagaTypeName = _sagaTypeName,
                CurrentState = instance.CurrentState,
                DataJson = JsonSerializer.Serialize(instance.Data, _jsonOptions),
                Version = instance.Version,
                CreatedAt = instance.CreatedAt,
                LastUpdatedAt = instance.LastUpdatedAt,
                CompletedAt = instance.CompletedAt,
                FaultedAt = instance.FaultedAt,
                ErrorMessage = instance.ErrorMessage,
                ErrorsJson = JsonSerializer.Serialize(instance.Errors, _jsonOptions),
                MetadataJson = JsonSerializer.Serialize(instance.Metadata, _jsonOptions),
                ScheduledTimeoutsJson = JsonSerializer.Serialize(instance.ScheduledTimeouts, _jsonOptions),
                StateHistoryJson = JsonSerializer.Serialize(instance.StateHistory, _jsonOptions)
            };
        }
    }

    /// <summary>
    /// Interface for saga database context.
    /// Implement this interface in your DbContext to use EFCoreSagaRepository.
    /// </summary>
    public interface ISagaDbContext
    {
        Task<SagaStateEntity?> GetSagaStateAsync(Guid correlationId, string sagaTypeName, CancellationToken cancellationToken = default);
        Task AddSagaStateAsync(SagaStateEntity entity, CancellationToken cancellationToken = default);
        Task UpdateSagaStateAsync(SagaStateEntity entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteSagaStateAsync(Guid correlationId, string sagaTypeName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SagaStateEntity>> GetSagasByStateAsync(string sagaTypeName, string state, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SagaStateEntity>> GetTimedOutSagasAsync(string sagaTypeName, DateTime threshold, CancellationToken cancellationToken = default);
        Task<int> CleanupOldSagasAsync(string sagaTypeName, DateTime threshold, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Entity for storing saga state in a database.
    /// </summary>
    public class SagaStateEntity
    {
        public Guid CorrelationId { get; set; }
        public string SagaTypeName { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string DataJson { get; set; } = "{}";
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FaultedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorsJson { get; set; }
        public string? MetadataJson { get; set; }
        public string? ScheduledTimeoutsJson { get; set; }
        public string? StateHistoryJson { get; set; }
    }
}

