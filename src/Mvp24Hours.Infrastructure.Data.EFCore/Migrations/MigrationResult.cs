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
    /// Result of a migration operation.
    /// </summary>
    public sealed class MigrationResult
    {
        /// <summary>
        /// Gets whether the migration operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the list of migrations that were applied.
        /// </summary>
        public IReadOnlyList<string> AppliedMigrations { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the list of migrations that were rolled back.
        /// </summary>
        public IReadOnlyList<string> RolledBackMigrations { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the exception that caused the failure, if any.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets the duration of the migration operation.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the timestamp when the migration started.
        /// </summary>
        public DateTime StartedAt { get; init; }

        /// <summary>
        /// Gets the timestamp when the migration completed.
        /// </summary>
        public DateTime CompletedAt { get; init; }

        /// <summary>
        /// Gets additional data about the migration.
        /// </summary>
        public IDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful migration result.
        /// </summary>
        /// <param name="appliedMigrations">List of migrations that were applied.</param>
        /// <param name="duration">Duration of the operation.</param>
        /// <param name="startedAt">When the operation started.</param>
        /// <returns>A successful migration result.</returns>
        public static MigrationResult Succeeded(
            IReadOnlyList<string> appliedMigrations,
            TimeSpan duration,
            DateTime startedAt)
        {
            return new MigrationResult
            {
                Success = true,
                AppliedMigrations = appliedMigrations,
                Duration = duration,
                StartedAt = startedAt,
                CompletedAt = startedAt.Add(duration)
            };
        }

        /// <summary>
        /// Creates a successful rollback result.
        /// </summary>
        /// <param name="rolledBackMigrations">List of migrations that were rolled back.</param>
        /// <param name="duration">Duration of the operation.</param>
        /// <param name="startedAt">When the operation started.</param>
        /// <returns>A successful rollback result.</returns>
        public static MigrationResult RollbackSucceeded(
            IReadOnlyList<string> rolledBackMigrations,
            TimeSpan duration,
            DateTime startedAt)
        {
            return new MigrationResult
            {
                Success = true,
                RolledBackMigrations = rolledBackMigrations,
                Duration = duration,
                StartedAt = startedAt,
                CompletedAt = startedAt.Add(duration)
            };
        }

        /// <summary>
        /// Creates a failed migration result.
        /// </summary>
        /// <param name="errorMessage">Error message describing the failure.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">Duration until failure.</param>
        /// <param name="startedAt">When the operation started.</param>
        /// <returns>A failed migration result.</returns>
        public static MigrationResult Failed(
            string errorMessage,
            Exception? exception,
            TimeSpan duration,
            DateTime startedAt)
        {
            return new MigrationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Duration = duration,
                StartedAt = startedAt,
                CompletedAt = startedAt.Add(duration)
            };
        }

        /// <summary>
        /// Creates a result indicating no migrations were needed.
        /// </summary>
        /// <returns>A no-op migration result.</returns>
        public static MigrationResult NoMigrationsNeeded()
        {
            var now = DateTime.UtcNow;
            return new MigrationResult
            {
                Success = true,
                AppliedMigrations = Array.Empty<string>(),
                Duration = TimeSpan.Zero,
                StartedAt = now,
                CompletedAt = now
            };
        }
    }

    /// <summary>
    /// Result of schema validation.
    /// </summary>
    public sealed class SchemaValidationResult
    {
        /// <summary>
        /// Gets whether the schema is valid (matches the model).
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets the list of schema differences found.
        /// </summary>
        public IReadOnlyList<SchemaDifference> Differences { get; init; } = Array.Empty<SchemaDifference>();

        /// <summary>
        /// Gets warnings that don't necessarily indicate a problem.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the timestamp when the validation was performed.
        /// </summary>
        public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a valid schema result.
        /// </summary>
        public static SchemaValidationResult Valid() => new()
        {
            IsValid = true
        };

        /// <summary>
        /// Creates an invalid schema result with differences.
        /// </summary>
        public static SchemaValidationResult Invalid(IReadOnlyList<SchemaDifference> differences) => new()
        {
            IsValid = false,
            Differences = differences
        };
    }

    /// <summary>
    /// Represents a difference between the EF Core model and the database schema.
    /// </summary>
    public sealed class SchemaDifference
    {
        /// <summary>
        /// Gets the type of difference.
        /// </summary>
        public SchemaDifferenceType Type { get; init; }

        /// <summary>
        /// Gets the name of the affected object (table, column, index, etc.).
        /// </summary>
        public string ObjectName { get; init; } = string.Empty;

        /// <summary>
        /// Gets a description of the difference.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets the expected value (from the EF Core model).
        /// </summary>
        public string? ExpectedValue { get; init; }

        /// <summary>
        /// Gets the actual value (from the database).
        /// </summary>
        public string? ActualValue { get; init; }

        /// <summary>
        /// Gets whether this difference is critical (requires immediate action).
        /// </summary>
        public bool IsCritical { get; init; }
    }

    /// <summary>
    /// Types of schema differences.
    /// </summary>
    public enum SchemaDifferenceType
    {
        /// <summary>
        /// A table exists in the model but not in the database.
        /// </summary>
        MissingTable,

        /// <summary>
        /// A table exists in the database but not in the model.
        /// </summary>
        ExtraTable,

        /// <summary>
        /// A column exists in the model but not in the database.
        /// </summary>
        MissingColumn,

        /// <summary>
        /// A column exists in the database but not in the model.
        /// </summary>
        ExtraColumn,

        /// <summary>
        /// A column type mismatch between model and database.
        /// </summary>
        ColumnTypeMismatch,

        /// <summary>
        /// A column nullability mismatch between model and database.
        /// </summary>
        ColumnNullabilityMismatch,

        /// <summary>
        /// An index exists in the model but not in the database.
        /// </summary>
        MissingIndex,

        /// <summary>
        /// An index exists in the database but not in the model.
        /// </summary>
        ExtraIndex,

        /// <summary>
        /// A foreign key exists in the model but not in the database.
        /// </summary>
        MissingForeignKey,

        /// <summary>
        /// A foreign key exists in the database but not in the model.
        /// </summary>
        ExtraForeignKey,

        /// <summary>
        /// A primary key mismatch between model and database.
        /// </summary>
        PrimaryKeyMismatch,

        /// <summary>
        /// A default value mismatch between model and database.
        /// </summary>
        DefaultValueMismatch,

        /// <summary>
        /// Other differences not covered by specific types.
        /// </summary>
        Other
    }
}

