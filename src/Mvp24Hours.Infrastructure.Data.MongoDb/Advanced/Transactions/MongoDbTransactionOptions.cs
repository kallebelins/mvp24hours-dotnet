//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Transactions
{
    /// <summary>
    /// Configuration options for MongoDB transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configure transaction behavior including:
    /// <list type="bullet">
    ///   <item>Read/Write concerns for transaction isolation</item>
    ///   <item>Retry settings for transient errors</item>
    ///   <item>Timeout settings</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MongoDbTransactionOptions
    {
        /// <summary>
        /// Gets or sets the default read concern for transactions.
        /// Default is <see cref="ReadConcern.Snapshot"/>.
        /// </summary>
        public ReadConcern DefaultReadConcern { get; set; } = ReadConcern.Snapshot;

        /// <summary>
        /// Gets or sets the default write concern for transactions.
        /// Default is <see cref="WriteConcern.WMajority"/>.
        /// </summary>
        public WriteConcern DefaultWriteConcern { get; set; } = WriteConcern.WMajority;

        /// <summary>
        /// Gets or sets the default read preference for transactions.
        /// Default is <see cref="ReadPreference.Primary"/>.
        /// </summary>
        public ReadPreference DefaultReadPreference { get; set; } = ReadPreference.Primary;

        /// <summary>
        /// Gets or sets the maximum time allowed for a commit operation.
        /// Default is null (no limit).
        /// </summary>
        public TimeSpan? MaxCommitTime { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries for transient transaction errors.
        /// Default is 3.
        /// </summary>
        public int MaxTransactionRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum number of retries for unknown commit results.
        /// Default is 3.
        /// </summary>
        public int MaxCommitRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// Default is 100ms.
        /// </summary>
        public int RetryDelayMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to automatically retry read operations within a transaction.
        /// Default is true.
        /// </summary>
        public bool AutoRetryReads { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically retry write operations within a transaction.
        /// Default is true.
        /// </summary>
        public bool AutoRetryWrites { get; set; } = true;
    }
}

