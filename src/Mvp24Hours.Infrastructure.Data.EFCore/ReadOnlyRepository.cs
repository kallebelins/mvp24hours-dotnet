//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    /// Read-only repository implementation for Entity Framework Core.
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
    /// <item>Full Specification Pattern support</item>
    /// <item>Keyset pagination (cursor-based) for efficient large dataset queries</item>
    /// <item>No tracking by default for better read performance</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ReadOnlyRepository<T>(DbContext dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<T>(dbContext, options), IReadOnlyRepository<T>
        where T : class, IEntityBase
    {
        #region [ IQuery Methods ]

        /// <inheritdoc />
        public bool ListAny()
        {
            using var scope = CreateTransactionScope(true);
            var result = GetQuery(null, true).Any();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public int ListCount()
        {
            using var scope = CreateTransactionScope(true);
            var result = GetQuery(null, true).Count();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public IList<T> List()
        {
            return List(null);
        }

        /// <inheritdoc />
        public IList<T> List(IPagingCriteria? criteria)
        {
            using var scope = CreateTransactionScope();
            var result = GetQuery(criteria).AsNoTracking().ToList();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public bool GetByAny(Expression<Func<T, bool>> clause)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = GetQuery(query, null, true).Any();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public int GetByCount(Expression<Func<T, bool>> clause)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = GetQuery(query, null, true).Count();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <inheritdoc />
        public IList<T> GetBy(Expression<Func<T, bool>> clause, IPagingCriteria? criteria)
        {
            using var scope = CreateTransactionScope();
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = GetQuery(query, criteria).AsNoTracking().ToList();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public T? GetById(object id)
        {
            return GetById(id, null);
        }

        /// <inheritdoc />
        public T? GetById(object id, IPagingCriteria? criteria)
        {
            using var scope = CreateTransactionScope();
            var result = GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).AsNoTracking().SingleOrDefault();
            scope?.Complete();
            return result;
        }

        #endregion

        #region [ IQueryRelation Methods ]

        /// <inheritdoc />
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression)
            where TProperty : class
        {
            dbContext.Entry(entity).Reference(propertyExpression).Load();
        }

        /// <inheritdoc />
        public void LoadRelation<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            var query = dbContext.Entry(entity).Collection(propertyExpression).Query();

            if (clause != null)
            {
                query = query.Where(clause);
            }

            if (limit > 0)
            {
                query = query.Take(limit);
            }

            query.Load();
        }

        /// <inheritdoc />
        public void LoadRelationSortByAscending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            var query = dbContext.Entry(entity).Collection(propertyExpression).Query();

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

            query.Load();
        }

        /// <inheritdoc />
        public void LoadRelationSortByDescending<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0)
            where TProperty : class
        {
            var query = dbContext.Entry(entity).Collection(propertyExpression).Query();

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

            query.Load();
        }

        #endregion

        #region [ Specification Pattern Methods ]

        /// <inheritdoc />
        public bool AnyBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope(true);
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = query.Any();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public int CountBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope(true);
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = query.Count();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public IList<T> GetBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = query.AsNoTracking().ToList();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public T? GetSingleBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = query.AsNoTracking().SingleOrDefault();
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public T? GetFirstBySpecification<TSpec>(TSpec specification) where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = query.AsNoTracking().FirstOrDefault();
            scope?.Complete();
            return result;
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
            using var scope = CreateTransactionScope();

            var query = dbEntities.AsQueryable();

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
            var items = query.AsNoTracking().Take(pageSize + 1).ToList();

            var hasMore = items.Count > pageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            TKey? lastKeyValue = items.Count > 0
                ? keySelector.Compile()(items[^1])
                : lastKey;

            scope?.Complete();

            return new KeysetPageResult<T, TKey>(items, lastKeyValue, hasMore, pageSize);
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
            using var scope = CreateTransactionScope();

            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);

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
            var items = query.AsNoTracking().Take(pageSize + 1).ToList();

            var hasMore = items.Count > pageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            TKey? lastKeyValue = items.Count > 0
                ? keySelector.Compile()(items[^1])
                : lastKey;

            scope?.Complete();

            return new KeysetPageResult<T, TKey>(items, lastKeyValue, hasMore, pageSize);
        }

        /// <inheritdoc />
        public IKeysetPageResultString<T> GetByKeysetPagination(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true)
        {
            using var scope = CreateTransactionScope();

            var query = dbEntities.AsQueryable();

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
            var items = query.AsNoTracking().Take(pageSize + 1).ToList();

            var hasMore = items.Count > pageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            var lastKeyValue = items.Count > 0
                ? keySelector.Compile()(items[^1])
                : lastKey;

            scope?.Complete();

            return new KeysetPageResultString<T>(items, lastKeyValue, hasMore, pageSize);
        }

        #endregion

        #region [ Protected Properties ]

        /// <inheritdoc />
        protected override object EntityLogBy => (dbContext as Mvp24HoursContext)?.EntityLogBy!;

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

