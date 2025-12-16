//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Cqrs
{
    /// <summary>
    /// Marker interface for read-only DbContext.
    /// Used for CQRS query operations where write operations should be prevented.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>CQRS Pattern:</strong>
    /// In Command Query Responsibility Segregation, reads and writes are separated.
    /// This interface marks a DbContext as read-only, which should:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Use AsNoTracking by default for better performance</description></item>
    /// <item><description>Connect to read replicas when available</description></item>
    /// <item><description>Throw exceptions on SaveChanges attempts</description></item>
    /// </list>
    /// </remarks>
    public interface IReadDbContext : IDisposable
    {
        /// <summary>
        /// Gets a DbSet for querying entities of the specified type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>A DbSet configured for read-only operations.</returns>
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
    }

    /// <summary>
    /// Marker interface for write DbContext.
    /// Used for CQRS command operations where domain events may be raised.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>CQRS Pattern:</strong>
    /// The write context is used for command operations and includes:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Full change tracking</description></item>
    /// <item><description>Domain event collection and dispatch</description></item>
    /// <item><description>Connection to primary database</description></item>
    /// </list>
    /// </remarks>
    public interface IWriteDbContext : IDisposable
    {
        /// <summary>
        /// Gets a DbSet for the specified entity type.
        /// </summary>
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        /// <summary>
        /// Gets the change tracker for this context.
        /// </summary>
        ChangeTracker ChangeTracker { get; }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        int SaveChanges();

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base class for read-only DbContext in CQRS pattern.
    /// Configures the context for optimal read performance and prevents write operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Features:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>AsNoTracking enabled by default</description></item>
    /// <item><description>SaveChanges throws InvalidOperationException</description></item>
    /// <item><description>Optimized for read replicas</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AppReadDbContext : ReadDbContextBase
    /// {
    ///     public AppReadDbContext(DbContextOptions&lt;AppReadDbContext&gt; options)
    ///         : base(options) { }
    ///     
    ///     public DbSet&lt;Order&gt; Orders => Set&lt;Order&gt;();
    ///     public DbSet&lt;Customer&gt; Customers => Set&lt;Customer&gt;();
    /// }
    /// </code>
    /// </example>
    public abstract class ReadDbContextBase : DbContext, IReadDbContext
    {
        /// <summary>
        /// Creates a new read-only DbContext instance.
        /// </summary>
        /// <param name="options">The DbContext options.</param>
        protected ReadDbContextBase(DbContextOptions options)
            : base(options)
        {
            // Disable change tracking for read-only context
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        /// <summary>
        /// Throws InvalidOperationException as this is a read-only context.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        public override int SaveChanges()
        {
            throw new InvalidOperationException(
                "This is a read-only DbContext. Use IWriteDbContext for write operations.");
        }

        /// <summary>
        /// Throws InvalidOperationException as this is a read-only context.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new InvalidOperationException(
                "This is a read-only DbContext. Use IWriteDbContext for write operations.");
        }

        /// <summary>
        /// Throws InvalidOperationException as this is a read-only context.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        public override System.Threading.Tasks.Task<int> SaveChangesAsync(
            System.Threading.CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "This is a read-only DbContext. Use IWriteDbContext for write operations.");
        }

        /// <summary>
        /// Throws InvalidOperationException as this is a read-only context.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        public override System.Threading.Tasks.Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            System.Threading.CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "This is a read-only DbContext. Use IWriteDbContext for write operations.");
        }
    }

    /// <summary>
    /// Base class for write DbContext in CQRS pattern.
    /// Configures the context for command operations with domain event support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Features:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Full change tracking enabled</description></item>
    /// <item><description>Domain event collection support</description></item>
    /// <item><description>Connects to primary database</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AppWriteDbContext : WriteDbContextBase
    /// {
    ///     public AppWriteDbContext(DbContextOptions&lt;AppWriteDbContext&gt; options)
    ///         : base(options) { }
    ///     
    ///     public DbSet&lt;Order&gt; Orders => Set&lt;Order&gt;();
    ///     public DbSet&lt;Customer&gt; Customers => Set&lt;Customer&gt;();
    /// }
    /// </code>
    /// </example>
    public abstract class WriteDbContextBase : DbContext, IWriteDbContext
    {
        /// <summary>
        /// Creates a new write DbContext instance.
        /// </summary>
        /// <param name="options">The DbContext options.</param>
        protected WriteDbContextBase(DbContextOptions options)
            : base(options)
        {
            // Enable full change tracking for write context
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}

