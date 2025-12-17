//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents a failed operation stored in the dead letter queue.
    /// </summary>
    public class DeadLetterOperation
    {
        /// <summary>
        /// Gets or sets the unique identifier for this dead letter.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name/type of the operation that failed.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the operation.
        /// </summary>
        public Type? OperationType { get; set; }

        /// <summary>
        /// Gets or sets the pipeline message at the time of failure.
        /// </summary>
        public IPipelineMessage? Message { get; set; }

        /// <summary>
        /// Gets or sets the serialized message content for persistence.
        /// </summary>
        public string? SerializedMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception that caused the failure.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the serialized exception for persistence.
        /// </summary>
        public string? SerializedException { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the operation failed.
        /// </summary>
        public DateTimeOffset FailedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the number of retry attempts before being dead-lettered.
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the reason for dead-lettering.
        /// </summary>
        public DeadLetterReason Reason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the failure.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of times this operation has been reprocessed.
        /// </summary>
        public int ReprocessCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last reprocess attempt.
        /// </summary>
        public DateTimeOffset? LastReprocessAt { get; set; }

        /// <summary>
        /// Gets or sets whether this dead letter has been acknowledged/resolved.
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this dead letter was acknowledged.
        /// </summary>
        public DateTimeOffset? AcknowledgedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who acknowledged this dead letter.
        /// </summary>
        public string? AcknowledgedBy { get; set; }
    }

    /// <summary>
    /// Reasons why an operation was sent to the dead letter queue.
    /// </summary>
    public enum DeadLetterReason
    {
        /// <summary>
        /// Maximum retry attempts exceeded.
        /// </summary>
        MaxRetriesExceeded,

        /// <summary>
        /// Non-retryable exception occurred.
        /// </summary>
        NonRetryableException,

        /// <summary>
        /// Circuit breaker tripped.
        /// </summary>
        CircuitBreakerOpen,

        /// <summary>
        /// Bulkhead rejected the operation.
        /// </summary>
        BulkheadRejected,

        /// <summary>
        /// Fallback also failed.
        /// </summary>
        FallbackFailed,

        /// <summary>
        /// Operation timed out.
        /// </summary>
        Timeout,

        /// <summary>
        /// Manual intervention required.
        /// </summary>
        ManualIntervention,

        /// <summary>
        /// Unknown or unspecified reason.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Interface for storing and retrieving failed operations.
    /// </summary>
    public interface IDeadLetterStore
    {
        /// <summary>
        /// Stores a failed operation in the dead letter queue.
        /// </summary>
        /// <param name="deadLetter">The dead letter operation to store.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreAsync(DeadLetterOperation deadLetter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a dead letter by ID.
        /// </summary>
        /// <param name="id">The dead letter ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The dead letter operation, or null if not found.</returns>
        Task<DeadLetterOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all dead letters, optionally filtered.
        /// </summary>
        /// <param name="operationName">Optional: filter by operation name.</param>
        /// <param name="reason">Optional: filter by dead letter reason.</param>
        /// <param name="fromDate">Optional: filter by date range start.</param>
        /// <param name="toDate">Optional: filter by date range end.</param>
        /// <param name="includeAcknowledged">Whether to include acknowledged dead letters.</param>
        /// <param name="skip">Number of items to skip (for pagination).</param>
        /// <param name="take">Number of items to take (for pagination).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of dead letter operations.</returns>
        Task<IReadOnlyList<DeadLetterOperation>> GetAllAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            DateTimeOffset? fromDate = null,
            DateTimeOffset? toDate = null,
            bool includeAcknowledged = false,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of dead letters, optionally filtered.
        /// </summary>
        /// <param name="operationName">Optional: filter by operation name.</param>
        /// <param name="reason">Optional: filter by dead letter reason.</param>
        /// <param name="includeAcknowledged">Whether to include acknowledged dead letters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of dead letters.</returns>
        Task<long> GetCountAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            bool includeAcknowledged = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges a dead letter (marks it as resolved).
        /// </summary>
        /// <param name="id">The dead letter ID.</param>
        /// <param name="acknowledgedBy">The user acknowledging the dead letter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the dead letter was found and acknowledged.</returns>
        Task<bool> AcknowledgeAsync(Guid id, string? acknowledgedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a dead letter as reprocessed.
        /// </summary>
        /// <param name="id">The dead letter ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the dead letter was found and updated.</returns>
        Task<bool> MarkReprocessedAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a dead letter.
        /// </summary>
        /// <param name="id">The dead letter ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the dead letter was found and deleted.</returns>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all acknowledged dead letters older than the specified age.
        /// </summary>
        /// <param name="olderThan">The maximum age of dead letters to keep.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of dead letters deleted.</returns>
        Task<int> PurgeAcknowledgedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dead letters ready for reprocessing.
        /// </summary>
        /// <param name="maxReprocessAttempts">Maximum number of reprocess attempts.</param>
        /// <param name="take">Maximum number of items to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of dead letter operations eligible for reprocessing.</returns>
        Task<IReadOnlyList<DeadLetterOperation>> GetForReprocessingAsync(
            int maxReprocessAttempts = 3,
            int take = 100,
            CancellationToken cancellationToken = default);
    }
}

