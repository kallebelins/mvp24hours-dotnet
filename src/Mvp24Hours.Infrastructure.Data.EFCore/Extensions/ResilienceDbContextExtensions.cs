//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using System;
using System.Data.SqlClient;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring EF Core DbContext with resilience features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides methods to configure:
    /// <list type="bullet">
    /// <item>Connection retry on transient failures</item>
    /// <item>DbContext pooling for better performance</item>
    /// <item>Command timeouts per operation type</item>
    /// <item>Circuit breaker pattern</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class ResilienceDbContextExtensions
    {
        #region DbContext with Resilience (SQL Server)

        /// <summary>
        /// Adds a resilient DbContext configured with SQL Server and retry policies.
        /// </summary>
        /// <typeparam name="TDbContext">The type of DbContext to configure.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configureResilience">Optional resilience configuration.</param>
        /// <param name="configureDbContext">Optional additional DbContext configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method configures:
        /// <list type="bullet">
        /// <item>SQL Server provider with connection retry</item>
        /// <item>Custom execution strategy with exponential backoff</item>
        /// <item>Optional DbContext pooling</item>
        /// <item>Command timeout settings</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursDbContextWithResilience&lt;AppDbContext&gt;(
        ///     Configuration.GetConnectionString("DefaultConnection"));
        /// 
        /// // With custom configuration
        /// services.AddMvp24HoursDbContextWithResilience&lt;AppDbContext&gt;(
        ///     Configuration.GetConnectionString("DefaultConnection"),
        ///     resilience =>
        ///     {
        ///         resilience.MaxRetryCount = 10;
        ///         resilience.MaxRetryDelaySeconds = 60;
        ///         resilience.EnableDbContextPooling = true;
        ///         resilience.PoolSize = 512;
        ///     },
        ///     options =>
        ///     {
        ///         options.EnableSensitiveDataLogging();
        ///         options.AddInterceptors(new AuditSaveChangesInterceptor());
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDbContextWithResilience<TDbContext>(
            this IServiceCollection services,
            string connectionString,
            Action<EFCoreResilienceOptions>? configureResilience = null,
            Action<DbContextOptionsBuilder>? configureDbContext = null)
            where TDbContext : DbContext
        {
            var resilienceOptions = new EFCoreResilienceOptions();
            configureResilience?.Invoke(resilienceOptions);

            // Register resilience options
            services.Configure<EFCoreResilienceOptions>(opt =>
            {
                opt.EnableRetryOnFailure = resilienceOptions.EnableRetryOnFailure;
                opt.MaxRetryCount = resilienceOptions.MaxRetryCount;
                opt.MaxRetryDelaySeconds = resilienceOptions.MaxRetryDelaySeconds;
                opt.CommandTimeoutSeconds = resilienceOptions.CommandTimeoutSeconds;
                opt.EnableDbContextPooling = resilienceOptions.EnableDbContextPooling;
                opt.PoolSize = resilienceOptions.PoolSize;
                opt.EnableCircuitBreaker = resilienceOptions.EnableCircuitBreaker;
                opt.LogRetryAttempts = resilienceOptions.LogRetryAttempts;
            });

            if (resilienceOptions.EnableDbContextPooling)
            {
                services.AddDbContextPool<TDbContext>((sp, options) =>
                {
                    ConfigureSqlServerWithResilience(options, connectionString, resilienceOptions, sp);
                    configureDbContext?.Invoke(options);
                }, resilienceOptions.PoolSize);

                // Also register as DbContext for compatibility
                services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
            }
            else
            {
                services.AddDbContext<TDbContext>((sp, options) =>
                {
                    ConfigureSqlServerWithResilience(options, connectionString, resilienceOptions, sp);
                    configureDbContext?.Invoke(options);
                });

                // Also register as DbContext for compatibility
                services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
            }

            return services;
        }

        /// <summary>
        /// Adds a resilient DbContext for Azure SQL with recommended settings.
        /// </summary>
        /// <typeparam name="TDbContext">The type of DbContext to configure.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The Azure SQL connection string.</param>
        /// <param name="configureDbContext">Optional additional DbContext configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Uses Azure SQL-optimized settings:
        /// <list type="bullet">
        /// <item>Higher retry count (10 vs 6)</item>
        /// <item>Longer max delay (60s)</item>
        /// <item>Azure-specific transient error codes</item>
        /// <item>Circuit breaker enabled</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAzureSqlDbContext&lt;AppDbContext&gt;(
        ///     Configuration.GetConnectionString("AzureConnection"));
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAzureSqlDbContext<TDbContext>(
            this IServiceCollection services,
            string connectionString,
            Action<DbContextOptionsBuilder>? configureDbContext = null)
            where TDbContext : DbContext
        {
            var azureOptions = EFCoreResilienceOptions.AzureSql();
            return services.AddMvp24HoursDbContextWithResilience<TDbContext>(
                connectionString,
                options =>
                {
                    options.EnableRetryOnFailure = azureOptions.EnableRetryOnFailure;
                    options.MaxRetryCount = azureOptions.MaxRetryCount;
                    options.MaxRetryDelaySeconds = azureOptions.MaxRetryDelaySeconds;
                    options.CommandTimeoutSeconds = azureOptions.CommandTimeoutSeconds;
                    options.EnableDbContextPooling = azureOptions.EnableDbContextPooling;
                    options.PoolSize = azureOptions.PoolSize;
                    options.EnableCircuitBreaker = azureOptions.EnableCircuitBreaker;
                    options.CircuitBreakerFailureThreshold = azureOptions.CircuitBreakerFailureThreshold;
                    options.CircuitBreakerDurationSeconds = azureOptions.CircuitBreakerDurationSeconds;
                    foreach (var errorNumber in azureOptions.AdditionalTransientErrorNumbers)
                    {
                        options.AdditionalTransientErrorNumbers.Add(errorNumber);
                    }
                },
                configureDbContext);
        }

        /// <summary>
        /// Adds a DbContext for development with detailed logging and no pooling.
        /// </summary>
        /// <typeparam name="TDbContext">The type of DbContext to configure.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configureDbContext">Optional additional DbContext configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// if (env.IsDevelopment())
        /// {
        ///     services.AddMvp24HoursDevDbContext&lt;AppDbContext&gt;(connectionString);
        /// }
        /// else
        /// {
        ///     services.AddMvp24HoursDbContextWithResilience&lt;AppDbContext&gt;(connectionString);
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDevDbContext<TDbContext>(
            this IServiceCollection services,
            string connectionString,
            Action<DbContextOptionsBuilder>? configureDbContext = null)
            where TDbContext : DbContext
        {
            var devOptions = EFCoreResilienceOptions.Development();
            return services.AddMvp24HoursDbContextWithResilience<TDbContext>(
                connectionString,
                options =>
                {
                    options.EnableRetryOnFailure = devOptions.EnableRetryOnFailure;
                    options.MaxRetryCount = devOptions.MaxRetryCount;
                    options.MaxRetryDelaySeconds = devOptions.MaxRetryDelaySeconds;
                    options.CommandTimeoutSeconds = devOptions.CommandTimeoutSeconds;
                    options.EnableDbContextPooling = devOptions.EnableDbContextPooling;
                    options.LogRetryAttempts = devOptions.LogRetryAttempts;
                    options.LogPoolStatistics = devOptions.LogPoolStatistics;
                },
                options =>
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    configureDbContext?.Invoke(options);
                });
        }

        #endregion

        #region DbContextOptionsBuilder Extensions

        /// <summary>
        /// Configures the DbContext with resilience features.
        /// </summary>
        /// <param name="options">The DbContext options builder.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="resilienceOptions">The resilience configuration options.</param>
        /// <param name="serviceProvider">Optional service provider for dependency resolution.</param>
        /// <returns>The options builder for chaining.</returns>
        /// <example>
        /// <code>
        /// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        /// {
        ///     optionsBuilder.UseSqlServerWithResilience(
        ///         connectionString,
        ///         new EFCoreResilienceOptions { MaxRetryCount = 10 });
        /// }
        /// </code>
        /// </example>
        public static DbContextOptionsBuilder UseSqlServerWithResilience(
            this DbContextOptionsBuilder options,
            string connectionString,
            EFCoreResilienceOptions resilienceOptions,
            IServiceProvider? serviceProvider = null)
        {
            ConfigureSqlServerWithResilience(options, connectionString, resilienceOptions, serviceProvider);
            return options;
        }

        /// <summary>
        /// Sets the command timeout on the DbContext.
        /// </summary>
        /// <param name="options">The DbContext options builder.</param>
        /// <param name="timeoutSeconds">The command timeout in seconds.</param>
        /// <returns>The options builder for chaining.</returns>
        /// <example>
        /// <code>
        /// optionsBuilder.WithCommandTimeout(60);
        /// </code>
        /// </example>
        public static DbContextOptionsBuilder WithCommandTimeout(
            this DbContextOptionsBuilder options,
            int timeoutSeconds)
        {
            // Command timeout is set at the database level
            // This requires the options to already be configured with a provider
            var extension = options.Options.FindExtension<RelationalOptionsExtension>();
            if (extension != null)
            {
                var newExtension = extension.WithCommandTimeout(timeoutSeconds);
                ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(newExtension);
            }

            return options;
        }

        #endregion

        #region DbContext Extensions

        /// <summary>
        /// Sets the command timeout for the current DbContext instance.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="timeoutSeconds">The command timeout in seconds.</param>
        /// <returns>The DbContext for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This affects all subsequent commands on this DbContext instance.
        /// Use for long-running operations like bulk inserts or complex reports.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Increase timeout for a bulk operation
        /// await using (dbContext.WithTimeout(120))
        /// {
        ///     await dbContext.BulkInsertAsync(largeDataSet);
        /// }
        /// </code>
        /// </example>
        public static DbContext WithTimeout(this DbContext context, int timeoutSeconds)
        {
            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(timeoutSeconds));
            return context;
        }

        /// <summary>
        /// Creates a scope with a specific command timeout that resets after dispose.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="timeoutSeconds">The command timeout in seconds.</param>
        /// <returns>A disposable scope that resets the timeout on dispose.</returns>
        /// <example>
        /// <code>
        /// using (context.CreateTimeoutScope(120))
        /// {
        ///     await context.Database.ExecuteSqlRawAsync("EXEC LongRunningProcedure");
        /// }
        /// // Timeout is reset to original value here
        /// </code>
        /// </example>
        public static IDisposable CreateTimeoutScope(this DbContext context, int timeoutSeconds)
        {
            return new CommandTimeoutScope(context, timeoutSeconds);
        }

        #endregion

        #region Circuit Breaker and Pool Monitor

        /// <summary>
        /// Adds the DbContext circuit breaker for resilience.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureResilience">Optional resilience configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The circuit breaker temporarily blocks database requests after consecutive failures,
        /// allowing the database time to recover and preventing cascade failures.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContextCircuitBreaker(options =>
        /// {
        ///     options.EnableCircuitBreaker = true;
        ///     options.CircuitBreakerFailureThreshold = 5;
        ///     options.CircuitBreakerDurationSeconds = 30;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDbContextCircuitBreaker(
            this IServiceCollection services,
            Action<EFCoreResilienceOptions>? configureResilience = null)
        {
            var resilienceOptions = new EFCoreResilienceOptions();
            configureResilience?.Invoke(resilienceOptions);

            services.Configure<EFCoreResilienceOptions>(opt =>
            {
                opt.EnableCircuitBreaker = resilienceOptions.EnableCircuitBreaker;
                opt.CircuitBreakerFailureThreshold = resilienceOptions.CircuitBreakerFailureThreshold;
                opt.CircuitBreakerDurationSeconds = resilienceOptions.CircuitBreakerDurationSeconds;
            });

            services.AddSingleton(sp =>
            {
                var options = sp.GetService<IOptions<EFCoreResilienceOptions>>()?.Value 
                    ?? new EFCoreResilienceOptions();
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<DbContextCircuitBreaker>();
                return new DbContextCircuitBreaker(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds the DbContext pool monitor for diagnostics.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureResilience">Optional resilience configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The pool monitor logs statistics about DbContext pool usage:
        /// <list type="bullet">
        /// <item>Pool hit/miss ratio</item>
        /// <item>Active contexts</item>
        /// <item>Average checkout time</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContextPoolMonitor(options =>
        /// {
        ///     options.LogPoolStatistics = true;
        ///     options.PoolStatisticsLogIntervalSeconds = 60;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDbContextPoolMonitor(
            this IServiceCollection services,
            Action<EFCoreResilienceOptions>? configureResilience = null)
        {
            var resilienceOptions = new EFCoreResilienceOptions();
            configureResilience?.Invoke(resilienceOptions);

            services.Configure<EFCoreResilienceOptions>(opt =>
            {
                opt.LogPoolStatistics = resilienceOptions.LogPoolStatistics;
                opt.PoolStatisticsLogIntervalSeconds = resilienceOptions.PoolStatisticsLogIntervalSeconds;
            });

            services.AddHostedService<DbContextPoolMonitor>();

            return services;
        }

        /// <summary>
        /// Adds complete resilience infrastructure including circuit breaker and pool monitor.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureResilience">Optional resilience configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContextResilienceInfrastructure(options =>
        /// {
        ///     options.EnableCircuitBreaker = true;
        ///     options.LogPoolStatistics = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDbContextResilienceInfrastructure(
            this IServiceCollection services,
            Action<EFCoreResilienceOptions>? configureResilience = null)
        {
            services.AddMvp24HoursDbContextCircuitBreaker(configureResilience);
            services.AddMvp24HoursDbContextPoolMonitor(configureResilience);

            return services;
        }

        #endregion

        #region Private Helpers

        private static void ConfigureSqlServerWithResilience(
            DbContextOptionsBuilder options,
            string connectionString,
            EFCoreResilienceOptions resilienceOptions,
            IServiceProvider? serviceProvider)
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Configure command timeout
                sqlOptions.CommandTimeout(resilienceOptions.CommandTimeoutSeconds);

                // Configure retry on failure
                if (resilienceOptions.EnableRetryOnFailure)
                {
                    // Use custom execution strategy for advanced retry logic
                    sqlOptions.ExecutionStrategy(dependencies =>
                    {
                        var logger = serviceProvider?.GetService<ILoggerFactory>()
                            ?.CreateLogger<MvpExecutionStrategy>();

                        return new MvpExecutionStrategy(
                            dependencies,
                            resilienceOptions,
                            logger);
                    });
                }

                // Configure connection resiliency settings
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: resilienceOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(resilienceOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: resilienceOptions.AdditionalTransientErrorNumbers);
            });
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Disposable scope that manages command timeout.
        /// </summary>
        private sealed class CommandTimeoutScope : IDisposable
        {
            private readonly DbContext _context;
            private readonly int? _originalTimeout;
            private bool _disposed;

            public CommandTimeoutScope(DbContext context, int timeoutSeconds)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _originalTimeout = context.Database.GetCommandTimeout();
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(timeoutSeconds));
            }

            public void Dispose()
            {
                if (_disposed) return;

                if (_originalTimeout.HasValue)
                {
                    _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(_originalTimeout.Value));
                }
                else
                {
                    _context.Database.SetCommandTimeout((int?)null);
                }

                _disposed = true;
            }
        }

        #endregion
    }
}

