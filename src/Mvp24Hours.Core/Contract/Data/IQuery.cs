//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Provides query operations for database entities with support for filtering, sorting, and pagination.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that implements <see cref="IEntityBase"/>.</typeparam>
    /// <remarks>
    /// This interface defines the contract for read-only operations on entities in a data store.
    /// It supports various query patterns including:
    /// <list type="bullet">
    /// <item>Existence checks (Any operations)</item>
    /// <item>Count operations</item>
    /// <item>List all entities</item>
    /// <item>Filtered queries with LINQ expressions</item>
    /// <item>Pagination and sorting through criteria objects</item>
    /// <item>Single entity retrieval by ID</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerRepository : IQuery&lt;Customer&gt;
    /// {
    ///     public IList&lt;Customer&gt; GetBy(Expression&lt;Func&lt;Customer, bool&gt;&gt; clause)
    ///     {
    ///         return dbContext.Customers.Where(clause).ToList();
    ///     }
    /// }
    /// 
    /// // Usage
    /// var activeCustomers = repository.GetBy(c => c.IsActive);
    /// </code>
    /// </example>
    public interface IQuery<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Checks whether any records exist in the data store for this entity type.
        /// </summary>
        /// <returns><c>true</c> if at least one record exists; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is equivalent to calling <c>List().Any()</c> but is optimized to
        /// return as soon as a single record is found, without loading the entire dataset.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (repository.ListAny())
        /// {
        ///     // Process entities
        /// }
        /// </code>
        /// </example>
        bool ListAny();
        /// <summary>
        /// Gets the total count of records in the data store for this entity type.
        /// </summary>
        /// <returns>The number of records in the data store.</returns>
        /// <remarks>
        /// This method counts all records without loading them into memory, making it
        /// efficient for large datasets. It's equivalent to calling <c>List().Count()</c>
        /// but optimized for performance.
        /// </remarks>
        /// <example>
        /// <code>
        /// int totalCustomers = repository.ListCount();
        /// Console.WriteLine($"Total customers: {totalCustomers}");
        /// </code>
        /// </example>
        int ListCount();

        /// <summary>
        /// Retrieves all entities from the data store.
        /// </summary>
        /// <returns>A list containing all entities of type <typeparamref name="TEntity"/>.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Warning:</strong> Use this method with caution on large datasets as it loads
        /// all records into memory. Consider using <see cref="List(IPagingCriteria)"/> with
        /// pagination for better performance with large datasets.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var allCustomers = repository.List();
        /// foreach (var customer in allCustomers)
        /// {
        ///     Console.WriteLine(customer.Name);
        /// }
        /// </code>
        /// </example>
        IList<TEntity> List();

        /// <summary>
        /// Retrieves entities from the data store with pagination, sorting, and navigation properties.
        /// </summary>
        /// <param name="criteria">
        /// The paging criteria containing pagination parameters (page size, offset),
        /// sorting instructions, and navigation properties to include.
        /// </param>
        /// <returns>A list of entities matching the specified criteria.</returns>
        /// <remarks>
        /// This method allows for efficient data retrieval using pagination and selective loading
        /// of related entities. The criteria can specify:
        /// <list type="bullet">
        /// <item>Page size and offset for pagination</item>
        /// <item>OrderBy clauses for sorting (ascending or descending)</item>
        /// <item>Navigation properties to eager load (Include)</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var criteria = new PagingCriteria(
        ///     limit: 10, 
        ///     offset: 0, 
        ///     orderBy: new List&lt;string&gt; { "Name asc" },
        ///     navigation: new List&lt;string&gt; { "Orders", "Address" }
        /// );
        /// var customers = repository.List(criteria);
        /// </code>
        /// </example>
        IList<TEntity> List(IPagingCriteria criteria);
        /// <summary>
        /// Checks whether any records matching the specified filter exist in the data store.
        /// </summary>
        /// <param name="clause">A LINQ expression that defines the filter criteria.</param>
        /// <returns><c>true</c> if at least one record matches the filter; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is optimized to return as soon as a matching record is found,
        /// without loading all matching records into memory.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool hasActiveCustomers = repository.GetByAny(c => c.IsActive && c.CreatedDate > DateTime.Now.AddDays(-30));
        /// </code>
        /// </example>
        bool GetByAny(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets the count of records matching the specified filter.
        /// </summary>
        /// <param name="clause">A LINQ expression that defines the filter criteria.</param>
        /// <returns>The number of records matching the filter.</returns>
        /// <remarks>
        /// This method counts matching records without loading them into memory,
        /// making it efficient for large datasets.
        /// </remarks>
        /// <example>
        /// <code>
        /// int activeCustomerCount = repository.GetByCount(c => c.IsActive);
        /// Console.WriteLine($"Active customers: {activeCustomerCount}");
        /// </code>
        /// </example>
        int GetByCount(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Retrieves entities matching the specified filter.
        /// </summary>
        /// <param name="clause">A LINQ expression that defines the filter criteria.</param>
        /// <returns>A list of entities matching the filter.</returns>
        /// <remarks>
        /// This method loads all matching records into memory. For large result sets,
        /// consider using <see cref="GetBy(Expression{Func{TEntity, bool}}, IPagingCriteria)"/>
        /// with pagination.
        /// </remarks>
        /// <example>
        /// <code>
        /// var activeCustomers = repository.GetBy(c => c.IsActive && c.Country == "USA");
        /// </code>
        /// </example>
        IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Retrieves entities matching the specified filter with pagination, sorting, and navigation properties.
        /// </summary>
        /// <param name="clause">A LINQ expression that defines the filter criteria.</param>
        /// <param name="criteria">
        /// The paging criteria containing pagination parameters, sorting instructions,
        /// and navigation properties to include.
        /// </param>
        /// <returns>A list of entities matching the filter and criteria.</returns>
        /// <remarks>
        /// This method combines filtering with pagination and sorting for efficient data retrieval.
        /// It's the recommended approach for querying large datasets.
        /// </remarks>
        /// <example>
        /// <code>
        /// var criteria = new PagingCriteria(limit: 20, offset: 0, orderBy: new[] { "Name asc" });
        /// var customers = repository.GetBy(c => c.IsActive, criteria);
        /// </code>
        /// </example>
        IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Retrieves a single entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>The entity with the specified ID, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// The type of the ID parameter should match the entity's primary key type.
        /// Common types include <see cref="int"/>, <see cref="long"/>, <see cref="Guid"/>, and <see cref="string"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var customer = repository.GetById(123);
        /// if (customer != null)
        /// {
        ///     Console.WriteLine($"Found: {customer.Name}");
        /// }
        /// </code>
        /// </example>
        TEntity GetById(object id);

        /// <summary>
        /// Retrieves a single entity by its unique identifier with navigation properties.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <param name="criteria">
        /// The paging criteria specifying navigation properties to eager load.
        /// Pagination and sorting parameters are typically ignored for single-entity retrieval.
        /// </param>
        /// <returns>The entity with the specified ID and loaded navigation properties, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// Use this method when you need to load related entities along with the main entity
        /// to avoid lazy loading and N+1 query problems.
        /// </remarks>
        /// <example>
        /// <code>
        /// var criteria = new PagingCriteria(navigation: new[] { "Orders", "Orders.Items" });
        /// var customer = repository.GetById(123, criteria);
        /// // customer.Orders and customer.Orders[*].Items are now loaded
        /// </code>
        /// </example>
        TEntity GetById(object id, IPagingCriteria criteria);
    }
}
