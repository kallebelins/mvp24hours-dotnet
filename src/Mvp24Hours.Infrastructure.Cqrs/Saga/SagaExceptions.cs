//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Base exception for saga-related errors.
/// </summary>
public class SagaException : Exception
{
    /// <summary>
    /// Gets the saga identifier.
    /// </summary>
    public Guid SagaId { get; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaException"/> class.
    /// </summary>
    public SagaException(Guid sagaId, string message, string errorCode = "SAGA_ERROR")
        : base(message)
    {
        SagaId = sagaId;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaException"/> class.
    /// </summary>
    public SagaException(Guid sagaId, string message, Exception innerException, string errorCode = "SAGA_ERROR")
        : base(message, innerException)
    {
        SagaId = sagaId;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a saga step fails.
/// </summary>
public class SagaStepException : SagaException
{
    /// <summary>
    /// Gets the name of the failed step.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    /// Gets the index of the failed step.
    /// </summary>
    public int StepIndex { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStepException"/> class.
    /// </summary>
    public SagaStepException(Guid sagaId, string stepName, int stepIndex, string message)
        : base(sagaId, message, "SAGA_STEP_FAILED")
    {
        StepName = stepName;
        StepIndex = stepIndex;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStepException"/> class.
    /// </summary>
    public SagaStepException(Guid sagaId, string stepName, int stepIndex, string message, Exception innerException)
        : base(sagaId, message, innerException, "SAGA_STEP_FAILED")
    {
        StepName = stepName;
        StepIndex = stepIndex;
    }
}

/// <summary>
/// Exception thrown when saga compensation fails.
/// </summary>
public class SagaCompensationException : SagaException
{
    /// <summary>
    /// Gets the steps that failed compensation.
    /// </summary>
    public IReadOnlyList<string> FailedSteps { get; }

    /// <summary>
    /// Gets the compensation errors for each failed step.
    /// </summary>
    public IReadOnlyList<Exception> CompensationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaCompensationException"/> class.
    /// </summary>
    public SagaCompensationException(Guid sagaId, IEnumerable<string> failedSteps, IEnumerable<Exception> errors)
        : base(sagaId, $"Compensation failed for steps: {string.Join(", ", failedSteps)}", "SAGA_COMPENSATION_FAILED")
    {
        FailedSteps = failedSteps.ToList().AsReadOnly();
        CompensationErrors = errors.ToList().AsReadOnly();
    }
}

/// <summary>
/// Exception thrown when a saga times out.
/// </summary>
public class SagaTimeoutException : SagaException
{
    /// <summary>
    /// Gets the timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the step where timeout occurred.
    /// </summary>
    public string? StepName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTimeoutException"/> class.
    /// </summary>
    public SagaTimeoutException(Guid sagaId, TimeSpan timeout, string? stepName = null)
        : base(sagaId, $"Saga timed out after {timeout.TotalSeconds} seconds" +
            (stepName != null ? $" at step '{stepName}'" : ""), "SAGA_TIMEOUT")
    {
        Timeout = timeout;
        StepName = stepName;
    }
}

/// <summary>
/// Exception thrown when a saga is not found.
/// </summary>
public class SagaNotFoundException : SagaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SagaNotFoundException"/> class.
    /// </summary>
    public SagaNotFoundException(Guid sagaId)
        : base(sagaId, $"Saga with ID '{sagaId}' was not found", "SAGA_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a saga is in an invalid state for the requested operation.
/// </summary>
public class SagaInvalidStateException : SagaException
{
    /// <summary>
    /// Gets the current status of the saga.
    /// </summary>
    public SagaStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the expected status for the operation.
    /// </summary>
    public SagaStatus ExpectedStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaInvalidStateException"/> class.
    /// </summary>
    public SagaInvalidStateException(Guid sagaId, SagaStatus currentStatus, SagaStatus expectedStatus)
        : base(sagaId, $"Saga is in state '{currentStatus}' but expected '{expectedStatus}'", "SAGA_INVALID_STATE")
    {
        CurrentStatus = currentStatus;
        ExpectedStatus = expectedStatus;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaInvalidStateException"/> class.
    /// </summary>
    public SagaInvalidStateException(Guid sagaId, SagaStatus currentStatus, string message)
        : base(sagaId, message, "SAGA_INVALID_STATE")
    {
        CurrentStatus = currentStatus;
        ExpectedStatus = currentStatus;
    }
}

/// <summary>
/// Exception thrown when maximum retry attempts are exceeded.
/// </summary>
public class SagaMaxRetriesExceededException : SagaException
{
    /// <summary>
    /// Gets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the step where retries were exhausted.
    /// </summary>
    public string? StepName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaMaxRetriesExceededException"/> class.
    /// </summary>
    public SagaMaxRetriesExceededException(Guid sagaId, int maxRetries, string? stepName = null, Exception? innerException = null)
        : base(sagaId, $"Maximum retry attempts ({maxRetries}) exceeded" +
            (stepName != null ? $" for step '{stepName}'" : ""),
            innerException!, "SAGA_MAX_RETRIES_EXCEEDED")
    {
        MaxRetries = maxRetries;
        StepName = stepName;
    }
}

