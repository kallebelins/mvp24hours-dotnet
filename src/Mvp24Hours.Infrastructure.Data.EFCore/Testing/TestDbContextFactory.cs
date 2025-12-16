//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// Factory interface for creating DbContext instances in integration tests.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This interface defines the contract for creating database contexts
/// for integration testing scenarios. Implementations can support different
/// database providers (in-memory, SQLite, SQL Server LocalDB, etc.).
/// </para>
/// </remarks>
public interface ITestDbContextFactory<TContext> : IDisposable
    where TContext : DbContext
{
    /// <summary>
    /// Creates a new DbContext instance.
    /// </summary>
    /// <returns>A new DbContext instance.</returns>
    TContext CreateContext();

    /// <summary>
    /// Creates a new DbContext instance asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the new DbContext instance.</returns>
    Task<TContext> CreateContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up the database (removes all data or drops the database).
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task CleanupDatabaseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring the test DbContext factory.
/// </summary>
public class TestDbContextFactoryOptions
{
    /// <summary>
    /// Gets or sets the connection string for the test database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether to use migrations or EnsureCreated.
    /// Default is false (uses EnsureCreated for faster test setup).
    /// </summary>
    public bool UseMigrations { get; set; }

    /// <summary>
    /// Gets or sets whether to create a new database for each test.
    /// Default is true for test isolation.
    /// </summary>
    public bool CreateNewDatabasePerTest { get; set; } = true;

    /// <summary>
    /// Gets or sets the database name prefix.
    /// Default is "TestDb_".
    /// </summary>
    public string DatabaseNamePrefix { get; set; } = "TestDb_";

    /// <summary>
    /// Gets or sets whether to enable sensitive data logging.
    /// Default is true for debugging.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed errors.
    /// Default is true.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets additional interceptors to register.
    /// </summary>
    public List<IInterceptor> Interceptors { get; set; } = [];

    /// <summary>
    /// Gets or sets a custom configuration action for DbContextOptionsBuilder.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureOptions { get; set; }
}

/// <summary>
/// Base implementation of test DbContext factory.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This class provides a base implementation for test context factories
/// that can be extended for specific database providers.
/// </para>
/// </remarks>
public abstract class TestDbContextFactoryBase<TContext> : ITestDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly TestDbContextFactoryOptions _options;
    private readonly List<TContext> _createdContexts = [];
    private readonly string _databaseName;
    private readonly Func<DbContextOptions<TContext>, TContext>? _contextFactory;
    private bool _initialized;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    /// <param name="options">The factory options.</param>
    protected TestDbContextFactoryBase(TestDbContextFactoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _databaseName = _options.CreateNewDatabasePerTest
            ? $"{_options.DatabaseNamePrefix}{Guid.NewGuid():N}"
            : $"{_options.DatabaseNamePrefix}Shared";
    }

    /// <summary>
    /// Initializes a new instance with the specified options and context factory.
    /// </summary>
    /// <param name="options">The factory options.</param>
    /// <param name="contextFactory">Factory function to create the DbContext.</param>
    protected TestDbContextFactoryBase(
        TestDbContextFactoryOptions options,
        Func<DbContextOptions<TContext>, TContext> contextFactory)
        : this(options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Gets the database name being used.
    /// </summary>
    protected string DatabaseName => _databaseName;

    /// <summary>
    /// Gets the factory options.
    /// </summary>
    protected TestDbContextFactoryOptions Options => _options;

    /// <inheritdoc />
    public TContext CreateContext()
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "testdbcontextfactory-createcontext-start");
        
        try
        {
            var dbOptions = BuildDbContextOptions();
            var context = CreateContextInstance(dbOptions);
            
            _createdContexts.Add(context);
            
            return context;
        }
        finally
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "testdbcontextfactory-createcontext-end");
        }
    }

    /// <inheritdoc />
    public Task<TContext> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateContext());
    }

    /// <inheritdoc />
    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            using var context = CreateContext();
            
            if (_options.UseMigrations)
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CleanupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var context = CreateContext();
        await context.Database.EnsureDeletedAsync(cancellationToken);
        _initialized = false;
    }

    /// <summary>
    /// Builds the DbContextOptions for the test context.
    /// </summary>
    /// <returns>The configured options.</returns>
    protected abstract DbContextOptions<TContext> BuildDbContextOptions();

    /// <summary>
    /// Creates the DbContext instance.
    /// </summary>
    protected virtual TContext CreateContextInstance(DbContextOptions<TContext> options)
    {
        if (_contextFactory != null)
        {
            return _contextFactory(options);
        }
        
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    /// <summary>
    /// Applies common configuration to the options builder.
    /// </summary>
    protected virtual void ApplyCommonConfiguration(DbContextOptionsBuilder<TContext> builder)
    {
        if (_options.EnableSensitiveDataLogging)
        {
            builder.EnableSensitiveDataLogging();
        }

        if (_options.EnableDetailedErrors)
        {
            builder.EnableDetailedErrors();
        }

        if (_options.Interceptors.Count > 0)
        {
            builder.AddInterceptors([.. _options.Interceptors]);
        }

        _options.ConfigureOptions?.Invoke(builder);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var context in _createdContexts)
            {
                context.Dispose();
            }
            _createdContexts.Clear();
            _initLock.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// In-memory implementation of the test DbContext factory.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This implementation uses EF Core's in-memory database provider for fast
/// unit and integration testing without external database dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var factory = new InMemoryTestDbContextFactory&lt;AppDbContext&gt;();
/// await factory.InitializeDatabaseAsync();
/// 
/// using var context = factory.CreateContext();
/// context.Products.Add(new Product { Name = "Test" });
/// await context.SaveChangesAsync();
/// </code>
/// </example>
public class InMemoryTestDbContextFactory<TContext> : TestDbContextFactoryBase<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public InMemoryTestDbContextFactory()
        : this(new TestDbContextFactoryOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    /// <param name="options">The factory options.</param>
    public InMemoryTestDbContextFactory(TestDbContextFactoryOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified options and context factory.
    /// </summary>
    /// <param name="options">The factory options.</param>
    /// <param name="contextFactory">Factory function to create the DbContext.</param>
    public InMemoryTestDbContextFactory(
        TestDbContextFactoryOptions options,
        Func<DbContextOptions<TContext>, TContext> contextFactory)
        : base(options, contextFactory)
    {
    }

    /// <inheritdoc />
    protected override DbContextOptions<TContext> BuildDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        
        builder.UseInMemoryDatabase(DatabaseName);
        
        builder.ConfigureWarnings(warnings =>
        {
            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
        });
        
        ApplyCommonConfiguration(builder);
        
        return builder.Options;
    }
}

