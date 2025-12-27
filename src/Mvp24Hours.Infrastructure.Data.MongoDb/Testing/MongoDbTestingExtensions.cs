//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// Extension methods for configuring MongoDB testing services.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify the setup of in-memory MongoDB and fake repositories
/// for unit and integration testing scenarios.
/// </para>
/// </remarks>
public static class MongoDbTestingExtensions
{
    #region Fake Repositories

    /// <summary>
    /// Adds fake repository implementations for unit testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="MongoRepositoryFake{TEntity}"/> and <see cref="MongoUnitOfWorkFake"/>
    /// for unit testing without MongoDB dependencies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursMongoFakeRepository();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWork&gt;();
    /// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoFakeRepository(this IServiceCollection services)
    {
        services.AddSingleton<MongoUnitOfWorkFake>();
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<MongoUnitOfWorkFake>());
        services.AddSingleton(typeof(IRepository<>), typeof(MongoRepositoryFake<>));

        return services;
    }

    /// <summary>
    /// Adds fake async repository implementations for unit testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="MongoRepositoryFakeAsync{TEntity}"/> and <see cref="MongoUnitOfWorkFakeAsync"/>
    /// for unit testing without MongoDB dependencies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursMongoFakeRepositoryAsync();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWorkAsync&gt;();
    /// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoFakeRepositoryAsync(this IServiceCollection services)
    {
        services.AddSingleton<MongoUnitOfWorkFakeAsync>();
        services.AddSingleton<IUnitOfWorkAsync>(sp => sp.GetRequiredService<MongoUnitOfWorkFakeAsync>());
        services.AddSingleton(typeof(IRepositoryAsync<>), typeof(MongoRepositoryFakeAsync<>));

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
    /// services.AddMvp24HoursMongoFakeRepositoryWithData&lt;Customer&gt;(repo =>
    /// {
    ///     repo.SeedData(entities =>
    ///     {
    ///         entities.Add(new Customer { Id = ObjectId.GenerateNewId(), Name = "Test Customer" });
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoFakeRepositoryWithData<TEntity>(
        this IServiceCollection services,
        Action<MongoRepositoryFake<TEntity>> seedAction)
        where TEntity : class, IEntityBase
    {
        var repository = new MongoRepositoryFake<TEntity>();
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
    public static IServiceCollection AddMvp24HoursMongoFakeRepositoryAsyncWithData<TEntity>(
        this IServiceCollection services,
        Action<MongoRepositoryFakeAsync<TEntity>> seedAction)
        where TEntity : class, IEntityBase
    {
        var repository = new MongoRepositoryFakeAsync<TEntity>();
        seedAction?.Invoke(repository);

        services.AddSingleton<IRepositoryAsync<TEntity>>(repository);
        services.AddSingleton(repository);

        return services;
    }

    #endregion

    #region In-Memory Provider

    /// <summary>
    /// Adds an in-memory MongoDB provider for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for the in-memory options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursMongoInMemoryProvider(options =>
    /// {
    ///     options.DatabaseNamePrefix = "MyTestDb";
    ///     options.UseUniqueDatabaseName = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoInMemoryProvider(
        this IServiceCollection services,
        Action<MongoDbInMemoryOptions>? configureOptions = null)
    {
        var options = new MongoDbInMemoryOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<MongoDbInMemoryProvider>();

        return services;
    }

    #endregion

    #region Context Factory

    /// <summary>
    /// Adds a MongoDB context factory for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="configureOptions">Optional configuration for the factory options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The factory creates isolated context instances for each test,
    /// ensuring test independence and preventing state leakage.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursMongoContextFactory("mongodb://localhost:27017", options =>
    /// {
    ///     options.UseUniqueDatabaseName = true;
    ///     options.EnableLogging = true;
    /// });
    /// 
    /// // In test
    /// var factory = serviceProvider.GetRequiredService&lt;MongoDbContextFactory&gt;();
    /// using var context = factory.CreateContext();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoContextFactory(
        this IServiceCollection services,
        string connectionString,
        Action<MongoDbInMemoryOptions>? configureOptions = null)
    {
        var options = new MongoDbInMemoryOptions
        {
            ConnectionString = connectionString
        };
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<MongoDbContextFactory>();

        return services;
    }

    /// <summary>
    /// Adds a MongoDB context factory with custom context creation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="contextFactory">Factory function to create the context.</param>
    /// <param name="configureOptions">Optional configuration for the factory options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursMongoContextFactory(
        this IServiceCollection services,
        Func<MongoDbOptions, Mvp24HoursContext> contextFactory,
        Action<MongoDbInMemoryOptions>? configureOptions = null)
    {
        var options = new MongoDbInMemoryOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<MongoDbInMemoryOptions>();
            return new MongoDbContextFactory(opts, contextFactory);
        });

        return services;
    }

    #endregion

    #region Complete Test Setup

    /// <summary>
    /// Adds complete testing infrastructure with MongoDB connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="configureOptions">Optional configuration for the options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures a complete testing setup:
    /// <list type="bullet">
    /// <item>MongoDB Context</item>
    /// <item>Async repository implementation</item>
    /// <item>Unit of Work</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursMongoTestInfrastructure("mongodb://localhost:27017");
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// // Ready for testing!
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoTestInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<MongoDbOptions>? configureOptions = null)
    {
        var databaseName = $"TestDb_{Guid.NewGuid():N}";

        services.AddMvp24HoursDbContext(options =>
        {
            options.ConnectionString = connectionString;
            options.DatabaseName = databaseName;
            options.EnableCommandLogging = true;
            configureOptions?.Invoke(options);
        });

        services.AddMvp24HoursRepositoryAsync();

        return services;
    }

    /// <summary>
    /// Adds complete testing infrastructure with seeded data.
    /// </summary>
    /// <typeparam name="TSeeder">The data seeder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursMongoTestInfrastructureWithSeeder&lt;CustomerSeeder&gt;("mongodb://localhost:27017");
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoTestInfrastructureWithSeeder<TSeeder>(
        this IServiceCollection services,
        string connectionString)
        where TSeeder : class, IMongoDataSeeder, new()
    {
        services.AddMvp24HoursMongoTestInfrastructure(connectionString);
        services.AddSingleton<IMongoDataSeeder, TSeeder>();

        return services;
    }

    /// <summary>
    /// Adds fake testing infrastructure (no MongoDB required).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures a complete fake testing setup that doesn't
    /// require a MongoDB instance:
    /// <list type="bullet">
    /// <item>In-memory fake repositories</item>
    /// <item>Fake Unit of Work</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddMvp24HoursMongoFakeTestInfrastructure();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// // Ready for unit testing without MongoDB!
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursMongoFakeTestInfrastructure(this IServiceCollection services)
    {
        services.AddMvp24HoursMongoFakeRepository();
        services.AddMvp24HoursMongoFakeRepositoryAsync();

        return services;
    }

    #endregion
}

