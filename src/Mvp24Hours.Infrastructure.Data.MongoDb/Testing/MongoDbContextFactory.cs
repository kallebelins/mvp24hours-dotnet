//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// Factory for creating MongoDB context instances for testing.
/// </summary>
/// <remarks>
/// <para>
/// This factory simplifies the creation of MongoDB contexts for
/// unit and integration testing. It handles the boilerplate configuration
/// and provides isolation between tests.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Automatic database isolation per test</item>
/// <item>Built-in cleanup and disposal</item>
/// <item>Configurable options for different test scenarios</item>
/// <item>Support for data seeding</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a factory
/// var factory = new MongoDbContextFactory("mongodb://localhost:27017");
/// 
/// // Create an isolated context for a test
/// using var context = factory.CreateContext();
/// 
/// // Add test data
/// var collection = context.Set&lt;Customer&gt;();
/// await collection.InsertOneAsync(new Customer { Name = "Test" });
/// 
/// // Query and assert
/// var customers = await collection.Find(_ => true).ToListAsync();
/// Assert.Single(customers);
/// </code>
/// </example>
public class MongoDbContextFactory : IDisposable
{
    private readonly MongoDbInMemoryOptions _options;
    private readonly ConcurrentBag<Mvp24HoursContext> _createdContexts = [];
    private readonly Func<MongoDbOptions, Mvp24HoursContext>? _contextFactory;
    private readonly ITenantProvider? _tenantProvider;
    private readonly ICurrentUserProvider? _currentUserProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    public MongoDbContextFactory(string connectionString)
        : this(new MongoDbInMemoryOptions { ConnectionString = connectionString })
    {
    }

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public MongoDbContextFactory()
        : this(new MongoDbInMemoryOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public MongoDbContextFactory(MongoDbInMemoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        TelemetryHelper.Execute(TelemetryLevels.Verbose,
            "mongodbcontextfactory-created",
            new { DatabasePrefix = options.DatabaseNamePrefix });
    }

    /// <summary>
    /// Initializes a new instance with the specified options and custom context factory.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="contextFactory">Factory function to create the context.</param>
    /// <remarks>
    /// Use this constructor when your context requires additional dependencies
    /// beyond just MongoDbOptions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var factory = new MongoDbContextFactory(
    ///     new MongoDbInMemoryOptions(),
    ///     options => new CustomContext(options, additionalDependency)
    /// );
    /// </code>
    /// </example>
    public MongoDbContextFactory(
        MongoDbInMemoryOptions options,
        Func<MongoDbOptions, Mvp24HoursContext> contextFactory)
        : this(options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Initializes a new instance with multi-tenancy support.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="currentUserProvider">The current user provider.</param>
    public MongoDbContextFactory(
        MongoDbInMemoryOptions options,
        ITenantProvider? tenantProvider,
        ICurrentUserProvider? currentUserProvider = null)
        : this(options)
    {
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
    }

    /// <summary>
    /// Creates a new MongoDB context instance.
    /// </summary>
    /// <returns>A new context configured for testing.</returns>
    /// <remarks>
    /// Each call creates a new context pointing to the same or different
    /// database based on the <see cref="MongoDbInMemoryOptions.UseUniqueDatabaseName"/> setting.
    /// </remarks>
    public Mvp24HoursContext CreateContext()
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodbcontextfactory-createcontext-start");

        try
        {
            var mongoOptions = BuildMongoDbOptions();
            var context = CreateContextInstance(mongoOptions);

            _createdContexts.Add(context);

            return context;
        }
        finally
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodbcontextfactory-createcontext-end");
        }
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data.
    /// </summary>
    /// <typeparam name="TSeeder">The data seeder type.</typeparam>
    /// <returns>A new context with seeded test data.</returns>
    /// <example>
    /// <code>
    /// using var context = factory.CreateContextWithData&lt;CustomerSeeder&gt;();
    /// // Context now has test data from CustomerSeeder
    /// </code>
    /// </example>
    public Mvp24HoursContext CreateContextWithData<TSeeder>()
        where TSeeder : IMongoDataSeeder, new()
    {
        var context = CreateContext();
        var seeder = new TSeeder();
        seeder.Seed(context);
        return context;
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data using a custom seeder.
    /// </summary>
    /// <param name="seeder">The data seeder instance.</param>
    /// <returns>A new context with seeded test data.</returns>
    public Mvp24HoursContext CreateContextWithData(IMongoDataSeeder seeder)
    {
        ArgumentNullException.ThrowIfNull(seeder);

        var context = CreateContext();
        seeder.Seed(context);
        return context;
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data using a custom action.
    /// </summary>
    /// <param name="seedAction">The action to seed data.</param>
    /// <returns>A new context with seeded test data.</returns>
    /// <example>
    /// <code>
    /// using var context = factory.CreateContextWithData(ctx =>
    /// {
    ///     ctx.Set&lt;Customer&gt;().InsertMany(new[]
    ///     {
    ///         new Customer { Name = "Customer 1" },
    ///         new Customer { Name = "Customer 2" }
    ///     });
    /// });
    /// </code>
    /// </example>
    public Mvp24HoursContext CreateContextWithData(Action<Mvp24HoursContext> seedAction)
    {
        ArgumentNullException.ThrowIfNull(seedAction);

        var context = CreateContext();
        seedAction(context);
        return context;
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data asynchronously.
    /// </summary>
    /// <typeparam name="TSeeder">The data seeder type.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new context with seeded test data.</returns>
    public async Task<Mvp24HoursContext> CreateContextWithDataAsync<TSeeder>(CancellationToken cancellationToken = default)
        where TSeeder : IMongoDataSeederAsync, new()
    {
        var context = CreateContext();
        var seeder = new TSeeder();
        await seeder.SeedAsync(context, cancellationToken);
        return context;
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data asynchronously using a custom seeder.
    /// </summary>
    /// <param name="seeder">The data seeder instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new context with seeded test data.</returns>
    public async Task<Mvp24HoursContext> CreateContextWithDataAsync(
        IMongoDataSeederAsync seeder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seeder);

        var context = CreateContext();
        await seeder.SeedAsync(context, cancellationToken);
        return context;
    }

    /// <summary>
    /// Creates a new MongoDB context instance with seeded data asynchronously using a custom action.
    /// </summary>
    /// <param name="seedAction">The action to seed data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new context with seeded test data.</returns>
    public async Task<Mvp24HoursContext> CreateContextWithDataAsync(
        Func<Mvp24HoursContext, CancellationToken, Task> seedAction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seedAction);

        var context = CreateContext();
        await seedAction(context, cancellationToken);
        return context;
    }

    /// <summary>
    /// Builds MongoDbOptions from the configuration.
    /// </summary>
    protected virtual MongoDbOptions BuildMongoDbOptions()
    {
        var mongoOptions = new MongoDbOptions
        {
            DatabaseName = _options.GetEffectiveDatabaseName(),
            ConnectionString = _options.ConnectionString ?? "mongodb://localhost:27017",
            EnableTransaction = _options.EnableTransaction,
            EnableMultiTenancy = _options.EnableMultiTenancy,
            EnableCommandLogging = _options.EnableLogging,
            ConnectionTimeoutSeconds = _options.TimeoutSeconds,
            SocketTimeoutSeconds = _options.TimeoutSeconds
        };

        _options.ConfigureOptions?.Invoke(mongoOptions);

        return mongoOptions;
    }

    /// <summary>
    /// Creates the context instance.
    /// </summary>
    private Mvp24HoursContext CreateContextInstance(MongoDbOptions options)
    {
        if (_contextFactory != null)
        {
            return _contextFactory(options);
        }

        return new Mvp24HoursContext(options, _tenantProvider, _currentUserProvider);
    }

    /// <summary>
    /// Drops the database for the specified context.
    /// </summary>
    /// <param name="context">The context whose database should be dropped.</param>
    /// <remarks>
    /// Use this method to clean up test databases after integration tests.
    /// </remarks>
    public void DropDatabase(Mvp24HoursContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.MongoClient.DropDatabase(context.DatabaseName);

        TelemetryHelper.Execute(TelemetryLevels.Verbose,
            "mongodbcontextfactory-droppeddatabase",
            new { DatabaseName = context.DatabaseName });
    }

    /// <summary>
    /// Drops the database for the specified context asynchronously.
    /// </summary>
    /// <param name="context">The context whose database should be dropped.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DropDatabaseAsync(Mvp24HoursContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.MongoClient.DropDatabaseAsync(context.DatabaseName, cancellationToken);

        TelemetryHelper.Execute(TelemetryLevels.Verbose,
            "mongodbcontextfactory-droppeddatabaseasync",
            new { DatabaseName = context.DatabaseName });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes all created contexts.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            while (_createdContexts.TryTake(out var context))
            {
                // Optionally drop the test database on cleanup
                try
                {
                    if (_options.UseUniqueDatabaseName)
                    {
                        context.MongoClient.DropDatabase(context.DatabaseName);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                context.Dispose();
            }
        }

        _disposed = true;
    }
}

/// <summary>
/// Static helper class for creating MongoDB contexts quickly.
/// </summary>
/// <example>
/// <code>
/// // Quick context creation for simple tests
/// using var context = MongoDbContextHelper.CreateContext("mongodb://localhost:27017");
/// </code>
/// </example>
public static class MongoDbContextHelper
{
    /// <summary>
    /// Creates a simple MongoDB context with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">Optional database name. If null, a unique name is generated.</param>
    /// <returns>A new MongoDB context.</returns>
    public static Mvp24HoursContext CreateContext(string connectionString, string? databaseName = null)
    {
        var effectiveName = databaseName ?? $"TestDb_{Guid.NewGuid():N}";

        var options = new MongoDbOptions
        {
            DatabaseName = effectiveName,
            ConnectionString = connectionString,
            EnableCommandLogging = true
        };

        return new Mvp24HoursContext(options);
    }

    /// <summary>
    /// Creates MongoDbOptions for testing.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">Optional database name. If null, a unique name is generated.</param>
    /// <returns>The configured options.</returns>
    public static MongoDbOptions CreateOptions(string connectionString, string? databaseName = null)
    {
        var effectiveName = databaseName ?? $"TestDb_{Guid.NewGuid():N}";

        return new MongoDbOptions
        {
            DatabaseName = effectiveName,
            ConnectionString = connectionString,
            EnableCommandLogging = true,
            ConnectionTimeoutSeconds = 30,
            SocketTimeoutSeconds = 30
        };
    }
}

