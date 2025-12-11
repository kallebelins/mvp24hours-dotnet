//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Represents a saga that manages a distributed transaction through a sequence of local transactions.
/// </summary>
/// <typeparam name="TData">The type of data used throughout the saga.</typeparam>
/// <remarks>
/// <para>
/// <strong>Saga Pattern:</strong>
/// A saga is a sequence of local transactions where each transaction updates
/// data within a single service and publishes events to trigger the next step.
/// If a step fails, compensating transactions are executed to undo previous changes.
/// </para>
/// <para>
/// <strong>Saga Lifecycle:</strong>
/// <list type="number">
/// <item>NotStarted - Saga is created</item>
/// <item>Running - Steps are being executed</item>
/// <item>Completed - All steps succeeded</item>
/// <item>Failed - A step failed</item>
/// <item>Compensating - Compensation is in progress</item>
/// <item>Compensated - All compensations completed</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderSaga : SagaBase&lt;OrderSagaData&gt;
/// {
///     public OrderSaga(IServiceProvider serviceProvider) : base(serviceProvider)
///     {
///         ConfigureSteps(steps =>
///         {
///             steps.Add&lt;ReserveStockStep&gt;();
///             steps.Add&lt;ProcessPaymentStep&gt;();
///             steps.Add&lt;ShipOrderStep&gt;();
///         });
///     }
/// }
/// </code>
/// </example>
public interface ISaga<TData> where TData : class
{
    /// <summary>
    /// Gets the unique identifier of this saga instance.
    /// </summary>
    Guid SagaId { get; }

    /// <summary>
    /// Gets the saga data containing the execution context.
    /// </summary>
    TData Data { get; }

    /// <summary>
    /// Gets the current status of the saga.
    /// </summary>
    SagaStatus Status { get; }

    /// <summary>
    /// Gets the index of the current step (0-based).
    /// </summary>
    int CurrentStepIndex { get; }

    /// <summary>
    /// Gets the name of the current step.
    /// </summary>
    string? CurrentStepName { get; }

    /// <summary>
    /// Gets the timestamp when the saga was started.
    /// </summary>
    DateTime? StartedAt { get; }

    /// <summary>
    /// Gets the timestamp when the saga completed or failed.
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Gets any error that occurred during saga execution.
    /// </summary>
    Exception? Error { get; }

    /// <summary>
    /// Gets the list of steps in this saga.
    /// </summary>
    IReadOnlyList<ISagaStep<TData>> Steps { get; }

    /// <summary>
    /// Starts the saga execution with the provided data.
    /// </summary>
    /// <param name="data">The saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga result.</returns>
    Task<SagaResult> StartAsync(TData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a domain event that may trigger saga progression.
    /// </summary>
    /// <param name="event">The domain event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleEventAsync(IDomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes compensation for all completed steps in reverse order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompensateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a saga from a previously saved state.
    /// </summary>
    /// <param name="state">The saved saga state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga result.</returns>
    Task<SagaResult> ResumeAsync(SagaState<TData> state, CancellationToken cancellationToken = default);
}

/// <summary>
/// Non-generic saga interface for type-agnostic operations.
/// </summary>
public interface ISaga
{
    /// <summary>
    /// Gets the unique identifier of this saga instance.
    /// </summary>
    Guid SagaId { get; }

    /// <summary>
    /// Gets the current status of the saga.
    /// </summary>
    SagaStatus Status { get; }

    /// <summary>
    /// Gets the type of data used by this saga.
    /// </summary>
    Type DataType { get; }
}

