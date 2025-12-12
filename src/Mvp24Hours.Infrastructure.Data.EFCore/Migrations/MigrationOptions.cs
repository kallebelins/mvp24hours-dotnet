//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Migrations
{
    /// <summary>
    /// Configuration options for automatic database migrations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how migrations are applied during application startup:
    /// <list type="bullet">
    /// <item><strong>AutoMigrate</strong> - Apply pending migrations automatically</item>
    /// <item><strong>SeedData</strong> - Seed initial data after migrations</item>
    /// <item><strong>Validation</strong> - Validate schema before/after migrations</item>
    /// <item><strong>Backup</strong> - Create backup before applying migrations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursAutoMigration&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.AutoMigrateOnStartup = true;
    ///     options.ThrowOnPendingMigrations = true;
    ///     options.MigrationTimeout = TimeSpan.FromMinutes(5);
    ///     options.EnableDataSeeding = true;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class MigrationOptions
    {
        #region Migration Behavior

        /// <summary>
        /// When true, automatically applies pending migrations on application startup.
        /// Default is false (opt-in for safety).
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⚠️ <strong>Warning</strong>: Use with caution in production.
        /// Consider using a separate migration process for production deployments.
        /// </para>
        /// </remarks>
        public bool AutoMigrateOnStartup { get; set; }

        /// <summary>
        /// When true, throws an exception if there are pending migrations and AutoMigrate is false.
        /// Useful to ensure migrations are applied before the application starts.
        /// Default is false.
        /// </summary>
        public bool ThrowOnPendingMigrations { get; set; }

        /// <summary>
        /// When true, logs pending migrations as warnings instead of applying them.
        /// Useful for monitoring without automatic application.
        /// Default is true.
        /// </summary>
        public bool LogPendingMigrations { get; set; } = true;

        /// <summary>
        /// Maximum time allowed for migration operations.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan MigrationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// When true, uses a transaction for each migration.
        /// Some databases don't support DDL in transactions.
        /// Default is true (SQL Server and PostgreSQL support this).
        /// </summary>
        public bool UseTransactions { get; set; } = true;

        /// <summary>
        /// When true, creates the database if it doesn't exist before applying migrations.
        /// Default is true.
        /// </summary>
        public bool EnsureDatabaseCreated { get; set; } = true;

        #endregion

        #region Data Seeding

        /// <summary>
        /// When true, enables data seeding after migrations are applied.
        /// Default is false.
        /// </summary>
        public bool EnableDataSeeding { get; set; }

        /// <summary>
        /// When true, seeds data only if the database was just created or migrated.
        /// When false, checks and seeds data on every startup.
        /// Default is true.
        /// </summary>
        public bool SeedOnlyOnMigration { get; set; } = true;

        /// <summary>
        /// When true, runs data seeders in a transaction.
        /// Default is true.
        /// </summary>
        public bool SeedInTransaction { get; set; } = true;

        #endregion

        #region Validation and Safety

        /// <summary>
        /// When true, validates the model schema against the database before migrations.
        /// Helps detect schema drift.
        /// Default is false.
        /// </summary>
        public bool ValidateSchemaBeforeMigration { get; set; }

        /// <summary>
        /// When true, validates the model schema against the database after migrations.
        /// Ensures migrations were applied correctly.
        /// Default is false.
        /// </summary>
        public bool ValidateSchemaAfterMigration { get; set; }

        /// <summary>
        /// When true, creates a schema snapshot before applying migrations.
        /// Useful for rollback scenarios.
        /// Default is false.
        /// </summary>
        public bool CreateSchemaSnapshot { get; set; }

        /// <summary>
        /// Path where schema snapshots are stored.
        /// Default is "./migrations/snapshots".
        /// </summary>
        public string SchemaSnapshotPath { get; set; } = "./migrations/snapshots";

        /// <summary>
        /// Maximum number of schema snapshots to retain.
        /// Older snapshots are deleted. Default is 10.
        /// </summary>
        public int MaxSchemaSnapshots { get; set; } = 10;

        #endregion

        #region Retry and Resilience

        /// <summary>
        /// Maximum number of retry attempts for migration operations.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts.
        /// Default is 5 seconds.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When true, uses exponential backoff for retries.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Exception types that should trigger a retry.
        /// By default, includes common transient database exceptions.
        /// </summary>
        public ICollection<Type> RetryableExceptions { get; set; } = new List<Type>();

        #endregion

        #region Locking

        /// <summary>
        /// When true, uses a distributed lock to prevent concurrent migrations.
        /// Essential for clustered/scaled deployments.
        /// Default is true.
        /// </summary>
        public bool UseDistributedLock { get; set; } = true;

        /// <summary>
        /// Name of the distributed lock to acquire.
        /// Default is "ef-core-migration-lock".
        /// </summary>
        public string LockName { get; set; } = "ef-core-migration-lock";

        /// <summary>
        /// Maximum time to wait for the migration lock.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How long the lock is held before automatic release.
        /// Should be longer than expected migration time.
        /// Default is 30 minutes.
        /// </summary>
        public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(30);

        #endregion

        #region Logging and Diagnostics

        /// <summary>
        /// When true, logs detailed migration information.
        /// Default is true.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// When true, logs the SQL commands executed during migrations.
        /// ⚠️ May expose sensitive data.
        /// Default is false.
        /// </summary>
        public bool LogMigrationSql { get; set; }

        /// <summary>
        /// When true, emits telemetry events for migration operations.
        /// Default is true.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates options for development environments.
        /// </summary>
        public static MigrationOptions Development() => new()
        {
            AutoMigrateOnStartup = true,
            EnsureDatabaseCreated = true,
            EnableDataSeeding = true,
            SeedOnlyOnMigration = false,
            EnableDetailedLogging = true,
            LogMigrationSql = true,
            UseDistributedLock = false, // Not needed in dev
            MaxRetryAttempts = 1
        };

        /// <summary>
        /// Creates options for staging/testing environments.
        /// </summary>
        public static MigrationOptions Staging() => new()
        {
            AutoMigrateOnStartup = true,
            EnsureDatabaseCreated = true,
            EnableDataSeeding = true,
            SeedOnlyOnMigration = true,
            ValidateSchemaAfterMigration = true,
            EnableDetailedLogging = true,
            UseDistributedLock = true
        };

        /// <summary>
        /// Creates options for production environments.
        /// </summary>
        public static MigrationOptions Production() => new()
        {
            AutoMigrateOnStartup = false, // Manual migration in production
            ThrowOnPendingMigrations = true,
            LogPendingMigrations = true,
            EnableDataSeeding = false,
            ValidateSchemaBeforeMigration = true,
            CreateSchemaSnapshot = true,
            UseDistributedLock = true,
            EnableDetailedLogging = true,
            LogMigrationSql = false
        };

        /// <summary>
        /// Creates options with automatic migration disabled (log only).
        /// </summary>
        public static MigrationOptions LogOnly() => new()
        {
            AutoMigrateOnStartup = false,
            ThrowOnPendingMigrations = false,
            LogPendingMigrations = true,
            EnableDetailedLogging = true
        };

        #endregion
    }
}

