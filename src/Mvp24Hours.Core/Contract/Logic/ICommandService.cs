//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Standard contract with methods for data manipulation.
    /// </summary>
    public interface ICommandService<T>
        where T : class
    {
        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        IBusinessResult<int> Add(T entity);
        /// <summary>
        /// Adds list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        IBusinessResult<int> Add(IList<T> entities);
        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        IBusinessResult<int> Modify(T entity);
        /// <summary>
        /// Updates list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        IBusinessResult<int> Modify(IList<T> entities);
        /// <summary>
        /// Removes an entity (logical exclusion).
        /// </summary>
        /// <param name="entity">Entity instance</param>
        IBusinessResult<int> Remove(T entity);
        /// <summary>
        /// Removes list of entities (logical exclusion).
        /// </summary>
        /// <param name="entities">List of entities</param>
        IBusinessResult<int> Remove(IList<T> entities);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        IBusinessResult<int> RemoveById(object id);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="ids">List of identifiers</param>
        IBusinessResult<int> RemoveById(IList<object> ids);
    }
}
