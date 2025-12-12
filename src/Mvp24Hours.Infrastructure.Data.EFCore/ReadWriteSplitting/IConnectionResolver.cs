//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting
{
    /// <summary>
    /// Interface for resolving the appropriate database connection based on operation type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The connection resolver determines which connection string to use:
    /// <list type="bullet">
    /// <item><strong>Read operations</strong> - Select from read replicas</item>
    /// <item><strong>Write operations</strong> - Use primary database</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IConnectionResolver
    {
        /// <summary>
        /// Gets the connection string for read operations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Connection string for read operations.</returns>
        Task<string> GetReadConnectionStringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the connection string for write operations.
        /// </summary>
        /// <returns>Connection string for write operations.</returns>
        string GetWriteConnectionString();

        /// <summary>
        /// Forces the next read operation to use the primary database.
        /// Useful for read-after-write consistency.
        /// </summary>
        void ForceReadFromPrimary();

        /// <summary>
        /// Resets the force-read-from-primary flag.
        /// </summary>
        void ResetReadFromPrimary();

        /// <summary>
        /// Gets whether reads are currently forced to primary.
        /// </summary>
        bool IsReadForcedToPrimary { get; }

        /// <summary>
        /// Notifies the resolver that a write operation was performed.
        /// Used for read-after-write consistency tracking.
        /// </summary>
        void NotifyWritePerformed();
    }

    /// <summary>
    /// Marker interface for read-only database operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inject this interface when you need explicit read-only access.
    /// Read operations will be routed to read replicas.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ReportService
    /// {
    ///     private readonly IReadOnlyDbContext _readContext;
    ///     
    ///     public ReportService(IReadOnlyDbContext readContext)
    ///     {
    ///         _readContext = readContext;
    ///     }
    ///     
    ///     public async Task&lt;Report&gt; GenerateReportAsync()
    ///     {
    ///         // This query goes to a read replica
    ///         return await _readContext.Context.Reports.ToListAsync();
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IReadOnlyDbContext
    {
        /// <summary>
        /// Gets the DbContext configured for read operations.
        /// </summary>
        Microsoft.EntityFrameworkCore.DbContext Context { get; }
    }

    /// <summary>
    /// Marker interface for write database operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inject this interface when you need explicit write access.
    /// Write operations always go to the primary database.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderService
    /// {
    ///     private readonly IWriteDbContext _writeContext;
    ///     
    ///     public OrderService(IWriteDbContext writeContext)
    ///     {
    ///         _writeContext = writeContext;
    ///     }
    ///     
    ///     public async Task&lt;Order&gt; CreateOrderAsync(Order order)
    ///     {
    ///         // This operation goes to the primary database
    ///         await _writeContext.Context.Orders.AddAsync(order);
    ///         await _writeContext.Context.SaveChangesAsync();
    ///         return order;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IWriteDbContext
    {
        /// <summary>
        /// Gets the DbContext configured for write operations.
        /// </summary>
        Microsoft.EntityFrameworkCore.DbContext Context { get; }
    }
}

