//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Interface for persisting saga state.
/// Enables saga recovery and monitoring.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Implementations should be:</strong>
/// <list type="bullet">
/// <item>Thread-safe - Multiple sagas may run concurrently</item>
/// <item>Durable - State must survive process restarts</item>
/// <item>Efficient - State updates are frequent during saga execution</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SqlSagaStateStore : ISagaStateStore
/// {
///     private readonly DbContext _dbContext;
///     
///     public async Task SaveAsync(SagaState state, CancellationToken cancellationToken)
///     {
///         _dbContext.SagaStates.Add(state);
///         await _dbContext.SaveChangesAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface ISagaStateStore
{
    /// <summary>
    /// Saves the initial state of a saga.
    /// </summary>
    /// <param name="state">The saga state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(SagaState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the initial state of a typed saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <param name="state">The saga state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync<TData>(SagaState<TData> state, CancellationToken cancellationToken = default) where TData : class;

    /// <summary>
    /// Gets a saga state by ID.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found, null otherwise.</returns>
    Task<SagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed saga state by ID.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found, null otherwise.</returns>
    Task<SagaState<TData>?> GetAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default) where TData : class;

    /// <summary>
    /// Updates an existing saga state.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="update">Action to update the state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid sagaId, Action<SagaState> update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing typed saga state.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="update">Action to update the state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync<TData>(Guid sagaId, Action<SagaState<TData>> update, CancellationToken cancellationToken = default) where TData : class;

    /// <summary>
    /// Deletes a saga state.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sagas with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of saga states.</returns>
    Task<IReadOnlyList<SagaState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sagas that need compensation (failed but not compensated).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of saga states pending compensation.</returns>
    Task<IReadOnlyList<SagaState>> GetPendingCompensationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sagas that have timed out.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of timed out saga states.</returns>
    Task<IReadOnlyList<SagaState>> GetTimedOutSagasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sagas that are ready to retry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of saga states ready for retry.</returns>
    Task<IReadOnlyList<SagaState>> GetReadyForRetryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired saga states that can be cleaned up.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expired saga states.</returns>
    Task<IReadOnlyList<SagaState>> GetExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired saga states.
    /// </summary>
    /// <param name="olderThan">Delete states older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of states deleted.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

