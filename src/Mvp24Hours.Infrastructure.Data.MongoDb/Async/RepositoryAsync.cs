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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    public class RepositoryAsync<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options) : RepositoryBase<T>(dbContext, options), IRepositoryAsync<T>
        where T : class, IEntityBase
    {
        #region [ IQueryAsync ]

        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listanyasync-start");
            try
            {
                return await GetQuery(null, true)
                .AnyAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listanyasync-end"); }
        }

        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listcountasync-start");
            try
            {
                return await GetQuery(null, true)
                .CountAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listcountasync-end"); }
        }

        public async Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await ListAsync(null, cancellationToken: cancellationToken);
        }

        public async Task<IList<T>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listasync-start");
            try
            {
                return await GetQuery(criteria)
                                .ToListAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-listasync-end"); }
        }

        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyanyasync-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return await GetQuery(query, null, true)
                    .AnyAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyanyasync-end"); }
        }

        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbycountasync-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return await GetQuery(query, null, true)
                    .CountAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbycountasync-end"); }
        }

        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return await GetByAsync(clause, null, cancellationToken: cancellationToken);
        }

        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyasync-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return await GetQuery(query, criteria)
                    .ToListAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyasync-end"); }
        }

        public async Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await GetByIdAsync(id, null, cancellationToken: cancellationToken);
        }

        public async Task<T> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyidasync-start");
            try
            {
                return await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-getbyidasync-end"); }
        }

        #endregion

        #region [ IQueryRelationAsync ]
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException();
        }
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException();
        }
        public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException();
        }
        public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException();
        }
        #endregion

        #region [ ICommandAsync ]

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-addasync-start");
            try
            {
                if (entity == null)
                {
                    return;
                }
                await dbEntities.InsertOneAsync(entity, cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-addasync-end"); }
        }

        public async Task AddAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-addlistasync-start");
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await AddAsync(entity, cancellationToken: cancellationToken);
                    }
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-addlistasync-end"); }
        }

        public async Task ModifyAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-modifyasync-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                T entityDb = (await dbContext.Set<T>().FindAsync(GetKeyFilter(entity), cancellationToken: cancellationToken)).FirstOrDefault(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Key value not found.");

                // properties that can not be changed

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-modifyasync-log");
                    var entityLog = entity as IEntityLog<object>;
                    var entityDbLog = entityDb as IEntityLog<object>;
                    entityLog.Created = entityDbLog.Created;
                    entityLog.CreatedBy = entityDbLog.CreatedBy;
                    entityLog.Modified = entityDbLog.Modified;
                    entityLog.ModifiedBy = entityDbLog.ModifiedBy;
                }

                await dbEntities.ReplaceOneAsync(GetKeyFilter(entity), entity, cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-modifyasync-end"); }
        }

        public async Task ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-modifylistasync-start");
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await ModifyAsync(entity, cancellationToken: cancellationToken);
                    }
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-modifylistasync-end"); }
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removeasync-start");
            try
            {
                if (entity == null)
                {
                    return;
                }

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removeasync-log");
                    var entityLog = entity as IEntityLog<object>;
                    entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                    entityLog.RemovedBy = EntityLogBy;
                    await ModifyAsync(entity, cancellationToken: cancellationToken);
                }
                else
                {
                    await ForceRemoveAsync(entity, cancellationToken: cancellationToken);
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removeasync-end"); }
        }

        public async Task RemoveAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removelistasync-start");
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await RemoveAsync(entity, cancellationToken: cancellationToken);
                    }
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removelistasync-end"); }
        }

        public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removebyidasync-start");
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
                if (entity == null)
                {
                    return;
                }
                await RemoveAsync(entity, cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removebyidasync-end"); }
        }

        public async Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removebyidlistasync-start");
            try
            {
                if (ids.AnySafe())
                {
                    foreach (var id in ids)
                    {
                        await RemoveByIdAsync(id, cancellationToken: cancellationToken);
                    }
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-removebyidlistasync-end"); }
        }

        private async Task ForceRemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-forceremoveasync-start");
            try
            {
                if (entity == null)
                {
                    return;
                }
                await dbEntities.DeleteOneAsync(GetKeyFilter(entity), cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositoryasync-forceremoveasync-end"); }
        }

        #endregion

        #region [ Properties ]

        protected override object EntityLogBy => throw new NotSupportedException();

        #endregion
    }
}
