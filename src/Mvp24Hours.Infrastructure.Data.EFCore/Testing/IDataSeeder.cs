//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// Interface for seeding test data into a DbContext.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
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
/// public class CustomerSeeder : IDataSeeder&lt;AppDbContext&gt;
/// {
///     public void Seed(AppDbContext context)
///     {
///         context.Customers.AddRange(
///             new Customer { Id = 1, Name = "Customer 1", Active = true },
///             new Customer { Id = 2, Name = "Customer 2", Active = true },
///             new Customer { Id = 3, Name = "Inactive Customer", Active = false }
///         );
///     }
/// }
/// 
/// // Usage
/// var factory = new InMemoryDbContextFactory&lt;AppDbContext&gt;();
/// using var context = factory.CreateContextWithData&lt;CustomerSeeder&gt;();
/// </code>
/// </example>
public interface IDataSeeder<in TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Seeds test data into the context.
    /// </summary>
    /// <param name="context">The DbContext to seed data into.</param>
    /// <remarks>
    /// This method should add entities to the context but NOT call SaveChanges.
    /// The caller is responsible for saving changes after seeding.
    /// </remarks>
    void Seed(TContext context);
}

/// <summary>
/// Interface for asynchronously seeding test data into a DbContext.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// Use this interface when your seeding logic requires asynchronous operations,
/// such as loading test data from external sources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomerSeederAsync : IDataSeederAsync&lt;AppDbContext&gt;
/// {
///     public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
///     {
///         var customers = await LoadTestDataFromFileAsync();
///         context.Customers.AddRange(customers);
///     }
/// }
/// </code>
/// </example>
public interface IDataSeederAsync<in TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Seeds test data into the context asynchronously.
    /// </summary>
    /// <param name="context">The DbContext to seed data into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedAsync(TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic interface for seeding test data for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type to seed.</typeparam>
/// <remarks>
/// <para>
/// This interface is useful when you want to create entity-specific seeders
/// that can be reused across different DbContext types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ProductSeeder : IEntitySeeder&lt;Product&gt;
/// {
///     public IEnumerable&lt;Product&gt; GetSeedData()
///     {
///         return new[]
///         {
///             new Product { Id = 1, Name = "Product A", Price = 100 },
///             new Product { Id = 2, Name = "Product B", Price = 200 }
///         };
///     }
/// }
/// </code>
/// </example>
public interface IEntitySeeder<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets the seed data for the entity type.
    /// </summary>
    /// <returns>A collection of entities to seed.</returns>
    System.Collections.Generic.IEnumerable<TEntity> GetSeedData();
}

/// <summary>
/// Composite seeder that combines multiple seeders.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// Use this class to compose multiple seeders together for complex test scenarios
/// that require data from multiple entity types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var compositeSeeder = new CompositeDataSeeder&lt;AppDbContext&gt;(
///     new CustomerSeeder(),
///     new ProductSeeder(),
///     new OrderSeeder()
/// );
/// 
/// using var context = factory.CreateContextWithData(compositeSeeder);
/// </code>
/// </example>
public class CompositeDataSeeder<TContext> : IDataSeeder<TContext>
    where TContext : DbContext
{
    private readonly IDataSeeder<TContext>[] _seeders;

    /// <summary>
    /// Initializes a new instance with the specified seeders.
    /// </summary>
    /// <param name="seeders">The seeders to compose.</param>
    public CompositeDataSeeder(params IDataSeeder<TContext>[] seeders)
    {
        _seeders = seeders ?? throw new ArgumentNullException(nameof(seeders));
    }

    /// <inheritdoc />
    public void Seed(TContext context)
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
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// Use this class when you need a simple seeder that can be defined inline
/// without creating a separate class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var seeder = new ActionDataSeeder&lt;AppDbContext&gt;(ctx =>
/// {
///     ctx.Customers.Add(new Customer { Name = "Test Customer" });
/// });
/// 
/// using var context = factory.CreateContextWithData(seeder);
/// </code>
/// </example>
public class ActionDataSeeder<TContext> : IDataSeeder<TContext>
    where TContext : DbContext
{
    private readonly Action<TContext> _seedAction;

    /// <summary>
    /// Initializes a new instance with the specified seed action.
    /// </summary>
    /// <param name="seedAction">The action to execute for seeding.</param>
    public ActionDataSeeder(Action<TContext> seedAction)
    {
        _seedAction = seedAction ?? throw new ArgumentNullException(nameof(seedAction));
    }

    /// <inheritdoc />
    public void Seed(TContext context)
    {
        _seedAction(context);
    }
}

