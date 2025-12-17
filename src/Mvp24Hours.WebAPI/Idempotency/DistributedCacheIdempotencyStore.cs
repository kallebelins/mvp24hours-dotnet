//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// Distributed cache implementation of idempotency storage.
    /// Uses <see cref="IDistributedCache"/> for storage, supporting Redis, SQL Server, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation is suitable for production environments with multiple
    /// application instances. It ensures consistent idempotency across all instances.
    /// </para>
    /// <para>
    /// <strong>Supported Backends:</strong>
    /// <list type="bullet">
    /// <item>Redis (recommended for production)</item>
    /// <item>SQL Server Distributed Cache</item>
    /// <item>NCache</item>
    /// <item>Any IDistributedCache implementation</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> The atomic lock acquisition uses a simple check-then-set
    /// pattern. For high-concurrency scenarios with strict requirements, consider
    /// using Redis with Lua scripts or RedLock algorithm.
    /// </para>
    /// </remarks>
    public class DistributedCacheIdempotencyStore : IIdempotencyStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheIdempotencyStore>? _logger;
        private readonly IdempotencyOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string LockSuffix = ":lock";
        private const string DataSuffix = ":data";

        /// <summary>
        /// Creates a new instance of the distributed cache idempotency store.
        /// </summary>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="options">Idempotency options.</param>
        /// <param name="logger">Optional logger.</param>
        public DistributedCacheIdempotencyStore(
            IDistributedCache cache,
            IOptions<IdempotencyOptions> options,
            ILogger<DistributedCacheIdempotencyStore>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options?.Value ?? new IdempotencyOptions();
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public async Task<IdempotencyLockResult> TryAcquireLockAsync(
            string key,
            string requestPath,
            string requestMethod,
            string? requestBodyHash,
            TimeSpan duration,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            var lockKey = fullKey + LockSuffix;
            var dataKey = fullKey + DataSuffix;

            try
            {
                // First, check if there's an existing record
                var existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    var existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
                    if (existing != null && !existing.IsExpired)
                    {
                        _logger?.LogDebug(
                            "[Idempotency] Found existing record for key {Key}. Status: {Status}",
                            key, existing.Status);

                        return IdempotencyLockResult.Existing(existing);
                    }
                }

                // Try to acquire lock by setting a lock key
                var lockValue = Guid.NewGuid().ToString();
                var lockOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                };

                // Note: This is not truly atomic. For production with high concurrency,
                // consider using Redis Lua scripts or RedLock.
                var existingLock = await _cache.GetStringAsync(lockKey, cancellationToken);
                if (!string.IsNullOrEmpty(existingLock))
                {
                    // Lock exists, another request is processing
                    var processingRecord = new IdempotencyRecord
                    {
                        Key = key,
                        Status = IdempotencyRecordStatus.Processing,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ExpiresAt = DateTimeOffset.UtcNow.Add(duration)
                    };
                    return IdempotencyLockResult.Existing(processingRecord);
                }

                // Set the lock
                await _cache.SetStringAsync(lockKey, lockValue, lockOptions, cancellationToken);

                // Create the processing record
                var now = DateTimeOffset.UtcNow;
                var record = new IdempotencyRecord
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

                var recordJson = JsonSerializer.Serialize(record, _jsonOptions);
                await _cache.SetStringAsync(dataKey, recordJson, lockOptions, cancellationToken);

                _logger?.LogDebug(
                    "[Idempotency] Acquired lock for key {Key}. Path: {Path}, Method: {Method}",
                    key, requestPath, requestMethod);

                return IdempotencyLockResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[Idempotency] Error acquiring lock for key {Key}: {Message}",
                    key, ex.Message);

                // On error, allow the request to proceed (fail open)
                return IdempotencyLockResult.Success();
            }
        }

        /// <inheritdoc />
        public async Task CompleteAsync(
            string key,
            int statusCode,
            byte[] responseBody,
            string contentType,
            string? responseHeadersJson = null,
            CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            var dataKey = fullKey + DataSuffix;
            var lockKey = fullKey + LockSuffix;

            try
            {
                // Get existing record to preserve metadata
                IdempotencyRecord? existing = null;
                var existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
                }

                var record = new IdempotencyRecord
                {
                    Key = key,
                    Status = IdempotencyRecordStatus.Completed,
                    CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow,
                    ExpiresAt = existing?.ExpiresAt ?? DateTimeOffset.UtcNow.Add(_options.CacheDuration),
                    StatusCode = statusCode,
                    ResponseBody = responseBody,
                    ContentType = contentType,
                    ResponseHeadersJson = responseHeadersJson,
                    RequestPath = existing?.RequestPath ?? string.Empty,
                    RequestMethod = existing?.RequestMethod ?? string.Empty,
                    RequestBodyHash = existing?.RequestBodyHash,
                    CorrelationId = existing?.CorrelationId
                };

                var recordJson = JsonSerializer.Serialize(record, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = record.ExpiresAt
                };

                await _cache.SetStringAsync(dataKey, recordJson, options, cancellationToken);

                // Release the lock
                await _cache.RemoveAsync(lockKey, cancellationToken);

                _logger?.LogDebug(
                    "[Idempotency] Completed record for key {Key}. StatusCode: {StatusCode}, BodySize: {BodySize}",
                    key, statusCode, responseBody.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[Idempotency] Error completing record for key {Key}: {Message}",
                    key, ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task FailAsync(
            string key,
            bool removeRecord = true,
            CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            var dataKey = fullKey + DataSuffix;
            var lockKey = fullKey + LockSuffix;

            try
            {
                if (removeRecord)
                {
                    await _cache.RemoveAsync(dataKey, cancellationToken);
                    _logger?.LogDebug("[Idempotency] Removed failed record for key {Key}", key);
                }
                else
                {
                    var existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
                    if (!string.IsNullOrEmpty(existingJson))
                    {
                        var existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
                        if (existing != null)
                        {
                            var record = new IdempotencyRecord
                            {
                                Key = existing.Key,
                                Status = IdempotencyRecordStatus.Failed,
                                CreatedAt = existing.CreatedAt,
                                ExpiresAt = existing.ExpiresAt,
                                RequestPath = existing.RequestPath,
                                RequestMethod = existing.RequestMethod,
                                RequestBodyHash = existing.RequestBodyHash,
                                CorrelationId = existing.CorrelationId
                            };

                            var recordJson = JsonSerializer.Serialize(record, _jsonOptions);
                            var options = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration = record.ExpiresAt
                            };

                            await _cache.SetStringAsync(dataKey, recordJson, options, cancellationToken);
                            _logger?.LogDebug("[Idempotency] Marked record as failed for key {Key}", key);
                        }
                    }
                }

                // Always release the lock
                await _cache.RemoveAsync(lockKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[Idempotency] Error failing record for key {Key}: {Message}",
                    key, ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<IdempotencyRecord?> GetAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            var dataKey = fullKey + DataSuffix;

            try
            {
                var json = await _cache.GetStringAsync(dataKey, cancellationToken);
                if (!string.IsNullOrEmpty(json))
                {
                    var record = JsonSerializer.Deserialize<IdempotencyRecord>(json, _jsonOptions);
                    if (record != null && !record.IsExpired)
                    {
                        return record;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[Idempotency] Error getting record for key {Key}: {Message}",
                    key, ex.Message);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            var dataKey = fullKey + DataSuffix;
            var lockKey = fullKey + LockSuffix;

            try
            {
                await _cache.RemoveAsync(dataKey, cancellationToken);
                await _cache.RemoveAsync(lockKey, cancellationToken);
                _logger?.LogDebug("[Idempotency] Removed record for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[Idempotency] Error removing record for key {Key}: {Message}",
                    key, ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var record = await GetAsync(key, cancellationToken);
            return record != null;
        }

        private string GetFullKey(string key)
        {
            return $"{_options.CacheKeyPrefix}{key}";
        }
    }
}

