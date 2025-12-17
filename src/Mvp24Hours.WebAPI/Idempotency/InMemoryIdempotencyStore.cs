//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// In-memory implementation of idempotency storage.
    /// Suitable for single-instance deployments or development/testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This implementation stores data in memory and will
    /// lose all records when the application restarts. It also does not share state
    /// across multiple application instances.
    /// </para>
    /// <para>
    /// For production use in distributed environments, use
    /// <see cref="DistributedCacheIdempotencyStore"/> instead.
    /// </para>
    /// <para>
    /// This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}"/>
    /// for thread-safe operations and includes automatic cleanup of expired records.
    /// </para>
    /// </remarks>
    public class InMemoryIdempotencyStore : IIdempotencyStore, IDisposable
    {
        private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();
        private readonly ILogger<InMemoryIdempotencyStore>? _logger;
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of the in-memory idempotency store.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public InMemoryIdempotencyStore(ILogger<InMemoryIdempotencyStore>? logger = null)
        {
            _logger = logger;

            // Start periodic cleanup of expired records
            _cleanupTimer = new Timer(
                CleanupExpiredRecords,
                null,
                _cleanupInterval,
                _cleanupInterval);
        }

        /// <inheritdoc />
        public Task<IdempotencyLockResult> TryAcquireLockAsync(
            string key,
            string requestPath,
            string requestMethod,
            string? requestBodyHash,
            TimeSpan duration,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTimeOffset.UtcNow;
            var newRecord = new IdempotencyRecord
            {
                Key = key,
                Status = IdempotencyRecordStatus.Processing,
                CreatedAt = now,
                ExpiresAt = now.Add(duration),
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                RequestBodyHash = requestBodyHash,
                CorrelationId = correlationId
            };

            // Try to add atomically
            var added = _records.TryAdd(key, newRecord);

            if (added)
            {
                _logger?.LogDebug(
                    "[Idempotency] Acquired lock for key {Key}. Path: {Path}, Method: {Method}",
                    key, requestPath, requestMethod);

                return Task.FromResult(IdempotencyLockResult.Success());
            }

            // Key exists, check if expired
            if (_records.TryGetValue(key, out var existing))
            {
                if (existing.IsExpired)
                {
                    // Try to replace expired record
                    if (_records.TryUpdate(key, newRecord, existing))
                    {
                        _logger?.LogDebug(
                            "[Idempotency] Replaced expired record for key {Key}",
                            key);

                        return Task.FromResult(IdempotencyLockResult.Success());
                    }
                }

                _logger?.LogDebug(
                    "[Idempotency] Found existing record for key {Key}. Status: {Status}",
                    key, existing.Status);

                return Task.FromResult(IdempotencyLockResult.Existing(existing));
            }

            // Race condition - record was removed between TryAdd and TryGetValue
            // Try again
            return TryAcquireLockAsync(key, requestPath, requestMethod, requestBodyHash, duration, correlationId, cancellationToken);
        }

        /// <inheritdoc />
        public Task CompleteAsync(
            string key,
            int statusCode,
            byte[] responseBody,
            string contentType,
            string? responseHeadersJson = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_records.TryGetValue(key, out var record))
            {
                var updatedRecord = new IdempotencyRecord
                {
                    Key = record.Key,
                    Status = IdempotencyRecordStatus.Completed,
                    CreatedAt = record.CreatedAt,
                    ExpiresAt = record.ExpiresAt,
                    StatusCode = statusCode,
                    ResponseBody = responseBody,
                    ContentType = contentType,
                    ResponseHeadersJson = responseHeadersJson,
                    RequestPath = record.RequestPath,
                    RequestMethod = record.RequestMethod,
                    RequestBodyHash = record.RequestBodyHash,
                    CorrelationId = record.CorrelationId
                };

                _records.TryUpdate(key, updatedRecord, record);

                _logger?.LogDebug(
                    "[Idempotency] Completed record for key {Key}. StatusCode: {StatusCode}, BodySize: {BodySize}",
                    key, statusCode, responseBody.Length);
            }
            else
            {
                _logger?.LogWarning(
                    "[Idempotency] Attempted to complete non-existent record for key {Key}",
                    key);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task FailAsync(
            string key,
            bool removeRecord = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (removeRecord)
            {
                _records.TryRemove(key, out _);
                _logger?.LogDebug("[Idempotency] Removed failed record for key {Key}", key);
            }
            else if (_records.TryGetValue(key, out var record))
            {
                var updatedRecord = new IdempotencyRecord
                {
                    Key = record.Key,
                    Status = IdempotencyRecordStatus.Failed,
                    CreatedAt = record.CreatedAt,
                    ExpiresAt = record.ExpiresAt,
                    RequestPath = record.RequestPath,
                    RequestMethod = record.RequestMethod,
                    RequestBodyHash = record.RequestBodyHash,
                    CorrelationId = record.CorrelationId
                };

                _records.TryUpdate(key, updatedRecord, record);
                _logger?.LogDebug("[Idempotency] Marked record as failed for key {Key}", key);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IdempotencyRecord?> GetAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_records.TryGetValue(key, out var record) && !record.IsExpired)
            {
                return Task.FromResult<IdempotencyRecord?>(record);
            }

            return Task.FromResult<IdempotencyRecord?>(null);
        }

        /// <inheritdoc />
        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _records.TryRemove(key, out _);
            _logger?.LogDebug("[Idempotency] Removed record for key {Key}", key);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_records.TryGetValue(key, out var record))
            {
                return Task.FromResult(!record.IsExpired);
            }

            return Task.FromResult(false);
        }

        private void CleanupExpiredRecords(object? state)
        {
            var expiredCount = 0;
            var now = DateTimeOffset.UtcNow;

            foreach (var kvp in _records)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    if (_records.TryRemove(kvp.Key, out _))
                    {
                        expiredCount++;
                    }
                }
            }

            if (expiredCount > 0)
            {
                _logger?.LogDebug(
                    "[Idempotency] Cleanup removed {Count} expired records. Remaining: {Remaining}",
                    expiredCount, _records.Count);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer.Dispose();
                _records.Clear();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}

