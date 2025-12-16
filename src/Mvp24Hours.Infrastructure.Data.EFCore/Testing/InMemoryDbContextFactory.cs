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
using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// Factory for creating in-memory DbContext instances for testing.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This factory simplifies the creation of in-memory database contexts for
/// unit and integration testing. It handles the boilerplate configuration
/// and provides isolation between tests.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Automatic database isolation per test</item>
/// <item>Pooled context support for performance</item>
/// <item>Built-in cleanup and disposal</item>
/// <item>Configurable options for different test scenarios</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a factory
/// var factory = new InMemoryDbContextFactory&lt;MyDbContext&gt;();
/// 
/// // Create an isolated context for a test
/// using var context = factory.CreateContext();
/// 
/// // Add test data
/// context.Products.Add(new Product { Name = "Test" });
/// await context.SaveChangesAsync();
/// 
/// // Assert
/// var products = await context.Products.ToListAsync();
/// Assert.Single(products);
/// </code>
/// </example>
public class InMemoryDbContextFactory<TContext> : IDisposable
    where TContext : DbContext
{
    private readonly InMemoryDbContextOptions _options;
    private readonly ConcurrentBag<TContext> _createdContexts = [];
    private readonly Func<DbContextOptions<TContext>, TContext>? _contextFactory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the factory with default options.
    /// </summary>
    public InMemoryDbContextFactory()
        : this(new InMemoryDbContextOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the factory with the specified options.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public InMemoryDbContextFactory(InMemoryDbContextOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the factory with a custom context factory.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="contextFactory">Factory function to create the DbContext.</param>
    /// <remarks>
    /// Use this constructor when your DbContext requires constructor parameters
    /// beyond just DbContextOptions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var factory = new InMemoryDbContextFactory&lt;AppDbContext&gt;(
    ///     new InMemoryDbContextOptions(),
    ///     options => new AppDbContext(options, mockTenantService.Object)
    /// );
    /// </code>
    /// </example>
    public InMemoryDbContextFactory(
        InMemoryDbContextOptions options,
        Func<DbContextOptions<TContext>, TContext> contextFactory)
        : this(options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Creates a new in-memory DbContext instance.
    /// </summary>
    /// <returns>A new DbContext instance configured for in-memory testing.</returns>
    /// <remarks>
    /// Each call creates a new context pointing to the same or different
    /// in-memory database based on the <see cref="InMemoryDbContextOptions.UseUniqueDatabaseName"/> setting.
    /// </remarks>
    public TContext CreateContext()
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "inmemorydbcontextfactory-createcontext-start");
        
        try
        {
            var dbOptions = BuildDbContextOptions();
            var context = CreateContextInstance(dbOptions);
            
            _createdContexts.Add(context);
            
            if (_options.ValidateModel)
            {
                // Force model validation by accessing the model
                _ = context.Model;
            }
            
            return context;
        }
        finally
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "inmemorydbcontextfactory-createcontext-end");
        }
    }

    /// <summary>
    /// Creates a new in-memory DbContext instance with the database already created.
    /// </summary>
    /// <returns>A new DbContext instance with EnsureCreated called.</returns>
    public TContext CreateContextWithDatabase()
    {
        var context = CreateContext();
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory DbContext instance with seeded data.
    /// </summary>
    /// <typeparam name="TSeeder">The data seeder type.</typeparam>
    /// <returns>A new DbContext instance with seeded test data.</returns>
    /// <example>
    /// <code>
    /// using var context = factory.CreateContextWithData&lt;CustomerSeeder&gt;();
    /// // Context now has test data from CustomerSeeder
    /// </code>
    /// </example>
    public TContext CreateContextWithData<TSeeder>()
        where TSeeder : IDataSeeder<TContext>, new()
    {
        var context = CreateContextWithDatabase();
        var seeder = new TSeeder();
        seeder.Seed(context);
        context.SaveChanges();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory DbContext instance with seeded data using a custom seeder.
    /// </summary>
    /// <param name="seeder">The data seeder instance.</param>
    /// <returns>A new DbContext instance with seeded test data.</returns>
    public TContext CreateContextWithData(IDataSeeder<TContext> seeder)
    {
        ArgumentNullException.ThrowIfNull(seeder);
        
        var context = CreateContextWithDatabase();
        seeder.Seed(context);
        context.SaveChanges();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory DbContext instance with seeded data using a custom action.
    /// </summary>
    /// <param name="seedAction">The action to seed data.</param>
    /// <returns>A new DbContext instance with seeded test data.</returns>
    /// <example>
    /// <code>
    /// using var context = factory.CreateContextWithData(ctx =>
    /// {
    ///     ctx.Customers.AddRange(
    ///         new Customer { Name = "Customer 1" },
    ///         new Customer { Name = "Customer 2" }
    ///     );
    /// });
    /// </code>
    /// </example>
    public TContext CreateContextWithData(Action<TContext> seedAction)
    {
        ArgumentNullException.ThrowIfNull(seedAction);
        
        var context = CreateContextWithDatabase();
        seedAction(context);
        context.SaveChanges();
        return context;
    }

    /// <summary>
    /// Creates DbContextOptions configured for in-memory testing.
    /// </summary>
    /// <returns>The configured options.</returns>
    protected virtual DbContextOptions<TContext> BuildDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        
        // Configure in-memory database
        builder.UseInMemoryDatabase(_options.GetEffectiveDatabaseName());
        
        // Configure sensitive data logging
        if (_options.EnableSensitiveDataLogging)
        {
            builder.EnableSensitiveDataLogging();
        }
        
        // Configure detailed errors
        if (_options.EnableDetailedErrors)
        {
            builder.EnableDetailedErrors();
        }
        
        // Configure warnings
        builder.ConfigureWarnings(warnings =>
        {
            if (_options.SuppressTransactionWarning)
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
            }
            
            if (_options.ThrowOnClientEvaluationWarning)
            {
                warnings.Throw(CoreEventId.FirstWithoutOrderByAndFilterWarning);
            }
            
            // Apply custom warning configuration
            _options.ConfigureWarnings?.Invoke(warnings);
        });
        
        // Apply custom configuration
        _options.ConfigureOptions?.Invoke(builder);
        
        return builder.Options;
    }

    /// <summary>
    /// Creates the DbContext instance.
    /// </summary>
    private TContext CreateContextInstance(DbContextOptions<TContext> options)
    {
        if (_contextFactory != null)
        {
            return _contextFactory(options);
        }
        
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    /// <summary>
    /// Disposes all created contexts.
    /// </summary>
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
                context.Dispose();
            }
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Static helper class for creating in-memory contexts quickly.
/// </summary>
/// <example>
/// <code>
/// // Quick context creation for simple tests
/// using var context = InMemoryDbContextHelper.CreateContext&lt;MyDbContext&gt;();
/// </code>
/// </example>
public static class InMemoryDbContextHelper
{
    /// <summary>
    /// Creates a simple in-memory context with default options.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <returns>A new in-memory DbContext.</returns>
    public static TContext CreateContext<TContext>()
        where TContext : DbContext
    {
        using var factory = new InMemoryDbContextFactory<TContext>();
        return factory.CreateContext();
    }

    /// <summary>
    /// Creates a simple in-memory context with the database created.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <returns>A new in-memory DbContext with EnsureCreated called.</returns>
    public static TContext CreateContextWithDatabase<TContext>()
        where TContext : DbContext
    {
        using var factory = new InMemoryDbContextFactory<TContext>();
        return factory.CreateContextWithDatabase();
    }

    /// <summary>
    /// Creates DbContextOptions for in-memory testing.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="databaseName">Optional database name. If null, a unique name is generated.</param>
    /// <returns>The configured options.</returns>
    public static DbContextOptions<TContext> CreateOptions<TContext>(string? databaseName = null)
        where TContext : DbContext
    {
        var effectiveName = databaseName ?? $"InMemoryTestDb_{Guid.NewGuid():N}";
        
        return new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(effectiveName)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }
}

