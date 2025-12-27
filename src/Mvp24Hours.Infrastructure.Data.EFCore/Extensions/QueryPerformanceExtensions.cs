//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Extensions
{
    /// <summary>
    /// Extension methods for query performance optimizations including split queries and query tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides methods to optimize query performance:
    /// <list type="bullet">
    /// <item><b>Split Queries</b> - Avoid cartesian explosion by splitting includes into separate queries</item>
    /// <item><b>Query Tags</b> - Add SQL comments for profiling and debugging</item>
    /// <item><b>Conditional optimization</b> - Apply optimizations based on conditions</item>
    /// </list>
    /// </para>
    /// <para>
    /// For basic split query and single query operations, use EF Core's built-in methods:
    /// <list type="bullet">
    /// <item><c>query.AsSplitQuery()</c> - Split query mode</item>
    /// <item><c>query.AsSingleQuery()</c> - Single query mode</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class QueryPerformanceExtensions
    {
        #region Split Queries - Conditional

        /// <summary>
        /// Conditionally applies split query based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, uses split queries.</param>
        /// <returns>The query with conditional split applied.</returns>
        /// <remarks>
        /// <para>
        /// Split queries generate a separate SQL query for each Include, avoiding the 
        /// "cartesian explosion" problem that occurs when joining multiple collection navigations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var hasMultipleCollections = includeItems &amp;&amp; includePayments;
        /// var orders = await dbContext.Orders
        ///     .Include(o => o.Items)
        ///     .Include(o => o.Payments)
        ///     .AsSplitQueryIf(hasMultipleCollections)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsSplitQueryIf<T>(this IQueryable<T> query, bool condition)
            where T : class
        {
            return condition ? query.AsSplitQuery() : query;
        }

        /// <summary>
        /// Conditionally applies single query based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, uses single query.</param>
        /// <returns>The query with conditional single query applied.</returns>
        public static IQueryable<T> AsSingleQueryIf<T>(this IQueryable<T> query, bool condition)
            where T : class
        {
            return condition ? query.AsSingleQuery() : query;
        }

        /// <summary>
        /// Applies split query when multiple collection navigations are included.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="collectionCount">Number of collection navigations being included.</param>
        /// <param name="threshold">Threshold for enabling split queries (default: 2).</param>
        /// <returns>The query with optimized split behavior.</returns>
        /// <remarks>
        /// <para>
        /// Automatically switches to split queries when the number of collection includes
        /// exceeds the threshold, preventing cartesian explosion.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var orders = await dbContext.Orders
        ///     .Include(o => o.Items)
        ///     .Include(o => o.Payments)
        ///     .OptimizeSplitQuery(collectionCount: 2, threshold: 2)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeSplitQuery<T>(
            this IQueryable<T> query,
            int collectionCount,
            int threshold = 2)
            where T : class
        {
            return collectionCount >= threshold ? query.AsSplitQuery() : query;
        }

        #endregion

        #region Query Tags

        /// <summary>
        /// Adds a tag with the calling method name automatically.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="prefix">Optional prefix for the tag.</param>
        /// <param name="callerMemberName">Automatically populated with the calling method name.</param>
        /// <param name="callerFilePath">Automatically populated with the source file path.</param>
        /// <param name="callerLineNumber">Automatically populated with the line number.</param>
        /// <returns>The tagged query.</returns>
        /// <remarks>
        /// <para>
        /// Uses CallerMemberName attribute to automatically tag queries with their origin.
        /// </para>
        /// <para>
        /// Example SQL output:
        /// <code>
        /// -- Mvp24Hours: CustomerRepository.GetActiveCustomersAsync (CustomerRepository.cs:42)
        /// SELECT * FROM Customers WHERE IsActive = 1
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// public async Task&lt;IList&lt;Customer&gt;&gt; GetActiveCustomersAsync()
        /// {
        ///     return await dbContext.Customers
        ///         .TagWithCallerInfo("CustomerRepo")  // Tags with "CustomerRepo: GetActiveCustomersAsync"
        ///         .Where(c => c.IsActive)
        ///         .ToListAsync();
        /// }
        /// </code>
        /// </example>
        public static IQueryable<T> TagWithCallerInfo<T>(
            this IQueryable<T> query,
            string prefix = "Mvp24Hours",
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
            where T : class
        {
            var fileName = System.IO.Path.GetFileName(callerFilePath);
            var tag = $"{prefix}: {callerMemberName} ({fileName}:{callerLineNumber})";

            return query.TagWith(tag);
        }

        /// <summary>
        /// Adds multiple tags to the query.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="tags">Tags to add.</param>
        /// <returns>The tagged query.</returns>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .TagWithMany("GetCustomers", "Admin", "Report")
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> TagWithMany<T>(this IQueryable<T> query, params string[] tags)
            where T : class
        {
            foreach (var tag in tags)
            {
                query = query.TagWith(tag);
            }
            return query;
        }

        /// <summary>
        /// Conditionally adds a tag based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, adds the tag.</param>
        /// <param name="tag">The tag text to add.</param>
        /// <returns>The query with conditional tag.</returns>
        public static IQueryable<T> TagWithIf<T>(this IQueryable<T> query, bool condition, string tag)
            where T : class
        {
            return condition ? query.TagWith(tag) : query;
        }

        /// <summary>
        /// Adds operation context as a query tag.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="operationName">Name of the operation (e.g., "GetById", "List").</param>
        /// <param name="correlationId">Optional correlation ID for tracing.</param>
        /// <param name="userId">Optional user ID for audit.</param>
        /// <returns>The tagged query with operation context.</returns>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .TagWithOperationContext(
        ///         operationName: "GetActiveCustomers",
        ///         correlationId: "abc-123",
        ///         userId: "user@example.com")
        ///     .Where(c => c.IsActive)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> TagWithOperationContext<T>(
            this IQueryable<T> query,
            string operationName,
            string correlationId = null,
            string userId = null)
            where T : class
        {
            var tag = $"Operation: {operationName}";

            if (!string.IsNullOrEmpty(correlationId))
            {
                tag += $" | CorrelationId: {correlationId}";
            }

            if (!string.IsNullOrEmpty(userId))
            {
                tag += $" | User: {userId}";
            }

            return query.TagWith(tag);
        }

        #endregion

        #region Query Hints - Conditional

        /// <summary>
        /// Conditionally ignores query filters based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, ignores query filters.</param>
        /// <returns>Query with conditional filter bypass.</returns>
        /// <remarks>
        /// <para>
        /// Use with caution - this bypasses soft delete filters, tenant filters, etc.
        /// Typically used for admin operations or data export.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .IgnoreQueryFiltersIf(isAdmin) // Only bypass filters for admins
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> IgnoreQueryFiltersIf<T>(this IQueryable<T> query, bool condition)
            where T : class
        {
            return condition ? query.IgnoreQueryFilters() : query;
        }

        /// <summary>
        /// Conditionally ignores auto-includes based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, ignores auto-includes.</param>
        /// <returns>Query with conditional auto-include bypass.</returns>
        public static IQueryable<T> IgnoreAutoIncludesIf<T>(this IQueryable<T> query, bool condition)
            where T : class
        {
            return condition ? query.IgnoreAutoIncludes() : query;
        }

        #endregion

        #region Performance Optimization Chains

        /// <summary>
        /// Applies a set of performance optimizations for read-only queries.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="hasCollectionIncludes">Whether the query has collection includes.</param>
        /// <param name="operationTag">Optional tag for the operation.</param>
        /// <returns>An optimized query.</returns>
        /// <remarks>
        /// <para>
        /// Applies:
        /// <list type="bullet">
        /// <item>NoTracking (or NoTrackingWithIdentityResolution if includes)</item>
        /// <item>Split queries if collection includes</item>
        /// <item>Query tag if provided</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var orders = await dbContext.Orders
        ///     .Include(o => o.Items)
        ///     .Where(o => o.Status == OrderStatus.Active)
        ///     .OptimizeForReadPerformance(
        ///         hasCollectionIncludes: true,
        ///         operationTag: "GetActiveOrders")
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeForReadPerformance<T>(
            this IQueryable<T> query,
            bool hasCollectionIncludes = false,
            string operationTag = null)
            where T : class
        {
            // Apply no tracking with appropriate mode
            query = hasCollectionIncludes
                ? query.AsNoTrackingWithIdentityResolution()
                : query.AsNoTracking();

            // Apply split query if collections
            if (hasCollectionIncludes)
            {
                query = query.AsSplitQuery();
            }

            // Apply tag if provided
            if (!string.IsNullOrEmpty(operationTag))
            {
                query = query.TagWith(operationTag);
            }

            return query;
        }

        /// <summary>
        /// Applies optimizations for a paginated query.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="take">Number of records to take.</param>
        /// <param name="operationTag">Optional tag for the operation.</param>
        /// <returns>An optimized paginated query.</returns>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .Where(c => c.IsActive)
        ///     .OptimizeForPaging(skip: 20, take: 10)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeForPaging<T>(
            this IQueryable<T> query,
            int skip,
            int take,
            string operationTag = null)
            where T : class
        {
            query = query.AsNoTracking();

            if (!string.IsNullOrEmpty(operationTag))
            {
                query = query.TagWith($"{operationTag} [Page: skip={skip}, take={take}]");
            }

            return query.Skip(skip).Take(take);
        }

        /// <summary>
        /// Applies optimizations for a count-only query.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="operationTag">Optional tag for the operation.</param>
        /// <returns>An optimized query for counting.</returns>
        /// <example>
        /// <code>
        /// var count = await dbContext.Customers
        ///     .Where(c => c.IsActive)
        ///     .OptimizeForCount("CountActiveCustomers")
        ///     .CountAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeForCount<T>(
            this IQueryable<T> query,
            string operationTag = null)
            where T : class
        {
            query = query.AsNoTracking();

            if (!string.IsNullOrEmpty(operationTag))
            {
                query = query.TagWith(operationTag);
            }

            return query;
        }

        /// <summary>
        /// Applies optimizations for single entity lookup by ID.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="forUpdate">Whether the entity will be modified (requires tracking).</param>
        /// <param name="operationTag">Optional tag for the operation.</param>
        /// <returns>An optimized query for single entity lookup.</returns>
        /// <example>
        /// <code>
        /// var customer = await dbContext.Customers
        ///     .OptimizeForSingleLookup(forUpdate: false)
        ///     .FirstOrDefaultAsync(c => c.Id == customerId);
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeForSingleLookup<T>(
            this IQueryable<T> query,
            bool forUpdate = false,
            string operationTag = null)
            where T : class
        {
            if (!forUpdate)
            {
                query = query.AsNoTracking();
            }

            if (!string.IsNullOrEmpty(operationTag))
            {
                query = query.TagWith(operationTag);
            }

            return query;
        }

        #endregion
    }
}
