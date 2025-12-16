//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Configuration
{
    /// <summary>
    /// Extended bulk operation options specific to MongoDB.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MongoDB-specific options for bulk operations:
    /// <list type="bullet">
    ///   <item><see cref="IsOrdered"/> - Controls whether operations execute in order</item>
    ///   <item><see cref="BypassDocumentValidation"/> - Skip server-side document validation</item>
    ///   <item><see cref="WriteConcern"/> - Configure write acknowledgment</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new MongoDbBulkOperationOptions
    /// {
    ///     BatchSize = 1000,
    ///     IsOrdered = false, // Continue on errors, better performance
    ///     BypassDocumentValidation = true,
    ///     ProgressCallback = (processed, total) => Console.WriteLine($"{processed}/{total}")
    /// };
    /// 
    /// var result = await repository.BulkInsertAsync(documents, options);
    /// </code>
    /// </example>
    public sealed class MongoDbBulkOperationOptions
    {
        #region [ Base BulkOperationOptions Properties ]

        /// <summary>
        /// Number of entities to process in each batch. Default is 1000.
        /// </summary>
        /// <remarks>
        /// Higher values can improve performance but use more memory.
        /// Lower values reduce memory footprint but may increase execution time.
        /// </remarks>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// When true, wraps the entire operation in a transaction.
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Optional progress callback for long-running operations.
        /// </summary>
        public BulkProgressCallback ProgressCallback { get; set; }

        /// <summary>
        /// Timeout for the bulk operation in seconds. Default is 300 (5 minutes).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        #endregion

        #region [ MongoDB-Specific Properties ]

        /// <summary>
        /// When true, bulk operations are executed in order. If an error occurs, 
        /// remaining operations are not executed.
        /// When false (unordered), operations may execute in parallel and errors 
        /// don't stop remaining operations.
        /// Default is true (ordered).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ordered (true)</b>: Operations execute sequentially. If one fails, 
        /// subsequent operations are aborted. Use when order matters or you want 
        /// fail-fast behavior.
        /// </para>
        /// <para>
        /// <b>Unordered (false)</b>: Operations may execute in parallel for better 
        /// performance. Errors don't stop other operations. Use for maximum 
        /// throughput when order doesn't matter.
        /// </para>
        /// </remarks>
        public bool IsOrdered { get; set; } = true;

        /// <summary>
        /// When true, bypasses document validation on the server.
        /// Default is false.
        /// </summary>
        /// <remarks>
        /// Use with caution - bypassing validation can insert invalid documents.
        /// </remarks>
        public bool BypassDocumentValidation { get; set; }

        /// <summary>
        /// Optional write concern level.
        /// Values: "w1", "w2", "w3", "majority", "acknowledged", "unacknowledged"
        /// </summary>
        /// <remarks>
        /// <para>
        /// Controls the acknowledgment behavior for write operations:
        /// <list type="bullet">
        ///   <item><b>w1</b> - Acknowledged by primary only</item>
        ///   <item><b>majority</b> - Acknowledged by majority of replica set</item>
        ///   <item><b>unacknowledged</b> - No acknowledgment (fire and forget)</item>
        /// </list>
        /// </para>
        /// </remarks>
        public string WriteConcern { get; set; }

        /// <summary>
        /// Maximum number of retry attempts for transient errors.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds.
        /// Default is 100ms.
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 100;

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates default options.
        /// </summary>
        public static MongoDbBulkOperationOptions Default => new();

        /// <summary>
        /// Creates options optimized for high-throughput inserts.
        /// Uses unordered operations, bypasses validation, and uses w1 write concern.
        /// </summary>
        /// <remarks>
        /// <b>Warning:</b> These settings prioritize performance over safety.
        /// Not recommended for critical data.
        /// </remarks>
        public static MongoDbBulkOperationOptions HighThroughput => new()
        {
            IsOrdered = false,
            BypassDocumentValidation = true,
            WriteConcern = "w1",
            BatchSize = 5000
        };

        /// <summary>
        /// Creates options optimized for data integrity.
        /// Uses ordered operations, enforces validation, and uses majority write concern.
        /// </summary>
        public static MongoDbBulkOperationOptions HighIntegrity => new()
        {
            IsOrdered = true,
            BypassDocumentValidation = false,
            WriteConcern = "majority",
            BatchSize = 500,
            UseTransaction = true
        };

        #endregion
    }
}

