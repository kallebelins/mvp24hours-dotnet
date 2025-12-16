//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations
{
    /// <summary>
    /// Interface for MongoDB database migrations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to define schema migrations for MongoDB.
    /// Migrations are executed in order of their version number.
    /// </para>
    /// <para>
    /// Migrations can perform operations like:
    /// <list type="bullet">
    ///   <item>Creating or dropping collections</item>
    ///   <item>Creating or dropping indexes</item>
    ///   <item>Modifying document structure</item>
    ///   <item>Data transformations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AddCustomerEmailIndex : IMongoDbMigration
    /// {
    ///     public int Version => 1;
    ///     public string Description => "Add email index to Customers collection";
    ///     
    ///     public async Task UpAsync(IMongoDatabase database, CancellationToken ct)
    ///     {
    ///         var collection = database.GetCollection&lt;Customer&gt;("Customers");
    ///         await collection.Indexes.CreateOneAsync(
    ///             new CreateIndexModel&lt;Customer&gt;(
    ///                 Builders&lt;Customer&gt;.IndexKeys.Ascending(c => c.Email),
    ///                 new CreateIndexOptions { Unique = true }),
    ///             cancellationToken: ct);
    ///     }
    ///     
    ///     public async Task DownAsync(IMongoDatabase database, CancellationToken ct)
    ///     {
    ///         var collection = database.GetCollection&lt;Customer&gt;("Customers");
    ///         await collection.Indexes.DropOneAsync("Email_1", ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IMongoDbMigration
    {
        /// <summary>
        /// Gets the version number of this migration.
        /// Migrations are executed in ascending order by version.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Gets a human-readable description of what this migration does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Applies the migration (upgrade).
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reverts the migration (downgrade).
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default);
    }
}

