//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for adding MongoDB health checks to the health check builder.
    /// </summary>
    public static class MongoDbHealthCheckExtensions
    {
        private const string DefaultMongoDbName = "mongodb";
        private const string DefaultReplicaSetName = "mongodb-replicaset";

        /// <summary>
        /// Adds a MongoDB connectivity health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The health check name. Default is "mongodb".</param>
        /// <param name="configureOptions">Action to configure health check options.</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This health check uses the MongoDbOptions registered in DI.
        /// Make sure to call AddMvp24HoursDbContext before adding this health check.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "mydb";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// });
        /// 
        /// services.AddHealthChecks()
        ///     .AddMongoDbHealthCheck(
        ///         name: "mongodb-primary",
        ///         configureOptions: opts =>
        ///         {
        ///             opts.VerifyDatabaseAccess = true;
        ///             opts.IncludeServerStatus = true;
        ///         },
        ///         tags: new[] { "database", "nosql" });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMongoDbHealthCheck(
            this IHealthChecksBuilder builder,
            string name = DefaultMongoDbName,
            Action<MongoDbHealthCheckOptions>? configureOptions = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new MongoDbHealthCheckOptions();
            configureOptions?.Invoke(options);

            builder.Services.Configure<MongoDbHealthCheckOptions>(opt =>
            {
                opt.VerifyDatabaseAccess = options.VerifyDatabaseAccess;
                opt.IncludeServerStatus = options.IncludeServerStatus;
                opt.ConnectionTimeoutSeconds = options.ConnectionTimeoutSeconds;
                opt.ServerSelectionTimeoutSeconds = options.ServerSelectionTimeoutSeconds;
            });

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var mongoOptions = sp.GetRequiredService<IOptions<MongoDbOptions>>();
                    var healthCheckOptions = sp.GetService<IOptions<MongoDbHealthCheckOptions>>();
                    return new MongoDbHealthCheck(mongoOptions, healthCheckOptions);
                },
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Adds a MongoDB connectivity health check with explicit connection options.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">The MongoDB connection string.</param>
        /// <param name="databaseName">The database name to connect to.</param>
        /// <param name="name">The health check name. Default is "mongodb".</param>
        /// <param name="configureOptions">Action to configure health check options.</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMongoDbHealthCheck(
        ///         connectionString: "mongodb://localhost:27017",
        ///         databaseName: "mydb",
        ///         name: "mongodb",
        ///         tags: new[] { "database" });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMongoDbHealthCheck(
            this IHealthChecksBuilder builder,
            string connectionString,
            string databaseName,
            string name = DefaultMongoDbName,
            Action<MongoDbHealthCheckOptions>? configureOptions = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var mongoOptions = new MongoDbOptions
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            };

            var healthCheckOptions = new MongoDbHealthCheckOptions();
            configureOptions?.Invoke(healthCheckOptions);

            return builder.Add(new HealthCheckRegistration(
                name,
                _ => new MongoDbHealthCheck(mongoOptions, healthCheckOptions),
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Adds a MongoDB replica set health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The health check name. Default is "mongodb-replicaset".</param>
        /// <param name="configureOptions">Action to configure replica set health check options.</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This health check monitors replica set health including primary availability,
        /// secondary node count, and replication lag.
        /// </para>
        /// <para>
        /// Requires connection to a MongoDB replica set. Will fail if connected to a standalone instance
        /// unless AllowStandaloneMode is set to true.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMongoDbReplicaSetHealthCheck(
        ///         name: "mongodb-cluster",
        ///         configureOptions: opts =>
        ///         {
        ///             opts.MinSecondaryNodes = 1;
        ///             opts.MaxReplicationLagSeconds = 30;
        ///             opts.AllowUnhealthyMembers = false;
        ///         },
        ///         failureStatus: HealthStatus.Degraded,
        ///         tags: new[] { "database", "cluster" });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMongoDbReplicaSetHealthCheck(
            this IHealthChecksBuilder builder,
            string name = DefaultReplicaSetName,
            Action<MongoDbReplicaSetHealthCheckOptions>? configureOptions = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new MongoDbReplicaSetHealthCheckOptions();
            configureOptions?.Invoke(options);

            builder.Services.Configure<MongoDbReplicaSetHealthCheckOptions>(opt =>
            {
                opt.MinSecondaryNodes = options.MinSecondaryNodes;
                opt.MaxReplicationLagSeconds = options.MaxReplicationLagSeconds;
                opt.AllowUnhealthyMembers = options.AllowUnhealthyMembers;
                opt.AllowStandaloneMode = options.AllowStandaloneMode;
                opt.IncludeMemberDetails = options.IncludeMemberDetails;
                opt.ConnectionTimeoutSeconds = options.ConnectionTimeoutSeconds;
                opt.ServerSelectionTimeoutSeconds = options.ServerSelectionTimeoutSeconds;
            });

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var mongoOptions = sp.GetRequiredService<IOptions<MongoDbOptions>>();
                    var healthCheckOptions = sp.GetService<IOptions<MongoDbReplicaSetHealthCheckOptions>>();
                    return new MongoDbReplicaSetHealthCheck(mongoOptions, healthCheckOptions);
                },
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Adds a MongoDB replica set health check with explicit connection options.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">The MongoDB connection string.</param>
        /// <param name="name">The health check name. Default is "mongodb-replicaset".</param>
        /// <param name="configureOptions">Action to configure replica set health check options.</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMongoDbReplicaSetHealthCheck(
            this IHealthChecksBuilder builder,
            string connectionString,
            string name = DefaultReplicaSetName,
            Action<MongoDbReplicaSetHealthCheckOptions>? configureOptions = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var mongoOptions = new MongoDbOptions
            {
                ConnectionString = connectionString,
                DatabaseName = "admin" // Replica set commands use admin database
            };

            var healthCheckOptions = new MongoDbReplicaSetHealthCheckOptions();
            configureOptions?.Invoke(healthCheckOptions);

            return builder.Add(new HealthCheckRegistration(
                name,
                _ => new MongoDbReplicaSetHealthCheck(mongoOptions, healthCheckOptions),
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Adds both MongoDB connectivity and replica set health checks.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectivityName">The connectivity health check name.</param>
        /// <param name="replicaSetName">The replica set health check name.</param>
        /// <param name="configureConnectivity">Action to configure connectivity health check options.</param>
        /// <param name="configureReplicaSet">Action to configure replica set health check options.</param>
        /// <param name="failureStatus">The failure status for both health checks.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMongoDbHealthChecks(
        ///         connectivityName: "mongodb",
        ///         replicaSetName: "mongodb-cluster",
        ///         configureConnectivity: opts => opts.IncludeServerStatus = true,
        ///         configureReplicaSet: opts => opts.MinSecondaryNodes = 1,
        ///         tags: new[] { "database" });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMongoDbHealthChecks(
            this IHealthChecksBuilder builder,
            string connectivityName = DefaultMongoDbName,
            string replicaSetName = DefaultReplicaSetName,
            Action<MongoDbHealthCheckOptions>? configureConnectivity = null,
            Action<MongoDbReplicaSetHealthCheckOptions>? configureReplicaSet = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null)
        {
            return builder
                .AddMongoDbHealthCheck(connectivityName, configureConnectivity, failureStatus, tags)
                .AddMongoDbReplicaSetHealthCheck(replicaSetName, configureReplicaSet, failureStatus, tags);
        }
    }
}

