//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Base;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Internal;

namespace Mvp24Hours.Infrastructure.Data.MongoDb;

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

    public IList<T> List(IPagingCriteria? criteria)
    {
        _logger?.LogDebug("MongoDB repository List started");
        try
        {
            return [.. GetQuery(criteria)];
        }
        finally { _logger?.LogDebug("MongoDB repository List completed"); }
    }

    public bool GetByAny(Expression<Func<T, bool>> clause)
    {
        _logger?.LogDebug("MongoDB repository GetByAny started");
        try
        {
            IQueryable<T> query = dbEntities.AsQueryable();
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
            IQueryable<T> query = dbEntities.AsQueryable();
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

    public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria? criteria)
    {
        _logger?.LogDebug("MongoDB repository GetBy started");
        try
        {
            IQueryable<T> query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            return [.. GetQuery(query, criteria)];
        }
        finally { _logger?.LogDebug("MongoDB repository GetBy completed"); }
    }

    public T? GetById(object id)
    {
        return GetById(id, null)!;
    }

    public T? GetById(object id, IPagingCriteria? criteria)
    {
        _logger?.LogDebug("MongoDB repository GetById started: Id={Id}", id);
        try
        {
            return GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefault()!;
        }
        finally { _logger?.LogDebug("MongoDB repository GetById completed: Id={Id}", id); }
    }

    #endregion

    #region [ IQueryRelation ]
    public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
    {
        throw new NotSupportedException();
    }
    public void LoadRelation<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null, int limit = 0) where TProperty : class
    {
        throw new NotSupportedException();
    }
    public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0) where TProperty : class
    {
        throw new NotSupportedException();
    }
    public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0) where TProperty : class
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
            if (entities?.AnySafe() == true)
            {
                var nonNullEntities = entities.Where(e => e != null).ToList();
                if (nonNullEntities.Count > 0)
                {
                    dbEntities.InsertMany(nonNullEntities);
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

            T entityDb = dbContext.Set<T>().Find(GetKeyFilter(entity)).FirstOrDefault()
                ?? throw new InvalidOperationException("Key value not found.");

            // properties that can not be changed

            if (entity is IEntityDateLog dateLog && entityDb is IEntityDateLog dateLogDb)
            {
                _logger?.LogDebug("MongoDB repository Modify: preserving log fields");
                dateLog.Created = dateLogDb.Created;
                dateLog.Modified = dateLogDb.Modified;
            }

            if (EntityLogAccessor.HasEntityLog(entity))
            {
                EntityLogAccessor.CopyPropertyValue(entityDb, entity, "CreatedBy");
                EntityLogAccessor.CopyPropertyValue(entityDb, entity, "ModifiedBy");
            }

            dbEntities.ReplaceOne(GetKeyFilter(entity), entity);
        }
        finally { _logger?.LogDebug("MongoDB repository Modify completed"); }
    }

    public void Modify(IList<T> entities)
    {
        _logger?.LogDebug("MongoDB repository Modify list started: Count={Count}", entities?.Count ?? 0);
        try
        {
            if (entities?.AnySafe() == true)
            {
                foreach (T entity in entities)
                {
                    Modify(entity);
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

            if (entity is IEntityDateLog dateLog)
            {
                _logger?.LogDebug("MongoDB repository Remove: performing soft delete");
                dateLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                if (EntityLogBy != null && EntityLogAccessor.HasEntityLog(entity))
                {
                    EntityLogAccessor.TrySetPropertyValue(entity, "RemovedBy", EntityLogBy);
                }
                Modify(entity);
            }
            else
            {
                ForceRemove(entity);
            }
        }
        finally { _logger?.LogDebug("MongoDB repository Remove completed"); }
    }

    public void Remove(IList<T> entities)
    {
        _logger?.LogDebug("MongoDB repository Remove list started: Count={Count}", entities?.Count ?? 0);
        try
        {
            if (entities?.AnySafe() == true)
            {
                foreach (T entity in entities)
                {
                    Remove(entity);
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
            T? entity = GetById(id);
            if (entity == null)
            {
                return;
            }
            Remove(entity);
        }
        finally { _logger?.LogDebug("MongoDB repository RemoveById completed: Id={Id}", id); }
    }

    public void RemoveById(IList<object> ids)
    {
        _logger?.LogDebug("MongoDB repository RemoveById list started: Count={Count}", ids?.Count ?? 0);
        try
        {
            if (ids?.AnySafe() == true)
            {
                foreach (object id in ids)
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
            dbEntities.DeleteOne(GetKeyFilter(entity));
        }
        finally { _logger?.LogDebug("MongoDB repository ForceRemove completed"); }
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Returns <c>null</c> because this repository does not track a current user by itself.
    /// When <c>RemovedBy</c> (from <c>IEntityLog{TForeignKey}</c>) needs to be populated
    /// on soft delete, use <c>ICurrentUserProvider</c> with
    /// <see cref="Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors.AuditInterceptor"/> /
    /// <see cref="Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors.SoftDeleteInterceptor"/>
    /// via <see cref="RepositoryAsyncWithInterceptors{T}"/> instead.
    /// </summary>
    protected override object? EntityLogBy => null;

    #endregion
}

/// <summary>
///  <see cref="IRepository{T, TId}"/>
/// </summary>
/// <remarks>
/// Additive wrapper over <see cref="Repository{T}"/>. Every typed member delegates to the
/// <see cref="object"/>-based member of the base class, so each operation keeps a single
/// real implementation and cannot diverge in behavior.
/// </remarks>
public class Repository<T, TId>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<Repository<T, TId>>? logger = null)
    : Repository<T>(dbContext, options, logger), IRepository<T, TId>
    where T : class, IEntityBase, IEntity<TId>
{
    /// <summary>
    ///  <see cref="IRepository{T, TId}.GetById(TId)"/>
    /// </summary>
    public T? GetById(TId id)
    {
        return base.GetById((object)id!);
    }

    /// <summary>
    ///  <see cref="IRepository{T, TId}.GetById(TId, IPagingCriteria)"/>
    /// </summary>
    public T? GetById(TId id, IPagingCriteria? criteria)
    {
        return base.GetById((object)id!, criteria);
    }

    /// <summary>
    ///  <see cref="IRepository{T, TId}.RemoveById(TId)"/>
    /// </summary>
    public void RemoveById(TId id)
    {
        base.RemoveById((object)id!);
    }

    /// <summary>
    ///  <see cref="IRepository{T, TId}.RemoveById(IList{TId})"/>
    /// </summary>
    public void RemoveById(IList<TId> ids)
    {
        base.RemoveById(ids?.Cast<object>().ToList()!);
    }
}
