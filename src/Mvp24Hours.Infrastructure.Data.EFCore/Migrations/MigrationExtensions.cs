//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering EF Core migration services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides easy registration of:
    /// <list type="bullet">
    /// <item><strong>MigrationService</strong> - Programmatic migration management</item>
    /// <item><strong>MigrationHostedService</strong> - Automatic migrations on startup</item>
    /// <item><strong>DataSeeders</strong> - Initial data seeding</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class MigrationExtensions
    {
        #region Migration Service

        /// <summary>
        /// Adds the migration service for programmatic migration management.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for migration options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursMigrationService&lt;AppDbContext&gt;(options =>
        /// {
        ///     options.MigrationTimeout = TimeSpan.FromMinutes(10);
        /// });
        /// 
        /// // Later in code
        /// var migrationService = serviceProvider.GetRequiredService&lt;IMigrationService&gt;();
        /// var result = await migrationService.MigrateAsync();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMigrationService<TContext>(
            this IServiceCollection services,
            Action<MigrationOptions>? configureOptions = null)
            where TContext : DbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationextensions-addmvp24hoursmigrationservice-start");

            var options = new MigrationOptions();
            configureOptions?.Invoke(options);

            services.Configure<MigrationOptions>(opt =>
            {
                opt.AutoMigrateOnStartup = options.AutoMigrateOnStartup;
                opt.ThrowOnPendingMigrations = options.ThrowOnPendingMigrations;
                opt.LogPendingMigrations = options.LogPendingMigrations;
                opt.MigrationTimeout = options.MigrationTimeout;
                opt.UseTransactions = options.UseTransactions;
                opt.EnsureDatabaseCreated = options.EnsureDatabaseCreated;
                opt.EnableDataSeeding = options.EnableDataSeeding;
                opt.SeedOnlyOnMigration = options.SeedOnlyOnMigration;
                opt.SeedInTransaction = options.SeedInTransaction;
                opt.ValidateSchemaBeforeMigration = options.ValidateSchemaBeforeMigration;
                opt.ValidateSchemaAfterMigration = options.ValidateSchemaAfterMigration;
                opt.MaxRetryAttempts = options.MaxRetryAttempts;
                opt.RetryDelay = options.RetryDelay;
                opt.UseExponentialBackoff = options.UseExponentialBackoff;
                opt.UseDistributedLock = options.UseDistributedLock;
                opt.LockName = options.LockName;
                opt.LockTimeout = options.LockTimeout;
                opt.LockDuration = options.LockDuration;
                opt.EnableDetailedLogging = options.EnableDetailedLogging;
                opt.LogMigrationSql = options.LogMigrationSql;
                opt.EnableTelemetry = options.EnableTelemetry;
            });

            services.AddScoped<IMigrationService, MigrationService<TContext>>();

            return services;
        }

        #endregion

        #region Auto Migration

        /// <summary>
        /// Adds automatic migration on application startup.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for migration options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This adds a hosted service that:
        /// <list type="bullet">
        /// <item>Checks for pending migrations on startup</item>
        /// <item>Applies migrations if AutoMigrateOnStartup is true</item>
        /// <item>Throws if ThrowOnPendingMigrations is true and there are pending migrations</item>
        /// <item>Runs data seeders if EnableDataSeeding is true</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Development - auto migrate
        /// services.AddMvp24HoursAutoMigration&lt;AppDbContext&gt;(MigrationOptions.Development());
        /// 
        /// // Production - throw on pending
        /// services.AddMvp24HoursAutoMigration&lt;AppDbContext&gt;(MigrationOptions.Production());
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMigration<TContext>(
            this IServiceCollection services,
            Action<MigrationOptions>? configureOptions = null)
            where TContext : DbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationextensions-addmvp24hoursautomigration-start");

            services.AddMvp24HoursMigrationService<TContext>(configureOptions);
            services.AddHostedService<MigrationHostedService<TContext>>();

            return services;
        }

        /// <summary>
        /// Adds automatic migration with predefined options.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Predefined migration options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMigration&lt;AppDbContext&gt;(MigrationOptions.Development());
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMigration<TContext>(
            this IServiceCollection services,
            MigrationOptions options)
            where TContext : DbContext
        {
            return services.AddMvp24HoursAutoMigration<TContext>(opt =>
            {
                opt.AutoMigrateOnStartup = options.AutoMigrateOnStartup;
                opt.ThrowOnPendingMigrations = options.ThrowOnPendingMigrations;
                opt.LogPendingMigrations = options.LogPendingMigrations;
                opt.MigrationTimeout = options.MigrationTimeout;
                opt.UseTransactions = options.UseTransactions;
                opt.EnsureDatabaseCreated = options.EnsureDatabaseCreated;
                opt.EnableDataSeeding = options.EnableDataSeeding;
                opt.SeedOnlyOnMigration = options.SeedOnlyOnMigration;
                opt.SeedInTransaction = options.SeedInTransaction;
                opt.ValidateSchemaBeforeMigration = options.ValidateSchemaBeforeMigration;
                opt.ValidateSchemaAfterMigration = options.ValidateSchemaAfterMigration;
                opt.MaxRetryAttempts = options.MaxRetryAttempts;
                opt.RetryDelay = options.RetryDelay;
                opt.UseExponentialBackoff = options.UseExponentialBackoff;
                opt.UseDistributedLock = options.UseDistributedLock;
                opt.LockName = options.LockName;
                opt.LockTimeout = options.LockTimeout;
                opt.LockDuration = options.LockDuration;
                opt.EnableDetailedLogging = options.EnableDetailedLogging;
                opt.LogMigrationSql = options.LogMigrationSql;
                opt.EnableTelemetry = options.EnableTelemetry;
            });
        }

        #endregion

        #region Data Seeders

        /// <summary>
        /// Adds a data seeder to be run after migrations.
        /// </summary>
        /// <typeparam name="TSeeder">The data seeder type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services
        ///     .AddMvp24HoursAutoMigration&lt;AppDbContext&gt;(options =>
        ///     {
        ///         options.AutoMigrateOnStartup = true;
        ///         options.EnableDataSeeding = true;
        ///     })
        ///     .AddMvp24HoursDataSeeder&lt;DefaultUserSeeder&gt;()
        ///     .AddMvp24HoursDataSeeder&lt;DefaultRolesSeeder&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDataSeeder<TSeeder>(this IServiceCollection services)
            where TSeeder : class, IDataSeeder
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationextensions-addmvp24hoursdataseeder-start");
            services.AddScoped<IDataSeeder, TSeeder>();
            return services;
        }

        /// <summary>
        /// Adds a data seeder with a factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory to create the data seeder.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursDataSeeder(
            this IServiceCollection services,
            Func<IServiceProvider, IDataSeeder> factory)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationextensions-addmvp24hoursdataseeder-factory-start");
            services.AddScoped(factory);
            return services;
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Adds migration service configured for development (auto-migrate, seed data).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursDevMigration<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursAutoMigration<TContext>(MigrationOptions.Development());
        }

        /// <summary>
        /// Adds migration service configured for staging (auto-migrate, validate schema).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursStagingMigration<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursAutoMigration<TContext>(MigrationOptions.Staging());
        }

        /// <summary>
        /// Adds migration service configured for production (no auto-migrate, throw on pending).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursProductionMigration<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursAutoMigration<TContext>(MigrationOptions.Production());
        }

        /// <summary>
        /// Adds migration service that only logs pending migrations (no action).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursMigrationLogOnly<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursAutoMigration<TContext>(MigrationOptions.LogOnly());
        }

        #endregion
    }
}

