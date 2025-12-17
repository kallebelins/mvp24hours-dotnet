//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Asynchronous read-only application service contract for data projection operations.
    /// This interface provides async query-only access to entities without any modification capabilities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be queried by this service.</typeparam>
    /// <remarks>
    /// <para>
    /// Use this interface when you need a service that only performs async read operations,
    /// such as reporting services, lookup services, or read-side services in CQRS patterns.
    /// </para>
    /// <para>
    /// This interface is a subset of <see cref="IQueryServiceAsync{TEntity}"/> and enforces
    /// the principle of least privilege by not exposing write operations.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enforces read-only access at the contract level</item>
    /// <item>Supports CQRS patterns where reads are separated from writes</item>
    /// <item>Improves security by limiting service capabilities</item>
    /// <item>Can be used with read replicas or cached data sources</item>
    /// <item>Fully asynchronous for better scalability</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class ProductCatalogService : IReadOnlyApplicationServiceAsync&lt;Product&gt;
    /// {
    ///     // Only provides async read access to product catalog
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IQueryServiceAsync{TEntity}"/>
    /// <seealso cref="IApplicationServiceAsync{TEntity}"/>
    public interface IReadOnlyApplicationServiceAsync<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Asynchronously checks whether any records exist in the data source.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result indicating whether any records exist.</returns>
        Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the total count of records in the data source.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the count of records.</returns>
        Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with all entities.</returns>
        Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source with paging criteria.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with entities matching the criteria.</returns>
        Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously checks whether any records match the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result indicating whether any matching records exist.</returns>
        Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the count of records matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the count of matching records.</returns>
        Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities.</returns>
        Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter with paging criteria.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities.</returns>
        Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity, or null if not found.</returns>
        Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier with paging criteria (for includes/navigation properties).
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity, or null if not found.</returns>
        Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    }
}

