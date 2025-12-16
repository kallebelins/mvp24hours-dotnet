//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks
{
    /// <summary>
    /// Health check implementation for MongoDB connectivity verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies MongoDB connectivity by:
    /// <list type="bullet">
    ///   <item>Attempting to connect to the MongoDB server</item>
    ///   <item>Executing a ping command to verify server responsiveness</item>
    ///   <item>Optionally listing databases to verify permissions</item>
    /// </list>
    /// </para>
    /// <para>
    /// Register using the health check builder:
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddMongoDbHealthCheck("mongodb");
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Startup.cs or Program.cs
    /// services.AddHealthChecks()
    ///     .AddMongoDbHealthCheck("mongodb", 
    ///         failureStatus: HealthStatus.Degraded,
    ///         tags: new[] { "database", "nosql" });
    ///         
    /// app.MapHealthChecks("/health/mongodb", new HealthCheckOptions
    /// {
    ///     Predicate = check => check.Tags.Contains("database")
    /// });
    /// </code>
    /// </example>
    public sealed class MongoDbHealthCheck : IHealthCheck
    {
        private readonly MongoDbOptions _options;
        private readonly MongoDbHealthCheckOptions _healthCheckOptions;
        private IMongoClient? _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class.
        /// </summary>
        /// <param name="options">The MongoDB configuration options.</param>
        /// <param name="healthCheckOptions">The health check specific options.</param>
        public MongoDbHealthCheck(
            IOptions<MongoDbOptions> options,
            IOptions<MongoDbHealthCheckOptions>? healthCheckOptions = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _healthCheckOptions = healthCheckOptions?.Value ?? new MongoDbHealthCheckOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class with explicit options.
        /// </summary>
        /// <param name="options">The MongoDB configuration options.</param>
        /// <param name="healthCheckOptions">The health check specific options.</param>
        public MongoDbHealthCheck(
            MongoDbOptions options,
            MongoDbHealthCheckOptions? healthCheckOptions = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _healthCheckOptions = healthCheckOptions ?? new MongoDbHealthCheckOptions();
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                ["database"] = _options.DatabaseName ?? "unknown",
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            try
            {
                var client = GetOrCreateClient();
                var database = client.GetDatabase(_options.DatabaseName);

                // Execute ping command to verify connectivity
                var pingCommand = new BsonDocument("ping", 1);
                var pingResult = await database.RunCommandAsync<BsonDocument>(
                    pingCommand,
                    readPreference: null,
                    cancellationToken);

                data["pingResult"] = pingResult?["ok"]?.AsDouble ?? 0;

                // Optionally verify database permissions by listing collections
                if (_healthCheckOptions.VerifyDatabaseAccess)
                {
                    var collections = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
                    var collectionCount = 0;
                    while (await collections.MoveNextAsync(cancellationToken))
                    {
                        collectionCount += collections.Current is ICollection<string> coll ? coll.Count : 0;
                    }
                    data["collectionsAccessible"] = true;
                    data["collectionCount"] = collectionCount;
                }

                // Optionally get server status for additional diagnostics
                if (_healthCheckOptions.IncludeServerStatus)
                {
                    try
                    {
                        var adminDb = client.GetDatabase("admin");
                        var serverStatus = await adminDb.RunCommandAsync<BsonDocument>(
                            new BsonDocument("serverStatus", 1),
                            readPreference: null,
                            cancellationToken);

                        if (serverStatus != null)
                        {
                            data["version"] = serverStatus.GetValue("version", "unknown").AsString;
                            data["uptime"] = serverStatus.GetValue("uptime", 0).ToDouble();
                            
                            // Get connections info
                            if (serverStatus.TryGetValue("connections", out var connections) && connections.IsBsonDocument)
                            {
                                var connDoc = connections.AsBsonDocument;
                                data["currentConnections"] = connDoc.GetValue("current", 0).ToInt32();
                                data["availableConnections"] = connDoc.GetValue("available", 0).ToInt32();
                            }
                        }
                    }
                    catch
                    {
                        // Server status requires admin privileges, so we ignore failures
                        data["serverStatusAvailable"] = false;
                    }
                }

                return HealthCheckResult.Healthy(
                    $"MongoDB connection to '{_options.DatabaseName}' is healthy.",
                    data);
            }
            catch (MongoAuthenticationException ex)
            {
                data["error"] = ex.Message;
                data["errorType"] = "AuthenticationError";

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    $"MongoDB authentication failed: {ex.Message}",
                    ex,
                    data);
            }
            catch (MongoConnectionException ex)
            {
                data["error"] = ex.Message;
                data["errorType"] = "ConnectionError";

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    $"MongoDB connection failed: {ex.Message}",
                    ex,
                    data);
            }
            catch (TimeoutException ex)
            {
                data["error"] = ex.Message;
                data["errorType"] = "TimeoutError";

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    $"MongoDB connection timed out: {ex.Message}",
                    ex,
                    data);
            }
            catch (Exception ex)
            {
                data["error"] = ex.Message;
                data["errorType"] = ex.GetType().Name;

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    $"MongoDB health check failed: {ex.Message}",
                    ex,
                    data);
            }
        }

        private IMongoClient GetOrCreateClient()
        {
            if (_client != null) return _client;

            var settings = MongoClientSettings.FromConnectionString(_options.ConnectionString);

            // Apply health check timeout settings
            if (_healthCheckOptions.ConnectionTimeoutSeconds > 0)
            {
                settings.ConnectTimeout = TimeSpan.FromSeconds(_healthCheckOptions.ConnectionTimeoutSeconds);
            }

            if (_healthCheckOptions.ServerSelectionTimeoutSeconds > 0)
            {
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(_healthCheckOptions.ServerSelectionTimeoutSeconds);
            }

            _client = new MongoClient(settings);
            return _client;
        }
    }

    /// <summary>
    /// Configuration options for MongoDB health checks.
    /// </summary>
    public sealed class MongoDbHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets whether to verify database access by listing collections.
        /// Default is false.
        /// </summary>
        public bool VerifyDatabaseAccess { get; set; }

        /// <summary>
        /// Gets or sets whether to include server status information.
        /// Requires admin privileges. Default is false.
        /// </summary>
        public bool IncludeServerStatus { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// Default is 5 seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the server selection timeout in seconds.
        /// Default is 5 seconds.
        /// </summary>
        public int ServerSelectionTimeoutSeconds { get; set; } = 5;
    }
}

