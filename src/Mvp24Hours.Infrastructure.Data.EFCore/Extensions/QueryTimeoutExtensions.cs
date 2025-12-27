//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring timeouts per query or operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides fine-grained timeout control for different types of database operations:
    /// <list type="bullet">
    /// <item>Read queries (SELECT)</item>
    /// <item>Write operations (INSERT, UPDATE, DELETE)</item>
    /// <item>Bulk operations (large data processing)</item>
    /// <item>Report queries (complex aggregations)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class QueryTimeoutExtensions
    {
        #region Default Timeout Values

        /// <summary>
        /// Default timeout for read operations (30 seconds).
        /// </summary>
        public const int DefaultReadTimeoutSeconds = 30;

        /// <summary>
        /// Default timeout for write operations (60 seconds).
        /// </summary>
        public const int DefaultWriteTimeoutSeconds = 60;

        /// <summary>
        /// Default timeout for bulk operations (120 seconds).
        /// </summary>
        public const int DefaultBulkTimeoutSeconds = 120;

        /// <summary>
        /// Default timeout for report queries (300 seconds).
        /// </summary>
        public const int DefaultReportTimeoutSeconds = 300;

        #endregion

        #region DbContext Timeout Extensions

        /// <summary>
        /// Executes an action with a specific command timeout.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the action.</returns>
        /// <example>
        /// <code>
        /// var result = await dbContext.WithTimeoutAsync(120, async () =>
        /// {
        ///     return await dbContext.Reports.ToListAsync();
        /// });
        /// </code>
        /// </example>
        public static async Task<T> WithTimeoutAsync<T>(
            this DbContext context,
            int timeoutSeconds,
            Func<Task<T>> action)
        {
            var originalTimeout = context.Database.GetCommandTimeout();
            try
            {
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(timeoutSeconds));
                return await action();
            }
            finally
            {
                context.Database.SetCommandTimeout(originalTimeout);
            }
        }

        /// <summary>
        /// Executes an action with a specific command timeout.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="action">The action to execute.</param>
        /// <example>
        /// <code>
        /// await dbContext.WithTimeoutAsync(120, async () =>
        /// {
        ///     await dbContext.BulkInsertAsync(entities);
        /// });
        /// </code>
        /// </example>
        public static async Task WithTimeoutAsync(
            this DbContext context,
            int timeoutSeconds,
            Func<Task> action)
        {
            var originalTimeout = context.Database.GetCommandTimeout();
            try
            {
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(timeoutSeconds));
                await action();
            }
            finally
            {
                context.Database.SetCommandTimeout(originalTimeout);
            }
        }

        /// <summary>
        /// Executes an action with a bulk operation timeout.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="context">The DbContext.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the action.</returns>
        public static Task<T> WithBulkTimeoutAsync<T>(
            this DbContext context,
            Func<Task<T>> action)
        {
            return context.WithTimeoutAsync(DefaultBulkTimeoutSeconds, action);
        }

        /// <summary>
        /// Executes an action with a report query timeout.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="context">The DbContext.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the action.</returns>
        public static Task<T> WithReportTimeoutAsync<T>(
            this DbContext context,
            Func<Task<T>> action)
        {
            return context.WithTimeoutAsync(DefaultReportTimeoutSeconds, action);
        }

        #endregion

        #region Query Timeout Extensions

        /// <summary>
        /// Executes a query with a specific timeout and returns the result.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result as a list.</returns>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .Where(c => c.IsActive)
        ///     .ToListWithTimeoutAsync(dbContext, 60);
        /// </code>
        /// </example>
        public static async Task<System.Collections.Generic.List<T>> ToListWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.ToListAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Executes a query with a specific timeout and returns a single result or default.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first result or default.</returns>
        public static async Task<T?> FirstOrDefaultWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.FirstOrDefaultAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Executes a count query with a specific timeout.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count result.</returns>
        public static async Task<int> CountWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.CountAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Executes an any query with a specific timeout.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if any results exist.</returns>
        public static async Task<bool> AnyWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.AnyAsync(cancellationToken);
            });
        }

        #endregion

        #region SaveChanges with Timeout

        /// <summary>
        /// Saves changes with a specific timeout.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <example>
        /// <code>
        /// var affected = await dbContext.SaveChangesWithTimeoutAsync(60);
        /// </code>
        /// </example>
        public static async Task<int> SaveChangesWithTimeoutAsync(
            this DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await context.SaveChangesAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Saves changes with the default write timeout.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public static Task<int> SaveChangesWithWriteTimeoutAsync(
            this DbContext context,
            CancellationToken cancellationToken = default)
        {
            return context.SaveChangesWithTimeoutAsync(DefaultWriteTimeoutSeconds, cancellationToken);
        }

        #endregion

        #region ExecuteUpdate/Delete with Timeout

        /// <summary>
        /// Executes an update with a specific timeout.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="setPropertyCalls">The properties to update.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows affected.</returns>
        /// <example>
        /// <code>
        /// var affected = await dbContext.Customers
        ///     .Where(c => c.IsActive)
        ///     .ExecuteUpdateWithTimeoutAsync(dbContext,
        ///         s => s.SetProperty(c => c.LastContactDate, DateTime.UtcNow),
        ///         60);
        /// </code>
        /// </example>
        public static async Task<int> ExecuteUpdateWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>,
                Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>>> setPropertyCalls,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
            });
        }

        /// <summary>
        /// Executes a delete with a specific timeout.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="context">The DbContext.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows affected.</returns>
        /// <example>
        /// <code>
        /// var deleted = await dbContext.Logs
        ///     .Where(l => l.CreatedAt &lt; DateTime.UtcNow.AddYears(-1))
        ///     .ExecuteDeleteWithTimeoutAsync(dbContext, 120);
        /// </code>
        /// </example>
        public static async Task<int> ExecuteDeleteWithTimeoutAsync<T>(
            this IQueryable<T> query,
            DbContext context,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return await context.WithTimeoutAsync(timeoutSeconds, async () =>
            {
                return await query.ExecuteDeleteAsync(cancellationToken);
            });
        }

        #endregion
    }
}

