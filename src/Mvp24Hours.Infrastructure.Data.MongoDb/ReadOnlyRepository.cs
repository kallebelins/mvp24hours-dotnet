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
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Helpers;
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
    public class ReadOnlyRepository<T>(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options) 
        : RepositoryBase<T>(dbContext, options), IReadOnlyRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery Methods ]

        /// <inheritdoc />
        public bool ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-listany-start");
            try
            {
                return GetQuery(null, true).Any();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-listany-end"); }
        }

        /// <inheritdoc />
        public int ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-listcount-start");
            try
            {
                return GetQuery(null, true).Count();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-listcount-end"); }
        }

        /// <inheritdoc />
        public IList<T> List()
        {
            return List(null);
        }

        /// <inheritdoc />
        public IList<T> List(IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-list-start");
            try
            {
                return GetQuery(criteria).ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-list-end"); }
        }

        /// <inheritdoc />
        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyany-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Any();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyany-end"); }
        }

        /// <inheritdoc />
        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbycount-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, null, true).Count();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbycount-end"); }
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getby-start");
            try
            {
                var query = dbEntities.AsQueryable();
                if (clause != null)
                {
                    query = query.Where(clause);
                }
                return GetQuery(query, criteria).ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getby-end"); }
        }

        /// <inheritdoc />
        public T GetById(object id)
        {
            return GetById(id, null)!;
        }

        /// <inheritdoc />
        public T GetById(object id, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyid-start");
            try
            {
                return GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id)
                    .SingleOrDefault()!;
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyid-end"); }
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-anybyspecification-start");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.Any();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-anybyspecification-end"); }
        }

        /// <inheritdoc />
        public int CountBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-countbyspecification-start");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.Count();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-countbyspecification-end"); }
        }

        /// <inheritdoc />
        public IList<T> GetBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyspecification-start");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.ToList();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbyspecification-end"); }
        }

        /// <inheritdoc />
        public T? GetSingleBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getsinglebyspecification-start");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.SingleOrDefault();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getsinglebyspecification-end"); }
        }

        /// <inheritdoc />
        public T? GetFirstBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getfirstbyspecification-start");
            try
            {
                var query = MongoDbSpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
                return query.FirstOrDefault();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getfirstbyspecification-end"); }
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-end"); }
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-spec-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-spec-end"); }
        }

        /// <inheritdoc />
        public IKeysetPageResultString<T> GetByKeysetPagination(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-string-start");
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-readonlyrepository-getbykeysetpagination-string-end"); }
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

