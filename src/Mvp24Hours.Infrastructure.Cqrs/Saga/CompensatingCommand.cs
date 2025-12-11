//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Marker interface for commands that can be compensated.
/// </summary>
/// <remarks>
/// <para>
/// Compensating commands are used in saga patterns to undo the effects
/// of a previously executed command when a subsequent step fails.
/// </para>
/// <para>
/// <strong>Important:</strong>
/// <list type="bullet">
/// <item>Compensation must be idempotent</item>
/// <item>Compensation may not restore exact original state</item>
/// <item>Store enough context during execution to enable compensation</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record ReserveStockCommand : ICompensatableCommand&lt;Guid&gt;
/// {
///     public Guid OrderId { get; init; }
///     public List&lt;StockItem&gt; Items { get; init; } = new();
/// }
/// 
/// public record ReleaseStockCommand : IMediatorCommand
/// {
///     public Guid ReservationId { get; init; }
/// }
/// </code>
/// </example>
public interface ICompensatableCommand<out TResponse> : IMediatorCommand<TResponse>
{
    /// <summary>
    /// Gets the type of command to execute for compensation.
    /// </summary>
    Type CompensationCommandType { get; }
}

/// <summary>
/// Marker interface for compensating commands without return value.
/// </summary>
public interface ICompensatableCommand : ICompensatableCommand<Unit>
{
}

/// <summary>
/// Base record for compensatable commands.
/// </summary>
/// <typeparam name="TCompensation">The type of compensation command.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract record CompensatableCommandBase<TCompensation, TResponse> : ICompensatableCommand<TResponse>
    where TCompensation : IMediatorCommand
{
    /// <inheritdoc />
    public Type CompensationCommandType => typeof(TCompensation);

    /// <summary>
    /// Creates the compensation command using the current state.
    /// Override this to provide the specific compensation command.
    /// </summary>
    /// <param name="response">The response from the original command execution.</param>
    /// <returns>The compensation command to execute.</returns>
    public abstract TCompensation CreateCompensationCommand(TResponse response);
}

/// <summary>
/// Represents a compensating command that undoes a previous action.
/// </summary>
/// <example>
/// <code>
/// public record ReleaseStockCommand : CompensatingCommand
/// {
///     public Guid ReservationId { get; init; }
///     
///     public ReleaseStockCommand(Guid originalCommandId, Guid reservationId) 
///         : base(originalCommandId)
///     {
///         ReservationId = reservationId;
///     }
/// }
/// </code>
/// </example>
public abstract record CompensatingCommand : IMediatorCommand
{
    /// <summary>
    /// Gets the ID of the original command being compensated.
    /// </summary>
    public Guid OriginalCommandId { get; init; }

    /// <summary>
    /// Gets the timestamp when compensation was initiated.
    /// </summary>
    public DateTime CompensationInitiatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the reason for compensation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the saga ID if this compensation is part of a saga.
    /// </summary>
    public Guid? SagaId { get; init; }

    /// <summary>
    /// Initializes a new compensating command.
    /// </summary>
    /// <param name="originalCommandId">The ID of the command to compensate.</param>
    protected CompensatingCommand(Guid originalCommandId)
    {
        OriginalCommandId = originalCommandId;
    }
}

/// <summary>
/// Represents a compensating command with typed response.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract record CompensatingCommand<TResponse> : IMediatorCommand<TResponse>
{
    /// <summary>
    /// Gets the ID of the original command being compensated.
    /// </summary>
    public Guid OriginalCommandId { get; init; }

    /// <summary>
    /// Gets the timestamp when compensation was initiated.
    /// </summary>
    public DateTime CompensationInitiatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the reason for compensation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the saga ID if this compensation is part of a saga.
    /// </summary>
    public Guid? SagaId { get; init; }

    /// <summary>
    /// Initializes a new compensating command.
    /// </summary>
    /// <param name="originalCommandId">The ID of the command to compensate.</param>
    protected CompensatingCommand(Guid originalCommandId)
    {
        OriginalCommandId = originalCommandId;
    }
}

/// <summary>
/// Result of a compensation operation.
/// </summary>
public sealed class CompensationResult
{
    private CompensationResult(bool isSuccess, string? errorMessage = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Gets whether the compensation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if compensation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception if compensation failed.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Creates a successful compensation result.
    /// </summary>
    public static CompensationResult Success() => new(true);

    /// <summary>
    /// Creates a failed compensation result.
    /// </summary>
    public static CompensationResult Failed(string errorMessage, Exception? exception = null) =>
        new(false, errorMessage, exception);
}

/// <summary>
/// Represents a record of a compensatable action for audit and recovery.
/// </summary>
public class CompensationRecord
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original command type.
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original command as JSON.
    /// </summary>
    public string CommandJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response as JSON.
    /// </summary>
    public string? ResponseJson { get; set; }

    /// <summary>
    /// Gets or sets the compensation command type.
    /// </summary>
    public string? CompensationCommandType { get; set; }

    /// <summary>
    /// Gets or sets the compensation command as JSON.
    /// </summary>
    public string? CompensationCommandJson { get; set; }

    /// <summary>
    /// Gets or sets when the command was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets when compensation was executed.
    /// </summary>
    public DateTime? CompensatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether compensation has been executed.
    /// </summary>
    public bool IsCompensated { get; set; }

    /// <summary>
    /// Gets or sets any compensation error.
    /// </summary>
    public string? CompensationError { get; set; }
}

