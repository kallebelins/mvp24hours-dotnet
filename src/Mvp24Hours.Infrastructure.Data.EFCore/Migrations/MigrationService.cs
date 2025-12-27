//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Migrations
{
    /// <summary>
    /// Service for managing EF Core database migrations programmatically.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to manage migrations for.</typeparam>
    /// <remarks>
    /// <para>
    /// This service provides a comprehensive API for:
    /// <list type="bullet">
    /// <item>Applying pending migrations</item>
    /// <item>Rolling back migrations</item>
    /// <item>Validating database schema</item>
    /// <item>Generating migration scripts</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MigrationService<TContext> : IMigrationService where TContext : DbContext
    {
        private readonly TContext _dbContext;
        private readonly MigrationOptions _options;
        private readonly ILogger<MigrationService<TContext>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationService{TContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="options">Migration configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public MigrationService(
            TContext dbContext,
            IOptions<MigrationOptions> options,
            ILogger<MigrationService<TContext>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _options = options?.Value ?? new MigrationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService getting pending migrations for {DbContext}", typeof(TContext).Name);
            var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            return pending.ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService getting applied migrations for {DbContext}", typeof(TContext).Name);
            var applied = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            return applied.ToList();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<string>> GetAllMigrationsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService getting all migrations for {DbContext}", typeof(TContext).Name);
            var migrations = _dbContext.Database.GetMigrations().ToList();
            return Task.FromResult<IReadOnlyList<string>>(migrations);
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService starting migration for {DbContext}", typeof(TContext).Name);
            
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);
                
                if (pendingMigrations.Count == 0)
                {
                    _logger.LogInformation("No pending migrations for {DbContext}", typeof(TContext).Name);
                    return MigrationResult.NoMigrationsNeeded();
                }

                _logger.LogInformation(
                    "Applying {Count} pending migrations for {DbContext}: {Migrations}",
                    pendingMigrations.Count,
                    typeof(TContext).Name,
                    string.Join(", ", pendingMigrations));

                if (_options.EnsureDatabaseCreated)
                {
                    await EnsureDatabaseCreatedAsync(cancellationToken);
                }

                await _dbContext.Database.MigrateAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Successfully applied {Count} migrations for {DbContext} in {Duration}ms",
                    pendingMigrations.Count,
                    typeof(TContext).Name,
                    stopwatch.ElapsedMilliseconds);

                return MigrationResult.Succeeded(pendingMigrations, stopwatch.Elapsed, startedAt);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Migration failed for {DbContext}", typeof(TContext).Name);

                return MigrationResult.Failed(ex.Message, ex, stopwatch.Elapsed, startedAt);
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateToAsync(string targetMigration, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(targetMigration))
                throw new ArgumentNullException(nameof(targetMigration));

            _logger.LogDebug("MigrationService migrating to {TargetMigration} for {DbContext}", targetMigration, typeof(TContext).Name);
            
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var allMigrations = await GetAllMigrationsAsync(cancellationToken);
                if (!allMigrations.Contains(targetMigration))
                {
                    throw new InvalidOperationException($"Migration '{targetMigration}' not found");
                }

                _logger.LogInformation(
                    "Migrating {DbContext} to {TargetMigration}",
                    typeof(TContext).Name,
                    targetMigration);

                var migrator = _dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
                await migrator.MigrateAsync(targetMigration, cancellationToken);

                stopwatch.Stop();

                var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully migrated {DbContext} to {TargetMigration} in {Duration}ms",
                    typeof(TContext).Name,
                    targetMigration,
                    stopwatch.ElapsedMilliseconds);

                return MigrationResult.Succeeded(appliedMigrations, stopwatch.Elapsed, startedAt);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Migration to {TargetMigration} failed for {DbContext}", targetMigration, typeof(TContext).Name);

                return MigrationResult.Failed(ex.Message, ex, stopwatch.Elapsed, startedAt);
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> RollbackToAsync(string targetMigration, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(targetMigration))
                throw new ArgumentNullException(nameof(targetMigration));

            _logger.LogDebug("MigrationService rolling back to {TargetMigration} for {DbContext}", targetMigration, typeof(TContext).Name);
            
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
                
                if (!appliedMigrations.Contains(targetMigration))
                {
                    throw new InvalidOperationException($"Migration '{targetMigration}' has not been applied");
                }

                var migrationsToRollback = appliedMigrations
                    .SkipWhile(m => m != targetMigration)
                    .Skip(1)
                    .Reverse()
                    .ToList();

                _logger.LogWarning(
                    "Rolling back {Count} migrations for {DbContext} to {TargetMigration}: {Migrations}",
                    migrationsToRollback.Count,
                    typeof(TContext).Name,
                    targetMigration,
                    string.Join(", ", migrationsToRollback));

                var migrator = _dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
                await migrator.MigrateAsync(targetMigration, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Successfully rolled back to {TargetMigration} for {DbContext} in {Duration}ms",
                    targetMigration,
                    typeof(TContext).Name,
                    stopwatch.ElapsedMilliseconds);

                return MigrationResult.RollbackSucceeded(migrationsToRollback, stopwatch.Elapsed, startedAt);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Rollback to {TargetMigration} failed for {DbContext}", targetMigration, typeof(TContext).Name);

                return MigrationResult.Failed(ex.Message, ex, stopwatch.Elapsed, startedAt);
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> RollbackLastAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService rolling back last migration for {DbContext}", typeof(TContext).Name);

            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
            
            if (appliedMigrations.Count < 2)
            {
                _logger.LogWarning("Cannot rollback: less than 2 migrations applied for {DbContext}", typeof(TContext).Name);
                return MigrationResult.Failed(
                    "Cannot rollback: less than 2 migrations applied",
                    null,
                    TimeSpan.Zero,
                    DateTime.UtcNow);
            }

            var targetMigration = appliedMigrations[^2]; // Second to last
            return await RollbackToAsync(targetMigration, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService ensuring database created for {DbContext}", typeof(TContext).Name);
            
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                _logger.LogInformation("Creating database for {DbContext}", typeof(TContext).Name);
                var created = await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
                
                if (created)
                {
                    _logger.LogInformation("Database created for {DbContext}", typeof(TContext).Name);
                }
                
                return created;
            }
            
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDatabaseAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("MigrationService deleting database for {DbContext}", typeof(TContext).Name);
            var deleted = await _dbContext.Database.EnsureDeletedAsync(cancellationToken);
            
            if (deleted)
            {
                _logger.LogInformation("Database deleted for {DbContext}", typeof(TContext).Name);
            }
            
            return deleted;
        }

        /// <inheritdoc/>
        public async Task<bool> HasPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            var pending = await GetPendingMigrationsAsync(cancellationToken);
            return pending.Count > 0;
        }

        /// <inheritdoc/>
        public Task<SchemaValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService validating schema for {DbContext}", typeof(TContext).Name);

            // Note: Full schema validation requires comparing the EF Core model with the actual database schema.
            // This is a simplified implementation. For production, consider using a tool like EFCore.Tools.
            
            var differences = new List<SchemaDifference>();
            
            try
            {
                // Get the model from DbContext
                var model = _dbContext.Model;
                
                foreach (var entityType in model.GetEntityTypes())
                {
                    // Basic validation - check if entity is configured correctly
                    if (entityType.GetTableName() == null)
                    {
                        differences.Add(new SchemaDifference
                        {
                            Type = SchemaDifferenceType.MissingTable,
                            ObjectName = entityType.ClrType.Name,
                            Description = $"Entity {entityType.ClrType.Name} has no table mapping",
                            IsCritical = true
                        });
                    }
                }
                
                if (differences.Count > 0)
                {
                    _logger.LogWarning(
                        "Schema validation found {Count} differences for {DbContext}",
                        differences.Count,
                        typeof(TContext).Name);
                    
                    return Task.FromResult(SchemaValidationResult.Invalid(differences));
                }
                
                _logger.LogInformation("Schema validation passed for {DbContext}", typeof(TContext).Name);
                return Task.FromResult(SchemaValidationResult.Valid());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Schema validation failed for {DbContext}", typeof(TContext).Name);
                
                differences.Add(new SchemaDifference
                {
                    Type = SchemaDifferenceType.Other,
                    ObjectName = "Schema",
                    Description = $"Schema validation error: {ex.Message}",
                    IsCritical = true
                });
                
                return Task.FromResult(SchemaValidationResult.Invalid(differences));
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetMigrationScriptAsync(CancellationToken cancellationToken = default)
        {
            return await GetMigrationScriptAsync(null, null, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<string> GetMigrationScriptAsync(string? fromMigration, string? toMigration, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MigrationService generating migration script from '{From}' to '{To}' for {DbContext}", 
                fromMigration ?? "(beginning)", 
                toMigration ?? "(latest)", 
                typeof(TContext).Name);

            var migrator = _dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var script = migrator.GenerateScript(fromMigration, toMigration);

            _logger.LogInformation(
                "Generated migration script from '{From}' to '{To}' for {DbContext}",
                fromMigration ?? "(beginning)",
                toMigration ?? "(latest)",
                typeof(TContext).Name);

            return Task.FromResult(script);
        }
    }
}

