//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// Interface for seeding test data into a MongoDB context.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create reusable test data seeders that can
/// be used across multiple tests to set up consistent test scenarios.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Reusable across multiple tests</item>
/// <item>Encapsulates test data creation logic</item>
/// <item>Supports both sync and async seeding</item>
/// <item>Can be composed with other seeders</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomerSeeder : IMongoDataSeeder
/// {
///     public void Seed(Mvp24HoursContext context)
///     {
///         var collection = context.Set&lt;Customer&gt;();
///         collection.InsertMany(new[]
///         {
///             new Customer { Id = ObjectId.GenerateNewId(), Name = "Customer 1", Active = true },
///             new Customer { Id = ObjectId.GenerateNewId(), Name = "Customer 2", Active = true },
///             new Customer { Id = ObjectId.GenerateNewId(), Name = "Inactive Customer", Active = false }
///         });
///     }
/// }
/// 
/// // Usage
/// var factory = new MongoDbContextFactory();
/// using var context = factory.CreateContextWithData&lt;CustomerSeeder&gt;();
/// </code>
/// </example>
public interface IMongoDataSeeder
{
    /// <summary>
    /// Seeds test data into the context.
    /// </summary>
    /// <param name="context">The MongoDB context to seed data into.</param>
    /// <remarks>
    /// This method should add entities to the context.
    /// </remarks>
    void Seed(Mvp24HoursContext context);
}

/// <summary>
/// Interface for asynchronously seeding test data into a MongoDB context.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when your seeding logic requires asynchronous operations,
/// such as loading test data from external sources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomerSeederAsync : IMongoDataSeederAsync
/// {
///     public async Task SeedAsync(Mvp24HoursContext context, CancellationToken cancellationToken = default)
///     {
///         var customers = await LoadTestDataFromFileAsync();
///         var collection = context.Set&lt;Customer&gt;();
///         await collection.InsertManyAsync(customers, cancellationToken: cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IMongoDataSeederAsync
{
    /// <summary>
    /// Seeds test data into the context asynchronously.
    /// </summary>
    /// <param name="context">The MongoDB context to seed data into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedAsync(Mvp24HoursContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic interface for seeding test data for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type to seed.</typeparam>
/// <remarks>
/// <para>
/// This interface is useful when you want to create entity-specific seeders
/// that can be reused across different MongoDB contexts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ProductSeeder : IMongoEntitySeeder&lt;Product&gt;
/// {
///     public IEnumerable&lt;Product&gt; GetSeedData()
///     {
///         return new[]
///         {
///             new Product { Id = ObjectId.GenerateNewId(), Name = "Product A", Price = 100 },
///             new Product { Id = ObjectId.GenerateNewId(), Name = "Product B", Price = 200 }
///         };
///     }
/// }
/// </code>
/// </example>
public interface IMongoEntitySeeder<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets the seed data for the entity type.
    /// </summary>
    /// <returns>A collection of entities to seed.</returns>
    IEnumerable<TEntity> GetSeedData();
}

/// <summary>
/// Composite seeder that combines multiple seeders.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to compose multiple seeders together for complex test scenarios
/// that require data from multiple entity types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var compositeSeeder = new CompositeMongoDataSeeder(
///     new CustomerSeeder(),
///     new ProductSeeder(),
///     new OrderSeeder()
/// );
/// 
/// using var context = factory.CreateContextWithData(compositeSeeder);
/// </code>
/// </example>
public class CompositeMongoDataSeeder : IMongoDataSeeder
{
    private readonly IMongoDataSeeder[] _seeders;

    /// <summary>
    /// Initializes a new instance with the specified seeders.
    /// </summary>
    /// <param name="seeders">The seeders to compose.</param>
    public CompositeMongoDataSeeder(params IMongoDataSeeder[] seeders)
    {
        _seeders = seeders ?? throw new ArgumentNullException(nameof(seeders));
    }

    /// <inheritdoc />
    public void Seed(Mvp24HoursContext context)
    {
        foreach (var seeder in _seeders)
        {
            seeder.Seed(context);
        }
    }
}

/// <summary>
/// Action-based seeder for simple inline seeding.
/// </summary>
/// <remarks>
/// <para>
/// Use this class when you need a simple seeder that can be defined inline
/// without creating a separate class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var seeder = new ActionMongoDataSeeder(ctx =>
/// {
///     ctx.Set&lt;Customer&gt;().InsertOne(new Customer { Name = "Test Customer" });
/// });
/// 
/// using var context = factory.CreateContextWithData(seeder);
/// </code>
/// </example>
public class ActionMongoDataSeeder : IMongoDataSeeder
{
    private readonly Action<Mvp24HoursContext> _seedAction;

    /// <summary>
    /// Initializes a new instance with the specified seed action.
    /// </summary>
    /// <param name="seedAction">The action to execute for seeding.</param>
    public ActionMongoDataSeeder(Action<Mvp24HoursContext> seedAction)
    {
        _seedAction = seedAction ?? throw new ArgumentNullException(nameof(seedAction));
    }

    /// <inheritdoc />
    public void Seed(Mvp24HoursContext context)
    {
        _seedAction(context);
    }
}

/// <summary>
/// Async action-based seeder for simple inline seeding.
/// </summary>
/// <remarks>
/// <para>
/// Use this class when you need a simple async seeder that can be defined inline
/// without creating a separate class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var seeder = new ActionMongoDataSeederAsync(async (ctx, ct) =>
/// {
///     await ctx.Set&lt;Customer&gt;().InsertOneAsync(new Customer { Name = "Test Customer" }, ct);
/// });
/// 
/// using var context = await factory.CreateContextWithDataAsync(seeder);
/// </code>
/// </example>
public class ActionMongoDataSeederAsync : IMongoDataSeederAsync
{
    private readonly Func<Mvp24HoursContext, CancellationToken, Task> _seedAction;

    /// <summary>
    /// Initializes a new instance with the specified seed action.
    /// </summary>
    /// <param name="seedAction">The action to execute for seeding.</param>
    public ActionMongoDataSeederAsync(Func<Mvp24HoursContext, CancellationToken, Task> seedAction)
    {
        _seedAction = seedAction ?? throw new ArgumentNullException(nameof(seedAction));
    }

    /// <inheritdoc />
    public async Task SeedAsync(Mvp24HoursContext context, CancellationToken cancellationToken = default)
    {
        await _seedAction(context, cancellationToken);
    }
}

