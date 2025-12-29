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

#nullable enable

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Synchronous read-only repository implementation for MongoDB.
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
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> For async operations, prefer using <see cref="ReadOnlyRepositoryAsync{T}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with Specification Pattern
    /// var spec = new ActiveCustomerSpecification();
    /// var customers = repository.GetBySpecification(spec);
    /// 
    /// // Using with keyset pagination
    /// var page = repository.GetByKeysetPagination&lt;DateTime&gt;(
    ///     clause: c => c.IsActive,
    ///     keySelector: c => c.CreatedAt,
    ///     lastKey: null,
    ///     pageSize: 20);
    /// </code>
    /// </example>
    public class ReadOnlyRepository<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<RepositoryBase<T>>? logger = null) 
        : RepositoryBase<T>(dbContext, options, logger), IReadOnlyRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery Methods ]

        /// <inheritdoc />
        public bool ListAny()
        {
            _logger?.LogDebug("MongoDB read-only repository ListAny operation started.");
            try
            {
                return GetQuery(null, true).Any();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository ListAny operation completed."); }
        }

        /// <inheritdoc />
        public int ListCount()
        {
            _logger?.LogDebug("MongoDB read-only repository ListCount operation started.");
            try
            {
                return GetQuery(null, true).Count();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository ListCount operation completed."); }
        }

        /// <inheritdoc />
        public IList<T> List()
        {
            return List(null);
        }

        /// <inheritdoc />
        public IList<T> List(IPagingCriteria? criteria)
        {
            _logger?.LogDebug("MongoDB read-only repository List operation started.");
            try
            {
                return GetQuery(criteria).ToList();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository List operation completed."); }
        }

        /// <inheritdoc />
        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            _logger?.LogDebug("MongoDB read-only repository GetByAny operation started.");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Any();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetByAny operation completed."); }
        }

        /// <inheritdoc />
        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            _logger?.LogDebug("MongoDB read-only repository GetByCount operation started.");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Count();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetByCount operation completed."); }
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria? criteria)
        {
            _logger?.LogDebug("MongoDB read-only repository GetBy operation started.");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, criteria).ToList();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetBy operation completed."); }
        }

        /// <inheritdoc />
        public T GetById(object id)
        {
            return GetById(id, null)!;
        }

        /// <inheritdoc />
        public T GetById(object id, IPagingCriteria? criteria)
        {
            _logger?.LogDebug("MongoDB read-only repository GetById operation started.");
            try
            {
                return GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id)
                    .SingleOrDefault()!;
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetById operation completed."); }
        }

        #endregion

        #region [ IQueryRelation Methods ]

        /// <inheritdoc />
        /// <remarks>
        /// MongoDB does not support lazy loading of navigation properties like EF Core.
        /// Consider using embedded documents or aggregation pipeline with $lookup.
        /// </remarks>
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        /// <inheritdoc />
        public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            throw new NotSupportedException(
                "Relationship loading via navigation not available for MongoDB. " +
                "Consider using embedded documents or aggregation pipeline with $lookup.");
        }

        #endregion

        #region [ Specification Pattern Methods ]

        /// <inheritdoc />
        public bool AnyBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository AnyBySpecification operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.Any();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository AnyBySpecification operation completed."); }
        }

        /// <inheritdoc />
        public int CountBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository CountBySpecification operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.Count();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository CountBySpecification operation completed."); }
        }

        /// <inheritdoc />
        public IList<T> GetBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository GetBySpecification operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.ToList();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetBySpecification operation completed."); }
        }

        /// <inheritdoc />
        public T? GetSingleBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository GetSingleBySpecification operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.SingleOrDefault();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetSingleBySpecification operation completed."); }
        }

        /// <inheritdoc />
        public T? GetFirstBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository GetFirstBySpecification operation started.");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.FirstOrDefault();
            }
            finally { _logger?.LogDebug("MongoDB read-only repository GetFirstBySpecification operation completed."); }
        }

        #endregion

        #region [ Keyset Pagination Methods ]

        /// <inheritdoc />
        public IKeysetPageResult<T, TKey> GetByKeysetPagination<TKey>(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true) where TKey : struct
        {
            _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination operation started.");
            try
            {
                IQueryable<T> query = dbEntities.AsQueryable();

                // Apply filter clause
                if (clause != null)
                {
                    query = query.Where(clause);
                }

                // Apply keyset condition
                if (lastKey.HasValue)
                {
                    query = ApplyKeysetCondition(query, keySelector, lastKey.Value, ascending);
                }

                // Apply ordering
                query = ascending
                    ? query.OrderBy(keySelector)
                    : query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = query.Take(pageSize + 1).ToList();

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
            finally { _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination operation completed."); }
        }

        /// <inheritdoc />
        public IKeysetPageResult<T, TKey> GetByKeysetPagination<TKey, TSpec>(
            TSpec specification,
            Expression<Func<T, TKey>> keySelector,
            TKey? lastKey,
            int pageSize,
            bool ascending = true) 
            where TKey : struct
            where TSpec : ISpecificationQuery<T>
        {
            _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination with specification operation started.");
            try
            {
                IQueryable<T> query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);

                // Apply keyset condition
                if (lastKey.HasValue)
                {
                    query = ApplyKeysetCondition(query, keySelector, lastKey.Value, ascending);
                }

                // Apply ordering (override specification ordering for keyset)
                query = ascending
                    ? query.OrderBy(keySelector)
                    : query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = query.Take(pageSize + 1).ToList();

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
            finally { _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination with specification operation completed."); }
        }

        /// <inheritdoc />
        public IKeysetPageResultString<T> GetByKeysetPagination(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true)
        {
            _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination (string key) operation started.");
            try
            {
                IQueryable<T> query = dbEntities.AsQueryable();

                // Apply filter clause
                if (clause != null)
                {
                    query = query.Where(clause);
                }

                // Apply keyset condition
                if (!string.IsNullOrEmpty(lastKey))
                {
                    query = ApplyKeysetConditionString(query, keySelector, lastKey, ascending);
                }

                // Apply ordering
                query = ascending
                    ? query.OrderBy(keySelector)
                    : query.OrderByDescending(keySelector);

                // Fetch one extra item to determine if there are more pages
                var items = query.Take(pageSize + 1).ToList();

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
            finally { _logger?.LogDebug("MongoDB read-only repository GetByKeysetPagination (string key) operation completed."); }
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

