//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering MongoDB infrastructure services.
    /// </summary>
    public static class MongoDbInfrastructureExtensions
    {
        /// <summary>
        /// Adds MongoDB index verification service that runs on application startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure verification options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This service verifies that required indexes exist on MongoDB collections
        /// and optionally creates missing indexes.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///     .AddMongoDbIndexVerification(options =>
        ///     {
        ///         options.AssembliesToScan = new[] { typeof(Customer).Assembly };
        ///         options.CreateMissingIndexes = true;
        ///         options.FailOnMissingIndexes = false;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbIndexVerification(
            this IServiceCollection services,
            Action<MongoDbIndexVerificationOptions>? configure = null)
        {
            var options = new MongoDbIndexVerificationOptions();
            configure?.Invoke(options);

            services.Configure<MongoDbIndexVerificationOptions>(opt =>
            {
                opt.Enabled = options.Enabled;
                opt.AssembliesToScan = options.AssembliesToScan;
                opt.CreateMissingIndexes = options.CreateMissingIndexes;
                opt.FailOnMissingIndexes = options.FailOnMissingIndexes;
                opt.FailOnVerificationError = options.FailOnVerificationError;
                opt.StartupDelaySeconds = options.StartupDelaySeconds;
            });

            // Register index manager if not already registered
            services.TryAddSingleton<IMongoDbIndexManager, MongoDbIndexManager>();

            // Register the hosted service
            services.AddHostedService<MongoDbIndexVerificationService>();

            return services;
        }

        /// <summary>
        /// Adds MongoDB migration support with optional auto-migration on startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure migration options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This service provides schema versioning for MongoDB databases:
        /// <list type="bullet">
        ///   <item>Discovers migrations from configured assemblies</item>
        ///   <item>Tracks applied migrations in a "_migrations" collection</item>
        ///   <item>Optionally auto-migrates on startup</item>
        ///   <item>Supports rollback operations</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///     .AddMongoDbMigrations(options =>
        ///     {
        ///         options.MigrationAssemblies = new[] { typeof(MyMigration).Assembly };
        ///         options.AutoMigrateOnStartup = true;
        ///         options.FailOnMigrationError = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbMigrations(
            this IServiceCollection services,
            Action<MongoDbMigrationOptions>? configure = null)
        {
            var options = new MongoDbMigrationOptions();
            configure?.Invoke(options);

            services.Configure<MongoDbMigrationOptions>(opt =>
            {
                opt.MigrationAssemblies = options.MigrationAssemblies;
                opt.AutoMigrateOnStartup = options.AutoMigrateOnStartup;
                opt.AppliedBy = options.AppliedBy;
                opt.FailOnMigrationError = options.FailOnMigrationError;
                opt.StartupDelaySeconds = options.StartupDelaySeconds;
            });

            // Register the migration runner
            services.AddScoped<IMongoDbMigrationRunner, MongoDbMigrationRunner>();

            // Register the hosted service if auto-migrate is enabled
            if (options.AutoMigrateOnStartup)
            {
                services.AddHostedService<MongoDbMigrationHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds all MongoDB infrastructure services (index verification, migrations, and index manager).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureIndexVerification">Action to configure index verification options.</param>
        /// <param name="configureMigrations">Action to configure migration options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///     .AddMongoDbInfrastructure(
        ///         indexOpts =>
        ///         {
        ///             indexOpts.AssembliesToScan = new[] { typeof(Customer).Assembly };
        ///             indexOpts.CreateMissingIndexes = true;
        ///         },
        ///         migrationOpts =>
        ///         {
        ///             migrationOpts.MigrationAssemblies = new[] { typeof(MyMigration).Assembly };
        ///             migrationOpts.AutoMigrateOnStartup = true;
        ///         });
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbInfrastructure(
            this IServiceCollection services,
            Action<MongoDbIndexVerificationOptions>? configureIndexVerification = null,
            Action<MongoDbMigrationOptions>? configureMigrations = null)
        {
            // Register index manager (used by both index verification and general index operations)
            services.TryAddSingleton<IMongoDbIndexManager, MongoDbIndexManager>();

            if (configureIndexVerification != null)
            {
                services.AddMongoDbIndexVerification(configureIndexVerification);
            }

            if (configureMigrations != null)
            {
                services.AddMongoDbMigrations(configureMigrations);
            }

            return services;
        }

        /// <summary>
        /// Adds the MongoDB index manager for programmatic index management.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMongoDbIndexManager();
        /// 
        /// // Later, use in code:
        /// var indexManager = serviceProvider.GetRequiredService&lt;IMongoDbIndexManager&gt;();
        /// await indexManager.EnsureIndexesAsync&lt;Customer&gt;(collection);
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbIndexManager(this IServiceCollection services)
        {
            services.TryAddSingleton<IMongoDbIndexManager, MongoDbIndexManager>();
            return services;
        }
    }
}

