//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation
{
    /// <summary>
    /// Interface for validating database schema against EF Core model.
    /// </summary>
    public interface ISchemaValidator
    {
        /// <summary>
        /// Validates the database schema against the EF Core model.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with any issues found.</returns>
        Task<SchemaValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets summary information about the EF Core model.
        /// </summary>
        /// <returns>Model summary.</returns>
        ModelSummary GetModelSummary();

        /// <summary>
        /// Validates connectivity to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if connection is successful.</returns>
        Task<bool> ValidateConnectivityAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of schema validation.
    /// </summary>
    public sealed class SchemaValidationResult
    {
        /// <summary>
        /// Gets whether the schema is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets the list of validation issues.
        /// </summary>
        public IReadOnlyList<SchemaIssue> Issues { get; init; } = new List<SchemaIssue>();

        /// <summary>
        /// Gets any warnings (non-fatal issues).
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; } = new List<string>();

        /// <summary>
        /// Gets whether there are pending migrations.
        /// </summary>
        public bool HasPendingMigrations { get; init; }

        /// <summary>
        /// Gets the list of pending migrations.
        /// </summary>
        public IReadOnlyList<string> PendingMigrations { get; init; } = new List<string>();

        /// <summary>
        /// Gets the validation timestamp.
        /// </summary>
        public System.DateTime ValidatedAt { get; init; } = System.DateTime.UtcNow;

        /// <summary>
        /// Gets the duration of the validation.
        /// </summary>
        public System.TimeSpan Duration { get; init; }

        /// <summary>
        /// Creates a valid result.
        /// </summary>
        public static SchemaValidationResult Valid(System.TimeSpan duration) => new()
        {
            IsValid = true,
            Duration = duration
        };

        /// <summary>
        /// Creates an invalid result with issues.
        /// </summary>
        public static SchemaValidationResult Invalid(
            IReadOnlyList<SchemaIssue> issues,
            System.TimeSpan duration) => new()
        {
            IsValid = false,
            Issues = issues,
            Duration = duration
        };
    }

    /// <summary>
    /// Represents a schema validation issue.
    /// </summary>
    public sealed class SchemaIssue
    {
        /// <summary>
        /// Gets the severity of the issue.
        /// </summary>
        public IssueSeverity Severity { get; init; }

        /// <summary>
        /// Gets the type of issue.
        /// </summary>
        public IssueType Type { get; init; }

        /// <summary>
        /// Gets the affected object name (table, column, etc.).
        /// </summary>
        public string ObjectName { get; init; } = string.Empty;

        /// <summary>
        /// Gets a description of the issue.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets the expected value (from EF Core model).
        /// </summary>
        public string? Expected { get; init; }

        /// <summary>
        /// Gets the actual value (from database).
        /// </summary>
        public string? Actual { get; init; }

        /// <summary>
        /// Gets a suggested fix for the issue.
        /// </summary>
        public string? SuggestedFix { get; init; }
    }

    /// <summary>
    /// Severity levels for schema issues.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Informational only.
        /// </summary>
        Info,

        /// <summary>
        /// Warning - application may work but with issues.
        /// </summary>
        Warning,

        /// <summary>
        /// Error - application will likely fail.
        /// </summary>
        Error,

        /// <summary>
        /// Critical - application cannot function.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Types of schema issues.
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// Cannot connect to database.
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// Table missing from database.
        /// </summary>
        MissingTable,

        /// <summary>
        /// Column missing from table.
        /// </summary>
        MissingColumn,

        /// <summary>
        /// Column type mismatch.
        /// </summary>
        ColumnTypeMismatch,

        /// <summary>
        /// Column nullability mismatch.
        /// </summary>
        NullabilityMismatch,

        /// <summary>
        /// Index missing from database.
        /// </summary>
        MissingIndex,

        /// <summary>
        /// Foreign key missing from database.
        /// </summary>
        MissingForeignKey,

        /// <summary>
        /// Primary key mismatch.
        /// </summary>
        PrimaryKeyMismatch,

        /// <summary>
        /// Pending migration not applied.
        /// </summary>
        PendingMigration,

        /// <summary>
        /// Model configuration error.
        /// </summary>
        ModelError,

        /// <summary>
        /// Other validation issue.
        /// </summary>
        Other
    }

    /// <summary>
    /// Summary information about the EF Core model.
    /// </summary>
    public sealed class ModelSummary
    {
        /// <summary>
        /// Gets the DbContext type name.
        /// </summary>
        public string ContextType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the database provider name.
        /// </summary>
        public string ProviderName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the total number of entity types.
        /// </summary>
        public int EntityCount { get; init; }

        /// <summary>
        /// Gets the list of entity type names.
        /// </summary>
        public IReadOnlyList<string> EntityTypes { get; init; } = new List<string>();

        /// <summary>
        /// Gets the total number of tables.
        /// </summary>
        public int TableCount { get; init; }

        /// <summary>
        /// Gets the list of table names.
        /// </summary>
        public IReadOnlyList<string> Tables { get; init; } = new List<string>();

        /// <summary>
        /// Gets the total number of applied migrations.
        /// </summary>
        public int AppliedMigrationCount { get; init; }

        /// <summary>
        /// Gets the list of applied migration names.
        /// </summary>
        public IReadOnlyList<string> AppliedMigrations { get; init; } = new List<string>();
    }
}

