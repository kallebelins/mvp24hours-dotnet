//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with synchronous functions to load entities from instances
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface IQueryRelation<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Load entity related to model entity
        /// </summary>
        void LoadRelation<TProperty>(TEntity entity,
            Expression<Func<TEntity, TProperty>> propertyExpression)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity with clause
        /// </summary>
        void LoadRelation<TProperty>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity sorted by ascending
        /// </summary>
        void LoadRelationSortByAscending<TProperty, TKey>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class;
        /// <summary>
        /// Load entities related to model entity sorted by descending
        /// </summary>
        void LoadRelationSortByDescending<TProperty, TKey>(TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class;
    }
}
