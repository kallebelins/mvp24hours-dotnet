//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for applying tenant filters to MongoDB queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide automatic tenant filtering for MongoDB queries,
    /// ensuring data isolation between tenants in a multi-tenant application.
    /// </para>
    /// <para>
    /// <strong>Usage patterns:</strong>
    /// <code>
    /// // Apply tenant filter to IQueryable:
    /// var query = collection.AsQueryable().ApplyTenantFilter(tenantProvider);
    /// 
    /// // Apply tenant filter to FilterDefinition:
    /// var filter = Builders&lt;MyEntity&gt;.Filter.Empty.WithTenantFilter(tenantProvider);
    /// 
    /// // Create aggregation pipeline with tenant filter:
    /// var pipeline = collection.Aggregate().MatchTenant(tenantProvider);
    /// </code>
    /// </para>
    /// </remarks>
    public static class MongoDbTenantExtensions
    {
        #region IQueryable Extensions

        /// <summary>
        /// Applies a tenant filter to the query for entities implementing <see cref="ITenantEntity"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The queryable to filter.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>The filtered queryable.</returns>
        /// <remarks>
        /// If the entity doesn't implement <see cref="ITenantEntity"/>, the query is returned unchanged.
        /// If no tenant is set (tenantProvider.TenantId is null), the query is returned unchanged (bypass filter).
        /// </remarks>
        public static IQueryable<T> ApplyTenantFilter<T>(
            this IQueryable<T> query,
            ITenantProvider tenantProvider)
            where T : class
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            // Check if entity implements ITenantEntity
            if (!typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
            {
                return query;
            }

            // Bypass filter if no tenant is set
            if (string.IsNullOrEmpty(tenantProvider.TenantId))
            {
                return query;
            }

            var tenantId = tenantProvider.TenantId;

            // Build expression: e => e.TenantId == tenantId
            var parameter = Expression.Parameter(typeof(T), "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var tenantIdConstant = Expression.Constant(tenantId, typeof(string));
            var equalExpression = Expression.Equal(tenantIdProperty, tenantIdConstant);
            var lambda = Expression.Lambda<Func<T, bool>>(equalExpression, parameter);

            return query.Where(lambda);
        }

        /// <summary>
        /// Applies a tenant filter with a specific tenant ID.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The queryable to filter.</param>
        /// <param name="tenantId">The tenant ID to filter by.</param>
        /// <returns>The filtered queryable.</returns>
        public static IQueryable<T> ApplyTenantFilter<T>(
            this IQueryable<T> query,
            string tenantId)
            where T : class, ITenantEntity
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return query;
            }

            return query.Where(e => e.TenantId == tenantId);
        }

        /// <summary>
        /// Applies a tenant filter for generic ITenantEntity&lt;TTenantId&gt;.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TTenantId">The tenant ID type.</typeparam>
        /// <param name="query">The queryable to filter.</param>
        /// <param name="tenantId">The tenant ID to filter by.</param>
        /// <returns>The filtered queryable.</returns>
        public static IQueryable<T> ApplyTenantFilter<T, TTenantId>(
            this IQueryable<T> query,
            TTenantId tenantId)
            where T : class, ITenantEntity<TTenantId>
        {
            if (tenantId == null || tenantId.Equals(default(TTenantId)))
            {
                return query;
            }

            // Build expression: e => e.TenantId.Equals(tenantId)
            var parameter = Expression.Parameter(typeof(T), "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity<TTenantId>.TenantId));
            var tenantIdConstant = Expression.Constant(tenantId, typeof(TTenantId));
            var equalExpression = Expression.Equal(tenantIdProperty, tenantIdConstant);
            var lambda = Expression.Lambda<Func<T, bool>>(equalExpression, parameter);

            return query.Where(lambda);
        }

        #endregion

        #region FilterDefinition Extensions

        /// <summary>
        /// Adds a tenant filter to an existing filter definition.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="filter">The existing filter.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>A combined filter with tenant restriction.</returns>
        public static FilterDefinition<T> WithTenantFilter<T>(
            this FilterDefinition<T> filter,
            ITenantProvider tenantProvider)
            where T : class, ITenantEntity
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            // Bypass filter if no tenant is set
            if (string.IsNullOrEmpty(tenantProvider.TenantId))
            {
                return filter;
            }

            var tenantFilter = Builders<T>.Filter.Eq(e => e.TenantId, tenantProvider.TenantId);
            return Builders<T>.Filter.And(filter, tenantFilter);
        }

        /// <summary>
        /// Adds a tenant filter with a specific tenant ID.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="filter">The existing filter.</param>
        /// <param name="tenantId">The tenant ID to filter by.</param>
        /// <returns>A combined filter with tenant restriction.</returns>
        public static FilterDefinition<T> WithTenantFilter<T>(
            this FilterDefinition<T> filter,
            string tenantId)
            where T : class, ITenantEntity
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return filter;
            }

            var tenantFilter = Builders<T>.Filter.Eq(e => e.TenantId, tenantId);
            return Builders<T>.Filter.And(filter, tenantFilter);
        }

        /// <summary>
        /// Creates a tenant filter definition.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>A filter definition for the current tenant.</returns>
        public static FilterDefinition<T> TenantFilter<T>(ITenantProvider tenantProvider)
            where T : class, ITenantEntity
        {
            if (tenantProvider == null || string.IsNullOrEmpty(tenantProvider.TenantId))
            {
                return Builders<T>.Filter.Empty;
            }

            return Builders<T>.Filter.Eq(e => e.TenantId, tenantProvider.TenantId);
        }

        #endregion

        #region Aggregation Pipeline Extensions

        /// <summary>
        /// Adds a $match stage for tenant filtering at the beginning of an aggregation pipeline.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="aggregate">The aggregate fluent interface.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>The aggregate with tenant match stage.</returns>
        public static IAggregateFluent<T> MatchTenant<T>(
            this IAggregateFluent<T> aggregate,
            ITenantProvider tenantProvider)
            where T : class, ITenantEntity
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            if (string.IsNullOrEmpty(tenantProvider.TenantId))
            {
                return aggregate;
            }

            var filter = Builders<T>.Filter.Eq(e => e.TenantId, tenantProvider.TenantId);

            return aggregate.Match(filter);
        }

        /// <summary>
        /// Adds a $match stage for tenant filtering with a specific tenant ID.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="aggregate">The aggregate fluent interface.</param>
        /// <param name="tenantId">The tenant ID to filter by.</param>
        /// <returns>The aggregate with tenant match stage.</returns>
        public static IAggregateFluent<T> MatchTenant<T>(
            this IAggregateFluent<T> aggregate,
            string tenantId)
            where T : class, ITenantEntity
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return aggregate;
            }

            var filter = Builders<T>.Filter.Eq(e => e.TenantId, tenantId);
            return aggregate.Match(filter);
        }

        #endregion

        #region Combined Filters (Tenant + Soft Delete)

        /// <summary>
        /// Applies both tenant and soft delete filters to a query.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The queryable to filter.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>The filtered queryable.</returns>
        /// <remarks>
        /// This method applies:
        /// - Tenant filter if entity implements ITenantEntity
        /// - Soft delete filter if entity implements ISoftDeletable (IsDeleted == false)
        /// </remarks>
        public static IQueryable<T> ApplyGlobalFilters<T>(
            this IQueryable<T> query,
            ITenantProvider tenantProvider)
            where T : class
        {
            // Apply tenant filter
            if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)) && tenantProvider?.HasTenant == true)
            {
                query = query.ApplyTenantFilter(tenantProvider);
            }

            // Apply soft delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                query = ApplySoftDeleteFilter(query);
            }

            return query;
        }

        private static IQueryable<T> ApplySoftDeleteFilter<T>(IQueryable<T> query)
            where T : class
        {
            // Build expression: e => !((ISoftDeletable)e).IsDeleted
            var parameter = Expression.Parameter(typeof(T), "e");
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notExpression = Expression.Not(isDeletedProperty);
            var lambda = Expression.Lambda<Func<T, bool>>(notExpression, parameter);

            return query.Where(lambda);
        }

        /// <summary>
        /// Creates a combined filter for tenant and soft delete.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="includeSoftDeleted">If true, includes soft-deleted records.</param>
        /// <returns>A combined filter definition.</returns>
        public static FilterDefinition<T> CreateGlobalFilter<T>(
            ITenantProvider tenantProvider,
            bool includeSoftDeleted = false)
            where T : class
        {
            var filters = new System.Collections.Generic.List<FilterDefinition<T>>();

            // Tenant filter
            if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)) && tenantProvider?.HasTenant == true)
            {
                var tenantFilter = Builders<T>.Filter.Eq(nameof(ITenantEntity.TenantId), tenantProvider.TenantId);
                filters.Add(tenantFilter);
            }

            // Soft delete filter
            if (!includeSoftDeleted && typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                var softDeleteFilter = Builders<T>.Filter.Eq(nameof(ISoftDeletable.IsDeleted), false);
                filters.Add(softDeleteFilter);
            }

            if (filters.Count == 0)
            {
                return Builders<T>.Filter.Empty;
            }

            return Builders<T>.Filter.And(filters);
        }

        #endregion
    }
}

