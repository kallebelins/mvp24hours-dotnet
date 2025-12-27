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
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    ///  <see cref="Core.Contract.Data.Async.IRepositoryAsync{T}"/>
    /// </summary>
    public class RepositoryAsync<T>(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<T>(_dbContext, options), IRepositoryAsync<T>
        where T : class, IEntityBase
    {
        #region [ IQueryAsync ]

        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var result = await GetQuery(null, true).AnyAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var result = await GetQuery(null, true).CountAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        public async Task<IList<T>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var result = await GetQuery(criteria).ToListAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, null, true).AnyAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, null, true).CountAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, criteria).ToListAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        public Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken);
        }

        public async Task<T> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var result = await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).SingleOrDefaultAsync(cancellationToken);
            if (scope != null)
            {
                scope.Complete();
            }
            return result;
        }

        #endregion

        #region [ IQueryRelationAsync ]

        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            return this.dbContext.Entry(entity).Reference(propertyExpression).LoadAsync(cancellationToken);
        }

        public Task LoadRelationAsync<TProperty>(T entity,
            Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default)
            where TProperty : class
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

            return query.ToListAsync(cancellationToken);
        }

        public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity,
            Expression<Func<T, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default) where TProperty : class
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

            return query.ToListAsync(cancellationToken);
        }

        public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity,
            Expression<Func<T, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0,
            CancellationToken cancellationToken = default) where TProperty : class
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

            var entry = dbContext.Entry(entity);
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

            return Task.WhenAll(entities?.Select(x => AddAsync(x, cancellationToken)));
        }

        public async Task ModifyAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                return;
            }

            var entityDb = await dbContext.Set<T>().FindAsync(new object[] { entity.EntityKey }, cancellationToken);

            if (entityDb == null)
            {
                return;
            }

            // properties that can not be changed

            if (entity.GetType().InheritsOrImplements(typeof(IEntityLog<>)) || entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>)))
            {
                var entityLog = (dynamic)entity;
                var entityDbLog = (dynamic)entityDb;
                entityLog.Created = entityDbLog.Created;
                entityLog.CreatedBy = entityDbLog.CreatedBy;
                entityLog.Modified = entityDbLog.Modified;
                entityLog.ModifiedBy = entityDbLog.ModifiedBy;
            }

            dbContext.Entry(entityDb).CurrentValues.SetValues(entity);
        }

        public Task ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            if (!entities.AnySafe())
            {
                return Task.FromResult(false);
            }

            return Task.WhenAll(entities?.Select(x => ModifyAsync(x, cancellationToken)));
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                return;
            }

            // properties that can not be changed

            if (entity.GetType().InheritsOrImplements(typeof(IEntityLog<>)) || entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>)))
            {
                var entityLog = (dynamic)entity;
                entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                entityLog.RemovedBy = (dynamic)EntityLogBy;
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

            return Task.WhenAll(entities?.Select(x => RemoveAsync(x, cancellationToken)));
        }

        public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
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

            return Task.WhenAll(ids?.Select(x => RemoveByIdAsync(x, cancellationToken)));
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

            var entry = dbContext.Entry(entity);
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

        protected override object EntityLogBy => (dbContext as Mvp24HoursContext)?.EntityLogBy;

        #endregion
    }
}
