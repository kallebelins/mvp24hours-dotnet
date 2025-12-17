//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// Status of an idempotency record.
    /// </summary>
    public enum IdempotencyRecordStatus
    {
        /// <summary>
        /// The request is currently being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// The request has completed and the response is cached.
        /// </summary>
        Completed,

        /// <summary>
        /// The request failed and should be retried.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Represents a cached idempotency record containing the response for a previous request.
    /// </summary>
    public class IdempotencyRecord
    {
        /// <summary>
        /// Gets or sets the idempotency key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status of the record.
        /// </summary>
        public IdempotencyRecordStatus Status { get; set; }

        /// <summary>
        /// Gets or sets when the record was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the record expires.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the cached response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public byte[] ResponseBody { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the response content type.
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Gets or sets the cached response headers as JSON.
        /// </summary>
        public string? ResponseHeadersJson { get; set; }

        /// <summary>
        /// Gets or sets the request path.
        /// </summary>
        public string RequestPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the request method.
        /// </summary>
        public string RequestMethod { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a hash of the request body (for validation).
        /// </summary>
        public string? RequestBodyHash { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID associated with this request.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets whether this record has expired.
        /// </summary>
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

        /// <summary>
        /// Gets whether this record is currently processing.
        /// </summary>
        public bool IsProcessing => Status == IdempotencyRecordStatus.Processing;

        /// <summary>
        /// Gets whether this record is completed and can be replayed.
        /// </summary>
        public bool IsCompleted => Status == IdempotencyRecordStatus.Completed;
    }

    /// <summary>
    /// Result of attempting to acquire an idempotency lock.
    /// </summary>
    public class IdempotencyLockResult
    {
        /// <summary>
        /// Gets whether the lock was acquired (new request).
        /// </summary>
        public bool Acquired { get; init; }

        /// <summary>
        /// Gets the existing record if the lock was not acquired.
        /// </summary>
        public IdempotencyRecord? ExistingRecord { get; init; }

        /// <summary>
        /// Gets whether a duplicate request is currently in-flight.
        /// </summary>
        public bool IsInFlight => !Acquired && ExistingRecord?.IsProcessing == true;

        /// <summary>
        /// Gets whether a cached response exists for replay.
        /// </summary>
        public bool HasCachedResponse => !Acquired && ExistingRecord?.IsCompleted == true;

        /// <summary>
        /// Creates a successful lock acquisition result.
        /// </summary>
        public static IdempotencyLockResult Success() => new() { Acquired = true };

        /// <summary>
        /// Creates a result indicating an existing record was found.
        /// </summary>
        public static IdempotencyLockResult Existing(IdempotencyRecord record) => new()
        {
            Acquired = false,
            ExistingRecord = record
        };
    }

    /// <summary>
    /// Storage interface for idempotency records.
    /// Provides atomic operations for managing idempotency state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations must ensure thread-safety and atomic operations,
    /// especially for <see cref="TryAcquireLockAsync"/> which must be atomic
    /// to prevent race conditions.
    /// </para>
    /// <para>
    /// <strong>Implementations:</strong>
    /// <list type="bullet">
    /// <item><see cref="InMemoryIdempotencyStore"/> - Single instance, in-memory storage</item>
    /// <item><see cref="DistributedCacheIdempotencyStore"/> - Distributed via IDistributedCache</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// Attempts to acquire a lock for the given idempotency key.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="requestPath">The request path.</param>
        /// <param name="requestMethod">The HTTP method.</param>
        /// <param name="requestBodyHash">Optional hash of the request body.</param>
        /// <param name="duration">Duration for the lock/cache.</param>
        /// <param name="correlationId">Optional correlation ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Lock result indicating success or existing record.</returns>
        /// <remarks>
        /// This operation must be atomic. If the key doesn't exist, create a
        /// "processing" record and return success. If it exists, return the existing record.
        /// </remarks>
        Task<IdempotencyLockResult> TryAcquireLockAsync(
            string key,
            string requestPath,
            string requestMethod,
            string? requestBodyHash,
            TimeSpan duration,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a request as completed and stores the response.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="responseBody">The response body bytes.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="responseHeadersJson">Optional response headers as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CompleteAsync(
            string key,
            int statusCode,
            byte[] responseBody,
            string contentType,
            string? responseHeadersJson = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a request as failed and releases the lock.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="removeRecord">Whether to remove the record entirely.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Failed requests should generally have their record removed so they can be retried.
        /// Set removeRecord to false if you want to keep the failure state for debugging.
        /// </remarks>
        Task FailAsync(
            string key,
            bool removeRecord = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an existing idempotency record.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The record if found, null otherwise.</returns>
        Task<IdempotencyRecord?> GetAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an idempotency record.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists and is not expired.
        /// </summary>
        /// <param name="key">The idempotency key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<bool> ExistsAsync(
            string key,
            CancellationToken cancellationToken = default);
    }
}

