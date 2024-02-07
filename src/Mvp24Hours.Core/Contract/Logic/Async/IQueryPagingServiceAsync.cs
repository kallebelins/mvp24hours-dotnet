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
    public interface IQueryPagingServiceAsync<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets all representations of the entity typed with criteria.
        /// </summary>
        Task<IPagingResult<IList<TEntity>>> ListWithPaginationAsync(IPagingCriteria criteria = null, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the filter-based representations of the entity typed with criteria.
        /// </summary>
        Task<IPagingResult<IList<TEntity>>> GetByWithPaginationAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null, CancellationToken cancellationToken = default);
    }
}
