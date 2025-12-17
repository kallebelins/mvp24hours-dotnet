//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
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
    /// Redis-based implementation of saga repository using IDistributedCache.
    /// Provides fast, distributed saga state storage.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Fast read/write operations</item>
    /// <item>Automatic expiration of old sagas</item>
    /// <item>Distributed across multiple instances</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RedisSagaRepository<TData> : ISagaRepository<TData> where TData : class, new()
    {
        private readonly IDistributedCache _cache;
        private readonly RedisSagaRepositoryOptions _options;
        private readonly ILogger<RedisSagaRepository<TData>>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private string KeyPrefix => $"saga:{typeof(TData).Name}:";
        private string IndexKey => $"saga:{typeof(TData).Name}:index";
        private string StateIndexKey(string state) => $"saga:{typeof(TData).Name}:state:{state}";

        /// <summary>
        /// Creates a new Redis saga repository.
        /// </summary>
        public RedisSagaRepository(
            IDistributedCache cache,
            RedisSagaRepositoryOptions? options = null,
            ILogger<RedisSagaRepository<TData>>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options ?? new RedisSagaRepositoryOptions();
            _logger = logger;
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
            var key = KeyPrefix + correlationId;
            var json = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SagaInstance<TData>>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to deserialize saga instance {SagaId}", correlationId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<SagaInstance<TData>> CreateAsync(
            Guid correlationId,
            string initialState,
            TData? initialData = null,
            CancellationToken cancellationToken = default)
        {
            var key = KeyPrefix + correlationId;

            // Check if already exists
            var existing = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(existing))
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

            var json = JsonSerializer.Serialize(instance, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = _options.DefaultExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);

            // Update state index
            await AddToStateIndexAsync(correlationId, initialState, cancellationToken);

            _logger?.LogDebug("Created saga instance {SagaId} in Redis", correlationId);

            return instance;
        }

        /// <inheritdoc />
        public async Task SaveAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);

            var key = KeyPrefix + instance.CorrelationId;
            var previousState = await GetPreviousStateAsync(instance.CorrelationId, cancellationToken);

            instance.LastUpdatedAt = DateTime.UtcNow;
            instance.Version++;

            var json = JsonSerializer.Serialize(instance, _jsonOptions);

            // Set appropriate expiration based on state
            var expiration = instance.IsCompleted || instance.IsFaulted
                ? _options.CompletedExpiration
                : _options.DefaultExpiration;

            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = expiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);

            // Update state indexes if state changed
            if (previousState != instance.CurrentState)
            {
                await RemoveFromStateIndexAsync(instance.CorrelationId, previousState, cancellationToken);
                await AddToStateIndexAsync(instance.CorrelationId, instance.CurrentState, cancellationToken);
            }

            _logger?.LogDebug("Saved saga instance {SagaId} to Redis", instance.CorrelationId);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            var key = KeyPrefix + correlationId;
            var instance = await FindAsync(correlationId, cancellationToken);

            if (instance == null)
            {
                return false;
            }

            await _cache.RemoveAsync(key, cancellationToken);
            await RemoveFromStateIndexAsync(correlationId, instance.CurrentState, cancellationToken);

            _logger?.LogDebug("Deleted saga instance {SagaId} from Redis", correlationId);

            return true;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(
            string state,
            CancellationToken cancellationToken = default)
        {
            var indexKey = StateIndexKey(state);
            var idsJson = await _cache.GetStringAsync(indexKey, cancellationToken);

            if (string.IsNullOrEmpty(idsJson))
            {
                return Array.Empty<SagaInstance<TData>>();
            }

            var ids = JsonSerializer.Deserialize<List<Guid>>(idsJson, _jsonOptions) ?? new List<Guid>();
            var results = new List<SagaInstance<TData>>();

            foreach (var id in ids)
            {
                var instance = await FindAsync(id, cancellationToken);
                if (instance != null && instance.CurrentState == state)
                {
                    results.Add(instance);
                }
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(
            TimeSpan timeoutThreshold,
            CancellationToken cancellationToken = default)
        {
            // This is less efficient in Redis - consider using sorted sets for production
            var indexJson = await _cache.GetStringAsync(IndexKey, cancellationToken);
            if (string.IsNullOrEmpty(indexJson))
            {
                return Array.Empty<SagaInstance<TData>>();
            }

            var ids = JsonSerializer.Deserialize<List<Guid>>(indexJson, _jsonOptions) ?? new List<Guid>();
            var threshold = DateTime.UtcNow.Subtract(timeoutThreshold);
            var results = new List<SagaInstance<TData>>();

            foreach (var id in ids)
            {
                var instance = await FindAsync(id, cancellationToken);
                if (instance != null && instance.IsActive && instance.LastUpdatedAt < threshold)
                {
                    results.Add(instance);
                }
            }

            return results;
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
            var completed = await FindByStateAsync("Completed", cancellationToken);
            var faulted = await FindByStateAsync("Faulted", cancellationToken);

            var toCleanup = completed.Concat(faulted)
                .Where(s => s.LastUpdatedAt < threshold)
                .ToList();

            var cleaned = 0;
            foreach (var instance in toCleanup)
            {
                if (await DeleteAsync(instance.CorrelationId, cancellationToken))
                {
                    cleaned++;
                }
            }

            _logger?.LogInformation("Cleaned up {Count} old saga instances from Redis", cleaned);

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

        private async Task<string?> GetPreviousStateAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            var instance = await FindAsync(correlationId, cancellationToken);
            return instance?.CurrentState;
        }

        private async Task AddToStateIndexAsync(Guid correlationId, string state, CancellationToken cancellationToken)
        {
            var indexKey = StateIndexKey(state);
            var idsJson = await _cache.GetStringAsync(indexKey, cancellationToken);
            var ids = string.IsNullOrEmpty(idsJson)
                ? new List<Guid>()
                : JsonSerializer.Deserialize<List<Guid>>(idsJson, _jsonOptions) ?? new List<Guid>();

            if (!ids.Contains(correlationId))
            {
                ids.Add(correlationId);
                await _cache.SetStringAsync(indexKey, JsonSerializer.Serialize(ids, _jsonOptions), cancellationToken);
            }

            // Also maintain main index
            var mainIndexJson = await _cache.GetStringAsync(IndexKey, cancellationToken);
            var mainIds = string.IsNullOrEmpty(mainIndexJson)
                ? new List<Guid>()
                : JsonSerializer.Deserialize<List<Guid>>(mainIndexJson, _jsonOptions) ?? new List<Guid>();

            if (!mainIds.Contains(correlationId))
            {
                mainIds.Add(correlationId);
                await _cache.SetStringAsync(IndexKey, JsonSerializer.Serialize(mainIds, _jsonOptions), cancellationToken);
            }
        }

        private async Task RemoveFromStateIndexAsync(Guid correlationId, string? state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(state))
            {
                return;
            }

            var indexKey = StateIndexKey(state);
            var idsJson = await _cache.GetStringAsync(indexKey, cancellationToken);

            if (!string.IsNullOrEmpty(idsJson))
            {
                var ids = JsonSerializer.Deserialize<List<Guid>>(idsJson, _jsonOptions) ?? new List<Guid>();
                if (ids.Remove(correlationId))
                {
                    await _cache.SetStringAsync(indexKey, JsonSerializer.Serialize(ids, _jsonOptions), cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Configuration options for RedisSagaRepository.
    /// </summary>
    public class RedisSagaRepositoryOptions
    {
        /// <summary>
        /// Default expiration time for active sagas.
        /// Default: 24 hours.
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Expiration time for completed/faulted sagas.
        /// Default: 1 hour.
        /// </summary>
        public TimeSpan CompletedExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Key prefix for saga keys.
        /// </summary>
        public string? KeyPrefix { get; set; }
    }
}

