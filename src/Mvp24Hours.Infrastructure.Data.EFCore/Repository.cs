//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
///  <see cref="IRepository{T}"/>
/// </summary>
public class Repository<T>(DbContext dbContext, IOptions<EFCoreRepositoryOptions> options, ILogger<Repository<T>>? logger = null) : RepositoryBase<T>(dbContext, options), IRepository<T>
    where T : class, IEntityBase
{
    private readonly ILogger<Repository<T>>? _logger = logger;
    #region [ IQuery ]

    public bool ListAny()
    {
        _logger?.LogDebug("Repository: ListAny started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope(true);
            bool result = GetQuery(null, true).Any();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: ListAny transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: ListAny finished"); }
    }

    public int ListCount()
    {
        _logger?.LogDebug("Repository: ListCount started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope(true);
            int result = GetQuery(null, true).Count();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: ListCount transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: ListCount finished"); }
    }

    public IList<T> List()
    {
        return List(null);
    }

    public IList<T> List(IPagingCriteria? criteria)
    {
        _logger?.LogDebug("Repository: List started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope();
            var result = GetQuery(criteria).ToList();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: List transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: List finished"); }
    }

    public bool GetByAny(Expression<Func<T, bool>> clause)
    {
        _logger?.LogDebug("Repository: GetByAny started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope(true);
            IQueryable<T> query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            bool result = GetQuery(query, null, true).Any();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: GetByAny transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: GetByAny finished"); }
    }

    public int GetByCount(Expression<Func<T, bool>> clause)
    {
        _logger?.LogDebug("Repository: GetByCount started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope(true);
            IQueryable<T> query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            int result = GetQuery(query, null, true).Count();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: GetByCount transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: GetByCount finished"); }
    }

    public IList<T> GetBy(Expression<Func<T, bool>> clause)
    {
        return GetBy(clause, null);
    }

    public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria? criteria)
    {
        _logger?.LogDebug("Repository: GetBy started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope();
            IQueryable<T> query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = GetQuery(query, criteria).ToList();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: GetBy transaction scope complete");
            }
            return result;
        }
        finally { _logger?.LogDebug("Repository: GetBy finished"); }
    }

    public T GetById(object id)
    {
        return GetById(id, null)!;
    }

    public T GetById(object id, IPagingCriteria? criteria)
    {
        _logger?.LogDebug("Repository: GetById started");
        try
        {
            using TransactionScope? scope = CreateTransactionScope();
            T? result = GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefault();
            if (scope != null)
            {
                scope.Complete();
                _logger?.LogDebug("Repository: GetById transaction scope complete");
            }
            return result!;
        }
        finally { _logger?.LogDebug("Repository: GetById finished"); }
    }

    #endregion

    #region [ IQueryRelation ]

    public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression)
        where TProperty : class
    {
        _logger?.LogDebug("Repository: LoadRelation started");
        try
        {
            dbContext.Entry(entity).Reference(Expression.Lambda<Func<T, TProperty?>>(propertyExpression.Body, propertyExpression.Parameters)).Load();
        }
        finally { _logger?.LogDebug("Repository: LoadRelation finished"); }
    }

    public void LoadRelation<TProperty>(T entity,
        Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0)
        where TProperty : class
    {
        _logger?.LogDebug("Repository: LoadRelation (collection) started");
        try
        {
            IQueryable<TProperty> query = dbContext.Entry(entity).Collection(propertyExpression).Query();

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
        finally { _logger?.LogDebug("Repository: LoadRelation (collection) finished"); }
    }

    public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0) where TProperty : class
    {
        _logger?.LogDebug("Repository: LoadRelationSortByAscending started");
        try
        {
            IQueryable<TProperty> query = dbContext.Entry(entity).Collection(propertyExpression).Query();

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
        finally { _logger?.LogDebug("Repository: LoadRelationSortByAscending finished"); }
    }

    public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0) where TProperty : class
    {
        _logger?.LogDebug("Repository: LoadRelationSortByDescending started");
        try
        {
            IQueryable<TProperty> query = dbContext.Entry(entity).Collection(propertyExpression).Query();

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
        finally { _logger?.LogDebug("Repository: LoadRelationSortByDescending finished"); }
    }

    #endregion

    #region [ ICommand ]

    public void Add(T entity)
    {
        _logger?.LogDebug("Repository: Add started");
        try
        {
            if (entity == null)
            {
                return;
            }

            EntityEntry<T> entry = dbContext.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
            else
            {
                dbEntities.Add(entity);
            }
        }
        finally { _logger?.LogDebug("Repository: Add finished"); }
    }

    public void Add(IList<T> entities)
    {
        _logger?.LogDebug("Repository: Add (list) started");
        try
        {
            if (entities.AnySafe())
            {
                foreach (T entity in entities)
                {
                    Add(entity);
                }
            }
        }
        finally { _logger?.LogDebug("Repository: Add (list) finished"); }
    }

    public void Modify(T entity)
    {
        _logger?.LogDebug("Repository: Modify started");
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
                _logger?.LogDebug("Repository: Modify with entity log");
                var entityLog = (dynamic)entity;
                var entityDbLog = (dynamic)entityDb;
                entityLog.Created = entityDbLog.Created;
                entityLog.CreatedBy = entityDbLog.CreatedBy;
                entityLog.Modified = entityDbLog.Modified;
                entityLog.ModifiedBy = entityDbLog.ModifiedBy;
            }

            dbContext.Entry(entityDb).CurrentValues.SetValues(entity);
        }
        finally { _logger?.LogDebug("Repository: Modify finished"); }
    }

    public void Modify(IList<T> entities)
    {
        _logger?.LogDebug("Repository: Modify (list) started");
        try
        {
            if (entities.AnySafe())
            {
                foreach (T entity in entities)
                {
                    Modify(entity);
                }
            }
        }
        finally { _logger?.LogDebug("Repository: Modify (list) finished"); }
    }

    public void Remove(T entity)
    {
        _logger?.LogDebug("Repository: Remove started");
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
                _logger?.LogDebug("Repository: Remove with entity log");
                var entityLog = (dynamic)entity;
                entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                if (hasUserLog)
                {
                    entityLog.RemovedBy = (dynamic)(EntityLogBy ?? throw new InvalidOperationException("EntityLogBy is not available."));
                }
                Modify(entity);
            }
            else
            {
                ForceRemove(entity);
            }
        }
        finally { _logger?.LogDebug("Repository: Remove finished"); }
    }

    public void Remove(IList<T> entities)
    {
        _logger?.LogDebug("Repository: Remove (list) started");
        try
        {
            if (entities.AnySafe())
            {
                foreach (T entity in entities)
                {
                    Remove(entity);
                }
            }
        }
        finally { _logger?.LogDebug("Repository: Remove (list) finished"); }
    }

    public void RemoveById(object id)
    {
        _logger?.LogDebug("Repository: RemoveById started");
        try
        {
            T entity = GetById(id);
            if (entity == null)
            {
                return;
            }

            Remove(entity);
        }
        finally { _logger?.LogDebug("Repository: RemoveById finished"); }
    }

    public void RemoveById(IList<object> ids)
    {
        _logger?.LogDebug("Repository: RemoveById (list) started");
        try
        {
            if (ids.AnySafe())
            {
                foreach (object id in ids)
                {
                    RemoveById(id);
                }
            }
        }
        finally { _logger?.LogDebug("Repository: RemoveById (list) finished"); }
    }

    /// <summary>
    ///  If entity is not log
    /// </summary>
    private void ForceRemove(T entity)
    {
        _logger?.LogDebug("Repository: ForceRemove started");
        try
        {
            if (entity == null)
            {
                return;
            }

            EntityEntry<T> entry = dbContext.Entry(entity);
            if (entry.State != EntityState.Deleted)
            {
                entry.State = EntityState.Deleted;
            }
            else
            {
                dbEntities.Attach(entity);
                dbEntities.Remove(entity);
            }
        }
        finally { _logger?.LogDebug("Repository: ForceRemove finished"); }
    }

    #endregion

    #region [ Properties ]

    protected override object? EntityLogBy => (dbContext as Mvp24HoursContext)?.EntityLogBy;

    #endregion
}
