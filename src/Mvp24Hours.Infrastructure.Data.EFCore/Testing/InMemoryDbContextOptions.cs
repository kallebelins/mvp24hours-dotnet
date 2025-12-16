//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// Options for configuring in-memory database contexts for testing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration options for creating in-memory database contexts
/// that are suitable for unit and integration testing scenarios.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Automatic unique database naming per test</item>
/// <item>Configurable warning/error handling for query limitations</item>
/// <item>Optional foreign key constraint enforcement</item>
/// <item>Support for model validation</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new InMemoryDbContextOptions
/// {
///     DatabaseName = "TestDatabase",
///     EnableSensitiveDataLogging = true,
///     SuppressTransactionWarning = true,
///     EnforceForeignKeys = false
/// };
/// </code>
/// </example>
public class InMemoryDbContextOptions
{
    /// <summary>
    /// Gets or sets the name of the in-memory database.
    /// If null, a unique name will be generated automatically.
    /// </summary>
    /// <remarks>
    /// Each unique database name creates an isolated in-memory database instance.
    /// Use different names to isolate tests from each other.
    /// </remarks>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets whether to use a unique database name per test.
    /// When true, appends a unique identifier to the DatabaseName.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Setting this to true ensures each test runs with a fresh database,
    /// preventing test pollution and making tests independent of each other.
    /// </remarks>
    public bool UseUniqueDatabaseName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable sensitive data logging.
    /// Default is true (useful for debugging tests).
    /// </summary>
    /// <remarks>
    /// When enabled, parameter values and potentially sensitive data will appear in logs.
    /// This is safe for testing but should never be enabled in production.
    /// </remarks>
    public bool EnableSensitiveDataLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed error messages.
    /// Default is true.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to suppress transaction warnings.
    /// The in-memory provider does not support transactions.
    /// Default is true.
    /// </summary>
    public bool SuppressTransactionWarning { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw on client evaluation warnings.
    /// Default is false (warnings only).
    /// </summary>
    /// <remarks>
    /// Set to true to make tests fail when queries that cannot be translated
    /// to SQL are detected. This helps catch potential performance issues early.
    /// </remarks>
    public bool ThrowOnClientEvaluationWarning { get; set; }

    /// <summary>
    /// Gets or sets whether to enforce foreign key constraints.
    /// The in-memory provider can optionally enforce this.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to false, you can insert entities with missing related entities,
    /// which simplifies some test scenarios.
    /// </para>
    /// <para>
    /// When set to true, the in-memory database will validate foreign key
    /// relationships, more closely mimicking real database behavior.
    /// </para>
    /// </remarks>
    public bool EnforceForeignKeys { get; set; }

    /// <summary>
    /// Gets or sets a custom action to configure additional DbContextOptions.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureOptions { get; set; }

    /// <summary>
    /// Gets or sets a custom action to configure warnings.
    /// </summary>
    public Action<WarningsConfigurationBuilder>? ConfigureWarnings { get; set; }

    /// <summary>
    /// Gets or sets whether to validate the model on context creation.
    /// Default is true.
    /// </summary>
    public bool ValidateModel { get; set; } = true;

    /// <summary>
    /// Generates the effective database name based on configuration.
    /// </summary>
    /// <returns>The database name to use.</returns>
    public string GetEffectiveDatabaseName()
    {
        var baseName = DatabaseName ?? "InMemoryTestDb";
        return UseUniqueDatabaseName 
            ? $"{baseName}_{Guid.NewGuid():N}" 
            : baseName;
    }
}

