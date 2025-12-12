//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Helpers;
using System;
using System.Transactions;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Configuration
{
    /// <summary>
    /// Configuration options for EF Core repository behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive configuration for repository operations including:
    /// <list type="bullet">
    /// <item>Pagination limits</item>
    /// <item>Transaction isolation levels</item>
    /// <item>Query tracking behavior</item>
    /// <item>Performance optimizations (split queries, query tags)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursRepositoryAsync(options =>
    /// {
    ///     options.MaxQtyByQueryPage = 100;
    ///     options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
    ///     options.UseSplitQueries = true;
    ///     options.EnableQueryTags = true;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class EFCoreRepositoryOptions
    {
        /// <summary>
        /// Maximum number of records per query page. Default is defined in ContantsHelper.Data.MaxQtyByQueryPage.
        /// </summary>
        public int MaxQtyByQueryPage { get; set; } = ContantsHelper.Data.MaxQtyByQueryPage;

        /// <summary>
        /// Transaction isolation level for repository operations. Default is null (uses database default).
        /// </summary>
        public IsolationLevel? TransactionIsolationLevel { get; set; }

        #region Performance Options

        /// <summary>
        /// Default tracking behavior for queries.
        /// <list type="bullet">
        /// <item><see cref="QueryTrackingBehavior.TrackAll"/> - All entities are tracked (default EF behavior)</item>
        /// <item><see cref="QueryTrackingBehavior.NoTracking"/> - No entities are tracked (better for read-only scenarios)</item>
        /// <item><see cref="QueryTrackingBehavior.NoTrackingWithIdentityResolution"/> - No tracking but resolves identity for complex graphs</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use <see cref="QueryTrackingBehavior.NoTracking"/> for read-only queries to improve performance.
        /// Use <see cref="QueryTrackingBehavior.NoTrackingWithIdentityResolution"/> when you have complex 
        /// entity graphs with multiple references to the same entity.
        /// </para>
        /// </remarks>
        public QueryTrackingBehavior DefaultTrackingBehavior { get; set; } = QueryTrackingBehavior.TrackAll;

        /// <summary>
        /// When true, uses split queries for Include operations.
        /// Split queries generate separate SQL queries for each Include, avoiding cartesian explosion.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Split queries are beneficial when:
        /// <list type="bullet">
        /// <item>Loading entities with multiple collection navigations</item>
        /// <item>Avoiding cartesian explosion (N x M rows)</item>
        /// <item>Working with large datasets</item>
        /// </list>
        /// </para>
        /// <para>
        /// However, split queries make multiple round trips to the database.
        /// For simple includes or low-latency databases, single queries may be faster.
        /// </para>
        /// </remarks>
        public bool UseSplitQueries { get; set; }

        /// <summary>
        /// When true, automatically adds query tags with repository and method information.
        /// Query tags appear as SQL comments, useful for profiling and debugging.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example SQL output:
        /// <code>
        /// -- Mvp24Hours: RepositoryAsync.GetByIdAsync
        /// SELECT * FROM [Customers] WHERE [Id] = @p0
        /// </code>
        /// </para>
        /// </remarks>
        public bool EnableQueryTags { get; set; }

        /// <summary>
        /// Custom prefix for query tags. Default is "Mvp24Hours".
        /// </summary>
        public string QueryTagPrefix { get; set; } = "Mvp24Hours";

        /// <summary>
        /// When true, logging includes parameter values.
        /// Use with caution in production as it may expose sensitive data.
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; }

        /// <summary>
        /// Timeout in milliseconds for slow query logging. Queries exceeding this threshold are logged as warnings.
        /// Default is 1000ms (1 second). Set to 0 to disable.
        /// </summary>
        public int SlowQueryThresholdMs { get; set; } = 1000;

        #endregion

        #region Streaming Options

        /// <summary>
        /// Default buffer size for streaming operations with IAsyncEnumerable.
        /// Higher values reduce round trips but use more memory.
        /// </summary>
        public int StreamingBufferSize { get; set; } = 100;

        #endregion

        #region Projection Options

        /// <summary>
        /// When true, uses AutoMapper's ProjectTo for projection queries.
        /// Requires AutoMapper to be registered in DI.
        /// </summary>
        public bool UseAutoMapperProjection { get; set; }

        #endregion
    }
}
