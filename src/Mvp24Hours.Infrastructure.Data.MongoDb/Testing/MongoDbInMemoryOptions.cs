//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// Configuration options for in-memory MongoDB testing.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of in-memory MongoDB contexts
/// for unit and integration testing scenarios.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Unique database naming for test isolation</item>
/// <item>Connection string configuration</item>
/// <item>Logging and tracing options</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new MongoDbInMemoryOptions
/// {
///     DatabaseNamePrefix = "TestDb",
///     UseUniqueDatabaseName = true,
///     EnableLogging = true
/// };
/// 
/// var factory = new MongoDbInMemoryContextFactory(options);
/// </code>
/// </example>
public class MongoDbInMemoryOptions
{
    /// <summary>
    /// Gets or sets the prefix for the database name.
    /// </summary>
    /// <remarks>
    /// Default is "InMemoryMongoTestDb".
    /// </remarks>
    public string DatabaseNamePrefix { get; set; } = "InMemoryMongoTestDb";

    /// <summary>
    /// Gets or sets the fixed database name.
    /// </summary>
    /// <remarks>
    /// If set, this takes precedence over <see cref="DatabaseNamePrefix"/>.
    /// When <see cref="UseUniqueDatabaseName"/> is true, a unique suffix is still appended.
    /// </remarks>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a unique database name per context.
    /// </summary>
    /// <remarks>
    /// Default is true. When true, a GUID suffix is appended to ensure test isolation.
    /// </remarks>
    public bool UseUniqueDatabaseName { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection string for MongoDB.
    /// </summary>
    /// <remarks>
    /// For in-memory testing with Mongo2Go or MongoDbRunner, this will be set automatically.
    /// For Testcontainers, the connection string is provided by the container.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether to enable detailed logging.
    /// </summary>
    /// <remarks>
    /// Default is true for debugging test scenarios.
    /// </remarks>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable transaction support.
    /// </summary>
    /// <remarks>
    /// Transactions require a replica set. In-memory providers like Mongo2Go
    /// may not support transactions. Default is false.
    /// </remarks>
    public bool EnableTransaction { get; set; }

    /// <summary>
    /// Gets or sets whether to enable multi-tenancy support.
    /// </summary>
    public bool EnableMultiTenancy { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds for operations.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets an action to configure the MongoDbOptions.
    /// </summary>
    public Action<Configuration.MongoDbOptions>? ConfigureOptions { get; set; }

    /// <summary>
    /// Gets the effective database name based on configuration.
    /// </summary>
    /// <returns>The database name to use.</returns>
    public string GetEffectiveDatabaseName()
    {
        var baseName = DatabaseName ?? DatabaseNamePrefix;
        return UseUniqueDatabaseName
            ? $"{baseName}_{Guid.NewGuid():N}"
            : baseName;
    }

    /// <summary>
    /// Creates default options for unit testing.
    /// </summary>
    /// <returns>Options configured for unit testing.</returns>
    public static MongoDbInMemoryOptions ForUnitTesting()
    {
        return new MongoDbInMemoryOptions
        {
            UseUniqueDatabaseName = true,
            EnableLogging = false,
            EnableTransaction = false,
            TimeoutSeconds = 5
        };
    }

    /// <summary>
    /// Creates default options for integration testing.
    /// </summary>
    /// <returns>Options configured for integration testing.</returns>
    public static MongoDbInMemoryOptions ForIntegrationTesting()
    {
        return new MongoDbInMemoryOptions
        {
            UseUniqueDatabaseName = true,
            EnableLogging = true,
            EnableTransaction = false,
            TimeoutSeconds = 60
        };
    }

    /// <summary>
    /// Creates options for shared database testing.
    /// </summary>
    /// <param name="databaseName">The shared database name.</param>
    /// <returns>Options configured for shared database testing.</returns>
    public static MongoDbInMemoryOptions ForSharedDatabase(string databaseName)
    {
        ArgumentNullException.ThrowIfNull(databaseName);

        return new MongoDbInMemoryOptions
        {
            DatabaseName = databaseName,
            UseUniqueDatabaseName = false,
            EnableLogging = true
        };
    }
}

