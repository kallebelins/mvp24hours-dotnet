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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with asynchronous functions to perform database queries (filters, sorting and pagination) for an entity
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface IQueryAsync<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Checks whether any records returned by the ListAsync() method
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<bool> ListAnyAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the amount of representations returned by the ListAsync() method.
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<int> ListCountAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all representations of the typed entity.
        /// </summary>
        /// <returns>List of entities async</returns>
        Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all representations of the entity typed with criteria.
        /// </summary>
        /// <returns>List of entities async</returns>
        Task<IList<TEntity>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);
        /// <summary>
        /// Checks whether any records returned by the GetBy() method.
        /// </summary>
        /// <returns>Indicates whether there is a record</returns>
        Task<bool> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the amount of representations returned by the GetByAsync() method.
        /// </summary>
        /// <returns>Number of representations async</returns>
        Task<int> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the representations based on the filter of the typed entity.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations async</returns>
        Task<IList<TEntity>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the filter-based representations of the entity typed with criteria.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations async</returns>
        Task<IList<TEntity>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets a representation of the typed entity.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity async</returns>
        Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets a representation of the entity typed with criteria.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity async</returns>
        Task<TEntity> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    }
}
