//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
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

        #region [ Specification Pattern Methods ]

        /// <summary>
        /// Asynchronously checks whether any records matching the specification exist.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result indicating whether any matching records exist.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the Specification pattern to encapsulate query logic,
        /// making it reusable and composable.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var result = await service.AnyBySpecificationAsync(spec);
        /// if (result.Data)
        /// {
        ///     // At least one active customer exists
        /// }
        /// </code>
        /// </example>
        Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Asynchronously gets the count of records matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the count of matching records.</returns>
        /// <example>
        /// <code>
        /// var spec = new PremiumCustomerSpecification();
        /// var result = await service.CountBySpecificationAsync(spec);
        /// Console.WriteLine($"Premium customers: {result.Data}");
        /// </code>
        /// </example>
        Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Asynchronously retrieves entities matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with entities matching the specification.</returns>
        /// <remarks>
        /// <para>
        /// The specification can include:
        /// <list type="bullet">
        /// <item>Filtering criteria via the expression</item>
        /// <item>Navigation properties to include (if enhanced specification)</item>
        /// <item>Ordering (if enhanced specification)</item>
        /// <item>Pagination (if enhanced specification)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomersWithOrdersSpecification();
        /// var result = await service.GetBySpecificationAsync(spec);
        /// </code>
        /// </example>
        Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Asynchronously retrieves a single entity matching the specification, or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity, or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one entity matches the specification.</exception>
        /// <example>
        /// <code>
        /// var spec = new CustomerByEmailSpecification("john@example.com");
        /// var result = await service.GetSingleBySpecificationAsync(spec);
        /// </code>
        /// </example>
        Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Asynchronously retrieves the first entity matching the specification, or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the first entity, or null if not found.</returns>
        /// <example>
        /// <code>
        /// var spec = new NewestCustomerSpecification();
        /// var result = await service.GetFirstBySpecificationAsync(spec);
        /// </code>
        /// </example>
        Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>;

        #endregion
    }
}

