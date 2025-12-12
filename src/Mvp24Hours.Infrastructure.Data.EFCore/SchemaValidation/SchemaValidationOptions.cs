//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation
{
    /// <summary>
    /// Configuration options for schema validation at startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Schema validation helps detect:
    /// <list type="bullet">
    /// <item><strong>Schema drift</strong> - Database schema differs from EF Core model</item>
    /// <item><strong>Missing migrations</strong> - Pending migrations not applied</item>
    /// <item><strong>Broken model</strong> - Entity configuration errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class SchemaValidationOptions
    {
        /// <summary>
        /// When true, validates schema on application startup.
        /// Default is false (opt-in for performance).
        /// </summary>
        public bool ValidateOnStartup { get; set; }

        /// <summary>
        /// When true, throws an exception if schema validation fails.
        /// Default is false (logs warning instead).
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; }

        /// <summary>
        /// When true, checks for pending migrations as part of validation.
        /// Default is true.
        /// </summary>
        public bool CheckPendingMigrations { get; set; } = true;

        /// <summary>
        /// When true, validates that all model entities have corresponding tables.
        /// Default is true.
        /// </summary>
        public bool ValidateTables { get; set; } = true;

        /// <summary>
        /// When true, validates that all columns exist with correct types.
        /// Default is false (requires database introspection).
        /// </summary>
        public bool ValidateColumns { get; set; }

        /// <summary>
        /// When true, validates that indexes exist in the database.
        /// Default is false.
        /// </summary>
        public bool ValidateIndexes { get; set; }

        /// <summary>
        /// When true, validates that foreign keys exist in the database.
        /// Default is false.
        /// </summary>
        public bool ValidateForeignKeys { get; set; }

        /// <summary>
        /// Timeout for schema validation queries.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Tables to exclude from validation.
        /// Useful for tables managed by other systems.
        /// </summary>
        public ICollection<string> ExcludedTables { get; set; } = new List<string>();

        /// <summary>
        /// When true, logs detailed validation progress.
        /// Default is true.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// When true, caches validation results to avoid repeated checks.
        /// Default is true.
        /// </summary>
        public bool CacheValidationResults { get; set; } = true;

        /// <summary>
        /// Duration to cache validation results.
        /// Default is 1 hour.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

        #region Factory Methods

        /// <summary>
        /// Creates options for development environments (strict validation).
        /// </summary>
        public static SchemaValidationOptions Development() => new()
        {
            ValidateOnStartup = true,
            ThrowOnValidationFailure = true,
            CheckPendingMigrations = true,
            ValidateTables = true,
            ValidateColumns = true,
            ValidateIndexes = false,
            ValidateForeignKeys = false,
            EnableDetailedLogging = true
        };

        /// <summary>
        /// Creates options for staging environments.
        /// </summary>
        public static SchemaValidationOptions Staging() => new()
        {
            ValidateOnStartup = true,
            ThrowOnValidationFailure = false,
            CheckPendingMigrations = true,
            ValidateTables = true,
            ValidateColumns = true,
            EnableDetailedLogging = true
        };

        /// <summary>
        /// Creates options for production environments (minimal validation).
        /// </summary>
        public static SchemaValidationOptions Production() => new()
        {
            ValidateOnStartup = true,
            ThrowOnValidationFailure = false,
            CheckPendingMigrations = true,
            ValidateTables = false, // Skip expensive checks in production
            ValidateColumns = false,
            EnableDetailedLogging = false
        };

        /// <summary>
        /// Creates options for CI/CD pipelines (comprehensive validation).
        /// </summary>
        public static SchemaValidationOptions ContinuousIntegration() => new()
        {
            ValidateOnStartup = true,
            ThrowOnValidationFailure = true,
            CheckPendingMigrations = true,
            ValidateTables = true,
            ValidateColumns = true,
            ValidateIndexes = true,
            ValidateForeignKeys = true,
            EnableDetailedLogging = true,
            CacheValidationResults = false
        };

        #endregion
    }
}

