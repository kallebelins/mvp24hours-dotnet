//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Base;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    public class Repository<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options) : RepositoryBase<T>(dbContext, options), IRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery ]

        public bool ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-listany-start");
            try
            {
                return GetQuery(null, true).Any();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-listany-end"); }
        }

        public int ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-listcount-start");
            try
            {
                return GetQuery(null, true).Count();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-listcount-end"); }
        }

        public IList<T> List()
        {
            return List(null);
        }

        public IList<T> List(IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-list-start");
            try
            {
                return GetQuery(criteria).ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-list-end"); }
        }

        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbyany-start");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Any();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbyany-end"); }
        }

        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbycount-start");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Count();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbycount-end"); }
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getby-start");
            try
            {
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, criteria).ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getby-end"); }
        }

        public T GetById(object id)
        {
            return GetById(id, null);
        }

        public T GetById(object id, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbyid-start");
            try
            {
                return GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefault();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-getbyid-end"); }
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-add-start");
            try
            {
                if (entity == null)
                {
                    return;
                }
                dbEntities.InsertOne(entity);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-add-end"); }
        }

        public void Add(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-addlist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-addlist-end"); }
        }

        public void Modify(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-modify-start");
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
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-modify-log");
                    var entityLog = entity as IEntityLog<object>;
                    var entityDbLog = entityDb as IEntityLog<object>;
                    entityLog.Created = entityDbLog.Created;
                    entityLog.CreatedBy = entityDbLog.CreatedBy;
                    entityLog.Modified = entityDbLog.Modified;
                    entityLog.ModifiedBy = entityDbLog.ModifiedBy;
                }

                this.dbEntities.ReplaceOne(GetKeyFilter(entity), entity);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-modify-end"); }
        }

        public void Modify(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-modifylist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-modifylist-end"); }
        }

        public void Remove(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-remove-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-remove-log");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-remove-end"); }
        }

        public void Remove(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removelist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removelist-end"); }
        }

        public void RemoveById(object id)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removebyid-start");
            try
            {
                var entity = this.GetById(id);
                if (entity == null)
                {
                    return;
                }
                this.Remove(entity);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removebyid-end"); }
        }

        public void RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removebyidlist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-removebyidlist-end"); }
        }

        /// <summary>
        ///  If entity is not log
        /// </summary>
        private void ForceRemove(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-forceremove-start");
            try
            {
                if (entity == null)
                {
                    return;
                }
                this.dbEntities.DeleteOne(GetKeyFilter(entity));
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repository-forceremove-end"); }
        }

        #endregion

        #region [ Properties ]

        protected override object EntityLogBy => throw new NotSupportedException();

        #endregion
    }
}
