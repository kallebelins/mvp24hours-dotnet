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

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Class with functions to perform database queries (filters, sorting and pagination) for an entity
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public interface IQuery<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Checks whether any records returned by the List() method
        /// </summary>
        /// <returns>Number of representations</returns>
        bool ListAny();
        /// <summary>
        /// Gets the amount of representations returned by the List() method.
        /// </summary>
        /// <returns>Number of representations</returns>
        int ListCount();
        /// <summary>
        /// Gets all representations of the typed entity.
        /// </summary>
        /// <returns>List of entities</returns>
        IList<TEntity> List();
        /// <summary>
        /// Gets all representations of the entity typed with criteria.
        /// </summary>
        /// <returns>List of entities</returns>
        IList<TEntity> List(IPagingCriteria criteria);
        /// <summary>
        /// Checks whether any records returned by the GetBy() method.
        /// </summary>
        /// <returns>Indicates whether there is a record</returns>
        bool GetByAny(Expression<Func<TEntity, bool>> clause);
        /// <summary>
        /// Gets the amount of representations returned by the GetBy() method.
        /// </summary>
        /// <returns>Number of representations</returns>
        int GetByCount(Expression<Func<TEntity, bool>> clause);
        /// <summary>
        /// Gets the representations based on the filter of the typed entity.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations</returns>
        IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause);
        /// <summary>
        /// Gets the filter-based representations of the entity typed with criteria.
        /// </summary>
        /// <param name="clause">Filter</param>
        /// <returns>Number of representations</returns>
        IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);
        /// <summary>
        /// Gets a representation of the typed entity.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity</returns>
        TEntity GetById(object id);
        /// <summary>
        /// Gets a representation of the entity typed with criteria.
        /// </summary>
        /// <param name="id">Identifier of entity</param>
        /// <returns>A representation of the entity</returns>
        TEntity GetById(object id, IPagingCriteria criteria);
    }
}
