//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.Idempotency;

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
    private readonly IDistributedLockFactory? _distributedLockFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string LockSuffix = ":lock";
    private const string DataSuffix = ":data";

    /// <summary>
    /// Creates a new instance of the distributed cache idempotency store.
    /// </summary>
    /// <param name="cache">The distributed cache.</param>
    /// <param name="options">Idempotency options.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="distributedLockFactory">Optional distributed lock factory used for atomic acquisition.</param>
    public DistributedCacheIdempotencyStore(
        IDistributedCache cache,
        IOptions<IdempotencyOptions> options,
        ILogger<DistributedCacheIdempotencyStore>? logger = null,
        IDistributedLockFactory? distributedLockFactory = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? new IdempotencyOptions();
        _distributedLockFactory = distributedLockFactory;
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
        string fullKey = GetFullKey(key);
        string lockKey = fullKey + LockSuffix;
        string dataKey = fullKey + DataSuffix;

        try
        {
            if (_options.EnableAtomicAcquisitionUsingDistributedLock && _distributedLockFactory != null)
            {
                return await TryAcquireWithAtomicDistributedLockAsync(
                    key,
                    requestPath,
                    requestMethod,
                    requestBodyHash,
                    duration,
                    correlationId,
                    fullKey,
                    lockKey,
                    dataKey,
                    cancellationToken);
            }

            // First, check if there's an existing record
            string? existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
            if (!string.IsNullOrEmpty(existingJson))
            {
                IdempotencyRecord? existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
                if (existing != null && !existing.IsExpired)
                {
                    _logger?.LogDebug(
                        "[Idempotency] Found existing record for key {Key}. Status: {Status}",
                        key, existing.Status);

                    return IdempotencyLockResult.Existing(existing);
                }
            }

            // Try to acquire lock by setting a lock key
            string lockValue = Guid.NewGuid().ToString();
            var lockOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            // Note: This is not truly atomic. For production with high concurrency,
            // consider using Redis Lua scripts or RedLock.
            string? existingLock = await _cache.GetStringAsync(lockKey, cancellationToken);
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
            DateTimeOffset now = DateTimeOffset.UtcNow;
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

            string recordJson = JsonSerializer.Serialize(record, _jsonOptions);
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

    private async Task<IdempotencyLockResult> TryAcquireWithAtomicDistributedLockAsync(
        string key,
        string requestPath,
        string requestMethod,
        string? requestBodyHash,
        TimeSpan duration,
        string? correlationId,
        string fullKey,
        string lockKey,
        string dataKey,
        CancellationToken cancellationToken)
    {
        IDistributedLock distributedLock = CreateDistributedLock();
        DistributedLockOptions lockOptions = CreateAcquisitionLockOptions();
        string resource = $"{fullKey}:acquire";

        LockAcquisitionResult lockResult = await distributedLock.TryAcquireAsync(resource, lockOptions, cancellationToken);
        if (!lockResult.IsAcquired || lockResult.LockHandle == null)
        {
            IdempotencyRecord? existingRecord = await ReadExistingRecordAsync(dataKey, cancellationToken);
            if (existingRecord != null)
            {
                return IdempotencyLockResult.Existing(existingRecord);
            }

            return IdempotencyLockResult.Existing(CreateProcessingRecord(key, duration));
        }

        await using (lockResult.LockHandle)
        {
            IdempotencyRecord? existingRecord = await ReadExistingRecordAsync(dataKey, cancellationToken);
            if (existingRecord != null)
            {
                return IdempotencyLockResult.Existing(existingRecord);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
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

            string recordJson = JsonSerializer.Serialize(record, _jsonOptions);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            await _cache.SetStringAsync(lockKey, Guid.NewGuid().ToString(), cacheOptions, cancellationToken);
            await _cache.SetStringAsync(dataKey, recordJson, cacheOptions, cancellationToken);

            _logger?.LogDebug(
                "[Idempotency] Acquired atomic lock for key {Key}. Path: {Path}, Method: {Method}",
                key, requestPath, requestMethod);

            return IdempotencyLockResult.Success();
        }
    }

    private async Task<IdempotencyRecord?> ReadExistingRecordAsync(string dataKey, CancellationToken cancellationToken)
    {
        string? existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
        if (string.IsNullOrEmpty(existingJson))
        {
            return null;
        }

        IdempotencyRecord? existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
        return existing != null && !existing.IsExpired ? existing : null;
    }

    private IDistributedLock CreateDistributedLock()
    {
        if (_distributedLockFactory == null)
        {
            throw new InvalidOperationException("Distributed lock factory is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(_options.DistributedLockProviderName))
        {
            return _distributedLockFactory.Create(_options.DistributedLockProviderName);
        }

        return _distributedLockFactory.Create();
    }

    private DistributedLockOptions CreateAcquisitionLockOptions()
    {
        TimeSpan acquisitionTimeout = _options.DistributedLockAcquisitionTimeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : _options.DistributedLockAcquisitionTimeout;

        TimeSpan lockDuration = _options.DistributedLockDuration <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(10)
            : _options.DistributedLockDuration;

        return new DistributedLockOptions
        {
            AcquisitionTimeout = acquisitionTimeout,
            LockDuration = lockDuration,
            EnableAutoRenewal = false,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            ThrowOnFailure = false
        };
    }

    private static IdempotencyRecord CreateProcessingRecord(string key, TimeSpan duration)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new IdempotencyRecord
        {
            Key = key,
            Status = IdempotencyRecordStatus.Processing,
            CreatedAt = now,
            ExpiresAt = now.Add(duration)
        };
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
        string fullKey = GetFullKey(key);
        string dataKey = fullKey + DataSuffix;
        string lockKey = fullKey + LockSuffix;

        try
        {
            // Get existing record to preserve metadata
            IdempotencyRecord? existing = null;
            string? existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
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

            string recordJson = JsonSerializer.Serialize(record, _jsonOptions);
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
        string fullKey = GetFullKey(key);
        string dataKey = fullKey + DataSuffix;
        string lockKey = fullKey + LockSuffix;

        try
        {
            if (removeRecord)
            {
                await _cache.RemoveAsync(dataKey, cancellationToken);
                _logger?.LogDebug("[Idempotency] Removed failed record for key {Key}", key);
            }
            else
            {
                string? existingJson = await _cache.GetStringAsync(dataKey, cancellationToken);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    IdempotencyRecord? existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson, _jsonOptions);
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

                        string recordJson = JsonSerializer.Serialize(record, _jsonOptions);
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
        string fullKey = GetFullKey(key);
        string dataKey = fullKey + DataSuffix;

        try
        {
            string? json = await _cache.GetStringAsync(dataKey, cancellationToken);
            if (!string.IsNullOrEmpty(json))
            {
                IdempotencyRecord? record = JsonSerializer.Deserialize<IdempotencyRecord>(json, _jsonOptions);
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
        string fullKey = GetFullKey(key);
        string dataKey = fullKey + DataSuffix;
        string lockKey = fullKey + LockSuffix;

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
        IdempotencyRecord? record = await GetAsync(key, cancellationToken);
        return record != null;
    }

    private string GetFullKey(string key)
    {
        return $"{_options.CacheKeyPrefix}{key}";
    }
}

