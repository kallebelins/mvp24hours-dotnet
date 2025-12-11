//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Orchestrator for managing saga execution and lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator is responsible for:
/// <list type="bullet">
/// <item>Starting new sagas</item>
/// <item>Managing saga state persistence</item>
/// <item>Handling saga recovery and retry</item>
/// <item>Processing timeouts and compensations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderController
/// {
///     private readonly ISagaOrchestrator _orchestrator;
///     
///     public async Task&lt;IActionResult&gt; CreateOrder(CreateOrderRequest request)
///     {
///         var data = new OrderSagaData { Items = request.Items };
///         var result = await _orchestrator.ExecuteAsync&lt;OrderSaga, OrderSagaData&gt;(data);
///         
///         if (result.IsSuccess)
///             return Ok(new { SagaId = result.SagaId });
///         
///         return BadRequest(result.ErrorMessage);
///     }
/// }
/// </code>
/// </example>
public interface ISagaOrchestrator
{
    /// <summary>
    /// Executes a saga with the provided data.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="data">The saga data.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga execution result.</returns>
    Task<SagaResult<TData>> ExecuteAsync<TSaga, TData>(
        TData data,
        SagaExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class;

    /// <summary>
    /// Resumes a previously suspended or failed saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga execution result.</returns>
    Task<SagaResult<TData>> ResumeAsync<TSaga, TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class;

    /// <summary>
    /// Compensates a failed saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga result after compensation.</returns>
    Task<SagaResult> CompensateAsync<TSaga, TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default)
        where TSaga : ISaga<TData>
        where TData : class;

    /// <summary>
    /// Gets the current status of a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found.</returns>
    Task<SagaState?> GetStatusAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="compensate">Whether to compensate completed steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga result.</returns>
    Task<SagaResult> CancelAsync(Guid sagaId, bool compensate = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all sagas that are ready for retry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sagas processed.</returns>
    Task<int> ProcessRetryQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all timed out sagas.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sagas processed.</returns>
    Task<int> ProcessTimeoutsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired saga states.
    /// </summary>
    /// <param name="olderThan">Clean up states older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of states cleaned up.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for saga execution.
/// </summary>
public class SagaExecutionOptions
{
    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the saga.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the expiration time for the saga state.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether to persist saga state.
    /// Default is true.
    /// </summary>
    public bool PersistState { get; set; } = true;

    /// <summary>
    /// Gets or sets custom metadata for the saga.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

