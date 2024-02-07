//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with functions to perform database commands (add, modify or delete) for an entity
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface ICommand<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        void Add(TEntity entity);
        /// <summary>
        /// Adds list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        void Add(IList<TEntity> entities);
        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        void Modify(TEntity entity);
        /// <summary>
        /// Updates list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        void Modify(IList<TEntity> entities);
        /// <summary>
        /// Removes an entity (logical exclusion).
        /// </summary>
        /// <param name="entity">Entity instance</param>
        void Remove(TEntity entity);
        /// <summary>
        /// Removes list of entities (logical exclusion).
        /// </summary>
        /// <param name="entities">List of entities</param>
        void Remove(IList<TEntity> entities);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        void RemoveById(object id);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="ids">List of identifiers</param>
        void RemoveById(IList<object> ids);
    }
}
