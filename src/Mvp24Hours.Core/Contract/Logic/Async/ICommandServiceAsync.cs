//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Standard contract with methods for data manipulation.
    /// </summary>
    public interface ICommandServiceAsync<T>
        where T : class
    {
        /// <summary>
        /// Add an entities.
        /// </summary>
        /// <param name="entity"></param>
        Task<IBusinessResult<int>> AddAsync(T entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Adds list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task<IBusinessResult<int>> AddAsync(IList<T> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        Task<IBusinessResult<int>> ModifyAsync(T entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates list of entities.
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task<IBusinessResult<int>> ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity (logical exclusion).
        /// </summary>
        /// <param name="entity">Entity instance</param>
        Task<IBusinessResult<int>> RemoveAsync(T entity, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes list of entities (logical exclusion).
        /// </summary>
        /// <param name="entities">List of entities</param>
        Task<IBusinessResult<int>> RemoveAsync(IList<T> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Removes an entity by the code identifier (logical exclusion).
        /// </summary>
        /// <param name="id">List of identifiers</param>
        Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default);
    }
}
