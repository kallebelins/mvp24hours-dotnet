//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Represents the persisted state of a saga.
/// </summary>
/// <typeparam name="TData">The type of saga data.</typeparam>
public class SagaState<TData> where TData : class
{
    /// <summary>
    /// Gets or sets the unique identifier of the saga.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the saga type name for deserialization.
    /// </summary>
    public string SagaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the saga.
    /// </summary>
    public SagaStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the index of the current step.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Gets or sets the name of the current step.
    /// </summary>
    public string? CurrentStepName { get; set; }

    /// <summary>
    /// Gets or sets the saga data.
    /// </summary>
    public TData Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp when the saga was started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the saga.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the expiration time for the saga state.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the names of steps that have been executed.
    /// </summary>
    public List<string> ExecutedSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages that occurred during execution.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages that occurred during compensation.
    /// </summary>
    public List<string> CompensationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the next retry time for suspended sagas.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the saga.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets whether the saga has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Gets whether the saga has timed out.
    /// </summary>
    public bool IsTimedOut => Timeout.HasValue && DateTime.UtcNow > StartedAt.Add(Timeout.Value);
}

/// <summary>
/// Non-generic saga state for type-agnostic storage.
/// </summary>
public class SagaState
{
    /// <summary>
    /// Gets or sets the unique identifier of the saga.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the saga type name for deserialization.
    /// </summary>
    public string SagaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the saga.
    /// </summary>
    public SagaStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the index of the current step.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Gets or sets the name of the current step.
    /// </summary>
    public string? CurrentStepName { get; set; }

    /// <summary>
    /// Gets or sets the saga data as JSON.
    /// </summary>
    public string DataJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type name for deserialization.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the saga was started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the saga in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the expiration time for the saga state.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the names of steps that have been executed.
    /// </summary>
    public List<string> ExecutedSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages that occurred during execution.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages that occurred during compensation.
    /// </summary>
    public List<string> CompensationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the next retry time for suspended sagas.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the saga.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

