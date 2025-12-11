//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Repository interface for read models (projections).
/// Optimized for query operations with optional write support for projections.
/// </summary>
/// <typeparam name="T">The read model type.</typeparam>
/// <remarks>
/// <para>
/// <strong>CQRS Read Side:</strong>
/// This repository is designed for the Query side of CQRS. It provides:
/// <list type="bullet">
/// <item>Fast query operations</item>
/// <item>Flexible filtering and paging</item>
/// <item>Write operations for projection handlers</item>
/// <item>Bulk operations for efficient rebuilds</item>
/// </list>
/// </para>
/// <para>
/// <strong>Difference from IQuery (Repository Pattern):</strong>
/// The existing IQuery&lt;T&gt; in Mvp24Hours is designed for traditional CRUD repositories.
/// IReadModelRepository&lt;T&gt; is specifically designed for:
/// <list type="bullet">
/// <item>Denormalized read models</item>
/// <item>Projection-based updates (not direct domain changes)</item>
/// <item>Bulk operations for rebuilds</item>
/// <item>Eventual consistency patterns</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query examples
/// var order = await repository.GetByIdAsync(orderId);
/// var orders = await repository.FindAsync(o => o.CustomerId == customerId);
/// 
/// // Projection update
/// await repository.UpsertAsync(orderSummary);
/// 
/// // Bulk operations for rebuild
/// await repository.DeleteAllAsync();
/// await repository.BulkInsertAsync(orderSummaries);
/// </code>
/// </example>
public interface IReadModelRepository<T> where T : class
{
    #region [ Query Operations ]

    /// <summary>
    /// Gets a read model by its identifier.
    /// </summary>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The read model if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a read model by its string identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The read model if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a read model by its GUID identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The read model if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds read models matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching read models.</returns>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds read models with paging support.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged collection of read models.</returns>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single read model matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching read model if found; otherwise, null.</returns>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all read models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All read models.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of read models matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count.</returns>
    Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any read model matches the predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if any matches; otherwise, false.</returns>
    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    #endregion

    #region [ Write Operations (for Projections) ]

    /// <summary>
    /// Inserts a new read model.
    /// </summary>
    /// <param name="entity">The read model to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InsertAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing read model.
    /// </summary>
    /// <param name="entity">The read model to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a read model (upsert).
    /// </summary>
    /// <param name="entity">The read model to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a read model by its identifier.
    /// </summary>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync<TId>(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes read models matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted records.</returns>
    Task<long> DeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    #endregion

    #region [ Bulk Operations (for Rebuild) ]

    /// <summary>
    /// Deletes all read models of this type.
    /// Used for projection rebuild.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts multiple read models.
    /// Optimized for projection rebuild.
    /// </summary>
    /// <param name="entities">The read models to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts multiple read models.
    /// </summary>
    /// <param name="entities">The read models to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkUpsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Extended read model repository with advanced query capabilities.
/// </summary>
/// <typeparam name="T">The read model type.</typeparam>
public interface IAdvancedReadModelRepository<T> : IReadModelRepository<T> where T : class
{
    /// <summary>
    /// Gets a queryable for advanced LINQ operations.
    /// </summary>
    /// <returns>An IQueryable for the read model.</returns>
    IQueryable<T> AsQueryable();

    /// <summary>
    /// Finds read models with ordering.
    /// </summary>
    /// <typeparam name="TKey">The key type for ordering.</typeparam>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="orderBy">The ordering expression.</param>
    /// <param name="descending">Whether to order descending.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered read models.</returns>
    Task<IReadOnlyList<T>> FindOrderedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds read models with ordering and paging.
    /// </summary>
    /// <typeparam name="TKey">The key type for ordering.</typeparam>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="orderBy">The ordering expression.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="descending">Whether to order descending.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered and paged read models.</returns>
    Task<IReadOnlyList<T>> FindOrderedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderBy,
        int skip,
        int take,
        bool descending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects read models to a different type.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="selector">The projection selector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Projected results.</returns>
    Task<IReadOnlyList<TResult>> ProjectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Paged result wrapper for read model queries.
/// </summary>
/// <typeparam name="T">The read model type.</typeparam>
public class PagedReadModelResult<T>
{
    /// <summary>
    /// Gets the items in this page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets the total count of all items.
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}


