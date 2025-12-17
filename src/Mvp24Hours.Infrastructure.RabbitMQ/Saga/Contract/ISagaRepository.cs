//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract
{
    /// <summary>
    /// Repository interface for persisting and retrieving saga instances.
    /// Provides saga state storage for the saga state machine pattern.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// Implementations can use various backends:
    /// <list type="bullet">
    /// <item>Redis - Fast, ideal for high-throughput scenarios</item>
    /// <item>SQL Server/PostgreSQL - Durable, supports complex queries</item>
    /// <item>MongoDB - Flexible schema, good for document-based sagas</item>
    /// <item>In-Memory - For testing and development</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ISagaRepository<TData> where TData : class, new()
    {
        /// <summary>
        /// Finds a saga instance by its correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saga instance or null if not found.</returns>
        Task<SagaInstance<TData>?> FindAsync(Guid correlationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new saga instance.
        /// </summary>
        /// <param name="correlationId">The correlation ID for the new saga.</param>
        /// <param name="initialState">The initial state of the saga.</param>
        /// <param name="initialData">The initial saga data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created saga instance.</returns>
        Task<SagaInstance<TData>> CreateAsync(
            Guid correlationId,
            string initialState,
            TData? initialData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves changes to a saga instance.
        /// </summary>
        /// <param name="instance">The saga instance to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveAsync(SagaInstance<TData> instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a saga instance.
        /// </summary>
        /// <param name="correlationId">The correlation ID of the saga to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the saga was deleted, false if not found.</returns>
        Task<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds all sagas in a specific state.
        /// </summary>
        /// <param name="state">The state to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of saga instances in the specified state.</returns>
        Task<IReadOnlyList<SagaInstance<TData>>> FindByStateAsync(string state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds sagas that have timed out.
        /// </summary>
        /// <param name="timeoutThreshold">The threshold for considering a saga timed out.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of timed out saga instances.</returns>
        Task<IReadOnlyList<SagaInstance<TData>>> FindTimedOutAsync(TimeSpan timeoutThreshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds faulted sagas that may need compensation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of faulted saga instances.</returns>
        Task<IReadOnlyList<SagaInstance<TData>>> FindFaultedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up completed or old sagas.
        /// </summary>
        /// <param name="olderThan">Delete sagas older than this timespan.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of sagas deleted.</returns>
        Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically updates a saga instance with optimistic concurrency.
        /// </summary>
        /// <param name="correlationId">The correlation ID.</param>
        /// <param name="expectedVersion">The expected version for concurrency check.</param>
        /// <param name="update">Action to update the saga.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if update succeeded, false if version mismatch.</returns>
        Task<bool> UpdateAsync(
            Guid correlationId,
            int expectedVersion,
            Action<SagaInstance<TData>> update,
            CancellationToken cancellationToken = default);
    }
}

