//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Read-only repository interface that provides query-only access to entities.
    /// Does not include any command methods (Add, Modify, Remove).
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that implements <see cref="IEntityBase"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// Use this interface when you need to enforce a read-only access pattern,
    /// such as in CQRS query handlers or reporting scenarios.
    /// </para>
    /// <para>
    /// This interface extends the query capabilities with Specification Pattern support,
    /// allowing for more expressive and reusable queries.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerQueryHandler : IQueryHandler&lt;GetActiveCustomersQuery, IList&lt;Customer&gt;&gt;
    /// {
    ///     private readonly IReadOnlyRepository&lt;Customer&gt; _repository;
    ///     
    ///     public async Task&lt;IList&lt;Customer&gt;&gt; HandleAsync(GetActiveCustomersQuery query)
    ///     {
    ///         var spec = new ActiveCustomerSpecification();
    ///         return _repository.GetBySpecification(spec);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IReadOnlyRepository<TEntity> : IQuery<TEntity>, IQueryRelation<TEntity>
        where TEntity : IEntityBase
    {
        #region [ Specification Pattern Methods ]

        /// <summary>
        /// Checks whether any records matching the specification exist in the data store.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns><c>true</c> if at least one record matches the specification; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is optimized to return as soon as a matching record is found,
        /// without loading all matching records into memory.
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// if (repository.AnyBySpecification(spec))
        /// {
        ///     // At least one active customer exists
        /// }
        /// </code>
        /// </example>
        bool AnyBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Gets the count of records matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>The number of records matching the specification.</returns>
        /// <remarks>
        /// This method counts matching records without loading them into memory,
        /// making it efficient for large datasets.
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new PremiumCustomerSpecification();
        /// int premiumCount = repository.CountBySpecification(spec);
        /// Console.WriteLine($"Premium customers: {premiumCount}");
        /// </code>
        /// </example>
        int CountBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves entities matching the specification.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A list of entities matching the specification.</returns>
        /// <remarks>
        /// <para>
        /// The specification can include:
        /// <list type="bullet">
        /// <item>Filtering criteria via <see cref="ISpecificationQuery{T}.IsSatisfiedByExpression"/></item>
        /// <item>Navigation properties to include (if using <see cref="ISpecificationQueryEnhanced{T}"/>)</item>
        /// <item>Ordering (if using <see cref="ISpecificationQueryEnhanced{T}"/>)</item>
        /// <item>Pagination (if using <see cref="ISpecificationQueryEnhanced{T}"/>)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomersWithOrdersSpecification();
        /// var customers = repository.GetBySpecification(spec);
        /// </code>
        /// </example>
        IList<TEntity> GetBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves a single entity matching the specification or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>The entity matching the specification, or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one entity matches the specification.</exception>
        /// <example>
        /// <code>
        /// var spec = new CustomerByEmailSpecification("john@example.com");
        /// var customer = repository.GetSingleBySpecification(spec);
        /// </code>
        /// </example>
        TEntity? GetSingleBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves the first entity matching the specification or null if not found.
        /// </summary>
        /// <typeparam name="TSpec">The specification type that implements <see cref="ISpecificationQuery{TEntity}"/>.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>The first entity matching the specification, or null if not found.</returns>
        /// <example>
        /// <code>
        /// var spec = new NewestCustomerSpecification();
        /// var customer = repository.GetFirstBySpecification(spec);
        /// </code>
        /// </example>
        TEntity? GetFirstBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<TEntity>;

        #endregion

        #region [ Keyset Pagination (Cursor-based) ]

        /// <summary>
        /// Retrieves entities using keyset pagination (cursor-based pagination).
        /// More efficient than offset pagination for large datasets.
        /// </summary>
        /// <typeparam name="TKey">The type of the key used for pagination.</typeparam>
        /// <param name="clause">Optional filter expression.</param>
        /// <param name="keySelector">Expression to select the ordering key.</param>
        /// <param name="lastKey">The last key from the previous page (null for first page).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="ascending">Whether to order ascending (true) or descending (false).</param>
        /// <returns>A keyset page result containing the entities and pagination metadata.</returns>
        /// <remarks>
        /// <para>
        /// Keyset pagination (also known as cursor-based pagination) is more efficient than
        /// offset-based pagination for large datasets because it doesn't need to scan
        /// all previous rows.
        /// </para>
        /// <para>
        /// <strong>Performance Benefits:</strong>
        /// <list type="bullet">
        /// <item>Consistent performance regardless of page number</item>
        /// <item>No duplicate or missing rows when data changes during pagination</item>
        /// <item>Better index utilization</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // First page
        /// var firstPage = repository.GetByKeysetPagination&lt;DateTime&gt;(
        ///     clause: c => c.IsActive,
        ///     keySelector: c => c.CreatedAt,
        ///     lastKey: null,
        ///     pageSize: 20,
        ///     ascending: false);
        /// 
        /// // Next page using the last key
        /// var nextPage = repository.GetByKeysetPagination&lt;DateTime&gt;(
        ///     clause: c => c.IsActive,
        ///     keySelector: c => c.CreatedAt,
        ///     lastKey: firstPage.LastKey,
        ///     pageSize: 20,
        ///     ascending: false);
        /// </code>
        /// </example>
        IKeysetPageResult<TEntity, TKey> GetByKeysetPagination<TKey>(
            Expression<Func<TEntity, bool>>? clause,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true) where TKey : struct;

        /// <summary>
        /// Retrieves entities using keyset pagination with a specification.
        /// </summary>
        /// <typeparam name="TKey">The type of the key used for pagination.</typeparam>
        /// <typeparam name="TSpec">The specification type.</typeparam>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="keySelector">Expression to select the ordering key.</param>
        /// <param name="lastKey">The last key from the previous page (null for first page).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="ascending">Whether to order ascending (true) or descending (false).</param>
        /// <returns>A keyset page result containing the entities and pagination metadata.</returns>
        IKeysetPageResult<TEntity, TKey> GetByKeysetPagination<TKey, TSpec>(
            TSpec specification,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true) 
            where TKey : struct
            where TSpec : ISpecificationQuery<TEntity>;

        /// <summary>
        /// Retrieves entities using keyset pagination with a string-based key (for IDs, etc.).
        /// </summary>
        /// <param name="clause">Optional filter expression.</param>
        /// <param name="keySelector">Expression to select the ordering key.</param>
        /// <param name="lastKey">The last key from the previous page (null for first page).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="ascending">Whether to order ascending (true) or descending (false).</param>
        /// <returns>A keyset page result containing the entities and pagination metadata.</returns>
        IKeysetPageResultString<TEntity> GetByKeysetPagination(
            Expression<Func<TEntity, bool>>? clause,
            Expression<Func<TEntity, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true);

        #endregion
    }
}

