//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Represents a single step in a saga.
/// Each step has an execute action and a compensate action for rollback.
/// </summary>
/// <typeparam name="TData">The type of data used by the saga.</typeparam>
/// <remarks>
/// <para>
/// <strong>Saga Step Lifecycle:</strong>
/// <list type="number">
/// <item>Execute - Performs the main action</item>
/// <item>Compensate - Reverts the action if subsequent steps fail</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong>
/// <list type="bullet">
/// <item>Compensation must be idempotent - safe to execute multiple times</item>
/// <item>Store enough data during execute to enable compensation</item>
/// <item>Compensation is not a true rollback - it's a new operation</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ReserveStockStep : ISagaStep&lt;OrderSagaData&gt;
/// {
///     private readonly IInventoryService _inventoryService;
///     
///     public string Name => "ReserveStock";
///     public bool CanCompensate => true;
///     
///     public async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken)
///     {
///         data.ReservationId = await _inventoryService.ReserveAsync(data.Items, cancellationToken);
///     }
///     
///     public async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken)
///     {
///         if (data.ReservationId.HasValue)
///             await _inventoryService.ReleaseAsync(data.ReservationId.Value, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface ISagaStep<TData> where TData : class
{
    /// <summary>
    /// Gets the name of this step for logging and state tracking.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the order in which this step should be executed (lower values execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets whether this step supports compensation.
    /// If false, the saga may not be fully compensable.
    /// </summary>
    bool CanCompensate { get; }

    /// <summary>
    /// Executes the step's main action.
    /// </summary>
    /// <param name="data">The saga data containing context and state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="SagaStepException">Thrown when the step fails.</exception>
    Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the compensating action to undo the step's effects.
    /// </summary>
    /// <param name="data">The saga data containing context and state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="SagaCompensationException">Thrown when compensation fails.</exception>
    Task CompensateAsync(TData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation of a saga step with default behavior.
/// </summary>
/// <typeparam name="TData">The type of data used by the saga.</typeparam>
public abstract class SagaStepBase<TData> : ISagaStep<TData> where TData : class
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual int Order => 0;

    /// <inheritdoc />
    public virtual bool CanCompensate => true;

    /// <inheritdoc />
    public abstract Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task CompensateAsync(TData data, CancellationToken cancellationToken = default)
    {
        // Default implementation does nothing - override for actual compensation
        return Task.CompletedTask;
    }
}

