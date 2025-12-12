//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Migrations
{
    /// <summary>
    /// Service for managing EF Core database migrations programmatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides operations for:
    /// <list type="bullet">
    /// <item><strong>Apply migrations</strong> - Execute pending migrations</item>
    /// <item><strong>Rollback migrations</strong> - Revert to a specific migration</item>
    /// <item><strong>List migrations</strong> - Get applied and pending migrations</item>
    /// <item><strong>Validate schema</strong> - Check database schema against model</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMigrationService
    {
        /// <summary>
        /// Gets the list of pending migrations that have not been applied.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of pending migration names.</returns>
        Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of migrations that have been applied to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of applied migration names.</returns>
        Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available migrations (both applied and pending).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all migration names.</returns>
        Task<IReadOnlyList<string>> GetAllMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies all pending migrations to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the migration operation.</returns>
        Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies migrations up to and including the specified target migration.
        /// </summary>
        /// <param name="targetMigration">The target migration name to migrate to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the migration operation.</returns>
        Task<MigrationResult> MigrateToAsync(string targetMigration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back migrations to the specified target migration.
        /// </summary>
        /// <param name="targetMigration">The target migration to rollback to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the rollback operation.</returns>
        Task<MigrationResult> RollbackToAsync(string targetMigration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the last applied migration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the rollback operation.</returns>
        Task<MigrationResult> RollbackLastAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the database exists, creating it if necessary.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the database was created; false if it already existed.</returns>
        Task<bool> EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the database if it exists.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the database was deleted; false if it didn't exist.</returns>
        Task<bool> DeleteDatabaseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if there are any pending migrations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if there are pending migrations.</returns>
        Task<bool> HasPendingMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the database schema against the EF Core model.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Schema validation result.</returns>
        Task<SchemaValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the SQL script for pending migrations without applying them.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>SQL script for pending migrations.</returns>
        Task<string> GetMigrationScriptAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the SQL script for migrating between two specific migrations.
        /// </summary>
        /// <param name="fromMigration">Starting migration (exclusive). Null for beginning.</param>
        /// <param name="toMigration">Target migration (inclusive). Null for latest.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>SQL script for the migration range.</returns>
        Task<string> GetMigrationScriptAsync(string? fromMigration, string? toMigration, CancellationToken cancellationToken = default);
    }
}

