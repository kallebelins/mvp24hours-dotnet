//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure
{
    /// <summary>
    /// Background service that verifies MongoDB indexes on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service runs once at application startup to:
    /// <list type="bullet">
    ///   <item>Verify that required indexes exist on all entity collections</item>
    ///   <item>Optionally create missing indexes based on attribute definitions</item>
    ///   <item>Log index status and any discrepancies</item>
    ///   <item>Optionally fail startup if critical indexes are missing</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddMongoDbIndexVerification(options =>
    /// {
    ///     options.AssembliesToScan = new[] { typeof(MyEntity).Assembly };
    ///     options.CreateMissingIndexes = true;
    ///     options.FailOnMissingIndexes = false;
    /// });
    /// </code>
    /// </example>
    public sealed class MongoDbIndexVerificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MongoDbIndexVerificationOptions _options;
        private readonly ILogger<MongoDbIndexVerificationService>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbIndexVerificationService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="options">The index verification options.</param>
        /// <param name="logger">The logger instance.</param>
        public MongoDbIndexVerificationService(
            IServiceProvider serviceProvider,
            IOptions<MongoDbIndexVerificationOptions> options,
            ILogger<MongoDbIndexVerificationService>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger?.LogInformation("MongoDB index verification is disabled.");
                return;
            }

            // Delay startup if configured (to allow MongoDB to be ready)
            if (_options.StartupDelaySeconds > 0)
            {
                _logger?.LogInformation(
                    "Waiting {DelaySeconds}s before verifying MongoDB indexes...",
                    _options.StartupDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);
            }

            await VerifyIndexesAsync(stoppingToken);
        }

        private async Task VerifyIndexesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<Mvp24HoursContext>();

            if (context == null)
            {
                _logger?.LogWarning("Mvp24HoursContext not registered. Skipping index verification.");
                return;
            }

            var indexManager = scope.ServiceProvider.GetService<IMongoDbIndexManager>()
                ?? new MongoDbIndexManager();

            var verificationResults = new List<IndexVerificationResult>();
            var assemblies = _options.AssembliesToScan ?? Array.Empty<Assembly>();

            _logger?.LogInformation(
                "Starting MongoDB index verification for {AssemblyCount} assemblies...",
                assemblies.Length);

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-index-verification-start",
                new { AssemblyCount = assemblies.Length });

            foreach (var assembly in assemblies)
            {
                try
                {
                    var result = await VerifyAssemblyIndexesAsync(
                        context.Database,
                        indexManager,
                        assembly,
                        cancellationToken);

                    verificationResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex,
                        "Error verifying indexes for assembly {AssemblyName}",
                        assembly.GetName().Name);

                    verificationResults.Add(new IndexVerificationResult
                    {
                        AssemblyName = assembly.GetName().Name ?? "unknown",
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            // Log summary
            var totalExpected = verificationResults.Sum(r => r.ExpectedIndexCount);
            var totalExisting = verificationResults.Sum(r => r.ExistingIndexCount);
            var totalCreated = verificationResults.Sum(r => r.CreatedIndexCount);
            var totalMissing = verificationResults.Sum(r => r.MissingIndexCount);
            var anyFailed = verificationResults.Any(r => !r.Success);

            _logger?.LogInformation(
                "MongoDB index verification completed. Expected: {Expected}, Existing: {Existing}, Created: {Created}, Missing: {Missing}",
                totalExpected, totalExisting, totalCreated, totalMissing);

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-index-verification-completed",
                new
                {
                    TotalExpected = totalExpected,
                    TotalExisting = totalExisting,
                    TotalCreated = totalCreated,
                    TotalMissing = totalMissing,
                    Success = !anyFailed
                });

            // Handle missing indexes based on configuration
            if (totalMissing > 0 && !_options.CreateMissingIndexes)
            {
                var message = $"MongoDB has {totalMissing} missing indexes. " +
                              "Set CreateMissingIndexes=true to auto-create them.";

                if (_options.FailOnMissingIndexes)
                {
                    _logger?.LogCritical(message);
                    throw new InvalidOperationException(message);
                }

                _logger?.LogWarning(message);
            }

            if (anyFailed && _options.FailOnVerificationError)
            {
                var failedAssemblies = verificationResults
                    .Where(r => !r.Success)
                    .Select(r => r.AssemblyName);

                throw new InvalidOperationException(
                    $"MongoDB index verification failed for assemblies: {string.Join(", ", failedAssemblies)}");
            }
        }

        private async Task<IndexVerificationResult> VerifyAssemblyIndexesAsync(
            IMongoDatabase database,
            IMongoDbIndexManager indexManager,
            Assembly assembly,
            CancellationToken cancellationToken)
        {
            var result = new IndexVerificationResult
            {
                AssemblyName = assembly.GetName().Name ?? "unknown"
            };

            try
            {
                // Get entity types with index attributes
                var entityTypes = GetIndexedEntityTypes(assembly);
                result.EntityCount = entityTypes.Count;

                foreach (var entityType in entityTypes)
                {
                    var collectionName = GetCollectionName(entityType);

                    // Get expected indexes via reflection
                    var expectedIndexes = GetExpectedIndexes(indexManager, entityType);
                    result.ExpectedIndexCount += expectedIndexes.Count;

                    // Get existing indexes
                    var existingIndexes = await GetExistingIndexesAsync(
                        database, collectionName, entityType, cancellationToken);
                    result.ExistingIndexCount += existingIndexes.Count;

                    // Check for missing indexes
                    var missingIndexes = expectedIndexes
                        .Where(e => !existingIndexes.Any(ex => IndexNamesMatch(e, ex)))
                        .ToList();

                    result.MissingIndexCount += missingIndexes.Count;

                    if (missingIndexes.Count > 0)
                    {
                        _logger?.LogWarning(
                            "Collection {Collection} is missing {Count} indexes: {Indexes}",
                            collectionName,
                            missingIndexes.Count,
                            string.Join(", ", missingIndexes));

                        // Create missing indexes if configured
                        if (_options.CreateMissingIndexes)
                        {
                            await CreateIndexesForTypeAsync(
                                database, indexManager, entityType, collectionName, cancellationToken);

                            result.CreatedIndexCount += missingIndexes.Count;

                            _logger?.LogInformation(
                                "Created {Count} indexes for collection {Collection}",
                                missingIndexes.Count,
                                collectionName);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug(
                            "Collection {Collection} has all {Count} expected indexes",
                            collectionName,
                            expectedIndexes.Count);
                    }
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                throw;
            }

            return result;
        }

        private static List<Type> GetIndexedEntityTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => HasIndexAttributes(t))
                .ToList();
        }

        private static bool HasIndexAttributes(Type type)
        {
            var hasClassAttr = type.GetCustomAttributes<Performance.Attributes.MongoCompoundIndexAttribute>().Any();
            if (hasClassAttr) return true;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Any(p =>
                p.GetCustomAttribute<Performance.Attributes.MongoIndexAttribute>() != null ||
                p.GetCustomAttribute<Performance.Attributes.MongoTtlIndexAttribute>() != null);
        }

        private static string GetCollectionName(Type type)
        {
            var collectionAttr = type.GetCustomAttribute<BsonCollectionAttribute>();
            return collectionAttr?.CollectionName ?? type.Name;
        }

        private static List<string> GetExpectedIndexes(IMongoDbIndexManager indexManager, Type entityType)
        {
            // Use reflection to call BuildIndexModels<T>
            var method = typeof(IMongoDbIndexManager)
                .GetMethod(nameof(IMongoDbIndexManager.BuildIndexModels))
                ?.MakeGenericMethod(entityType);

            if (method == null)
            {
                return new List<string>();
            }

            var result = method.Invoke(indexManager, null);
            if (result == null)
            {
                return new List<string>();
            }

            // Extract index names from the result
            var indexNames = new List<string>();
            var enumerableResult = result as System.Collections.IEnumerable;

            if (enumerableResult != null)
            {
                foreach (var item in enumerableResult)
                {
                    var optionsProp = item?.GetType().GetProperty("Options");
                    var options = optionsProp?.GetValue(item);
                    var nameProp = options?.GetType().GetProperty("Name");
                    var name = nameProp?.GetValue(options) as string;

                    if (!string.IsNullOrEmpty(name))
                    {
                        indexNames.Add(name);
                    }
                }
            }

            return indexNames;
        }

        private async Task<List<string>> GetExistingIndexesAsync(
            IMongoDatabase database,
            string collectionName,
            Type entityType,
            CancellationToken cancellationToken)
        {
            var indexNames = new List<string>();

            try
            {
                // Get collection as BsonDocument to avoid type constraints
                var collection = database.GetCollection<BsonDocument>(collectionName);
                var cursor = await collection.Indexes.ListAsync(cancellationToken);
                var indexes = await cursor.ToListAsync(cancellationToken);

                foreach (var index in indexes)
                {
                    if (index.TryGetValue("name", out var nameValue))
                    {
                        indexNames.Add(nameValue.AsString);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Error getting existing indexes for collection {Collection}",
                    collectionName);
            }

            return indexNames;
        }

        private static bool IndexNamesMatch(string expected, string existing)
        {
            return string.Equals(expected, existing, StringComparison.OrdinalIgnoreCase);
        }

        private async Task CreateIndexesForTypeAsync(
            IMongoDatabase database,
            IMongoDbIndexManager indexManager,
            Type entityType,
            string collectionName,
            CancellationToken cancellationToken)
        {
            // Use reflection to call EnsureIndexesAsync<T>
            var method = typeof(IMongoDbIndexManager)
                .GetMethod(nameof(IMongoDbIndexManager.EnsureIndexesAsync))
                ?.MakeGenericMethod(entityType);

            if (method == null)
            {
                return;
            }

            // Get the collection with proper generic type
            var getCollectionMethod = typeof(IMongoDatabase)
                .GetMethod(nameof(IMongoDatabase.GetCollection), new[] { typeof(string), typeof(MongoCollectionSettings) })
                ?.MakeGenericMethod(entityType);

            var collection = getCollectionMethod?.Invoke(database, new object?[] { collectionName, null });

            if (collection != null)
            {
                var task = method.Invoke(indexManager, new[] { collection, cancellationToken }) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }

        private sealed class IndexVerificationResult
        {
            public string AssemblyName { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? Error { get; set; }
            public int EntityCount { get; set; }
            public int ExpectedIndexCount { get; set; }
            public int ExistingIndexCount { get; set; }
            public int CreatedIndexCount { get; set; }
            public int MissingIndexCount { get; set; }
        }
    }

    /// <summary>
    /// Configuration options for MongoDB index verification on startup.
    /// </summary>
    public sealed class MongoDbIndexVerificationOptions
    {
        /// <summary>
        /// Gets or sets whether index verification is enabled.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the assemblies to scan for entity types with index attributes.
        /// </summary>
        public Assembly[]? AssembliesToScan { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically create missing indexes.
        /// Default is true.
        /// </summary>
        public bool CreateMissingIndexes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to fail startup if indexes are missing (and CreateMissingIndexes is false).
        /// Default is false.
        /// </summary>
        public bool FailOnMissingIndexes { get; set; }

        /// <summary>
        /// Gets or sets whether to fail startup on any verification error.
        /// Default is false.
        /// </summary>
        public bool FailOnVerificationError { get; set; }

        /// <summary>
        /// Gets or sets the delay in seconds before starting verification.
        /// Useful to allow MongoDB to fully start. Default is 0.
        /// </summary>
        public int StartupDelaySeconds { get; set; }
    }
}

