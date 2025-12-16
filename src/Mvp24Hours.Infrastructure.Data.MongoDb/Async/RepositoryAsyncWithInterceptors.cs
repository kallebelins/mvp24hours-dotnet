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
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// MongoDB repository with interceptor pipeline support for audit, soft delete, and logging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This repository extends the standard RepositoryAsync with interceptor support.
    /// Interceptors are executed before and after CRUD operations, enabling:
    /// <list type="bullet">
    ///   <item>Automatic audit field population (Created/Modified timestamps and users)</item>
    ///   <item>Soft delete conversion (physical deletes become logical updates)</item>
    ///   <item>Operation logging and performance monitoring</item>
    ///   <item>Custom business logic execution</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with interceptors:
    /// services.AddMvp24HoursDbContext(...)
    ///         .AddMvp24HoursRepositoryWithInterceptors()
    ///         .AddMongoDbAuditInterceptor()
    ///         .AddMongoDbSoftDeleteInterceptor();
    /// </code>
    /// </example>
    public class RepositoryAsyncWithInterceptors<T> : RepositoryBase<T>, IRepositoryAsync<T>
        where T : class, IEntityBase
    {
        private readonly IMongoDbInterceptorPipeline _interceptorPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryAsyncWithInterceptors{T}"/> class.
        /// </summary>
        /// <param name="dbContext">The MongoDB context.</param>
        /// <param name="options">Repository options.</param>
        /// <param name="interceptorPipeline">The interceptor pipeline (optional, defaults to no-op).</param>
        public RepositoryAsyncWithInterceptors(
            Mvp24HoursContext dbContext,
            IOptions<MongoDbRepositoryOptions> options,
            IMongoDbInterceptorPipeline interceptorPipeline = null)
            : base(dbContext, options)
        {
            _interceptorPipeline = interceptorPipeline ?? NoOpInterceptorPipeline.Instance;
        }

        #region [ IQueryAsync ]

        /// <inheritdoc />
        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listanyasync-start");
            try
            {
                return await GetQuery(null, true)
                    .AnyAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listanyasync-end"); }
        }

        /// <inheritdoc />
        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listcountasync-start");
            try
            {
                return await GetQuery(null, true)
                    .CountAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listcountasync-end"); }
        }

        /// <inheritdoc />
        public Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listasync-start");
            try
            {
                return await GetQuery(criteria)
                    .ToListAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-listasync-end"); }
        }

        /// <inheritdoc />
        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyanyasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyanyasync-end"); }
        }

        /// <inheritdoc />
        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbycountasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbycountasync-end"); }
        }

        /// <inheritdoc />
        public Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyasync-end"); }
        }

        /// <inheritdoc />
        public Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyidasync-start");
            try
            {
                return await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id)
                    .SingleOrDefaultAsync(cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-getbyidasync-end"); }
        }

        #endregion

        #region [ IQueryRelationAsync ]

        /// <inheritdoc />
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException("Relationship loading via navigation not available for MongoDB.");
        }

        /// <inheritdoc />
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException("Relationship loading via navigation not available for MongoDB.");
        }

        /// <inheritdoc />
        public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException("Relationship loading via navigation not available for MongoDB.");
        }

        /// <inheritdoc />
        public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>> clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException("Relationship loading via navigation not available for MongoDB.");
        }

        #endregion

        #region [ ICommandAsync with Interceptors ]

        /// <inheritdoc />
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-addasync-start");
            try
            {
                if (entity == null) return;

                await _interceptorPipeline.ExecuteInsertAsync(entity, async () =>
                {
                    await dbEntities.InsertOneAsync(entity, cancellationToken: cancellationToken);
                }, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-addasync-end"); }
        }

        /// <inheritdoc />
        public async Task AddAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-addlistasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-addlistasync-end"); }
        }

        /// <inheritdoc />
        public async Task ModifyAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-modifyasync-start");
            try
            {
                if (entity == null) return;

                await _interceptorPipeline.ExecuteUpdateAsync(entity, async () =>
                {
                    T entityDb = (await dbContext.Set<T>().FindAsync(GetKeyFilter(entity), cancellationToken: cancellationToken))
                        .FirstOrDefault(cancellationToken: cancellationToken)
                        ?? throw new InvalidOperationException("Key value not found.");

                    // Preserve creation audit fields for legacy IEntityLog
                    PreserveCreationAuditFields(entity, entityDb);

                    await dbEntities.ReplaceOneAsync(GetKeyFilter(entity), entity, cancellationToken: cancellationToken);
                }, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-modifyasync-end"); }
        }

        /// <inheritdoc />
        public async Task ModifyAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-modifylistasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-modifylistasync-end"); }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removeasync-start");
            try
            {
                if (entity == null) return;

                var wasSoftDeleted = await _interceptorPipeline.ExecuteDeleteAsync(
                    entity,
                    hardDeleteOperation: async () =>
                    {
                        await dbEntities.DeleteOneAsync(GetKeyFilter(entity), cancellationToken: cancellationToken);
                    },
                    softDeleteOperation: async () =>
                    {
                        // Soft delete via interceptor - entity already modified by SoftDeleteInterceptor
                        await dbEntities.ReplaceOneAsync(GetKeyFilter(entity), entity, cancellationToken: cancellationToken);
                    },
                    cancellationToken);

                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    wasSoftDeleted ? "mongodb-repositoryasync-softdeleteasync" : "mongodb-repositoryasync-harddeleteasync");
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removeasync-end"); }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(IList<T> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removelistasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removelistasync-end"); }
        }

        /// <inheritdoc />
        public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removebyidasync-start");
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
                if (entity != null)
                {
                    await RemoveAsync(entity, cancellationToken: cancellationToken);
                }
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removebyidasync-end"); }
        }

        /// <inheritdoc />
        public async Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removebyidlistasync-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-repositoryasync-removebyidlistasync-end"); }
        }

        #endregion

        #region [ Helpers ]

        /// <summary>
        /// Preserves creation audit fields when updating an entity.
        /// </summary>
        private static void PreserveCreationAuditFields(T entity, T entityDb)
        {
            // Handle legacy IEntityLog<T>
            if (entity is IEntityDateLog entityDateLog && entityDb is IEntityDateLog entityDbDateLog)
            {
                entityDateLog.Created = entityDbDateLog.Created;
            }

            // Handle IAuditableEntity
            if (entity is IAuditableEntity auditable && entityDb is IAuditableEntity auditableDb)
            {
                auditable.CreatedAt = auditableDb.CreatedAt;
                auditable.CreatedBy = auditableDb.CreatedBy;
            }

            // Try to preserve for generic IEntityLog<T>
            var entityType = entity.GetType();
            foreach (var iface in entityType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEntityLog<>))
                {
                    CopyPropertyValue(entityDb, entity, "Created");
                    CopyPropertyValue(entityDb, entity, "CreatedBy");
                    break;
                }
            }
        }

        private static void CopyPropertyValue(object source, object target, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }

        /// <inheritdoc />
        protected override object EntityLogBy => throw new NotSupportedException(
            "EntityLogBy is not supported. Use ICurrentUserProvider with AuditInterceptor instead.");

        #endregion
    }
}

