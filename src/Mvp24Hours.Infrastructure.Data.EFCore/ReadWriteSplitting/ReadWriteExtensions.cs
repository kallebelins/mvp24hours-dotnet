//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering read/write splitting services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read/write splitting allows:
    /// <list type="bullet">
    /// <item>Distributing read load across multiple replicas</item>
    /// <item>Improving read performance with dedicated read replicas</item>
    /// <item>Supporting geo-distributed read replicas</item>
    /// <item>Automatic failover between replicas</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class ReadWriteExtensions
    {
        #region Read/Write Splitting

        /// <summary>
        /// Adds read/write splitting support with the specified options.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration for read/write options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursReadWriteSplitting&lt;AppDbContext&gt;(options =>
        /// {
        ///     options.PrimaryConnectionString = "Server=primary;Database=MyDb;...";
        ///     options.ReplicaConnectionStrings = new[]
        ///     {
        ///         "Server=replica1;Database=MyDb;...",
        ///         "Server=replica2;Database=MyDb;..."
        ///     };
        ///     options.LoadBalancing = ReplicaLoadBalancing.RoundRobin;
        ///     options.EnableReadAfterWriteConsistency = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadWriteSplitting<TContext>(
            this IServiceCollection services,
            Action<ReadWriteOptions> configureOptions)
            where TContext : DbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "readwriteextensions-addmvp24hoursreadwritesplitting-start");

            var options = new ReadWriteOptions();
            configureOptions(options);

            services.Configure<ReadWriteOptions>(opt =>
            {
                opt.PrimaryConnectionString = options.PrimaryConnectionString;
                opt.ReplicaConnectionStrings = options.ReplicaConnectionStrings;
                opt.FallbackToPrimaryOnReplicaFailure = options.FallbackToPrimaryOnReplicaFailure;
                opt.LoadBalancing = options.LoadBalancing;
                opt.ReplicaWeights = options.ReplicaWeights;
                opt.EnableReplicaHealthChecks = options.EnableReplicaHealthChecks;
                opt.HealthCheckInterval = options.HealthCheckInterval;
                opt.HealthCheckTimeout = options.HealthCheckTimeout;
                opt.FailureThreshold = options.FailureThreshold;
                opt.RecoveryTimeout = options.RecoveryTimeout;
                opt.EnableReadAfterWriteConsistency = options.EnableReadAfterWriteConsistency;
                opt.ReadAfterWriteWindow = options.ReadAfterWriteWindow;
                opt.AutoDetectOperationType = options.AutoDetectOperationType;
                opt.LogReplicaSelection = options.LogReplicaSelection;
                opt.MaxReplicaRetries = options.MaxReplicaRetries;
                opt.RetryOnDifferentReplica = options.RetryOnDifferentReplica;
            });

            // Register core services
            services.AddSingleton<IReplicaSelector, ReplicaSelector>();
            services.AddScoped<IConnectionResolver, ConnectionResolver>();

            // Register DbContext with dynamic connection
            services.AddDbContext<TContext>((sp, dbOptions) =>
            {
                // By default, use primary (write) connection
                // Read operations can override this using IConnectionResolver
                dbOptions.UseSqlServer(options.PrimaryConnectionString);
            });

            return services;
        }

        /// <summary>
        /// Adds read/write splitting with predefined options.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Predefined read/write options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursReadWriteSplitting<TContext>(
            this IServiceCollection services,
            ReadWriteOptions options)
            where TContext : DbContext
        {
            return services.AddMvp24HoursReadWriteSplitting<TContext>(opt =>
            {
                opt.PrimaryConnectionString = options.PrimaryConnectionString;
                opt.ReplicaConnectionStrings = options.ReplicaConnectionStrings;
                opt.FallbackToPrimaryOnReplicaFailure = options.FallbackToPrimaryOnReplicaFailure;
                opt.LoadBalancing = options.LoadBalancing;
                opt.ReplicaWeights = options.ReplicaWeights;
                opt.EnableReplicaHealthChecks = options.EnableReplicaHealthChecks;
                opt.HealthCheckInterval = options.HealthCheckInterval;
                opt.HealthCheckTimeout = options.HealthCheckTimeout;
                opt.FailureThreshold = options.FailureThreshold;
                opt.RecoveryTimeout = options.RecoveryTimeout;
                opt.EnableReadAfterWriteConsistency = options.EnableReadAfterWriteConsistency;
                opt.ReadAfterWriteWindow = options.ReadAfterWriteWindow;
                opt.AutoDetectOperationType = options.AutoDetectOperationType;
                opt.LogReplicaSelection = options.LogReplicaSelection;
                opt.MaxReplicaRetries = options.MaxReplicaRetries;
                opt.RetryOnDifferentReplica = options.RetryOnDifferentReplica;
            });
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Adds a simple read/write splitting setup with one primary and one replica.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="primaryConnectionString">Primary (write) connection string.</param>
        /// <param name="replicaConnectionString">Replica (read) connection string.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursSimpleReadWriteSplitting<TContext>(
            this IServiceCollection services,
            string primaryConnectionString,
            string replicaConnectionString)
            where TContext : DbContext
        {
            return services.AddMvp24HoursReadWriteSplitting<TContext>(
                ReadWriteOptions.SimpleSetup(primaryConnectionString, replicaConnectionString));
        }

        /// <summary>
        /// Adds read/write splitting optimized for Azure SQL geo-replicated databases.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="primaryConnectionString">Primary (write) connection string.</param>
        /// <param name="replicaConnectionStrings">Geo-replica connection strings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursAzureSqlGeoReplica<TContext>(
            this IServiceCollection services,
            string primaryConnectionString,
            params string[] replicaConnectionStrings)
            where TContext : DbContext
        {
            return services.AddMvp24HoursReadWriteSplitting<TContext>(
                ReadWriteOptions.AzureSqlGeoReplica(primaryConnectionString, replicaConnectionStrings));
        }

        /// <summary>
        /// Adds read/write splitting optimized for PostgreSQL streaming replication.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="primaryConnectionString">Primary (write) connection string.</param>
        /// <param name="replicaConnectionStrings">Streaming replica connection strings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursPostgreSqlStreaming<TContext>(
            this IServiceCollection services,
            string primaryConnectionString,
            params string[] replicaConnectionStrings)
            where TContext : DbContext
        {
            return services.AddMvp24HoursReadWriteSplitting<TContext>(
                ReadWriteOptions.PostgreSqlStreaming(primaryConnectionString, replicaConnectionStrings));
        }

        #endregion
    }
}

