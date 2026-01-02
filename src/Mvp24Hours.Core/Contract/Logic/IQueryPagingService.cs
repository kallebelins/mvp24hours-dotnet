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

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Standard contract with methods for data projection.
    /// </summary>
    public interface IQueryPagingService<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Gets all representations of the entity typed with criteria.
        /// </summary>
        IPagingResult<IList<TEntity>> ListWithPagination(IPagingCriteria? criteria = null);
        /// <summary>
        /// Gets the filter-based representations of the entity typed with criteria.
        /// </summary>
        IPagingResult<IList<TEntity>> GetByWithPagination(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria = null);
    }
}
