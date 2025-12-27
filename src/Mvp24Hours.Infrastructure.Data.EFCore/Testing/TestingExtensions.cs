//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Extensions;
using System;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// Extension methods for configuring testing services.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify the setup of in-memory databases and fake repositories
/// for unit and integration testing scenarios.
/// </para>
/// </remarks>
public static class TestingExtensions
{
    #region In-Memory DbContext

    /// <summary>
    /// Adds an in-memory DbContext for testing.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">Optional database name. If null, a unique name is generated.</param>
    /// <param name="configureOptions">Optional additional configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures an in-memory database suitable for testing:
    /// <list type="bullet">
    /// <item>Unique database name per test (if databaseName is null)</item>
    /// <item>Sensitive data logging enabled for debugging</item>
    /// <item>Transaction warnings suppressed (in-memory doesn't support transactions)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursInMemoryDbContext&lt;AppDbContext&gt;();
    /// services.AddMvp24HoursRepositoryAsync();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var context = serviceProvider.GetRequiredService&lt;AppDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursInMemoryDbContext<TContext>(
        this IServiceCollection services,
        string? databaseName = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        var effectiveName = databaseName ?? $"InMemoryTestDb_{StringHelper.GenerateKey(10)}";

        services.AddDbContext<TContext>(options =>
        {
            options.UseInMemoryDatabase(effectiveName);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
            });

            configureOptions?.Invoke(options);
        });

        // Also register as DbContext for compatibility with Mvp24Hours repositories
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        return services;
    }

    /// <summary>
    /// Adds an in-memory DbContext for testing with unique database name per test.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseNamePrefix">Prefix for the database name.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Each call creates a database with a unique name based on the prefix,
    /// ensuring test isolation.
    /// </remarks>
    public static IServiceCollection AddMvp24HoursUniqueInMemoryDbContext<TContext>(
        this IServiceCollection services,
        string databaseNamePrefix = "TestDb")
        where TContext : DbContext
    {
        var uniqueName = $"{databaseNamePrefix}_{Guid.NewGuid():N}";
        return services.AddMvp24HoursInMemoryDbContext<TContext>(uniqueName);
    }

    #endregion

    #region Fake Repositories

    /// <summary>
    /// Adds fake repository implementations for unit testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="RepositoryFake{TEntity}"/> and <see cref="UnitOfWorkFake"/>
    /// for unit testing without database dependencies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursFakeRepository();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWork&gt;();
    /// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursFakeRepository(this IServiceCollection services)
    {
        services.AddSingleton<UnitOfWorkFake>();
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWorkFake>());
        services.AddSingleton(typeof(IRepository<>), typeof(RepositoryFake<>));

        return services;
    }

    /// <summary>
    /// Adds fake async repository implementations for unit testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="RepositoryFakeAsync{TEntity}"/> and <see cref="UnitOfWorkFakeAsync"/>
    /// for unit testing without database dependencies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursFakeRepositoryAsync();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWorkAsync&gt;();
    /// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursFakeRepositoryAsync(this IServiceCollection services)
    {
        services.AddSingleton<UnitOfWorkFakeAsync>();
        services.AddSingleton<IUnitOfWorkAsync>(sp => sp.GetRequiredService<UnitOfWorkFakeAsync>());
        services.AddSingleton(typeof(IRepositoryAsync<>), typeof(RepositoryFakeAsync<>));

        return services;
    }

    /// <summary>
    /// Adds a pre-configured fake repository with initial data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="seedAction">Action to seed initial data.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursFakeRepositoryWithData&lt;Customer&gt;(repo =>
    /// {
    ///     repo.SeedData(entities =>
    ///     {
    ///         entities.Add(new Customer { Id = 1, Name = "Test Customer" });
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursFakeRepositoryWithData<TEntity>(
        this IServiceCollection services,
        Action<RepositoryFake<TEntity>> seedAction)
        where TEntity : class, IEntityBase
    {
        var repository = new RepositoryFake<TEntity>();
        seedAction?.Invoke(repository);

        services.AddSingleton<IRepository<TEntity>>(repository);
        services.AddSingleton(repository);

        return services;
    }

    /// <summary>
    /// Adds a pre-configured async fake repository with initial data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="seedAction">Action to seed initial data.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursFakeRepositoryAsyncWithData<TEntity>(
        this IServiceCollection services,
        Action<RepositoryFakeAsync<TEntity>> seedAction)
        where TEntity : class, IEntityBase
    {
        var repository = new RepositoryFakeAsync<TEntity>();
        seedAction?.Invoke(repository);

        services.AddSingleton<IRepositoryAsync<TEntity>>(repository);
        services.AddSingleton(repository);

        return services;
    }

    #endregion

    #region Test DbContext Factory

    /// <summary>
    /// Adds an in-memory test DbContext factory.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for the factory.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The factory creates isolated DbContext instances for each test,
    /// ensuring test independence and preventing state leakage.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursTestDbContextFactory&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.CreateNewDatabasePerTest = true;
    ///     options.EnableSensitiveDataLogging = true;
    /// });
    /// 
    /// // In test
    /// var factory = serviceProvider.GetRequiredService&lt;ITestDbContextFactory&lt;AppDbContext&gt;&gt;();
    /// using var context = factory.CreateContext();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursTestDbContextFactory<TContext>(
        this IServiceCollection services,
        Action<TestDbContextFactoryOptions>? configureOptions = null)
        where TContext : DbContext
    {
        var options = new TestDbContextFactoryOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ITestDbContextFactory<TContext>, InMemoryTestDbContextFactory<TContext>>();

        return services;
    }

    /// <summary>
    /// Adds an in-memory DbContext factory for quick context creation.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for the factory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursInMemoryDbContextFactory<TContext>(
        this IServiceCollection services,
        Action<InMemoryDbContextOptions>? configureOptions = null)
        where TContext : DbContext
    {
        var options = new InMemoryDbContextOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<InMemoryDbContextFactory<TContext>>();

        return services;
    }

    #endregion

    #region Complete Test Setup

    /// <summary>
    /// Adds complete testing infrastructure with in-memory database and repositories.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">Optional database name.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures a complete testing setup:
    /// <list type="bullet">
    /// <item>In-memory DbContext</item>
    /// <item>Async repository implementation</item>
    /// <item>Unit of Work</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursTestInfrastructure&lt;AppDbContext&gt;();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// // Ready for testing!
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursTestInfrastructure<TContext>(
        this IServiceCollection services,
        string? databaseName = null)
        where TContext : DbContext
    {
        services.AddMvp24HoursInMemoryDbContext<TContext>(databaseName);
        services.AddMvp24HoursDbContext<TContext>();
        services.AddMvp24HoursRepositoryAsync();

        return services;
    }

    /// <summary>
    /// Adds complete testing infrastructure with seeded data.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TSeeder">The data seeder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursTestInfrastructureWithSeeder&lt;AppDbContext, CustomerSeeder&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursTestInfrastructureWithSeeder<TContext, TSeeder>(
        this IServiceCollection services)
        where TContext : DbContext
        where TSeeder : class, IDataSeeder<TContext>, new()
    {
        services.AddMvp24HoursTestInfrastructure<TContext>();
        services.AddSingleton<IDataSeeder<TContext>, TSeeder>();

        return services;
    }

    #endregion
}

