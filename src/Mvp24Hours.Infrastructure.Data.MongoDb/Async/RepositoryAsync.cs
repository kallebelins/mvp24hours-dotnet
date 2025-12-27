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
    public class RepositoryAsync<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<RepositoryAsync<T>> logger = null) : RepositoryBase<T>(dbContext, options), IRepositoryAsync<T>
        where T : class, IEntityBase
    {
        private readonly ILogger<RepositoryAsync<T>> _logger = logger;
        #region [ IQueryAsync ]

        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Checking if any entities exist in collection {CollectionName}", typeof(T).Name);
            try
            {
                var result = await GetQuery(null, true)
                .AnyAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("ListAnyAsync result: {Result} for collection {CollectionName}", result, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if any entities exist in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Counting entities in collection {CollectionName}", typeof(T).Name);
            try
            {
                var count = await GetQuery(null, true)
                .CountAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("ListCountAsync result: {Count} for collection {CollectionName}", count, typeof(T).Name);
                return count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error counting entities in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await ListAsync(null, cancellationToken: cancellationToken);
        }

        public async Task<IList<T>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Listing entities from collection {CollectionName} with paging criteria", typeof(T).Name);
            try
            {
                var result = await GetQuery(criteria)
                                .ToListAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("ListAsync returned {Count} entities from collection {CollectionName}", result.Count, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error listing entities from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Checking if any entities match clause in collection {CollectionName}", typeof(T).Name);
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var result = await GetQuery(query, null, true)
                    .AnyAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("GetByAnyAsync result: {Result} for collection {CollectionName}", result, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if any entities match clause in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Counting entities matching clause in collection {CollectionName}", typeof(T).Name);
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var count = await GetQuery(query, null, true)
                    .CountAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("GetByCountAsync result: {Count} for collection {CollectionName}", count, typeof(T).Name);
                return count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error counting entities matching clause in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return await GetByAsync(clause, null, cancellationToken: cancellationToken);
        }

        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting entities matching clause from collection {CollectionName}", typeof(T).Name);
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                var result = await GetQuery(query, criteria)
                    .ToListAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("GetByAsync returned {Count} entities from collection {CollectionName}", result.Count, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting entities matching clause from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await GetByIdAsync(id, null, cancellationToken: cancellationToken);
        }

        public async Task<T> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting entity by id {Id} from collection {CollectionName}", id, typeof(T).Name);
            try
            {
                var result = await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);
                _logger?.LogDebug("GetByIdAsync {(Found)} entity with id {Id} from collection {CollectionName}", result != null ? "found" : "not found", id, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting entity by id {Id} from collection {CollectionName}", id, typeof(T).Name);
                throw;
            }
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
            _logger?.LogDebug("Adding entity to collection {CollectionName}", typeof(T).Name);
            try
            {
                if (entity == null)
                {
                    _logger?.LogWarning("Attempted to add null entity to collection {CollectionName}", typeof(T).Name);
                    return;
                }
                await dbEntities.InsertOneAsync(entity, cancellationToken: cancellationToken);
                _logger?.LogDebug("Successfully added entity to collection {CollectionName}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding entity to collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task AddAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Adding {Count} entities to collection {CollectionName}", entities?.Count ?? 0, typeof(T).Name);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await AddAsync(entity, cancellationToken: cancellationToken);
                    }
                    _logger?.LogDebug("Successfully added {Count} entities to collection {CollectionName}", entities.Count, typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding entities to collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task ModifyAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Modifying entity in collection {CollectionName}", typeof(T).Name);
            try
            {
                if (entity == null)
                {
                    _logger?.LogWarning("Attempted to modify null entity in collection {CollectionName}", typeof(T).Name);
                    return;
                }

                T entityDb = (await dbContext.Set<T>().FindAsync(GetKeyFilter(entity), cancellationToken: cancellationToken)).FirstOrDefault(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Key value not found.");

                // properties that can not be changed

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    _logger?.LogDebug("Preserving audit fields for entity in collection {CollectionName}", typeof(T).Name);
                    var entityLog = entity as IEntityLog<object>;
                    var entityDbLog = entityDb as IEntityLog<object>;
                    entityLog.Created = entityDbLog.Created;
                    entityLog.CreatedBy = entityDbLog.CreatedBy;
                    entityLog.Modified = entityDbLog.Modified;
                    entityLog.ModifiedBy = entityDbLog.ModifiedBy;
                }

                await dbEntities.ReplaceOneAsync(GetKeyFilter(entity), entity, cancellationToken: cancellationToken);
                _logger?.LogDebug("Successfully modified entity in collection {CollectionName}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error modifying entity in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Modifying {Count} entities in collection {CollectionName}", entities?.Count ?? 0, typeof(T).Name);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await ModifyAsync(entity, cancellationToken: cancellationToken);
                    }
                    _logger?.LogDebug("Successfully modified {Count} entities in collection {CollectionName}", entities.Count, typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error modifying entities in collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Removing entity from collection {CollectionName}", typeof(T).Name);
            try
            {
                if (entity == null)
                {
                    _logger?.LogWarning("Attempted to remove null entity from collection {CollectionName}", typeof(T).Name);
                    return;
                }

                if (entity.GetType() == typeof(IEntityLog<>))
                {
                    _logger?.LogDebug("Performing soft delete for entity in collection {CollectionName}", typeof(T).Name);
                    var entityLog = entity as IEntityLog<object>;
                    entityLog.Removed = TimeZoneHelper.GetTimeZoneNow();
                    entityLog.RemovedBy = EntityLogBy;
                    await ModifyAsync(entity, cancellationToken: cancellationToken);
                }
                else
                {
                    await ForceRemoveAsync(entity, cancellationToken: cancellationToken);
                }
                _logger?.LogDebug("Successfully removed entity from collection {CollectionName}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing entity from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task RemoveAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Removing {Count} entities from collection {CollectionName}", entities?.Count ?? 0, typeof(T).Name);
            try
            {
                if (entities.AnySafe())
                {
                    foreach (var entity in entities)
                    {
                        await RemoveAsync(entity, cancellationToken: cancellationToken);
                    }
                    _logger?.LogDebug("Successfully removed {Count} entities from collection {CollectionName}", entities.Count, typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing entities from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Removing entity by id {Id} from collection {CollectionName}", id, typeof(T).Name);
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
                if (entity == null)
                {
                    _logger?.LogWarning("Entity with id {Id} not found in collection {CollectionName}", id, typeof(T).Name);
                    return;
                }
                await RemoveAsync(entity, cancellationToken: cancellationToken);
                _logger?.LogDebug("Successfully removed entity with id {Id} from collection {CollectionName}", id, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing entity by id {Id} from collection {CollectionName}", id, typeof(T).Name);
                throw;
            }
        }

        public async Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Removing {Count} entities by ids from collection {CollectionName}", ids?.Count ?? 0, typeof(T).Name);
            try
            {
                if (ids.AnySafe())
                {
                    foreach (var id in ids)
                    {
                        await RemoveByIdAsync(id, cancellationToken: cancellationToken);
                    }
                    _logger?.LogDebug("Successfully removed {Count} entities by ids from collection {CollectionName}", ids.Count, typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing entities by ids from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        private async Task ForceRemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Force removing entity from collection {CollectionName}", typeof(T).Name);
            try
            {
                if (entity == null)
                {
                    _logger?.LogWarning("Attempted to force remove null entity from collection {CollectionName}", typeof(T).Name);
                    return;
                }
                await dbEntities.DeleteOneAsync(GetKeyFilter(entity), cancellationToken: cancellationToken);
                _logger?.LogDebug("Successfully force removed entity from collection {CollectionName}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error force removing entity from collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        #endregion

        #region [ Properties ]

        protected override object EntityLogBy => throw new NotSupportedException();

        #endregion
    }
}

        #endregion
    }
}
