//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Options
{
    /// <summary>
    /// Configuration options for Quartz.NET background job provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the Quartz.NET provider for background job processing.
    /// Quartz.NET requires a database for job storage and provides clustering support
    /// for distributed job processing.
    /// </para>
    /// </remarks>
    public class QuartzJobOptions
    {
        /// <summary>
        /// Gets or sets the database connection string for Quartz storage.
        /// </summary>
        /// <remarks>
        /// Required for persistent job storage. Supported databases:
        /// - SQL Server
        /// - PostgreSQL
        /// - MySQL
        /// - SQLite
        /// - Oracle
        /// </remarks>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the storage provider type.
        /// </summary>
        /// <remarks>
        /// Determines which database/storage system Quartz.NET will use.
        /// </remarks>
        public QuartzStorageProvider StorageProvider { get; set; } = QuartzStorageProvider.SqlServer;

        /// <summary>
        /// Gets or sets the table prefix for Quartz tables.
        /// </summary>
        /// <remarks>
        /// Default is "QRTZ_". Change if you need to avoid conflicts with existing tables.
        /// </remarks>
        public string TablePrefix { get; set; } = "QRTZ_";

        /// <summary>
        /// Gets or sets the instance ID for Quartz scheduler.
        /// </summary>
        /// <remarks>
        /// Unique identifier for this scheduler instance. Used for clustering.
        /// Default is auto-generated.
        /// </remarks>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the instance name for Quartz scheduler.
        /// </summary>
        /// <remarks>
        /// Name of this scheduler instance. Used for identification in logs and monitoring.
        /// Default is "Mvp24HoursScheduler".
        /// </remarks>
        public string InstanceName { get; set; } = "Mvp24HoursScheduler";

        /// <summary>
        /// Gets or sets whether to enable clustering.
        /// </summary>
        /// <remarks>
        /// When enabled, multiple scheduler instances can run in a cluster for high availability
        /// and load distribution. Requires database storage.
        /// </summary>
        public bool EnableClustering { get; set; } = false;

        /// <summary>
        /// Gets or sets the cluster check-in interval.
        /// </summary>
        /// <remarks>
        /// How often scheduler instances check in with the database when clustering is enabled.
        /// Default is 20 seconds.
        /// </remarks>
        public TimeSpan ClusterCheckinInterval { get; set; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Gets or sets the maximum number of threads in the thread pool.
        /// </summary>
        /// <remarks>
        /// Default is 10. Increase for higher throughput, decrease for lower resource usage.
        /// </remarks>
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Gets or sets the misfire threshold.
        /// </summary>
        /// <remarks>
        /// Jobs that are delayed by more than this threshold are considered misfired.
        /// Default is 60 seconds.
        /// </remarks>
        public TimeSpan MisfireThreshold { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the thread priority.
        /// </summary>
        /// <remarks>
        /// Thread priority for job execution threads.
        /// Default is Normal.
        /// </remarks>
        public System.Threading.ThreadPriority ThreadPriority { get; set; } = System.Threading.ThreadPriority.Normal;

        /// <summary>
        /// Gets or sets whether to use UTC timezone.
        /// </summary>
        /// <remarks>
        /// When true, all times are interpreted as UTC. When false, system timezone is used.
        /// Default is true.
        /// </remarks>
        public bool UseUtcTimezone { get; set; } = true;

        /// <summary>
        /// Gets or sets the serialization type for job data.
        /// </summary>
        /// <remarks>
        /// How job data is serialized when stored in the database.
        /// Default is JSON.
        /// </remarks>
        public QuartzSerializationType SerializationType { get; set; } = QuartzSerializationType.Json;
    }

    /// <summary>
    /// Represents the storage provider for Quartz.NET.
    /// </summary>
    public enum QuartzStorageProvider
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
        /// SQLite storage (file-based, not suitable for clustering).
        /// </summary>
        Sqlite = 3,

        /// <summary>
        /// Oracle storage.
        /// </summary>
        Oracle = 4,

        /// <summary>
        /// In-memory storage (for testing only, not persistent).
        /// </summary>
        Memory = 5
    }

    /// <summary>
    /// Represents the serialization type for Quartz job data.
    /// </summary>
    public enum QuartzSerializationType
    {
        /// <summary>
        /// Binary serialization (default, requires [Serializable]).
        /// </summary>
        Binary = 0,

        /// <summary>
        /// JSON serialization (requires Quartz.Serialization.Json package).
        /// </summary>
        Json = 1
    }
}

