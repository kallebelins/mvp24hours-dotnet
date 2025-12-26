//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Options
{
    /// <summary>
    /// Configuration options for Hangfire background job provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the Hangfire provider for background job processing.
    /// Hangfire requires a database for job storage and provides a web dashboard for
    /// monitoring and managing jobs.
    /// </para>
    /// </remarks>
    public class HangfireJobOptions
    {
        /// <summary>
        /// Gets or sets the database connection string for Hangfire storage.
        /// </summary>
        /// <remarks>
        /// Required for persistent job storage. Supported databases:
        /// - SQL Server
        /// - PostgreSQL
        /// - MySQL
        /// - MongoDB
        /// - Redis
        /// </remarks>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the storage provider type.
        /// </summary>
        /// <remarks>
        /// Determines which database/storage system Hangfire will use.
        /// </remarks>
        public HangfireStorageProvider StorageProvider { get; set; } = HangfireStorageProvider.SqlServer;

        /// <summary>
        /// Gets or sets the schema name for SQL-based storage providers.
        /// </summary>
        /// <remarks>
        /// Only used for SQL Server, PostgreSQL, and MySQL providers.
        /// Default is "HangFire" for SQL Server, "hangfire" for PostgreSQL/MySQL.
        /// </remarks>
        public string? SchemaName { get; set; }

        /// <summary>
        /// Gets or sets the number of worker threads for processing jobs.
        /// </summary>
        /// <remarks>
        /// Default is 5. Increase for higher throughput, decrease for lower resource usage.
        /// </remarks>
        public int WorkerCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the dashboard path for Hangfire web UI.
        /// </summary>
        /// <remarks>
        /// Default is "/hangfire". Set to null to disable the dashboard.
        /// </remarks>
        public string DashboardPath { get; set; } = "/hangfire";

        /// <summary>
        /// Gets or sets whether to enable the Hangfire dashboard.
        /// </summary>
        /// <remarks>
        /// Default is true. Set to false to disable the web dashboard.
        /// </remarks>
        public bool EnableDashboard { get; set; } = true;

        /// <summary>
        /// Gets or sets the dashboard authorization policy.
        /// </summary>
        /// <remarks>
        /// Configure authorization for accessing the Hangfire dashboard.
        /// For example: "RequireAuthenticatedUser" or a custom policy name.
        /// </remarks>
        public string? DashboardAuthorizationPolicy { get; set; }

        /// <summary>
        /// Gets or sets the default queue name.
        /// </summary>
        /// <remarks>
        /// Default is "default". Jobs without a specified queue will use this queue.
        /// </remarks>
        public string DefaultQueue { get; set; } = "default";

        /// <summary>
        /// Gets or sets the job expiration timeout.
        /// </summary>
        /// <remarks>
        /// Jobs older than this will be automatically deleted.
        /// Default is 7 days.
        /// </remarks>
        public TimeSpan JobExpirationTimeout { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets the server check interval.
        /// </summary>
        /// <remarks>
        /// How often Hangfire checks for new jobs.
        /// Default is 15 seconds.
        /// </remarks>
        public TimeSpan ServerCheckInterval { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Gets or sets the heartbeat interval.
        /// </summary>
        /// <remarks>
        /// How often Hangfire servers send heartbeat signals.
        /// Default is 30 seconds.
        /// </remarks>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Represents the storage provider for Hangfire.
    /// </summary>
    public enum HangfireStorageProvider
    {
        /// <summary>
        /// SQL Server storage.
        /// </summary>
        SqlServer = 0,

        /// <summary>
        /// PostgreSQL storage.
        /// </summary>
        PostgreSql = 1,

        /// <summary>
        /// MySQL storage.
        /// </summary>
        MySql = 2,

        /// <summary>
        /// MongoDB storage.
        /// </summary>
        MongoDb = 3,

        /// <summary>
        /// Redis storage.
        /// </summary>
        Redis = 4,

        /// <summary>
        /// In-memory storage (for testing only).
        /// </summary>
        Memory = 5
    }
}

