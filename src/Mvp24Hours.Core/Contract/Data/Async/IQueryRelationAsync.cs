//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with asynchronous functions to load entities from instances
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface IQueryRelationAsync<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Load entity related to model entity
        /// </summary>
        Task LoadRelationAsync<TProperty>(TEntity entity,
            Expression<Func<TEntity, TProperty>> propertyExpression,
            CancellationToken cancellationToken = default)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity with clause
        /// </summary>
        Task LoadRelationAsync<TProperty>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity sorted by ascending
        /// </summary>
        Task LoadRelationSortByAscendingAsync<TProperty, TKey>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity sorted by descending
        /// </summary>
        Task LoadRelationSortByDescendingAsync<TProperty, TKey>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default)
            where TProperty : class;
    }
}
