//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that automatically increments version counters for entities implementing IVersionedEntityWithCounter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor works with entities that use a numeric version counter for optimistic concurrency.
    /// For entities using byte[] RowVersion (SQL Server ROWVERSION/TIMESTAMP), EF Core handles this automatically.
    /// </para>
    /// <para>
    /// The version counter is automatically incremented on every modification, allowing detection
    /// of concurrent modifications when combined with concurrency tokens.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Entity implementation:
    /// public class Document : IVersionedEntityWithCounter
    /// {
    ///     public int Id { get; set; }
    ///     public string Content { get; set; }
    ///     public long Version { get; set; }
    /// }
    /// 
    /// // Configure as concurrency token in OnModelCreating:
    /// modelBuilder.Entity&lt;Document&gt;()
    ///     .Property(d => d.Version)
    ///     .IsConcurrencyToken();
    /// 
    /// // Register the interceptor:
    /// services.AddDbContext&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new ConcurrencyInterceptor());
    /// });
    /// </code>
    /// </example>
    public class ConcurrencyInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyInterceptor"/> class.
        /// </summary>
        public ConcurrencyInterceptor()
        {
        }

        /// <inheritdoc />
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            IncrementVersionCounters(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            IncrementVersionCounters(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void IncrementVersionCounters(DbContext context)
        {
            if (context == null) return;

            var modifiedEntries = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .ToList();

            foreach (var entry in modifiedEntries)
            {
                if (entry.Entity is IVersionedEntityWithCounter versionedEntity)
                {
                    IncrementVersion(entry, versionedEntity);
                }
            }
        }

        private void IncrementVersion(EntityEntry entry, IVersionedEntityWithCounter entity)
        {
            if (entry.State == EntityState.Added)
            {
                // New entities start at version 1
                entity.Version = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Increment version on modification
                entity.Version++;
            }

            TelemetryHelper.Execute(Core.Enums.Infrastructure.TelemetryLevels.Verbose,
                $"concurrency-interceptor-version-{entry.Entity.GetType().Name}-v{entity.Version}");
        }
    }

    /// <summary>
    /// Extension methods for configuring concurrency tokens in EF Core.
    /// </summary>
    public static class ConcurrencyModelBuilderExtensions
    {
        /// <summary>
        /// Automatically configures concurrency tokens for all entities implementing IVersionedEntity or IVersionedEntityWithCounter.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The model builder for chaining.</returns>
        /// <example>
        /// <code>
        /// protected override void OnModelCreating(ModelBuilder modelBuilder)
        /// {
        ///     base.OnModelCreating(modelBuilder);
        ///     modelBuilder.ApplyConcurrencyTokens();
        /// }
        /// </code>
        /// </example>
        public static ModelBuilder ApplyConcurrencyTokens(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Configure byte[] RowVersion for IVersionedEntity
                if (typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IVersionedEntity.RowVersion))
                        .IsRowVersion();
                }

                // Configure long Version as concurrency token for IVersionedEntityWithCounter
                if (typeof(IVersionedEntityWithCounter).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IVersionedEntityWithCounter.Version))
                        .IsConcurrencyToken();
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Configures a RowVersion property for optimistic concurrency using SQL Server ROWVERSION/TIMESTAMP.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="builder">The entity type builder.</param>
        /// <returns>The entity type builder for chaining.</returns>
        public static EntityTypeBuilder<TEntity> HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : class, IVersionedEntity
        {
            builder.Property(e => e.RowVersion).IsRowVersion();
            return builder;
        }

        /// <summary>
        /// Configures a Version counter property as a concurrency token.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="builder">The entity type builder.</param>
        /// <returns>The entity type builder for chaining.</returns>
        public static EntityTypeBuilder<TEntity> HasVersionCounter<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : class, IVersionedEntityWithCounter
        {
            builder.Property(e => e.Version).IsConcurrencyToken();
            return builder;
        }
    }
}

