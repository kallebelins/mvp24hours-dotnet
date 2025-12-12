//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Migrations
{
    /// <summary>
    /// Background service that runs database migrations on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service:
    /// <list type="bullet">
    /// <item>Checks for pending migrations on startup</item>
    /// <item>Optionally applies migrations automatically</item>
    /// <item>Optionally runs data seeders after migrations</item>
    /// <item>Supports distributed locking for clustered deployments</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MigrationHostedService<TContext> : IHostedService
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MigrationOptions _options;
        private readonly ILogger<MigrationHostedService<TContext>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationHostedService{TContext}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for creating scopes.</param>
        /// <param name="options">Migration configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public MigrationHostedService(
            IServiceProvider serviceProvider,
            IOptions<MigrationOptions> options,
            ILogger<MigrationHostedService<TContext>> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? new MigrationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationhostedservice-startasync-start");

            _logger.LogInformation("Migration service starting for {DbContext}", typeof(TContext).Name);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();

                // Check for pending migrations
                var hasPending = await migrationService.HasPendingMigrationsAsync(cancellationToken);

                if (!hasPending)
                {
                    _logger.LogInformation("No pending migrations for {DbContext}", typeof(TContext).Name);
                    return;
                }

                var pendingMigrations = await migrationService.GetPendingMigrationsAsync(cancellationToken);

                if (_options.LogPendingMigrations)
                {
                    _logger.LogWarning(
                        "Found {Count} pending migrations for {DbContext}: {Migrations}",
                        pendingMigrations.Count,
                        typeof(TContext).Name,
                        string.Join(", ", pendingMigrations));
                }

                // Handle based on configuration
                if (_options.AutoMigrateOnStartup)
                {
                    await ApplyMigrationsAsync(migrationService, cancellationToken);
                }
                else if (_options.ThrowOnPendingMigrations)
                {
                    throw new InvalidOperationException(
                        $"Database has {pendingMigrations.Count} pending migrations. " +
                        $"Apply migrations or enable AutoMigrateOnStartup. " +
                        $"Pending: {string.Join(", ", pendingMigrations)}");
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationhostedservice-startasync-complete");
            }
            catch (Exception ex) when (_options.ThrowOnPendingMigrations || _options.AutoMigrateOnStartup)
            {
                _logger.LogCritical(ex, "Migration failed for {DbContext}", typeof(TContext).Name);
                TelemetryHelper.Execute(TelemetryLevels.Error, "migrationhostedservice-startasync-error", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration check failed for {DbContext}, but continuing startup", typeof(TContext).Name);
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Migration service stopping for {DbContext}", typeof(TContext).Name);
            return Task.CompletedTask;
        }

        private async Task ApplyMigrationsAsync(IMigrationService migrationService, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying migrations for {DbContext}", typeof(TContext).Name);

            var attempt = 0;
            MigrationResult? result = null;

            while (attempt < _options.MaxRetryAttempts)
            {
                attempt++;

                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_options.MigrationTimeout);

                    result = await migrationService.MigrateAsync(cts.Token);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Successfully applied {Count} migrations for {DbContext} in {Duration}ms",
                            result.AppliedMigrations.Count,
                            typeof(TContext).Name,
                            result.Duration.TotalMilliseconds);

                        // Run data seeders if enabled
                        if (_options.EnableDataSeeding)
                        {
                            await RunDataSeedersAsync(cancellationToken);
                        }

                        return;
                    }

                    _logger.LogError(
                        "Migration attempt {Attempt}/{MaxAttempts} failed for {DbContext}: {Error}",
                        attempt,
                        _options.MaxRetryAttempts,
                        typeof(TContext).Name,
                        result.ErrorMessage);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Migration attempt {Attempt}/{MaxAttempts} threw exception for {DbContext}",
                        attempt,
                        _options.MaxRetryAttempts,
                        typeof(TContext).Name);
                }

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.UseExponentialBackoff
                        ? TimeSpan.FromTicks(_options.RetryDelay.Ticks * (long)Math.Pow(2, attempt - 1))
                        : _options.RetryDelay;

                    _logger.LogInformation(
                        "Retrying migration for {DbContext} in {Delay}ms",
                        typeof(TContext).Name,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new InvalidOperationException(
                $"Migration failed after {_options.MaxRetryAttempts} attempts for {typeof(TContext).Name}. " +
                $"Last error: {result?.ErrorMessage ?? "Unknown"}");
        }

        private async Task RunDataSeedersAsync(CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "migrationhostedservice-rundataseeders-start");

            using var scope = _serviceProvider.CreateScope();

            // Try to resolve IDataSeeder instances
            var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();

            foreach (var seeder in seeders)
            {
                try
                {
                    _logger.LogInformation(
                        "Running data seeder {SeederType} for {DbContext}",
                        seeder.GetType().Name,
                        typeof(TContext).Name);

                    await seeder.SeedAsync(cancellationToken);

                    _logger.LogInformation(
                        "Data seeder {SeederType} completed for {DbContext}",
                        seeder.GetType().Name,
                        typeof(TContext).Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Data seeder {SeederType} failed for {DbContext}",
                        seeder.GetType().Name,
                        typeof(TContext).Name);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Interface for data seeders that run after migrations.
    /// </summary>
    public interface IDataSeeder
    {
        /// <summary>
        /// Gets the order in which this seeder should run. Lower values run first.
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Seeds data into the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SeedAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base class for data seeders with common functionality.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    public abstract class DataSeederBase<TContext> : IDataSeeder
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        /// <summary>
        /// Gets the DbContext for seeding operations.
        /// </summary>
        protected TContext DbContext { get; }

        /// <summary>
        /// Gets the logger for diagnostic output.
        /// </summary>
        protected ILogger Logger { get; }

        /// <inheritdoc/>
        public virtual int Order => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeederBase{TContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        protected DataSeederBase(TContext dbContext, ILogger logger)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public abstract Task SeedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Seeds data only if the table is empty.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to seed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected async Task SeedIfEmptyAsync<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default) where T : class
        {
            var dbSet = DbContext.Set<T>();
            
            if (!await dbSet.AnyAsync(cancellationToken))
            {
                Logger.LogInformation("Seeding {EntityType} data", typeof(T).Name);
                await dbSet.AddRangeAsync(entities, cancellationToken);
                await DbContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("Seeded {Count} {EntityType} records", entities.Count(), typeof(T).Name);
            }
            else
            {
                Logger.LogInformation("Skipping {EntityType} seeding - table not empty", typeof(T).Name);
            }
        }
    }
}

