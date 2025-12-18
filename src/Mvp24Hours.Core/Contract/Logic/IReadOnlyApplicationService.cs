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

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Read-only application service contract for data projection operations.
    /// This interface provides query-only access to entities without any modification capabilities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be queried by this service.</typeparam>
    /// <remarks>
    /// <para>
    /// Use this interface when you need a service that only performs read operations,
    /// such as reporting services, lookup services, or read-side services in CQRS patterns.
    /// </para>
    /// <para>
    /// This interface is a subset of <see cref="IQueryService{TEntity}"/> and enforces
    /// the principle of least privilege by not exposing write operations.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enforces read-only access at the contract level</item>
    /// <item>Supports CQRS patterns where reads are separated from writes</item>
    /// <item>Improves security by limiting service capabilities</item>
    /// <item>Can be used with read replicas or cached data sources</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class ProductCatalogService : IReadOnlyApplicationService&lt;Product&gt;
    /// {
    ///     // Only provides read access to product catalog
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IQueryService{TEntity}"/>
    /// <seealso cref="IApplicationService{TEntity}"/>
    public interface IReadOnlyApplicationService<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Checks whether any records exist in the data source.
        /// </summary>
        /// <returns>A business result indicating whether any records exist.</returns>
        IBusinessResult<bool> ListAny();

        /// <summary>
        /// Gets the total count of records in the data source.
        /// </summary>
        /// <returns>A business result containing the count of records.</returns>
        IBusinessResult<int> ListCount();

        /// <summary>
        /// Gets all entities from the data source.
        /// </summary>
        /// <returns>A business result containing all entities.</returns>
        IBusinessResult<IList<TEntity>> List();

        /// <summary>
        /// Gets all entities from the data source with paging criteria.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing entities matching the criteria.</returns>
        IBusinessResult<IList<TEntity>> List(IPagingCriteria criteria);

        /// <summary>
        /// Checks whether any records match the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <returns>A business result indicating whether any matching records exist.</returns>
        IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets the count of records matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <returns>A business result containing the count of matching records.</returns>
        IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <returns>A business result containing matching entities.</returns>
        IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter with paging criteria.
        /// </summary>
        /// <param name="clause">The filter expression.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing matching entities.</returns>
        IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Gets a single entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>A business result containing the entity, or null if not found.</returns>
        IBusinessResult<TEntity> GetById(object id);

        /// <summary>
        /// Gets a single entity by its identifier with paging criteria (for includes/navigation properties).
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing the entity, or null if not found.</returns>
        IBusinessResult<TEntity> GetById(object id, IPagingCriteria criteria);

        #region [ Specification Pattern Methods ]

        /// <summary>
        /// Checks whether any records matching the specification exist.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A business result indicating whether any matching records exist.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the Specification pattern to encapsulate query logic,
        /// making it reusable and composable.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var result = service.AnyBySpecification(spec);
        /// if (result.Data)
        /// {
        ///     // At least one active customer exists
        /// }
        /// </code>
        /// </example>
        IBusinessResult<bool> AnyBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Gets the count of records matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A business result containing the count of matching records.</returns>
        /// <example>
        /// <code>
        /// var spec = new PremiumCustomerSpecification();
        /// var result = service.CountBySpecification(spec);
        /// Console.WriteLine($"Premium customers: {result.Data}");
        /// </code>
        /// </example>
        IBusinessResult<int> CountBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves entities matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A business result containing entities matching the specification.</returns>
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
        /// var result = service.GetBySpecification(spec);
        /// </code>
        /// </example>
        IBusinessResult<IList<TEntity>> GetBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves a single entity matching the specification, or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A business result containing the entity, or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one entity matches the specification.</exception>
        /// <example>
        /// <code>
        /// var spec = new CustomerByEmailSpecification("john@example.com");
        /// var result = service.GetSingleBySpecification(spec);
        /// </code>
        /// </example>
        IBusinessResult<TEntity?> GetSingleBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves the first entity matching the specification, or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A business result containing the first entity, or null if not found.</returns>
        /// <example>
        /// <code>
        /// var spec = new NewestCustomerSpecification();
        /// var result = service.GetFirstBySpecification(spec);
        /// </code>
        /// </example>
        IBusinessResult<TEntity?> GetFirstBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>;

        #endregion
    }
}

