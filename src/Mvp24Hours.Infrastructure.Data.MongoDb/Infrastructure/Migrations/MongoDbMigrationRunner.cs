//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations
{
    /// <summary>
    /// Implementation of MongoDB database migration runner.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The migration runner manages schema versioning for MongoDB databases by:
    /// <list type="bullet">
    ///   <item>Tracking applied migrations in a "_migrations" collection</item>
    ///   <item>Discovering migrations from configured assemblies</item>
    ///   <item>Executing migrations in version order</item>
    ///   <item>Supporting rollback operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register migrations
    /// services.AddMongoDbMigrations(options =>
    /// {
    ///     options.MigrationAssemblies = new[] { typeof(MyMigration).Assembly };
    ///     options.AutoMigrateOnStartup = true;
    /// });
    /// 
    /// // Or run manually
    /// var runner = serviceProvider.GetRequiredService&lt;IMongoDbMigrationRunner&gt;();
    /// await runner.MigrateAsync();
    /// </code>
    /// </example>
    public sealed class MongoDbMigrationRunner : IMongoDbMigrationRunner
    {
        private const string MigrationsCollectionName = "_migrations";

        private readonly Mvp24HoursContext _context;
        private readonly MongoDbMigrationOptions _options;
        private readonly ILogger<MongoDbMigrationRunner>? _logger;
        private readonly List<IMongoDbMigration> _migrations;

        private IMongoCollection<MongoDbMigrationHistory> MigrationsCollection =>
            _context.Database.GetCollection<MongoDbMigrationHistory>(MigrationsCollectionName);

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbMigrationRunner"/> class.
        /// </summary>
        /// <param name="context">The MongoDB context.</param>
        /// <param name="options">The migration options.</param>
        /// <param name="logger">The logger instance.</param>
        public MongoDbMigrationRunner(
            Mvp24HoursContext context,
            IOptions<MongoDbMigrationOptions> options,
            ILogger<MongoDbMigrationRunner>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            // Discover and instantiate migrations
            _migrations = DiscoverMigrations();
        }

        /// <inheritdoc/>
        public async Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
        {
            var latest = await MigrationsCollection
                .Find(m => m.Status == MigrationStatus.Completed)
                .SortByDescending(m => m.Version)
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken);

            return latest?.Version ?? 0;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IMongoDbMigration>> GetPendingMigrationsAsync(
            CancellationToken cancellationToken = default)
        {
            var currentVersion = await GetCurrentVersionAsync(cancellationToken);

            return _migrations
                .Where(m => m.Version > currentVersion)
                .OrderBy(m => m.Version)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MongoDbMigrationHistory>> GetAppliedMigrationsAsync(
            CancellationToken cancellationToken = default)
        {
            return await MigrationsCollection
                .Find(m => m.Status == MigrationStatus.Completed)
                .SortBy(m => m.Version)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
        {
            var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Count == 0)
            {
                _logger?.LogInformation("No pending migrations.");
                return MigrationResult.Successful(
                    await GetCurrentVersionAsync(cancellationToken),
                    await GetCurrentVersionAsync(cancellationToken),
                    0, 0);
            }

            var targetVersion = pendingMigrations.Max(m => m.Version);
            return await MigrateToVersionAsync(targetVersion, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateToVersionAsync(
            int targetVersion,
            CancellationToken cancellationToken = default)
        {
            var startVersion = await GetCurrentVersionAsync(cancellationToken);
            var result = new MigrationResult
            {
                StartVersion = startVersion,
                EndVersion = startVersion
            };

            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "Starting migration from version {StartVersion} to {TargetVersion}",
                startVersion, targetVersion);

            var migrationsToApply = _migrations
                .Where(m => m.Version > startVersion && m.Version <= targetVersion)
                .OrderBy(m => m.Version)
                .ToList();

            foreach (var migration in migrationsToApply)
            {
                var stepResult = await ApplyMigrationAsync(migration, true, cancellationToken);
                result.Steps.Add(stepResult);

                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.Error = stepResult.Error;
                    result.TotalDurationMs = stopwatch.ElapsedMilliseconds;

                    _logger?.LogError(
                        "Migration {Version} failed: {Error}",
                        migration.Version, stepResult.Error);

                    return result;
                }

                result.EndVersion = migration.Version;
                result.MigrationsApplied++;
            }

            stopwatch.Stop();
            result.Success = true;
            result.TotalDurationMs = stopwatch.ElapsedMilliseconds;

            _logger?.LogInformation(
                "Migration completed. Applied {Count} migrations in {Duration}ms. Version: {StartVersion} -> {EndVersion}",
                result.MigrationsApplied, result.TotalDurationMs, startVersion, result.EndVersion);

            return result;
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> RollbackToVersionAsync(
            int targetVersion,
            CancellationToken cancellationToken = default)
        {
            var startVersion = await GetCurrentVersionAsync(cancellationToken);
            var result = new MigrationResult
            {
                StartVersion = startVersion,
                EndVersion = startVersion
            };

            if (targetVersion >= startVersion)
            {
                _logger?.LogWarning(
                    "Target version {TargetVersion} is not less than current version {CurrentVersion}. Nothing to rollback.",
                    targetVersion, startVersion);

                return MigrationResult.Successful(startVersion, startVersion, 0, 0);
            }

            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "Starting rollback from version {StartVersion} to {TargetVersion}",
                startVersion, targetVersion);

            var migrationsToRollback = _migrations
                .Where(m => m.Version <= startVersion && m.Version > targetVersion)
                .OrderByDescending(m => m.Version)
                .ToList();

            foreach (var migration in migrationsToRollback)
            {
                var stepResult = await ApplyMigrationAsync(migration, false, cancellationToken);
                result.Steps.Add(stepResult);

                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.Error = stepResult.Error;
                    result.TotalDurationMs = stopwatch.ElapsedMilliseconds;

                    _logger?.LogError(
                        "Rollback of migration {Version} failed: {Error}",
                        migration.Version, stepResult.Error);

                    return result;
                }

                result.EndVersion = migration.Version - 1;
                result.MigrationsApplied++;
            }

            stopwatch.Stop();
            result.Success = true;
            result.TotalDurationMs = stopwatch.ElapsedMilliseconds;

            _logger?.LogInformation(
                "Rollback completed. Rolled back {Count} migrations in {Duration}ms. Version: {StartVersion} -> {EndVersion}",
                result.MigrationsApplied, result.TotalDurationMs, startVersion, result.EndVersion);

            return result;
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> RollbackLastAsync(CancellationToken cancellationToken = default)
        {
            var currentVersion = await GetCurrentVersionAsync(cancellationToken);

            if (currentVersion == 0)
            {
                _logger?.LogWarning("No migrations to rollback.");
                return MigrationResult.Successful(0, 0, 0, 0);
            }

            // Find the previous version
            var previousVersion = _migrations
                .Where(m => m.Version < currentVersion)
                .Select(m => m.Version)
                .OrderByDescending(v => v)
                .FirstOrDefault();

            return await RollbackToVersionAsync(previousVersion, cancellationToken);
        }

        private async Task<MigrationStepResult> ApplyMigrationAsync(
            IMongoDbMigration migration,
            bool isUpgrade,
            CancellationToken cancellationToken)
        {
            var stepResult = new MigrationStepResult
            {
                Version = migration.Version,
                Description = migration.Description,
                IsUpgrade = isUpgrade
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Record migration as running
                var historyEntry = new MongoDbMigrationHistory
                {
                    Version = migration.Version,
                    Description = migration.Description,
                    TypeName = migration.GetType().FullName ?? migration.GetType().Name,
                    AppliedAt = DateTime.UtcNow,
                    Status = MigrationStatus.Running,
                    MachineName = Environment.MachineName,
                    AppliedBy = _options.AppliedBy ?? "system"
                };

                if (isUpgrade)
                {
                    await MigrationsCollection.InsertOneAsync(historyEntry, cancellationToken: cancellationToken);
                }

                _logger?.LogInformation(
                    "{Action} migration {Version}: {Description}",
                    isUpgrade ? "Applying" : "Rolling back",
                    migration.Version,
                    migration.Description);

                // Execute migration
                if (isUpgrade)
                {
                    await migration.UpAsync(_context.Database, cancellationToken);
                }
                else
                {
                    await migration.DownAsync(_context.Database, cancellationToken);
                }

                stopwatch.Stop();
                stepResult.Success = true;
                stepResult.DurationMs = stopwatch.ElapsedMilliseconds;

                // Update history
                if (isUpgrade)
                {
                    await MigrationsCollection.UpdateOneAsync(
                        m => m.Version == migration.Version && m.Status == MigrationStatus.Running,
                        Builders<MongoDbMigrationHistory>.Update
                            .Set(m => m.Status, MigrationStatus.Completed)
                            .Set(m => m.DurationMs, stepResult.DurationMs),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // For rollback, mark the entry as rolled back
                    await MigrationsCollection.UpdateOneAsync(
                        m => m.Version == migration.Version && m.Status == MigrationStatus.Completed,
                        Builders<MongoDbMigrationHistory>.Update
                            .Set(m => m.Status, MigrationStatus.RolledBack),
                        cancellationToken: cancellationToken);
                }

                _logger?.LogInformation(
                    "Migration {Version} {Action} successfully in {Duration}ms",
                    migration.Version,
                    isUpgrade ? "applied" : "rolled back",
                    stepResult.DurationMs);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                stepResult.Success = false;
                stepResult.DurationMs = stopwatch.ElapsedMilliseconds;
                stepResult.Error = ex.Message;

                // Update history with error
                await MigrationsCollection.UpdateOneAsync(
                    m => m.Version == migration.Version && m.Status == MigrationStatus.Running,
                    Builders<MongoDbMigrationHistory>.Update
                        .Set(m => m.Status, MigrationStatus.Failed)
                        .Set(m => m.DurationMs, stepResult.DurationMs)
                        .Set(m => m.Error, ex.Message),
                    cancellationToken: cancellationToken);

                _logger?.LogError(ex,
                    "Migration {Version} failed after {Duration}ms: {Error}",
                    migration.Version, stepResult.DurationMs, ex.Message);
            }

            return stepResult;
        }

        private List<IMongoDbMigration> DiscoverMigrations()
        {
            var migrations = new List<IMongoDbMigration>();
            var assemblies = _options.MigrationAssemblies ?? Array.Empty<Assembly>();

            foreach (var assembly in assemblies)
            {
                var migrationTypes = assembly.GetTypes()
                    .Where(t => typeof(IMongoDbMigration).IsAssignableFrom(t))
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .ToList();

                foreach (var type in migrationTypes)
                {
                    try
                    {
                        var migration = (IMongoDbMigration?)Activator.CreateInstance(type);
                        if (migration != null)
                        {
                            migrations.Add(migration);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex,
                            "Failed to instantiate migration type {Type}",
                            type.FullName);
                    }
                }
            }

            // Validate unique versions
            var duplicateVersions = migrations
                .GroupBy(m => m.Version)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateVersions.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate migration versions found: {string.Join(", ", duplicateVersions)}");
            }

            _logger?.LogInformation(
                "Discovered {Count} migrations from {AssemblyCount} assemblies",
                migrations.Count, assemblies.Length);

            return migrations.OrderBy(m => m.Version).ToList();
        }
    }

    /// <summary>
    /// Configuration options for MongoDB migrations.
    /// </summary>
    public sealed class MongoDbMigrationOptions
    {
        /// <summary>
        /// Gets or sets the assemblies to scan for migration classes.
        /// </summary>
        public Assembly[]? MigrationAssemblies { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically run migrations on startup.
        /// Default is false.
        /// </summary>
        public bool AutoMigrateOnStartup { get; set; }

        /// <summary>
        /// Gets or sets the user/service name to record as the applier.
        /// Default is "system".
        /// </summary>
        public string? AppliedBy { get; set; }

        /// <summary>
        /// Gets or sets whether to fail startup if migrations fail.
        /// Default is true.
        /// </summary>
        public bool FailOnMigrationError { get; set; } = true;

        /// <summary>
        /// Gets or sets the delay in seconds before running migrations on startup.
        /// Useful to allow MongoDB to fully start. Default is 0.
        /// </summary>
        public int StartupDelaySeconds { get; set; }
    }
}

