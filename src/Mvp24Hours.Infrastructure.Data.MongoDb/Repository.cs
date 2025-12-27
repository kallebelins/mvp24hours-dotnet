//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Base;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    public class Repository<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<RepositoryBase<T>>? logger = null) : RepositoryBase<T>(dbContext, options, logger), IRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery ]

        public bool ListAny()
        {
            _logger?.LogDebug("MongoDB repository ListAny started");
            try
            {
                return GetQuery(null, true).Any();
            }
            finally { _logger?.LogDebug("MongoDB repository ListAny completed"); }
        }

        public int ListCount()
        {
            _logger?.LogDebug("MongoDB repository ListCount started");
            try
            {
                return GetQuery(null, true).Count();
            }
            finally { _logger?.LogDebug("MongoDB repository ListCount completed"); }
        }

        public IList<T> List()
        {
            return List(null);
        }

        public IList<T> List(IPagingCriteria criteria)
        {
            _logger?.LogDebug("MongoDB repository List started");
            try
            {
                return GetQuery(criteria).ToList();
            }
            finally { _logger?.LogDebug("MongoDB repository List completed"); }
        }

        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            _logger?.LogDebug("MongoDB repository GetByAny started");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Any();
            }
            finally { _logger?.LogDebug("MongoDB repository GetByAny completed"); }
        }

        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            _logger?.LogDebug("MongoDB repository GetByCount started");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Count();
            }
            finally { _logger?.LogDebug("MongoDB repository GetByCount completed"); }
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria criteria)
        {
            _logger?.LogDebug("MongoDB repository GetBy started");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, criteria).ToList();
            }
            finally { _logger?.LogDebug("MongoDB repository GetBy completed"); }
        }

        public T GetById(object id)
        {
            return GetById(id, null);
        }

        public T GetById(object id, IPagingCriteria criteria)
        {
            _logger?.LogDebug("MongoDB repository GetById started: Id={Id}", id);
            try
            {
                return GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefault();
            }
            finally { _logger?.LogDebug("MongoDB repository GetById completed: Id={Id}", id); }
        }

        #endregion

        #region [ IQueryRelation ]
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
        {
            throw new NotSupportedException();
        }
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>> clause = null, int limit = 0) where TProperty : class
        {
            throw new NotSupportedException();
        }
        public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0) where TProperty : class
        {
            throw new NotSupportedException();
        }
        public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0) where TProperty : class
        {
            throw new NotSupportedException();
        }
        #endregion

        #region [ ICommand ]

        public void Add(T entity)
        {
            _logger?.LogDebug("MongoDB repository Add started");
            try
            {
                if (entity == null)
                {
                    return;
                }
                dbEntities.InsertOne(entity);
            }
            finally { _logger?.LogDebug("MongoDB repository Add completed"); }
        }

        public void Add(IList<T> entities)
        {
            _logger?.LogDebug("MongoDB repository Add list started: Count={Count}", entities?.Count ?? 0);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        this.Add(entity);
                    }
                }
            }
            finally { _logger?.LogDebug("MongoDB repository Add list completed: Count={Count}", entities?.Count ?? 0); }
        }

        public void Modify(T entity)
        {
            _logger?.LogDebug("MongoDB repository Modify started");
            try
            {
                if (entity == null)
                {
                    return;
                }

                var entityDb = dbContext.Set<T>().Find(GetKeyFilter(entity)).FirstOrDefault()
                    ?? throw new InvalidOperationException("Key value not found.");

                // properties that can not be changed

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    _logger?.LogDebug("MongoDB repository Modify: preserving log fields");
                    var entityLog = entity as IEntityLog<object>;
                    var entityDbLog = entityDb as IEntityLog<object>;
                    entityLog.Created = entityDbLog.Created;
                    entityLog.CreatedBy = entityDbLog.CreatedBy;
                    entityLog.Modified = entityDbLog.Modified;
                    entityLog.ModifiedBy = entityDbLog.ModifiedBy;
                }

                this.dbEntities.ReplaceOne(GetKeyFilter(entity), entity);
            }
            finally { _logger?.LogDebug("MongoDB repository Modify completed"); }
        }

        public void Modify(IList<T> entities)
        {
            _logger?.LogDebug("MongoDB repository Modify list started: Count={Count}", entities?.Count ?? 0);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        this.Modify(entity);
                    }
                }
            }
            finally { _logger?.LogDebug("MongoDB repository Modify list completed: Count={Count}", entities?.Count ?? 0); }
        }

        public void Remove(T entity)
        {
            _logger?.LogDebug("MongoDB repository Remove started");
            try
            {
                if (entity == null)
                {
                    return;
                }

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    _logger?.LogDebug("MongoDB repository Remove: performing soft delete");
                    var entityLog = entity as IEntityLog<object>;
                    entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                    entityLog.RemovedBy = EntityLogBy;
                    this.Modify(entity);
                }
                else
                {
                    this.ForceRemove(entity);
                }
            }
            finally { _logger?.LogDebug("MongoDB repository Remove completed"); }
        }

        public void Remove(IList<T> entities)
        {
            _logger?.LogDebug("MongoDB repository Remove list started: Count={Count}", entities?.Count ?? 0);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        this.Remove(entity);
                    }
                }
            }
            finally { _logger?.LogDebug("MongoDB repository Remove list completed: Count={Count}", entities?.Count ?? 0); }
        }

        public void RemoveById(object id)
        {
            _logger?.LogDebug("MongoDB repository RemoveById started: Id={Id}", id);
            try
            {
                var entity = this.GetById(id);
                if (entity == null)
                {
                    return;
                }
                this.Remove(entity);
            }
            finally { _logger?.LogDebug("MongoDB repository RemoveById completed: Id={Id}", id); }
        }

        public void RemoveById(IList<object> ids)
        {
            _logger?.LogDebug("MongoDB repository RemoveById list started: Count={Count}", ids?.Count ?? 0);
            try
            {
                if (ids.AnySafe())
                {
                    foreach (var id in ids)
                    {
                        RemoveById(id);
                    }
                }
            }
            finally { _logger?.LogDebug("MongoDB repository RemoveById list completed: Count={Count}", ids?.Count ?? 0); }
        }

        /// <summary>
        ///  If entity is not log
        /// </summary>
        private void ForceRemove(T entity)
        {
            _logger?.LogDebug("MongoDB repository ForceRemove started");
            try
            {
                if (entity == null)
                {
                    return;
                }
                this.dbEntities.DeleteOne(GetKeyFilter(entity));
            }
            finally { _logger?.LogDebug("MongoDB repository ForceRemove completed"); }
        }

        #endregion

        #region [ Properties ]

        protected override object EntityLogBy => throw new NotSupportedException();

        #endregion
    }
}
