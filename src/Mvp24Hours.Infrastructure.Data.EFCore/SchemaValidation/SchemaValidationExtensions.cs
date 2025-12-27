//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering schema validation services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Schema validation helps ensure database consistency:
    /// <list type="bullet">
    /// <item>Detect schema drift between EF Core model and database</item>
    /// <item>Check for pending migrations</item>
    /// <item>Validate table and column existence</item>
    /// <item>Fail fast on critical issues</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class SchemaValidationExtensions
    {
        #region Schema Validator

        /// <summary>
        /// Adds schema validation service for programmatic validation.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for validation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursSchemaValidator&lt;AppDbContext&gt;(options =>
        /// {
        ///     options.ValidateTables = true;
        ///     options.ValidateColumns = true;
        /// });
        /// 
        /// // Later in code
        /// var validator = serviceProvider.GetRequiredService&lt;ISchemaValidator&gt;();
        /// var result = await validator.ValidateAsync();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSchemaValidator<TContext>(
            this IServiceCollection services,
            Action<SchemaValidationOptions>? configureOptions = null)
            where TContext : DbContext
        {
            var options = new SchemaValidationOptions();
            configureOptions?.Invoke(options);

            services.Configure<SchemaValidationOptions>(opt =>
            {
                opt.ValidateOnStartup = options.ValidateOnStartup;
                opt.ThrowOnValidationFailure = options.ThrowOnValidationFailure;
                opt.CheckPendingMigrations = options.CheckPendingMigrations;
                opt.ValidateTables = options.ValidateTables;
                opt.ValidateColumns = options.ValidateColumns;
                opt.ValidateIndexes = options.ValidateIndexes;
                opt.ValidateForeignKeys = options.ValidateForeignKeys;
                opt.ValidationTimeout = options.ValidationTimeout;
                opt.ExcludedTables = options.ExcludedTables;
                opt.EnableDetailedLogging = options.EnableDetailedLogging;
                opt.CacheValidationResults = options.CacheValidationResults;
                opt.CacheDuration = options.CacheDuration;
            });

            services.AddScoped<ISchemaValidator, SchemaValidator<TContext>>();

            return services;
        }

        #endregion

        #region Schema Validation on Startup

        /// <summary>
        /// Adds schema validation on application startup.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for validation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This adds a hosted service that:
        /// <list type="bullet">
        /// <item>Validates schema on application startup</item>
        /// <item>Logs any schema issues found</item>
        /// <item>Optionally throws on critical issues</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Development - strict validation, throw on errors
        /// services.AddMvp24HoursSchemaValidationOnStartup&lt;AppDbContext&gt;(
        ///     SchemaValidationOptions.Development());
        /// 
        /// // Production - just log warnings
        /// services.AddMvp24HoursSchemaValidationOnStartup&lt;AppDbContext&gt;(
        ///     SchemaValidationOptions.Production());
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSchemaValidationOnStartup<TContext>(
            this IServiceCollection services,
            Action<SchemaValidationOptions>? configureOptions = null)
            where TContext : DbContext
        {
            // Ensure ValidateOnStartup is true
            services.AddMvp24HoursSchemaValidator<TContext>(options =>
            {
                options.ValidateOnStartup = true;
                configureOptions?.Invoke(options);
            });

            services.AddHostedService<SchemaValidationHostedService<TContext>>();

            return services;
        }

        /// <summary>
        /// Adds schema validation on startup with predefined options.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Predefined validation options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursSchemaValidationOnStartup<TContext>(
            this IServiceCollection services,
            SchemaValidationOptions options)
            where TContext : DbContext
        {
            options.ValidateOnStartup = true;
            return services.AddMvp24HoursSchemaValidationOnStartup<TContext>(opt =>
            {
                opt.ThrowOnValidationFailure = options.ThrowOnValidationFailure;
                opt.CheckPendingMigrations = options.CheckPendingMigrations;
                opt.ValidateTables = options.ValidateTables;
                opt.ValidateColumns = options.ValidateColumns;
                opt.ValidateIndexes = options.ValidateIndexes;
                opt.ValidateForeignKeys = options.ValidateForeignKeys;
                opt.ValidationTimeout = options.ValidationTimeout;
                opt.ExcludedTables = options.ExcludedTables;
                opt.EnableDetailedLogging = options.EnableDetailedLogging;
                opt.CacheValidationResults = options.CacheValidationResults;
                opt.CacheDuration = options.CacheDuration;
            });
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Adds schema validation configured for development (strict, throws on errors).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursDevSchemaValidation<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursSchemaValidationOnStartup<TContext>(SchemaValidationOptions.Development());
        }

        /// <summary>
        /// Adds schema validation configured for staging.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursStagingSchemaValidation<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursSchemaValidationOnStartup<TContext>(SchemaValidationOptions.Staging());
        }

        /// <summary>
        /// Adds schema validation configured for production (minimal, logs only).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursProductionSchemaValidation<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursSchemaValidationOnStartup<TContext>(SchemaValidationOptions.Production());
        }

        /// <summary>
        /// Adds schema validation configured for CI/CD pipelines (comprehensive).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursCISchemaValidation<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddMvp24HoursSchemaValidationOnStartup<TContext>(SchemaValidationOptions.ContinuousIntegration());
        }

        #endregion
    }
}

