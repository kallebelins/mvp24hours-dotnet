//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations
{
    /// <summary>
    /// Interface for running MongoDB database migrations.
    /// </summary>
    public interface IMongoDbMigrationRunner
    {
        /// <summary>
        /// Gets the current database version (highest applied migration version).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current version number, or 0 if no migrations have been applied.</returns>
        Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending migrations that have not been applied yet.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of pending migrations ordered by version.</returns>
        Task<IReadOnlyList<IMongoDbMigration>> GetPendingMigrationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all migrations that have been applied.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of applied migration history entries.</returns>
        Task<IReadOnlyList<MongoDbMigrationHistory>> GetAppliedMigrationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies all pending migrations up to the latest version.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing information about the migration execution.</returns>
        Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies migrations up to a specific target version.
        /// </summary>
        /// <param name="targetVersion">The target version to migrate to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing information about the migration execution.</returns>
        Task<MigrationResult> MigrateToVersionAsync(
            int targetVersion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back migrations to a specific version.
        /// </summary>
        /// <param name="targetVersion">The target version to roll back to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing information about the rollback execution.</returns>
        Task<MigrationResult> RollbackToVersionAsync(
            int targetVersion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the last applied migration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing information about the rollback execution.</returns>
        Task<MigrationResult> RollbackLastAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a migration operation.
    /// </summary>
    public sealed class MigrationResult
    {
        /// <summary>
        /// Gets or sets whether the migration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the starting version before migration.
        /// </summary>
        public int StartVersion { get; set; }

        /// <summary>
        /// Gets or sets the ending version after migration.
        /// </summary>
        public int EndVersion { get; set; }

        /// <summary>
        /// Gets or sets the number of migrations applied.
        /// </summary>
        public int MigrationsApplied { get; set; }

        /// <summary>
        /// Gets or sets the total duration of all migrations in milliseconds.
        /// </summary>
        public long TotalDurationMs { get; set; }

        /// <summary>
        /// Gets or sets error message if the migration failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the details of each migration executed.
        /// </summary>
        public List<MigrationStepResult> Steps { get; set; } = new();

        /// <summary>
        /// Creates a successful migration result.
        /// </summary>
        public static MigrationResult Successful(int startVersion, int endVersion, int migrationsApplied, long durationMs)
        {
            return new MigrationResult
            {
                Success = true,
                StartVersion = startVersion,
                EndVersion = endVersion,
                MigrationsApplied = migrationsApplied,
                TotalDurationMs = durationMs
            };
        }

        /// <summary>
        /// Creates a failed migration result.
        /// </summary>
        public static MigrationResult Failed(int startVersion, int currentVersion, string error)
        {
            return new MigrationResult
            {
                Success = false,
                StartVersion = startVersion,
                EndVersion = currentVersion,
                Error = error
            };
        }
    }

    /// <summary>
    /// Result of a single migration step.
    /// </summary>
    public sealed class MigrationStepResult
    {
        /// <summary>
        /// Gets or sets the migration version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the migration description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this step was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the duration of this step in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets or sets error message if this step failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets whether this was an upgrade (Up) or downgrade (Down) operation.
        /// </summary>
        public bool IsUpgrade { get; set; }
    }
}

