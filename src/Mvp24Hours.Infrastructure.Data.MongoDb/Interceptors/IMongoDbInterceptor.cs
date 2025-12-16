//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// Defines interception points for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides hooks for intercepting MongoDB operations before and after
    /// they are executed. Unlike EF Core's SaveChangesInterceptor, MongoDB operations are
    /// performed directly on each Add/Modify/Remove call.
    /// </para>
    /// <para>
    /// Interceptors can be used for:
    /// - Audit logging (tracking who did what and when)
    /// - Soft delete implementation (converting deletes to updates)
    /// - Data validation
    /// - Performance monitoring
    /// - Automatic field population (timestamps, user info)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyInterceptor : IMongoDbInterceptor
    /// {
    ///     public Task OnBeforeInsertAsync&lt;T&gt;(T entity, CancellationToken ct) where T : class, IEntityBase
    ///     {
    ///         Console.WriteLine($"Inserting {typeof(T).Name}");
    ///         return Task.CompletedTask;
    ///     }
    ///     // ... other methods
    /// }
    /// </code>
    /// </example>
    public interface IMongoDbInterceptor
    {
        /// <summary>
        /// Gets the order in which this interceptor should be executed.
        /// Lower values execute first.
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Called before an entity is inserted into the database.
        /// </summary>
        /// <typeparam name="T">The type of entity being inserted.</typeparam>
        /// <param name="entity">The entity being inserted.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Called after an entity has been inserted into the database.
        /// </summary>
        /// <typeparam name="T">The type of entity that was inserted.</typeparam>
        /// <param name="entity">The entity that was inserted.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Called before an entity is updated in the database.
        /// </summary>
        /// <typeparam name="T">The type of entity being updated.</typeparam>
        /// <param name="entity">The entity being updated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Called after an entity has been updated in the database.
        /// </summary>
        /// <typeparam name="T">The type of entity that was updated.</typeparam>
        /// <param name="entity">The entity that was updated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnAfterUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Called before an entity is deleted from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity being deleted.</typeparam>
        /// <param name="entity">The entity being deleted.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A <see cref="DeleteInterceptionResult"/> indicating whether to proceed with
        /// deletion, convert to soft delete, or suppress the operation.
        /// </returns>
        Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Called after an entity has been deleted from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity that was deleted.</typeparam>
        /// <param name="entity">The entity that was deleted.</param>
        /// <param name="wasSoftDeleted">True if the entity was soft deleted instead of physically removed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;
    }

    /// <summary>
    /// Represents the result of delete interception, indicating how the delete operation should proceed.
    /// </summary>
    public readonly struct DeleteInterceptionResult
    {
        /// <summary>
        /// Gets a value indicating whether to proceed with the physical delete.
        /// </summary>
        public bool ShouldProceed { get; }

        /// <summary>
        /// Gets a value indicating whether to convert this to a soft delete operation.
        /// </summary>
        public bool ConvertToSoftDelete { get; }

        /// <summary>
        /// Gets a value indicating whether the operation should be suppressed entirely.
        /// </summary>
        public bool Suppress { get; }

        private DeleteInterceptionResult(bool proceed, bool softDelete, bool suppress)
        {
            ShouldProceed = proceed;
            ConvertToSoftDelete = softDelete;
            Suppress = suppress;
        }

        /// <summary>
        /// Proceed with the physical delete operation.
        /// </summary>
        public static DeleteInterceptionResult Proceed() => new DeleteInterceptionResult(true, false, false);

        /// <summary>
        /// Convert the delete to a soft delete (update with IsDeleted flag).
        /// </summary>
        public static DeleteInterceptionResult SoftDelete() => new DeleteInterceptionResult(false, true, false);

        /// <summary>
        /// Suppress the delete operation entirely (no action taken).
        /// </summary>
        public static DeleteInterceptionResult SuppressOperation() => new DeleteInterceptionResult(false, false, true);
    }
}

