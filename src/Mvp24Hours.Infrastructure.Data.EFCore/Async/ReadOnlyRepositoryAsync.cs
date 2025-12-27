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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    /// Asynchronous read-only repository implementation for Entity Framework Core.
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
    /// <item>Full async/await support with CancellationToken</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ReadOnlyRepositoryAsync<T>(DbContext dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<T>(dbContext, options), IReadOnlyRepositoryAsync<T>
        where T : class, IEntityBase
    {
        #region [ IQueryAsync Methods ]

        /// <inheritdoc />
        public async Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var result = await GetQuery(null, true).AnyAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<int> ListCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var result = await GetQuery(null, true).CountAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public Task<IList<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var result = await GetQuery(criteria).AsNoTracking().ToListAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<bool> GetByAnyAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, null, true).AnyAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<int> GetByCountAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope(true);
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, null, true).CountAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IList<T>> GetByAsync(Expression<Func<T, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var query = dbEntities.AsQueryable();
            if (clause != null)
            {
                query = query.Where(clause);
            }
            var result = await GetQuery(query, criteria).AsNoTracking().ToListAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T?> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            using var scope = CreateTransactionScope();
            var result = await GetDynamicFilter(GetQuery(criteria, true), GetKeyInfo(), id).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        #endregion

        #region [ IQueryRelationAsync Methods ]

        /// <inheritdoc />
        public Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
            where TProperty : class
        {
            return dbContext.Entry(entity).Reference(propertyExpression).LoadAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task LoadRelationAsync<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
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

            await query.ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task LoadRelationSortByAscendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
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

            await query.ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task LoadRelationSortByDescendingAsync<TProperty, TKey>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, Expression<Func<TProperty, TKey>> orderKey, Expression<Func<TProperty, bool>>? clause = null, int limit = 0, CancellationToken cancellationToken = default)
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

            await query.ToListAsync(cancellationToken);
        }

        #endregion

        #region [ Specification Pattern Methods ]

        /// <inheritdoc />
        public async Task<bool> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope(true);
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = await query.AnyAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<int> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope(true);
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = await query.CountAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<IList<T>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = await query.AsNoTracking().ToListAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<T?> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = await query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            scope?.Complete();
            return result;
        }

        /// <inheritdoc />
        public async Task<T?> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<T>
        {
            using var scope = CreateTransactionScope();
            var query = SpecificationEvaluator<T>.Default.GetQuery(dbEntities.AsQueryable(), specification);
            var result = await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            scope?.Complete();
            return result;
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
            var items = await query.AsNoTracking().Take(pageSize + 1).ToListAsync(cancellationToken);

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
            var items = await query.AsNoTracking().Take(pageSize + 1).ToListAsync(cancellationToken);

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
        public async Task<IKeysetPageResultString<T>> GetByKeysetPaginationAsync(
            Expression<Func<T, bool>>? clause,
            Expression<Func<T, string>> keySelector,
            string? lastKey,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default)
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
            var items = await query.AsNoTracking().Take(pageSize + 1).ToListAsync(cancellationToken);

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

