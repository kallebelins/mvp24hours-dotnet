//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.ConnectionPool
{
    /// <summary>
    /// Advanced configuration options for MongoDB connection pooling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Connection pooling maintains a pool of database connections for reuse,
    /// improving performance by avoiding the overhead of creating new connections.
    /// </para>
    /// <para>
    /// Key considerations:
    /// <list type="bullet">
    ///   <item>MinPoolSize: Set based on minimum concurrent operations expected</item>
    ///   <item>MaxPoolSize: Set based on maximum concurrent operations and MongoDB limits</item>
    ///   <item>WaitQueueTimeout: How long to wait for a connection from the pool</item>
    ///   <item>MaxIdleTime: Connections idle longer than this are closed</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "mydb";
    /// })
    /// .ConfigureMongoDbConnectionPool(pool =>
    /// {
    ///     pool.MinPoolSize = 10;
    ///     pool.MaxPoolSize = 200;
    ///     pool.WaitQueueTimeoutSeconds = 30;
    ///     pool.MaxConnectionIdleTimeSeconds = 300;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class MongoDbConnectionPoolOptions
    {
        /// <summary>
        /// Gets or sets the minimum number of connections in the pool.
        /// </summary>
        /// <remarks>
        /// The driver will maintain at least this many connections, even when idle.
        /// Default: 0 (no minimum)
        /// </remarks>
        public int MinPoolSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum number of connections in the pool.
        /// </summary>
        /// <remarks>
        /// When this limit is reached, operations will wait for a connection to become available.
        /// Default: 100. MongoDB Atlas has different limits per tier.
        /// </remarks>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum time to wait for a connection from the pool (in seconds).
        /// </summary>
        /// <remarks>
        /// If no connection becomes available within this time, an exception is thrown.
        /// Default: 120 seconds.
        /// </remarks>
        public int WaitQueueTimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Gets or sets the maximum idle time for a connection (in seconds).
        /// </summary>
        /// <remarks>
        /// Connections idle longer than this are closed and removed from the pool.
        /// Default: 600 seconds (10 minutes).
        /// </remarks>
        public int MaxConnectionIdleTimeSeconds { get; set; } = 600;

        /// <summary>
        /// Gets or sets the maximum lifetime of a connection (in seconds).
        /// </summary>
        /// <remarks>
        /// Connections older than this are closed and removed, regardless of idle state.
        /// Useful for load balancing across MongoDB instances.
        /// Default: 1800 seconds (30 minutes). Set to 0 for no limit.
        /// </remarks>
        public int MaxConnectionLifetimeSeconds { get; set; } = 1800;

        /// <summary>
        /// Gets or sets the connection timeout (in seconds).
        /// </summary>
        /// <remarks>
        /// Time to wait for a socket connection to be established.
        /// Default: 30 seconds.
        /// </remarks>
        public int ConnectTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the socket timeout (in seconds).
        /// </summary>
        /// <remarks>
        /// Time to wait for a response after sending a request.
        /// Default: 0 (no timeout - not recommended for production).
        /// </remarks>
        public int SocketTimeoutSeconds { get; set; } = 0;

        /// <summary>
        /// Gets or sets the server selection timeout (in seconds).
        /// </summary>
        /// <remarks>
        /// Time to wait for a suitable server to be available.
        /// Default: 30 seconds.
        /// </remarks>
        public int ServerSelectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the heartbeat frequency (in seconds).
        /// </summary>
        /// <remarks>
        /// How often the driver checks server status.
        /// Default: 10 seconds.
        /// </remarks>
        public int HeartbeatFrequencySeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to use IPv6 if available.
        /// </summary>
        public bool IPv6 { get; set; }

        /// <summary>
        /// Gets or sets whether direct connection is enabled.
        /// </summary>
        /// <remarks>
        /// When true, connects directly to a single server, bypassing replica set topology.
        /// Useful for debugging or connecting to specific nodes.
        /// </remarks>
        public bool DirectConnection { get; set; }

        /// <summary>
        /// Gets or sets the compressors to use for network compression.
        /// </summary>
        /// <remarks>
        /// Options: "zstd", "zlib", "snappy".
        /// Compression reduces network bandwidth at the cost of CPU.
        /// </remarks>
        public string[] Compressors { get; set; }

        /// <summary>
        /// Gets or sets the local threshold for server selection (in milliseconds).
        /// </summary>
        /// <remarks>
        /// Servers with latency within this threshold of the fastest are considered equally suitable.
        /// Default: 15ms.
        /// </remarks>
        public int LocalThresholdMilliseconds { get; set; } = 15;

        /// <summary>
        /// Applies the connection pool options to MongoDB client settings.
        /// </summary>
        /// <param name="settings">The MongoDB client settings to configure.</param>
        public void ApplyTo(MongoClientSettings settings)
        {
            if (settings == null) return;

            settings.MinConnectionPoolSize = MinPoolSize;
            settings.MaxConnectionPoolSize = MaxPoolSize;
            settings.WaitQueueTimeout = TimeSpan.FromSeconds(WaitQueueTimeoutSeconds);
            settings.MaxConnectionIdleTime = TimeSpan.FromSeconds(MaxConnectionIdleTimeSeconds);
            settings.MaxConnectionLifeTime = TimeSpan.FromSeconds(MaxConnectionLifetimeSeconds);
            settings.ConnectTimeout = TimeSpan.FromSeconds(ConnectTimeoutSeconds);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(ServerSelectionTimeoutSeconds);
            settings.HeartbeatInterval = TimeSpan.FromSeconds(HeartbeatFrequencySeconds);
            settings.LocalThreshold = TimeSpan.FromMilliseconds(LocalThresholdMilliseconds);
            settings.IPv6 = IPv6;
            settings.DirectConnection = DirectConnection;

            if (SocketTimeoutSeconds > 0)
            {
                settings.SocketTimeout = TimeSpan.FromSeconds(SocketTimeoutSeconds);
            }

            // Note: Compression configuration requires additional setup via connection string
            // or MongoDB driver-specific configuration. Compressors array is stored for reference.
        }
    }
}

