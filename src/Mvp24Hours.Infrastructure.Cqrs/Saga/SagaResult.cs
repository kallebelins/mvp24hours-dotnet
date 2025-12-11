//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Represents the result of a saga execution.
/// </summary>
public sealed class SagaResult
{
    private SagaResult(Guid sagaId, bool isSuccess, SagaStatus status, string? errorMessage = null, Exception? exception = null)
    {
        SagaId = sagaId;
        IsSuccess = isSuccess;
        Status = status;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Gets the unique identifier of the saga.
    /// </summary>
    public Guid SagaId { get; }

    /// <summary>
    /// Gets whether the saga completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the final status of the saga.
    /// </summary>
    public SagaStatus Status { get; }

    /// <summary>
    /// Gets the error message if the saga failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets whether the saga was compensated after failure.
    /// </summary>
    public bool WasCompensated => Status is SagaStatus.Compensated or SagaStatus.PartiallyCompensated;

    /// <summary>
    /// Creates a successful saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <returns>A successful saga result.</returns>
    public static SagaResult Success(Guid sagaId) =>
        new(sagaId, true, SagaStatus.Completed);

    /// <summary>
    /// Creates a failed saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed saga result.</returns>
    public static SagaResult Failed(Guid sagaId, string errorMessage, Exception? exception = null) =>
        new(sagaId, false, SagaStatus.Failed, errorMessage, exception);

    /// <summary>
    /// Creates a compensated saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="errorMessage">The original error message.</param>
    /// <returns>A compensated saga result.</returns>
    public static SagaResult Compensated(Guid sagaId, string errorMessage) =>
        new(sagaId, false, SagaStatus.Compensated, errorMessage);

    /// <summary>
    /// Creates a partially compensated saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="errorMessage">The error message including compensation failures.</param>
    /// <returns>A partially compensated saga result.</returns>
    public static SagaResult PartiallyCompensated(Guid sagaId, string errorMessage) =>
        new(sagaId, false, SagaStatus.PartiallyCompensated, errorMessage);

    /// <summary>
    /// Creates a timed out saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <returns>A timed out saga result.</returns>
    public static SagaResult TimedOut(Guid sagaId) =>
        new(sagaId, false, SagaStatus.TimedOut, "Saga timed out");

    /// <summary>
    /// Creates a cancelled saga result.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <returns>A cancelled saga result.</returns>
    public static SagaResult Cancelled(Guid sagaId) =>
        new(sagaId, false, SagaStatus.Cancelled, "Saga was cancelled");
}

/// <summary>
/// Represents the result of a saga execution with typed data.
/// </summary>
/// <typeparam name="TData">The type of saga data.</typeparam>
public sealed class SagaResult<TData> where TData : class
{
    private SagaResult(Guid sagaId, bool isSuccess, SagaStatus status, TData? data = null, string? errorMessage = null, Exception? exception = null)
    {
        SagaId = sagaId;
        IsSuccess = isSuccess;
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Gets the unique identifier of the saga.
    /// </summary>
    public Guid SagaId { get; }

    /// <summary>
    /// Gets whether the saga completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the final status of the saga.
    /// </summary>
    public SagaStatus Status { get; }

    /// <summary>
    /// Gets the saga data (may contain partial data if failed).
    /// </summary>
    public TData? Data { get; }

    /// <summary>
    /// Gets the error message if the saga failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets whether the saga was compensated after failure.
    /// </summary>
    public bool WasCompensated => Status is SagaStatus.Compensated or SagaStatus.PartiallyCompensated;

    /// <summary>
    /// Creates a successful saga result.
    /// </summary>
    public static SagaResult<TData> Success(Guid sagaId, TData data) =>
        new(sagaId, true, SagaStatus.Completed, data);

    /// <summary>
    /// Creates a failed saga result.
    /// </summary>
    public static SagaResult<TData> Failed(Guid sagaId, string errorMessage, TData? data = null, Exception? exception = null) =>
        new(sagaId, false, SagaStatus.Failed, data, errorMessage, exception);

    /// <summary>
    /// Creates a compensated saga result.
    /// </summary>
    public static SagaResult<TData> Compensated(Guid sagaId, string errorMessage, TData? data = null) =>
        new(sagaId, false, SagaStatus.Compensated, data, errorMessage);

    /// <summary>
    /// Implicit conversion to non-generic SagaResult.
    /// </summary>
    public static implicit operator SagaResult(SagaResult<TData> result) =>
        result.IsSuccess
            ? SagaResult.Success(result.SagaId)
            : SagaResult.Failed(result.SagaId, result.ErrorMessage ?? "Unknown error", result.Exception);
}

