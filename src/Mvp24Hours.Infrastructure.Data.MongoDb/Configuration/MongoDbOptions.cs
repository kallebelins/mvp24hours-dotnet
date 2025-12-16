//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Configuration
{
    /// <summary>
    /// Configuration options for MongoDB connection and authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the MongoDB client including:
    /// <list type="bullet">
    ///   <item>Connection settings (connection string, database name)</item>
    ///   <item>TLS/SSL configuration</item>
    ///   <item>Authentication (SCRAM-SHA-256, X.509, etc.)</item>
    ///   <item>Multi-tenancy settings</item>
    ///   <item>Transaction support</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.DatabaseName = "mydb";
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.EnableTls = true;
    ///     options.EnableTransaction = true;
    ///     
    ///     // Configure authentication:
    ///     options.Authentication = new MongoDbAuthenticationOptions
    ///     {
    ///         Mechanism = MongoDbAuthMechanism.ScramSha256,
    ///         Username = "myuser",
    ///         Password = "mypassword"
    ///     };
    ///     
    ///     // Enable multi-tenancy:
    ///     options.EnableMultiTenancy = true;
    ///     options.TenantValidateOnUpdate = true;
    ///     options.TenantValidateOnDelete = true;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class MongoDbOptions
    {
        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the MongoDB connection string.
        /// </summary>
        /// <remarks>
        /// Can include authentication credentials in the connection string,
        /// or use the <see cref="Authentication"/> property for more control.
        /// </remarks>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets whether to enable TLS/SSL for the connection.
        /// </summary>
        /// <remarks>
        /// When true, uses TLS 1.2 by default. For more control over TLS settings,
        /// configure the <see cref="Authentication"/> property with specific TLS options.
        /// </remarks>
        public bool EnableTls { get; set; }

        /// <summary>
        /// Gets or sets whether to enable transaction support.
        /// </summary>
        /// <remarks>
        /// Transactions require MongoDB 4.0+ with replica set or sharded cluster.
        /// Single-node deployments do not support transactions.
        /// </remarks>
        public bool EnableTransaction { get; set; }

        /// <summary>
        /// Gets or sets the authentication configuration.
        /// </summary>
        /// <remarks>
        /// Supports SCRAM-SHA-1, SCRAM-SHA-256, X.509, AWS IAM, LDAP, and Kerberos authentication.
        /// If null, authentication credentials from the connection string are used.
        /// </remarks>
        public MongoDbAuthenticationOptions Authentication { get; set; }

        /// <summary>
        /// Gets or sets whether to enable automatic multi-tenancy filtering.
        /// </summary>
        /// <remarks>
        /// When enabled, entities implementing <see cref="Core.Contract.Domain.Entity.ITenantEntity"/>
        /// will have their TenantId automatically set on insert and queries will be filtered by tenant.
        /// </remarks>
        public bool EnableMultiTenancy { get; set; }

        /// <summary>
        /// Gets or sets whether to validate tenant ownership on update operations.
        /// Default is true.
        /// </summary>
        public bool TenantValidateOnUpdate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate tenant ownership on delete operations.
        /// Default is true.
        /// </summary>
        public bool TenantValidateOnDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to throw an exception when no tenant is set for tenant entities.
        /// Default is true.
        /// </summary>
        public bool TenantThrowOnMissing { get; set; } = true;

        /// <summary>
        /// Gets or sets the field-level encryption key (Base64 encoded).
        /// </summary>
        /// <remarks>
        /// Used for client-side field-level encryption of sensitive data.
        /// The key must be 256 bits (32 bytes) before Base64 encoding.
        /// </remarks>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the read preference for read operations.
        /// </summary>
        /// <remarks>
        /// Options: "primary", "primaryPreferred", "secondary", "secondaryPreferred", "nearest".
        /// Default is null (uses primary).
        /// </remarks>
        public string ReadPreference { get; set; }

        /// <summary>
        /// Gets or sets the write concern level.
        /// </summary>
        /// <remarks>
        /// Options: "w1", "w2", "w3", "majority", "acknowledged", "unacknowledged".
        /// Default is null (uses w1).
        /// </remarks>
        public string WriteConcern { get; set; }

        /// <summary>
        /// Gets or sets the read concern level.
        /// </summary>
        /// <remarks>
        /// Options: "local", "available", "majority", "linearizable", "snapshot".
        /// Default is null (uses local).
        /// </remarks>
        public string ReadConcern { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        public int? ConnectionTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the socket timeout in seconds.
        /// </summary>
        public int? SocketTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum connection pool size.
        /// </summary>
        public int? MaxConnectionPoolSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum connection pool size.
        /// </summary>
        public int? MinConnectionPoolSize { get; set; }

        /// <summary>
        /// Gets or sets whether to enable command logging.
        /// </summary>
        public bool EnableCommandLogging { get; set; }

        /// <summary>
        /// Gets or sets whether to retry reads on network errors.
        /// Default is true.
        /// </summary>
        public bool RetryReads { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to retry writes on network errors.
        /// Default is true.
        /// </summary>
        public bool RetryWrites { get; set; } = true;
    }
}
