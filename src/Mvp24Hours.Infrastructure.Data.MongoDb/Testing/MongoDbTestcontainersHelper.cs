//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// Configuration options for MongoDB Testcontainers.
/// </summary>
/// <remarks>
/// <para>
/// Use these options to configure the MongoDB container for integration testing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new MongoDbTestcontainersOptions
/// {
///     ImageTag = "6.0",
///     DatabaseName = "integration_tests",
///     EnableReplicaSet = true
/// };
/// </code>
/// </example>
public class MongoDbTestcontainersOptions
{
    /// <summary>
    /// Gets or sets the MongoDB Docker image tag.
    /// </summary>
    /// <remarks>
    /// Default is "latest". Use specific versions like "6.0", "5.0", "4.4" for consistency.
    /// </remarks>
    public string ImageTag { get; set; } = "latest";

    /// <summary>
    /// Gets or sets the database name for testing.
    /// </summary>
    /// <remarks>
    /// Default is "testdb". A unique suffix can be added if <see cref="UseUniqueDatabaseName"/> is true.
    /// </remarks>
    public string DatabaseName { get; set; } = "testdb";

    /// <summary>
    /// Gets or sets whether to use a unique database name per test.
    /// </summary>
    /// <remarks>
    /// Default is true. When true, a GUID suffix is appended to ensure test isolation.
    /// </remarks>
    public bool UseUniqueDatabaseName { get; set; } = true;

    /// <summary>
    /// Gets or sets the container port mapping.
    /// </summary>
    /// <remarks>
    /// Default is 27017. Set to 0 to use a random available port.
    /// </remarks>
    public int? Port { get; set; }

    /// <summary>
    /// Gets or sets the MongoDB username.
    /// </summary>
    /// <remarks>
    /// Optional. If set along with <see cref="Password"/>, authentication is enabled.
    /// </remarks>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the MongoDB password.
    /// </summary>
    /// <remarks>
    /// Optional. Required if <see cref="Username"/> is set.
    /// </remarks>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets whether to enable replica set mode.
    /// </summary>
    /// <remarks>
    /// Default is false. Enable for testing transactions and change streams.
    /// Note: Replica set mode requires additional configuration.
    /// </remarks>
    public bool EnableReplicaSet { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds for container startup.
    /// </summary>
    /// <remarks>
    /// Default is 60 seconds.
    /// </remarks>
    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to remove the container after the test completes.
    /// </summary>
    /// <remarks>
    /// Default is true. Set to false for debugging container issues.
    /// </remarks>
    public bool AutoRemove { get; set; } = true;

    /// <summary>
    /// Gets or sets the container name prefix.
    /// </summary>
    /// <remarks>
    /// Default is "mvp24hours-mongodb-test".
    /// </remarks>
    public string ContainerNamePrefix { get; set; } = "mvp24hours-mongodb-test";

    /// <summary>
    /// Gets the effective database name based on configuration.
    /// </summary>
    /// <returns>The database name to use.</returns>
    public string GetEffectiveDatabaseName()
    {
        return UseUniqueDatabaseName
            ? $"{DatabaseName}_{Guid.NewGuid():N}"
            : DatabaseName;
    }

    /// <summary>
    /// Gets the Docker image name including tag.
    /// </summary>
    /// <returns>The full image name.</returns>
    public string GetImageName()
    {
        return $"mongo:{ImageTag}";
    }

    /// <summary>
    /// Creates options configured for basic testing.
    /// </summary>
    /// <returns>Options for basic testing without authentication.</returns>
    public static MongoDbTestcontainersOptions ForBasicTesting()
    {
        return new MongoDbTestcontainersOptions
        {
            ImageTag = "6.0",
            UseUniqueDatabaseName = true,
            AutoRemove = true
        };
    }

    /// <summary>
    /// Creates options configured for testing with authentication.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>Options with authentication enabled.</returns>
    public static MongoDbTestcontainersOptions ForAuthenticatedTesting(string username, string password)
    {
        return new MongoDbTestcontainersOptions
        {
            ImageTag = "6.0",
            Username = username,
            Password = password,
            UseUniqueDatabaseName = true,
            AutoRemove = true
        };
    }

    /// <summary>
    /// Creates options configured for testing with replica set.
    /// </summary>
    /// <returns>Options with replica set enabled.</returns>
    public static MongoDbTestcontainersOptions ForReplicaSetTesting()
    {
        return new MongoDbTestcontainersOptions
        {
            ImageTag = "6.0",
            EnableReplicaSet = true,
            UseUniqueDatabaseName = true,
            AutoRemove = true
        };
    }
}

/// <summary>
/// Helper class for managing MongoDB Testcontainers in integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This helper provides utilities for creating and managing MongoDB containers
/// using Testcontainers for integration testing.
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// <list type="bullet">
/// <item>Docker must be installed and running</item>
/// <item>Testcontainers.MongoDb NuGet package must be installed</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <list type="number">
/// <item>Create a container using <see cref="CreateContainerAsync"/></item>
/// <item>Get the connection string from the returned container</item>
/// <item>Create your MongoDB context using the connection string</item>
/// <item>Dispose the container after tests complete</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In test fixture setup
/// var options = MongoDbTestcontainersOptions.ForBasicTesting();
/// var connectionInfo = await MongoDbTestcontainersHelper.StartContainerAsync(options);
/// 
/// // Use the connection string
/// var contextFactory = new MongoDbContextFactory(connectionInfo.ConnectionString);
/// using var context = contextFactory.CreateContext();
/// 
/// // Run tests...
/// 
/// // In test fixture teardown
/// await MongoDbTestcontainersHelper.StopContainerAsync(connectionInfo);
/// </code>
/// </example>
public static class MongoDbTestcontainersHelper
{
    /// <summary>
    /// Creates MongoDB configuration from Testcontainers options.
    /// </summary>
    /// <param name="connectionString">The connection string from the container.</param>
    /// <param name="options">The Testcontainers options.</param>
    /// <returns>MongoDbOptions configured for the container.</returns>
    public static MongoDbOptions CreateOptions(string connectionString, MongoDbTestcontainersOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(options);

        return new MongoDbOptions
        {
            ConnectionString = connectionString,
            DatabaseName = options.GetEffectiveDatabaseName(),
            EnableTransaction = options.EnableReplicaSet,
            EnableCommandLogging = true,
            ConnectionTimeoutSeconds = options.StartupTimeoutSeconds,
            SocketTimeoutSeconds = options.StartupTimeoutSeconds
        };
    }

    /// <summary>
    /// Creates a MongoDB context from container connection information.
    /// </summary>
    /// <param name="connectionString">The connection string from the container.</param>
    /// <param name="options">The Testcontainers options.</param>
    /// <returns>A new MongoDB context connected to the container.</returns>
    public static Mvp24HoursContext CreateContext(string connectionString, MongoDbTestcontainersOptions options)
    {
        var mongoOptions = CreateOptions(connectionString, options);
        return new Mvp24HoursContext(mongoOptions);
    }

    /// <summary>
    /// Creates a context factory for the container.
    /// </summary>
    /// <param name="connectionString">The connection string from the container.</param>
    /// <param name="options">Optional Testcontainers options.</param>
    /// <returns>A new context factory.</returns>
    public static MongoDbContextFactory CreateContextFactory(
        string connectionString,
        MongoDbTestcontainersOptions? options = null)
    {
        var effectiveOptions = options ?? MongoDbTestcontainersOptions.ForBasicTesting();

        var factoryOptions = new MongoDbInMemoryOptions
        {
            ConnectionString = connectionString,
            DatabaseName = effectiveOptions.GetEffectiveDatabaseName(),
            UseUniqueDatabaseName = false, // Already unique from options
            EnableTransaction = effectiveOptions.EnableReplicaSet,
            EnableLogging = true
        };

        return new MongoDbContextFactory(factoryOptions);
    }

    /// <summary>
    /// Validates that Docker is available.
    /// </summary>
    /// <returns>True if Docker is available; otherwise, false.</returns>
    public static bool IsDockerAvailable()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for MongoDB to be ready to accept connections.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="timeoutSeconds">Maximum time to wait.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if MongoDB is ready; otherwise, false.</returns>
    public static async Task<bool> WaitForMongoDbReadyAsync(
        string connectionString,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var retryDelay = TimeSpan.FromMilliseconds(500);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var client = new MongoDB.Driver.MongoClient(connectionString);
                var database = client.GetDatabase("admin");

                // Try to ping the server
                await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                    new MongoDB.Bson.BsonDocument("ping", 1),
                    cancellationToken: cancellationToken);

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-testcontainers-ready");
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    "mongodb-testcontainers-waiting",
                    new { Error = ex.Message });

                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        TelemetryHelper.Execute(TelemetryLevels.Warning, "mongodb-testcontainers-timeout");
        return false;
    }

    /// <summary>
    /// Drops all collections in a database.
    /// </summary>
    /// <param name="context">The MongoDB context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task CleanDatabaseAsync(
        Mvp24HoursContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var database = context.Database;
        var cursor = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collectionNames = await cursor.ToListAsync(cancellationToken);

        foreach (var collectionName in collectionNames)
        {
            // Skip system collections
            if (collectionName.StartsWith("system."))
                continue;

            await database.DropCollectionAsync(collectionName, cancellationToken);
        }

        TelemetryHelper.Execute(TelemetryLevels.Verbose,
            "mongodb-testcontainers-cleaned",
            new { DatabaseName = context.DatabaseName, CollectionsDropped = collectionNames.Count });
    }
}

/// <summary>
/// Connection information for a MongoDB Testcontainer.
/// </summary>
/// <remarks>
/// <para>
/// This record contains the connection details for a running MongoDB container.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var info = new MongoDbContainerInfo(
///     connectionString: "mongodb://localhost:27017",
///     host: "localhost",
///     port: 27017,
///     databaseName: "testdb"
/// );
/// </code>
/// </example>
public record MongoDbContainerInfo(
    string ConnectionString,
    string Host,
    int Port,
    string DatabaseName)
{
    /// <summary>
    /// Creates MongoDbOptions from this container info.
    /// </summary>
    /// <param name="configureOptions">Optional additional configuration.</param>
    /// <returns>Configured MongoDbOptions.</returns>
    public MongoDbOptions ToMongoDbOptions(Action<MongoDbOptions>? configureOptions = null)
    {
        var options = new MongoDbOptions
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName,
            EnableCommandLogging = true
        };

        configureOptions?.Invoke(options);
        return options;
    }

    /// <summary>
    /// Creates a MongoDB context from this container info.
    /// </summary>
    /// <param name="configureOptions">Optional additional configuration.</param>
    /// <returns>A new MongoDB context.</returns>
    public Mvp24HoursContext ToContext(Action<MongoDbOptions>? configureOptions = null)
    {
        var options = ToMongoDbOptions(configureOptions);
        return new Mvp24HoursContext(options);
    }
}

