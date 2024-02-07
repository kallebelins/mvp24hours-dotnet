//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IRepository"/>
    /// </summary>
    public class Repository<T>(DbContext dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<T>(dbContext, options), IRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery ]

        public bool ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listany-start");
            try
            {
                using var scope = CreateTransactionScope(true);
                var result = GetQuery(null, true).Any();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listany-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listany-end"); }
        }

        public int ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listcount-start");
            try
            {
                using var scope = CreateTransactionScope(true);
                var result = GetQuery(null, true).Count();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listcount-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-listcount-end"); }
        }

        public IList<T> List()
        {
            return List(null);
        }

        public IList<T> List(IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-list-start");
            try
            {
                using var scope = CreateTransactionScope();
                var result = GetQuery(criteria).ToList();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-list-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-list-end"); }
        }

        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyany-start");
            try
            {
                using var scope = CreateTransactionScope(true);
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var result = GetQuery(query, null, true).Any();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyany-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyany-end"); }
        }

        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbycount-start");
            try
            {
                using var scope = CreateTransactionScope(true);
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var result = GetQuery(query, null, true).Count();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbycount-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbycount-end"); }
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getby-start");
            try
            {
                using var scope = CreateTransactionScope();
                var query = this.dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var result = GetQuery(query, criteria).ToList();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getby-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getby-end"); }
        }

        public T GetById(object id)
        {
            return GetById(id, null);
        }

        public T GetById(object id, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyid-start");
            try
            {
                using var scope = CreateTransactionScope();
                var result = GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefault();
                if (scope != null)
                {
                    scope.Complete();
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyid-transactionscope-complete");
                }
                return result;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-getbyid-end"); }
        }

        #endregion

        #region [ IQueryRelation ]

        public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression)
            where TProperty : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelation-start");
            try
            {
                this.dbContext.Entry(entity).Reference(propertyExpression).Load();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelation-end"); }
        }

        public void LoadRelation<TProperty>(T entity,
            Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelation-start");
            try
            {
                var query = this.dbContext.Entry(entity).Collection(propertyExpression).Query();

                if (clause != null)
                {
                    query = query.Where(clause);
                }

                if (limit > 0)
                {
                    query = query.Take(limit);
                }

                _ = query.ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelation-end"); }
        }

        public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0) where TProperty : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelationsortbyascending-start");
            try
            {
                var query = this.dbContext.Entry(entity).Collection(propertyExpression).Query();

                if (clause != null)
                {
                    query = query.Where(clause);
                }

                if (orderKey != null)
                {
                    query = query.OrderBy(orderKey);
                }

                if (limit > 0)
                {
                    query = query.Take(limit);
                }

                _ = query.ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelationsortbyascending-end"); }
        }

        public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0) where TProperty : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelationsortbydescending-start");
            try
            {
                var query = this.dbContext.Entry(entity).Collection(propertyExpression).Query();

                if (clause != null)
                {
                    query = query.Where(clause);
                }

                if (orderKey != null)
                {
                    query = query.OrderByDescending(orderKey);
                }

                if (limit > 0)
                {
                    query = query.Take(limit);
                }

                _ = query.ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-loadrelationsortbydescending-end"); }
        }

        #endregion

        #region [ ICommand ]

        public void Add(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-add-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                var entry = this.dbContext.Entry(entity);
                if (entry.State != EntityState.Detached)
                {
                    entry.State = EntityState.Added;
                }
                else
                {
                    this.dbEntities.Add(entity);
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-add-end"); }
        }

        public void Add(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-addlist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-addlist-end"); }
        }

        public void Modify(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-modify-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                T entityDb = dbContext.Set<T>().Find(entity.EntityKey)
                    ?? throw new InvalidOperationException("Key value not found.");

                // properties that can not be changed

                if (entity.GetType().InheritsOrImplements(typeof(IEntityLog<>)) || entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>)))
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-modify-log");
                    var entityLog = (dynamic)entity;
                    var entityDbLog = (dynamic)entityDb;
                    entityLog.Created = entityDbLog.Created;
                    entityLog.CreatedBy = entityDbLog.CreatedBy;
                    entityLog.Modified = entityDbLog.Modified;
                    entityLog.ModifiedBy = entityDbLog.ModifiedBy;
                }

                this.dbContext.Entry(entityDb).CurrentValues.SetValues(entity);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-modify-end"); }
        }

        public void Modify(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-modifylist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-modifylist-end"); }
        }

        public void Remove(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-remove-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                bool hasUserLog = (entity.GetType().InheritsOrImplements(typeof(IEntityLog<>))
                    || entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>)));

                bool hasUserLogDate = hasUserLog || entity.GetType().InheritsOrImplements(typeof(IEntityDateLog));

                if (hasUserLog || hasUserLogDate)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-remove-log");
                    var entityLog = (dynamic)entity;
                    entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                    if (hasUserLog)
                    {
                        entityLog.RemovedBy = (dynamic)EntityLogBy;
                    }
                    this.Modify(entity);
                }
                else
                {
                    this.ForceRemove(entity);
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-remove-end"); }
        }

        public void Remove(IList<T> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removelist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removelist-end"); }
        }

        public void RemoveById(object id)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removebyid-start");
            try
            {
                var entity = this.GetById(id);
                if (entity == null)
                {
                    return;
                }

                this.Remove(entity);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removebyid-end"); }
        }

        public void RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removebyidlist-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-removebyidlist-end"); }
        }

        /// <summary>
        ///  If entity is not log
        /// </summary>
        private void ForceRemove(T entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-forceremove-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                var entry = this.dbContext.Entry(entity);
                if (entry.State != EntityState.Deleted)
                {
                    entry.State = EntityState.Deleted;
                }
                else
                {
                    this.dbEntities.Attach(entity);
                    this.dbEntities.Remove(entity);
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repository-forceremove-end"); }
        }

        #endregion

        #region [ Properties ]

        protected override object EntityLogBy => (dbContext as Mvp24HoursContext)?.EntityLogBy;

        #endregion
    }
}
