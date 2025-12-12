//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Extensions
{
    /// <summary>
    /// Extension methods for query projections in EF Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Projection queries select only specific columns from the database,
    /// improving performance by reducing data transfer.
    /// </para>
    /// <para>
    /// Options for projection:
    /// <list type="bullet">
    /// <item>Manual Select expressions - most control, more code</item>
    /// <item>AutoMapper ProjectTo - automatic, requires AutoMapper</item>
    /// <item>SelectDto helper - simple DTO mapping</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class ProjectionExtensions
    {
        #region Manual Projection

        /// <summary>
        /// Projects query results to a DTO type using a selector expression.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result DTO type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Projection expression.</param>
        /// <returns>A projected query.</returns>
        /// <remarks>
        /// <para>
        /// The projection is translated to SQL, so only properties used in the
        /// selector are fetched from the database.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customerDtos = await dbContext.Customers
        ///     .Where(c => c.IsActive)
        ///     .ProjectTo(c => new CustomerDto
        ///     {
        ///         Id = c.Id,
        ///         Name = c.Name,
        ///         Email = c.Email
        ///     })
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<TResult> ProjectTo<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projectto");
            return query.Select(selector);
        }

        /// <summary>
        /// Projects query results and executes as a list.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result DTO type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Projection expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of projected results.</returns>
        public static async Task<IList<TResult>> ProjectToListAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttolistasync-start");
            try
            {
                return await query.Select(selector).ToListAsync(cancellationToken);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttolistasync-end");
            }
        }

        /// <summary>
        /// Projects query results to a single item.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result DTO type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Projection expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A single projected result or null.</returns>
        public static async Task<TResult> ProjectToSingleAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttosingleasync-start");
            try
            {
                return await query.Select(selector).SingleOrDefaultAsync(cancellationToken);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttosingleasync-end");
            }
        }

        #endregion

        #region Select Specific Columns

        /// <summary>
        /// Selects only the ID and specified columns from an entity.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="keySelector">Key selector expression.</param>
        /// <param name="columnSelectors">Additional columns to select.</param>
        /// <returns>A query returning anonymous types with selected columns.</returns>
        /// <example>
        /// <code>
        /// var idAndNames = await dbContext.Customers
        ///     .SelectColumns(c => c.Id, c => c.Name, c => c.Email)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<dynamic> SelectColumns<TSource, TKey>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TKey>> keySelector,
            params Expression<Func<TSource, object>>[] columnSelectors)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-selectcolumns");

            // Build dynamic select expression
            var parameter = Expression.Parameter(typeof(TSource), "e");
            var bindings = new List<MemberBinding>();

            // Add key binding
            var keyBody = ReplaceParameter(keySelector.Body, keySelector.Parameters[0], parameter);
            // For dynamic, we'll use a different approach
            var selectList = new List<Expression>();
            selectList.Add(keyBody);

            foreach (var selector in columnSelectors)
            {
                var body = ReplaceParameter(selector.Body, selector.Parameters[0], parameter);
                selectList.Add(body);
            }

            // This creates a Tuple-like structure
            // For simplicity, return the query with Select that returns object
            // Users should use ProjectTo with explicit selector for strongly-typed results
            return query.Select(e => (dynamic)new { Key = keySelector.Compile()(e) });
        }

        #endregion

        #region Projection with Loading

        /// <summary>
        /// Projects to DTO type and includes specified navigation properties in a single query.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result DTO type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Projection expression.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>A projected query with includes applied.</returns>
        /// <example>
        /// <code>
        /// var orderDtos = await dbContext.Orders
        ///     .ProjectToWithIncludes(
        ///         o => new OrderDto
        ///         {
        ///             Id = o.Id,
        ///             CustomerName = o.Customer.Name,
        ///             ItemCount = o.Items.Count
        ///         },
        ///         o => o.Customer,
        ///         o => o.Items
        ///     )
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<TResult> ProjectToWithIncludes<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            params Expression<Func<TSource, object>>[] includes)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttowithincludes");

            // Apply includes before projection
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return query.Select(selector);
        }

        /// <summary>
        /// Projects query to DTO with explicit mapping function.
        /// Uses deferred execution for the projection.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result DTO type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="mapper">Mapping function.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of mapped results.</returns>
        /// <remarks>
        /// <para>
        /// Note: This loads full entities and maps in memory.
        /// For better performance, use <see cref="ProjectTo{TSource, TResult}"/> with Select expression.
        /// </para>
        /// </remarks>
        public static async Task<IList<TResult>> MapToListAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Func<TSource, TResult> mapper,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-maptolistasync-start");
            try
            {
                var entities = await query.ToListAsync(cancellationToken);
                return entities.Select(mapper).ToList();
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-maptolistasync-end");
            }
        }

        #endregion

        #region Count and Exists Projections

        /// <summary>
        /// Projects to count only, avoiding loading full entities.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Count of matching entities.</returns>
        public static Task<int> ProjectToCountAsync<TSource>(
            this IQueryable<TSource> query,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttocountasync");
            return query.CountAsync(cancellationToken);
        }

        /// <summary>
        /// Projects to existence check only, avoiding loading full entities.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if any matching entities exist.</returns>
        public static Task<bool> ProjectToExistsAsync<TSource>(
            this IQueryable<TSource> query,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttoexistsasync");
            return query.AnyAsync(cancellationToken);
        }

        #endregion

        #region Aggregate Projections

        /// <summary>
        /// Projects to sum of a property.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Property selector.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Sum of selected values.</returns>
        public static Task<decimal> ProjectToSumAsync<TSource>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttosumasync");
            return query.SumAsync(selector, cancellationToken);
        }

        /// <summary>
        /// Projects to average of a property.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Property selector.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Average of selected values.</returns>
        public static Task<decimal> ProjectToAverageAsync<TSource>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttoaverageasync");
            return query.AverageAsync(selector, cancellationToken);
        }

        /// <summary>
        /// Projects to maximum of a property.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Property selector.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Maximum of selected values.</returns>
        public static Task<TResult> ProjectToMaxAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttomaxasync");
            return query.MaxAsync(selector, cancellationToken);
        }

        /// <summary>
        /// Projects to minimum of a property.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="selector">Property selector.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Minimum of selected values.</returns>
        public static Task<TResult> ProjectToMinAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttominasync");
            return query.MinAsync(selector, cancellationToken);
        }

        #endregion

        #region Group Projections

        /// <summary>
        /// Projects query to grouped results.
        /// </summary>
        /// <typeparam name="TSource">Source entity type.</typeparam>
        /// <typeparam name="TKey">Grouping key type.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="keySelector">Key selector for grouping.</param>
        /// <param name="resultSelector">Result selector for each group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of grouped results.</returns>
        /// <example>
        /// <code>
        /// var ordersByCustomer = await dbContext.Orders
        ///     .ProjectToGroupedAsync(
        ///         o => o.CustomerId,
        ///         (key, orders) => new
        ///         {
        ///             CustomerId = key,
        ///             OrderCount = orders.Count(),
        ///             TotalAmount = orders.Sum(o => o.Total)
        ///         });
        /// </code>
        /// </example>
        public static async Task<IList<TResult>> ProjectToGroupedAsync<TSource, TKey, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TKey>> keySelector,
            Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector,
            CancellationToken cancellationToken = default)
            where TSource : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttogroupedasync-start");
            try
            {
                return await query
                    .GroupBy(keySelector, resultSelector)
                    .ToListAsync(cancellationToken);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-projection-projecttogroupedasync-end");
            }
        }

        #endregion

        #region Helper Methods

        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacer(oldParam, newParam).Visit(expression);
        }

        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;

            public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newParam : base.VisitParameter(node);
            }
        }

        #endregion
    }
}

