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
    /// Extension methods and helpers for working with EF Core compiled queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Compiled queries are pre-compiled LINQ expressions that can be reused,
    /// avoiding the overhead of query compilation on each execution.
    /// </para>
    /// <para>
    /// Benefits of compiled queries:
    /// <list type="bullet">
    /// <item>Eliminates query compilation overhead</item>
    /// <item>Faster execution for frequently-used queries</item>
    /// <item>Reduced memory allocations</item>
    /// </list>
    /// </para>
    /// <para>
    /// Best used for:
    /// <list type="bullet">
    /// <item>Hot paths executed frequently</item>
    /// <item>Simple queries with known parameters</item>
    /// <item>Performance-critical operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class CompiledQueryExtensions
    {
        #region Sync Compiled Queries

        /// <summary>
        /// Creates a compiled query that returns a single entity by ID.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="keySelector">Expression to select the key property.</param>
        /// <returns>A compiled query function.</returns>
        /// <example>
        /// <code>
        /// // Define compiled query (typically as a static field)
        /// private static readonly Func&lt;MyDbContext, int, Customer&gt; GetCustomerById =
        ///     CompiledQueryExtensions.CompileGetById&lt;MyDbContext, Customer, int&gt;(c => c.Id);
        /// 
        /// // Use compiled query
        /// var customer = GetCustomerById(dbContext, 123);
        /// </code>
        /// </example>
        public static Func<TContext, TKey, T> CompileGetById<TContext, T, TKey>(
            Expression<Func<T, TKey>> keySelector)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-getbyid-compile");

            var keyProperty = GetPropertyName(keySelector);

            return EF.CompileQuery((TContext ctx, TKey id) =>
                ctx.Set<T>().FirstOrDefault(CreateKeyPredicate<T, TKey>(keyProperty, id)));
        }

        /// <summary>
        /// Creates a compiled query that returns all entities.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A compiled query function returning IEnumerable.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, IEnumerable&lt;Customer&gt;&gt; GetAllCustomers =
        ///     CompiledQueryExtensions.CompileGetAll&lt;MyDbContext, Customer&gt;();
        /// 
        /// var customers = GetAllCustomers(dbContext);
        /// </code>
        /// </example>
        public static Func<TContext, IEnumerable<T>> CompileGetAll<TContext, T>()
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-getall-compile");

            return EF.CompileQuery((TContext ctx) => ctx.Set<T>());
        }

        /// <summary>
        /// Creates a compiled query that checks if any entity matches a condition.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TParam">The parameter type.</typeparam>
        /// <param name="predicate">The condition to check.</param>
        /// <returns>A compiled query function returning bool.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, string, bool&gt; CustomerExistsByEmail =
        ///     CompiledQueryExtensions.CompileAny&lt;MyDbContext, Customer, string&gt;(
        ///         (c, email) => c.Email == email);
        /// 
        /// var exists = CustomerExistsByEmail(dbContext, "test@example.com");
        /// </code>
        /// </example>
        public static Func<TContext, TParam, bool> CompileAny<TContext, T, TParam>(
            Expression<Func<T, TParam, bool>> predicate)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-any-compile");

            return EF.CompileQuery((TContext ctx, TParam param) =>
                ctx.Set<T>().Any(e => predicate.Compile()(e, param)));
        }

        /// <summary>
        /// Creates a compiled query that counts entities matching a condition.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TParam">The parameter type.</typeparam>
        /// <param name="predicate">The condition to count.</param>
        /// <returns>A compiled query function returning int.</returns>
        public static Func<TContext, TParam, int> CompileCount<TContext, T, TParam>(
            Expression<Func<T, TParam, bool>> predicate)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-count-compile");

            return EF.CompileQuery((TContext ctx, TParam param) =>
                ctx.Set<T>().Count(e => predicate.Compile()(e, param)));
        }

        #endregion

        #region Async Compiled Queries

        /// <summary>
        /// Creates a compiled async query that returns a single entity by ID.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="keySelector">Expression to select the key property.</param>
        /// <returns>A compiled async query function.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, int, Task&lt;Customer&gt;&gt; GetCustomerByIdAsync =
        ///     CompiledQueryExtensions.CompileGetByIdAsync&lt;MyDbContext, Customer, int&gt;(c => c.Id);
        /// 
        /// var customer = await GetCustomerByIdAsync(dbContext, 123);
        /// </code>
        /// </example>
        public static Func<TContext, TKey, Task<T>> CompileGetByIdAsync<TContext, T, TKey>(
            Expression<Func<T, TKey>> keySelector)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-getbyidasync-compile");

            var keyProperty = GetPropertyName(keySelector);

            return EF.CompileAsyncQuery((TContext ctx, TKey id) =>
                ctx.Set<T>().FirstOrDefault(CreateKeyPredicate<T, TKey>(keyProperty, id)));
        }

        /// <summary>
        /// Creates a compiled async query that returns all entities as IAsyncEnumerable.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A compiled async query function returning IAsyncEnumerable.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, IAsyncEnumerable&lt;Customer&gt;&gt; GetAllCustomersAsync =
        ///     CompiledQueryExtensions.CompileGetAllAsync&lt;MyDbContext, Customer&gt;();
        /// 
        /// await foreach (var customer in GetAllCustomersAsync(dbContext))
        /// {
        ///     // Process customer
        /// }
        /// </code>
        /// </example>
        public static Func<TContext, IAsyncEnumerable<T>> CompileGetAllAsync<TContext, T>()
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-getallasync-compile");

            return EF.CompileAsyncQuery((TContext ctx) => ctx.Set<T>());
        }

        /// <summary>
        /// Creates a compiled async query that returns entities matching a condition.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TParam">The parameter type.</typeparam>
        /// <param name="predicate">The filter condition.</param>
        /// <returns>A compiled async query function returning IAsyncEnumerable.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, bool, IAsyncEnumerable&lt;Customer&gt;&gt; GetActiveCustomersAsync =
        ///     CompiledQueryExtensions.CompileWhereAsync&lt;MyDbContext, Customer, bool&gt;(
        ///         (c, isActive) => c.IsActive == isActive);
        /// 
        /// await foreach (var customer in GetActiveCustomersAsync(dbContext, true))
        /// {
        ///     // Process active customer
        /// }
        /// </code>
        /// </example>
        public static Func<TContext, TParam, IAsyncEnumerable<T>> CompileWhereAsync<TContext, T, TParam>(
            Expression<Func<T, TParam, bool>> predicate)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-whereasync-compile");

            return EF.CompileAsyncQuery((TContext ctx, TParam param) =>
                ctx.Set<T>().Where(e => predicate.Compile()(e, param)));
        }

        /// <summary>
        /// Creates a compiled async query that returns first entity matching a condition.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TParam">The parameter type.</typeparam>
        /// <param name="predicate">The filter condition.</param>
        /// <returns>A compiled async query function.</returns>
        public static Func<TContext, TParam, Task<T>> CompileFirstOrDefaultAsync<TContext, T, TParam>(
            Expression<Func<T, TParam, bool>> predicate)
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-firstordefaultasync-compile");

            return EF.CompileAsyncQuery((TContext ctx, TParam param) =>
                ctx.Set<T>().FirstOrDefault(e => predicate.Compile()(e, param)));
        }

        /// <summary>
        /// Creates a compiled async query with paging support.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A compiled async query function with skip and take parameters.</returns>
        /// <example>
        /// <code>
        /// private static readonly Func&lt;MyDbContext, int, int, IAsyncEnumerable&lt;Customer&gt;&gt; GetPagedCustomersAsync =
        ///     CompiledQueryExtensions.CompilePagedAsync&lt;MyDbContext, Customer&gt;();
        /// 
        /// await foreach (var customer in GetPagedCustomersAsync(dbContext, 0, 10))
        /// {
        ///     // Process first page of customers
        /// }
        /// </code>
        /// </example>
        public static Func<TContext, int, int, IAsyncEnumerable<T>> CompilePagedAsync<TContext, T>()
            where TContext : DbContext
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-compiledquery-pagedasync-compile");

            return EF.CompileAsyncQuery((TContext ctx, int skip, int take) =>
                ctx.Set<T>().Skip(skip).Take(take));
        }

        #endregion

        #region Helper Methods

        private static string GetPropertyName<T, TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Key selector must be a simple property access expression.", nameof(keySelector));
        }

        private static Expression<Func<T, bool>> CreateKeyPredicate<T, TKey>(string propertyName, TKey value)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value, typeof(TKey));
            var equals = Expression.Equal(property, constant);

            return Expression.Lambda<Func<T, bool>>(equals, parameter);
        }

        #endregion
    }

    /// <summary>
    /// Base class for defining compiled queries as reusable fields.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// Inherit from this class to define compiled queries for your entities.
    /// Compiled queries should be stored as static readonly fields to avoid recompilation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerQueries : CompiledQueryBase&lt;MyDbContext&gt;
    /// {
    ///     public static readonly Func&lt;MyDbContext, int, CancellationToken, Task&lt;Customer&gt;&gt; GetByIdAsync =
    ///         CompileGetByIdAsync&lt;Customer, int&gt;(c => c.Id);
    ///     
    ///     public static readonly Func&lt;MyDbContext, string, IAsyncEnumerable&lt;Customer&gt;&gt; GetByEmailDomainAsync =
    ///         EF.CompileAsyncQuery((MyDbContext ctx, string domain) =>
    ///             ctx.Customers.Where(c => c.Email.EndsWith(domain)));
    /// }
    /// 
    /// // Usage
    /// var customer = await CustomerQueries.GetByIdAsync(dbContext, 123, ct);
    /// </code>
    /// </example>
    public abstract class CompiledQueryBase<TContext>
        where TContext : DbContext
    {
        /// <summary>
        /// Creates a compiled async query that returns a single entity by ID.
        /// </summary>
        protected static Func<TContext, TKey, Task<T>> CompileGetByIdAsync<T, TKey>(
            Expression<Func<T, TKey>> keySelector)
            where T : class, IEntityBase
        {
            return CompiledQueryExtensions.CompileGetByIdAsync<TContext, T, TKey>(keySelector);
        }

        /// <summary>
        /// Creates a compiled async query that returns all entities.
        /// </summary>
        protected static Func<TContext, IAsyncEnumerable<T>> CompileGetAllAsync<T>()
            where T : class, IEntityBase
        {
            return CompiledQueryExtensions.CompileGetAllAsync<TContext, T>();
        }

        /// <summary>
        /// Creates a compiled async query with paging support.
        /// </summary>
        protected static Func<TContext, int, int, IAsyncEnumerable<T>> CompilePagedAsync<T>()
            where T : class, IEntityBase
        {
            return CompiledQueryExtensions.CompilePagedAsync<TContext, T>();
        }
    }
}

