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
    /// Standard contract with methods for data projection.
    /// </summary>
    public interface IQueryServiceAsync<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Checks whether any records returned by the ListAsync() method
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the amount of representations returned by the ListAsync() method.
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all representations of the typed entity.
        /// </summary>
        /// <returns>List of entities async</returns>
        Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all representations of the entity typed with criteria.
        /// </summary>
        /// <returns>List of entities async</returns>
        Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);
        /// <summary>
        /// Checks whether any records returned by the GetBy() method.
        /// </summary>
        /// <returns>Indicates whether there is a record</returns>
        Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the amount of representations returned by the GetByAsync() method.
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the representations based on the filter of the typed entity.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations async</returns>
        Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the filter-based representations of the entity typed with criteria.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations async</returns>
        Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets a representation of the typed entity.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity async</returns>
        Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets a representation of the entity typed with criteria.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity async</returns>
        Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    }
}
