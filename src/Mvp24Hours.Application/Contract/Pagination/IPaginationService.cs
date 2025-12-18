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

namespace Mvp24Hours.Application.Contract.Pagination
{
    /// <summary>
    /// Provides pagination services for entities with enriched metadata.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This service provides two pagination strategies:
    /// <list type="bullet">
    ///   <item><description><strong>Offset-based:</strong> Traditional page number navigation (good for small datasets)</description></item>
    ///   <item><description><strong>Cursor-based:</strong> Keyset pagination (better for large datasets)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IPaginationService<TEntity>
        where TEntity : class
    {
        #region [ Offset-based Pagination ]

        /// <summary>
        /// Gets a paginated list of entities.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result with navigation metadata.</returns>
        Task<IPagedResult<TEntity>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of entities matching the specified filter.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result with navigation metadata.</returns>
        Task<IPagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of entities with paging criteria.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="criteria">Additional paging criteria (ordering, includes).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result with navigation metadata.</returns>
        Task<IPagedResult<TEntity>> GetPagedAsync(
            int page,
            int pageSize,
            IPagingCriteria? criteria,
            CancellationToken cancellationToken = default);

        #endregion

        #region [ Cursor-based Pagination ]

        /// <summary>
        /// Gets a cursor-paginated list of entities.
        /// </summary>
        /// <typeparam name="TCursor">The cursor type.</typeparam>
        /// <param name="keySelector">The cursor key selector expression.</param>
        /// <param name="afterCursor">The cursor value to start after (for forward pagination).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor-paged result.</returns>
        Task<ICursorPagedResult<TEntity, TCursor>> GetCursorPagedAsync<TCursor>(
            Expression<Func<TEntity, TCursor>> keySelector,
            TCursor? afterCursor,
            int pageSize,
            CancellationToken cancellationToken = default)
            where TCursor : struct;

        /// <summary>
        /// Gets a cursor-paginated list of entities matching the specified filter.
        /// </summary>
        /// <typeparam name="TCursor">The cursor type.</typeparam>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="keySelector">The cursor key selector expression.</param>
        /// <param name="afterCursor">The cursor value to start after (for forward pagination).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor-paged result.</returns>
        Task<ICursorPagedResult<TEntity, TCursor>> GetCursorPagedAsync<TCursor>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TCursor>> keySelector,
            TCursor? afterCursor,
            int pageSize,
            CancellationToken cancellationToken = default)
            where TCursor : struct;

        #endregion
    }

    /// <summary>
    /// Provides pagination services for entities with DTO mapping.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    public interface IPaginationService<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        /// <summary>
        /// Gets a paginated list of DTOs.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result with navigation metadata.</returns>
        Task<IPagedResult<TDto>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of DTOs matching the specified filter.
        /// </summary>
        /// <param name="predicate">The filter expression on entity.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result with navigation metadata.</returns>
        Task<IPagedResult<TDto>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cursor-paginated list of DTOs.
        /// </summary>
        /// <typeparam name="TCursor">The cursor type.</typeparam>
        /// <param name="keySelector">The cursor key selector expression on entity.</param>
        /// <param name="afterCursor">The cursor value to start after.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor-paged result.</returns>
        Task<ICursorPagedResult<TDto, TCursor>> GetCursorPagedAsync<TCursor>(
            Expression<Func<TEntity, TCursor>> keySelector,
            TCursor? afterCursor,
            int pageSize,
            CancellationToken cancellationToken = default)
            where TCursor : struct;
    }
}

