//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Internal;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
///  <see cref="IRepositoryAsync{T}"/>
/// </summary>
public class RepositoryAsync<T>(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<T>(_dbContext, options), IRepositoryAsync<T>
    where T : class, IEntityBase
{
    #region [ IQueryAsync ]

    public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope(true);
        bool result = await GetQuery(null, true).AnyAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope(true);
        int result = await GetQuery(null, true).CountAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken);
    }

    public async Task<IList<T>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope();
        List<T> result = await GetQuery(criteria).ToListAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope(true);
        IQueryable<T> query = dbEntities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        bool result = await GetQuery(query, null, true).AnyAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope(true);
        IQueryable<T> query = dbEntities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        int result = await GetQuery(query, null, true).CountAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
    {
        return GetByAsync(clause, null, cancellationToken);
    }

    public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope();
        IQueryable<T> query = dbEntities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        List<T> result = await GetQuery(query, criteria).ToListAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    public Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<T?> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        using TransactionScope? scope = CreateTransactionScope();
        T? result = await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefaultAsync(cancellationToken);
        scope?.Complete();
        return result;
    }

    #endregion

    #region [ IQueryRelationAsync ]

    public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
        where TProperty : class
    {
        return dbContext.Entry(entity).Reference(Expression.Lambda<Func<T, TProperty?>>(propertyExpression.Body, propertyExpression.Parameters)).LoadAsync(cancellationToken);
    }

    public Task LoadRelationAsync<TProperty>(T entity,
        Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default)
        where TProperty : class
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

        return query.ToListAsync(cancellationToken);
    }

    public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity,
        Expression<Func<T, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default) where TProperty : class
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

        return query.ToListAsync(cancellationToken);
    }

    public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity,
        Expression<Func<T, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default) where TProperty : class
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

        return query.ToListAsync(cancellationToken);
    }

    #endregion

    #region [ ICommandAsync ]

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
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
            await dbEntities.AddAsync(entity, cancellationToken);
        }
    }

    public Task AddAsync(IList<T> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return Task.FromResult(false);
        }

        return Task.WhenAll(entities.Select(x => AddAsync(x, cancellationToken)));
    }

    public async Task ModifyAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(entity.EntityKey);
        T? entityDb = await dbContext.Set<T>().FindAsync([entity.EntityKey], cancellationToken);

        if (entityDb == null)
        {
            return;
        }

        // properties that can not be changed

        if (entity is IEntityDateLog dateLog && entityDb is IEntityDateLog dateLogDb)
        {
            dateLog.Created = dateLogDb.Created;
            dateLog.Modified = dateLogDb.Modified;
        }

        if (EntityLogAccessor.HasEntityLog(entity))
        {
            EntityLogAccessor.CopyPropertyValue(entityDb, entity, "CreatedBy");
            EntityLogAccessor.CopyPropertyValue(entityDb, entity, "ModifiedBy");
        }

        dbContext.Entry(entityDb).CurrentValues.SetValues(entity);
    }

    public Task ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return Task.FromResult(false);
        }

        return Task.WhenAll(entities.Select(x => ModifyAsync(x, cancellationToken)));
    }

    public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return;
        }

        bool hasUserLog = EntityLogAccessor.HasEntityLog(entity);

        if (entity is IEntityDateLog dateLog)
        {
            // TODO (task 4.2b): TimeZoneHelper is obsolete. Swapping it for IClock requires
            // injecting the clock into the repository and would change the timezone of the
            // stamped value (helper resolves South America; IClock.Now uses TimeZoneInfo.Local).
#pragma warning disable CS0618 // intentional: legacy IEntityDateLog stamping until removal in v12
            dateLog.Removed = TimeZoneHelper.GetTimeZoneNow();
#pragma warning restore CS0618
            if (hasUserLog)
            {
                object removedBy = EntityLogBy ?? throw new InvalidOperationException("EntityLogBy is not available.");
                EntityLogAccessor.TrySetPropertyValue(entity, "RemovedBy", removedBy);
            }
            await ModifyAsync(entity, cancellationToken);
        }
        else
        {
            await ForceRemoveAsync(entity, cancellationToken);
        }
    }

    public Task RemoveAsync(IList<T> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return Task.FromResult(false);
        }

        return Task.WhenAll(entities.Select(x => RemoveAsync(x, cancellationToken)));
    }

    public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        T? entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return;
        }
        await RemoveAsync(entity, cancellationToken);
    }

    public Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
    {
        if (!ids.AnySafe())
        {
            return Task.FromResult(false);
        }

        return Task.WhenAll(ids.Select(x => RemoveByIdAsync(x, cancellationToken)));
    }

    public Task ForceRemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(false);
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
        return Task.FromResult(true);
    }

    #endregion

    #region [ Properties ]

    protected override object? EntityLogBy => (dbContext as Mvp24HoursContext)?.EntityLogBy;

    #endregion
}

/// <summary>
///  <see cref="IRepositoryAsync{T, TId}"/>
/// </summary>
/// <remarks>
/// Additive wrapper over <see cref="RepositoryAsync{T}"/>. Every typed member delegates to the
/// <see cref="object"/>-based member of the base class, so each operation keeps a single
/// real implementation and cannot diverge in behavior.
/// </remarks>
public class RepositoryAsync<T, TId>(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options)
    : RepositoryAsync<T>(_dbContext, options), IRepositoryAsync<T, TId>
    where T : class, IEntityBase, IEntity<TId>
{
    /// <summary>
    ///  <see cref="IRepositoryAsync{T, TId}.GetByIdAsync(TId, CancellationToken)"/>
    /// </summary>
    public Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return base.GetByIdAsync((object)id!, cancellationToken);
    }

    /// <summary>
    ///  <see cref="IRepositoryAsync{T, TId}.GetByIdAsync(TId, IPagingCriteria, CancellationToken)"/>
    /// </summary>
    public Task<T?> GetByIdAsync(TId id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return base.GetByIdAsync((object)id!, criteria, cancellationToken);
    }

    /// <summary>
    ///  <see cref="IRepositoryAsync{T, TId}.RemoveByIdAsync(TId, CancellationToken)"/>
    /// </summary>
    public Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return base.RemoveByIdAsync((object)id!, cancellationToken);
    }

    /// <summary>
    ///  <see cref="IRepositoryAsync{T, TId}.RemoveByIdAsync(IList{TId}, CancellationToken)"/>
    /// </summary>
    public Task RemoveByIdAsync(IList<TId> ids, CancellationToken cancellationToken = default)
    {
        return base.RemoveByIdAsync(ids?.Cast<object>().ToList()!, cancellationToken);
    }
}
