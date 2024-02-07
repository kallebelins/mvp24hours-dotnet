//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with asynchronous functions to perform database commands (add, modify or delete) for an entity
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface ICommandAsync<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Add an entities.
        /// </summary>
        /// <param name="entity"></param>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Adds list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        Task ModifyAsync(TEntity entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity (logical exclusion).
        /// </summary>
        /// <param name="entity">Entity instance</param>
        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes list of entities (logical exclusion).
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">List of identifiers</param>
        Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default);
    }
}
