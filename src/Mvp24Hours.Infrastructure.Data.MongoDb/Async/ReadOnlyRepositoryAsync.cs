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
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb.Base;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Asynchronous read-only repository implementation for MongoDB.
    /// Provides query-only access to entities with Specification Pattern support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This repository does not include any command methods (Add, Modify, Remove),
    /// making it suitable for CQRS query handlers and read-only scenarios.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    ///   <item>Full Specification Pattern support</item>
    ///   <item>Keyset pagination (cursor-based) for efficient large dataset queries</item>
    ///   <item>Full async/await support with CancellationToken</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with Specification Pattern
    /// var spec = new ActiveCustomerSpecification();
    /// var customers = await repository.GetBySpecificationAsync(spec);
    /// 
    /// // Using with keyset pagination
    /// var page = await repository.GetByKeysetPaginationAsync&lt;DateTime&gt;(
    ///     clause: c => c.IsActive,
    ///     keySelector: c => c.CreatedAt,
    ///     lastKey: null,
    ///     pageSize: 20);
    /// </code>
    /// </example>
    public class ReadOnlyRepositoryAsync<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<RepositoryBase<T>>? logger = null) 
        : RepositoryBase<T>(dbContext, options, logger), IReadOnlyRepositoryAsync<T>
        where T : class, IEntityBase
    {
        #region [ IQueryAsync Methods ]

        /// <inheritdoc />
        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async ListAnyAsync operation started.");
            try
            {
                return await ((IMongoQueryable<T>)GetQuery(null, true)).AnyAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async ListAnyAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async ListCountAsync operation started.");
            try
            {
                return await ((IMongoQueryable<T>)GetQuery(null, true)).CountAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async ListCountAsync operation completed."); }
        }

        /// <inheritdoc />
        public Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async ListAsync operation started.");
            try
            {
                return await ((IMongoQueryable<T>)GetQuery(criteria)).ToListAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async ListAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByAnyAsync operation started.");
            try
            {
                IMongoQueryable<T> query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = (IMongoQueryable<T>)query.Where(clause);
                }
                return await ((IMongoQueryable<T>)GetQuery(query, null, true)).AnyAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByAnyAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByCountAsync operation started.");
            try
            {
                IMongoQueryable<T> query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = (IMongoQueryable<T>)query.Where(clause);
                }
                return await ((IMongoQueryable<T>)GetQuery(query, null, true)).CountAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByCountAsync operation completed."); }
        }

        /// <inheritdoc />
        public Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByAsync operation started.");
            try
            {
                IMongoQueryable<T> query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = (IMongoQueryable<T>)query.Where(clause);
                }
                return await ((IMongoQueryable<T>)GetQuery(query, criteria)).ToListAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByAsync operation completed."); }
        }

        /// <inheritdoc />
        public Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken)!;
        }

        /// <inheritdoc />
        public async Task<T> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByIdAsync operation started.");
            try
            {
                return await ((IMongoQueryable<T>)GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id))
                    .SingleOrDefaultAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByIdAsync operation completed."); }
        }

        #endregion

        #region [ IQueryRelationAsync Methods ]

        /// <inheritdoc />
        /// <remarks>
        /// MongoDB does not support lazy loading of navigation properties like EF Core.
        /// Consider using embedded documents or aggregation pipeline with $lookup.
        /// </remarks>
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        #endregion

        #region [ Specification Pattern Methods ]

        /// <inheritdoc />
        public async Task<bool> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async AnyBySpecificationAsync operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return await ((IMongoQueryable<T>)query).AnyAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async AnyBySpecificationAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<int> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async CountBySpecificationAsync operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return await ((IMongoQueryable<T>)query).CountAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async CountBySpecificationAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<IList<T>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async GetBySpecificationAsync operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return await ((IMongoQueryable<T>)query).ToListAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetBySpecificationAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<T?> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async GetSingleBySpecificationAsync operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return await ((IMongoQueryable<T>)query).SingleOrDefaultAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetSingleBySpecificationAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<T?> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async GetFirstBySpecificationAsync operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return await ((IMongoQueryable<T>)query).FirstOrDefaultAsync(cancellationToken);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetFirstBySpecificationAsync operation completed."); }
        }

        #endregion

        #region [ Keyset Pagination Methods ]

        /// <inheritdoc />
        public async Task<IKeysetPageResult<T, TKey>> GetByKeysetPaginationAsync<TKey>(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default) where TKey : struct
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync operation started.");
            try
            {
                IMongoQueryable<T> query = dbEntities.AsQueryable();

                // Apply filter clause
                if (clause != null)
                {
                    query = (IMongoQueryable<T>)query.Where(clause);
                }

                // Apply keyset condition
                if (lastKey.HasValue)
                {
                    query = (IMongoQueryable<T>)ApplyKeysetCondition(query, keySelector, lastKey.Value, ascending);
                }

                // Apply ordering
                query = ascending
                    ? (IMongoQueryable<T>)query.OrderBy(keySelector)
                    : (IMongoQueryable<T>)query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);

                var hasMore = items.Count > pageSize;
                if (hasMore)
                {
                    items.RemoveAt(items.Count - 1);
                }

                TKey? lastKeyValue = items.Count > 0
                    ? keySelector.Compile()(items[^1])
                    : lastKey;

                return new KeysetPageResult<T, TKey>(items, lastKeyValue, hasMore, pageSize);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync operation completed."); }
        }

        /// <inheritdoc />
        public async Task<IKeysetPageResult<T, TKey>> GetByKeysetPaginationAsync<TKey, TSpec>(
            TSpec specification,
            Expression<Func<T, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default)
            where TKey : struct
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync with specification operation started.");
            try
            {
                var baseQuery = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                IMongoQueryable<T> query = (IMongoQueryable<T>)baseQuery;

                // Apply keyset condition
                if (lastKey.HasValue)
                {
                    query = (IMongoQueryable<T>)ApplyKeysetCondition(query, keySelector, lastKey.Value, ascending);
                }

                // Apply ordering (override specification ordering for keyset)
                query = ascending
                    ? (IMongoQueryable<T>)query.OrderBy(keySelector)
                    : (IMongoQueryable<T>)query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);

                var hasMore = items.Count > pageSize;
                if (hasMore)
                {
                    items.RemoveAt(items.Count - 1);
                }

                TKey? lastKeyValue = items.Count > 0
                    ? keySelector.Compile()(items[^1])
                    : lastKey;

                return new KeysetPageResult<T, TKey>(items, lastKeyValue, hasMore, pageSize);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync with specification operation completed."); }
        }

        /// <inheritdoc />
        public async Task<IKeysetPageResultString<T>> GetByKeysetPaginationAsync(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync (string key) operation started.");
            try
            {
                IMongoQueryable<T> query = dbEntities.AsQueryable();

                // Apply filter clause
                if (clause != null)
                {
                    query = (IMongoQueryable<T>)query.Where(clause);
                }

                // Apply keyset condition
                if (!string.IsNullOrEmpty(lastKey))
                {
                    query = (IMongoQueryable<T>)ApplyKeysetConditionString(query, keySelector, lastKey, ascending);
                }

                // Apply ordering
                query = ascending
                    ? (IMongoQueryable<T>)query.OrderBy(keySelector)
                    : (IMongoQueryable<T>)query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);

                var hasMore = items.Count > pageSize;
                if (hasMore)
                {
                    items.RemoveAt(items.Count - 1);
                }

                var lastKeyValue = items.Count > 0
                    ? keySelector.Compile()(items[^1])
                    : lastKey;

                return new KeysetPageResultString<T>(items, lastKeyValue, hasMore, pageSize);
            }
            finally { _logger?.LogDebug("MongoDB read-only repository async GetByKeysetPaginationAsync (string key) operation completed."); }
        }

        #endregion

        #region [ Protected Properties ]

        /// <inheritdoc />
        protected override object EntityLogBy => throw new NotSupportedException(
            "EntityLogBy is not used in ReadOnlyRepository as it does not perform write operations.");

        #endregion

        #region [ Private Helper Methods ]

        /// <summary>
        /// Applies the keyset condition to the query for struct keys.
        /// </summary>
        private static IQueryable<T> ApplyKeysetCondition<TKey>(
            IQueryable<T> query,
            Expression<Func<T, TKey>> keySelector,
            TKey lastKey,
            bool ascending) where TKey : struct
        {
            var parameter = keySelector.Parameters[0];
            var keyProperty = keySelector.Body;
            var lastKeyConstant = Expression.Constant(lastKey, typeof(TKey));

            var comparison = ascending
                ? Expression.GreaterThan(keyProperty, lastKeyConstant)
                : Expression.LessThan(keyProperty, lastKeyConstant);

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }

        /// <summary>
        /// Applies the keyset condition to the query for string keys.
        /// </summary>
        private static IQueryable<T> ApplyKeysetConditionString(
            IQueryable<T> query,
            Expression<Func<T, string>> keySelector,
            string lastKey,
            bool ascending)
        {
            var parameter = keySelector.Parameters[0];
            var keyProperty = keySelector.Body;
            var lastKeyConstant = Expression.Constant(lastKey, typeof(string));

            // Use string.CompareTo for string comparison
            var compareToMethod = typeof(string).GetMethod("CompareTo", [typeof(string)])!;
            var compareCall = Expression.Call(keyProperty, compareToMethod, lastKeyConstant);
            var zero = Expression.Constant(0);

            var comparison = ascending
                ? Expression.GreaterThan(compareCall, zero)
                : Expression.LessThan(compareCall, zero);

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }

        #endregion
    }
}

