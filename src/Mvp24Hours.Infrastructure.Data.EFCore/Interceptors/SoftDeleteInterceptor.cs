//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that converts physical deletes to soft deletes for entities implementing ISoftDeletable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor intercepts delete operations and converts them to updates,
    /// setting the IsDeleted flag, DeletedAt timestamp, and DeletedBy user instead of
    /// physically removing the record from the database.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> This interceptor works in conjunction with global query filters.
    /// You should also configure query filters to exclude soft-deleted entities from normal queries:
    /// </para>
    /// <code>
    /// modelBuilder.Entity&lt;Product&gt;().HasQueryFilter(p => !p.IsDeleted);
    /// // Or use the extension method:
    /// modelBuilder.ApplyGlobalFilters&lt;ISoftDeletable&gt;(e => !e.IsDeleted);
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DbContext configuration:
    /// services.AddDbContext&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new SoftDeleteInterceptor(currentUserProvider, systemClock));
    /// });
    /// </code>
    /// </example>
    public class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IClock _clock;
        private readonly string _defaultUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
        /// </summary>
        /// <param name="currentUserProvider">Optional provider for the current user. If null, "System" is used.</param>
        /// <param name="clock">Optional clock for getting current time. If null, UTC now is used.</param>
        /// <param name="defaultUser">Default user identifier when no user is available. Defaults to "System".</param>
        public SoftDeleteInterceptor(
            ICurrentUserProvider currentUserProvider = null,
            IClock clock = null,
            string defaultUser = "System")
        {
            _currentUserProvider = currentUserProvider;
            _clock = clock;
            _defaultUser = defaultUser ?? "System";
        }

        /// <inheritdoc />
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ConvertDeleteToSoftDelete(DbContext context)
        {
            if (context == null) return;

            var now = GetCurrentTime();
            var currentUser = GetCurrentUser();

            var entriesToSoftDelete = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entriesToSoftDelete)
            {
                if (entry.Entity is ISoftDeletable softDeletable)
                {
                    ApplySoftDelete(entry, softDeletable, now, currentUser);
                }
                else
                {
                    // Try to apply soft delete for generic ISoftDeletable<T>
                    TryApplyGenericSoftDelete(entry, now, currentUser);
                }
            }
        }

        private void ApplySoftDelete(
            EntityEntry entry,
            ISoftDeletable entity,
            DateTime now,
            string currentUser)
        {
            // Change state from Deleted to Modified
            entry.State = EntityState.Modified;

            // Set soft delete properties
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = currentUser;

            TelemetryHelper.Execute(Core.Enums.Infrastructure.TelemetryLevels.Verbose,
                $"softdelete-interceptor-deleted-{entry.Entity.GetType().Name}");
        }

        private void TryApplyGenericSoftDelete(EntityEntry entry, DateTime now, string currentUser)
        {
            var entityType = entry.Entity.GetType();
            var interfaces = entityType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ISoftDeletable<>))
                {
                    // Change state from Deleted to Modified
                    entry.State = EntityState.Modified;

                    // Set soft delete properties
                    SetPropertyValue(entry, "IsDeleted", true);
                    SetPropertyValue(entry, "DeletedAt", now);
                    SetPropertyValue(entry, "DeletedBy", currentUser);

                    TelemetryHelper.Execute(Core.Enums.Infrastructure.TelemetryLevels.Verbose,
                        $"softdelete-interceptor-deleted-{entry.Entity.GetType().Name}");
                    break;
                }
            }
        }

        private static void SetPropertyValue(EntityEntry entry, string propertyName, object value)
        {
            var property = entry.Property(propertyName);
            if (property != null)
            {
                property.CurrentValue = value;
            }
        }

        private DateTime GetCurrentTime()
        {
            return _clock?.UtcNow ?? DateTime.UtcNow;
        }

        private string GetCurrentUser()
        {
            if (_currentUserProvider != null && !string.IsNullOrEmpty(_currentUserProvider.UserId))
            {
                return _currentUserProvider.UserId;
            }
            return _defaultUser;
        }
    }

    /// <summary>
    /// Extension methods for configuring soft delete global query filters.
    /// </summary>
    public static class SoftDeleteModelBuilderExtensions
    {
        /// <summary>
        /// Applies global query filter to exclude soft-deleted entities for all entities implementing ISoftDeletable.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The model builder for chaining.</returns>
        /// <example>
        /// <code>
        /// protected override void OnModelCreating(ModelBuilder modelBuilder)
        /// {
        ///     base.OnModelCreating(modelBuilder);
        ///     modelBuilder.ApplySoftDeleteGlobalFilter();
        /// }
        /// </code>
        /// </example>
        public static ModelBuilder ApplySoftDeleteGlobalFilter(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Check if entity implements ISoftDeletable
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                    var falseConstant = System.Linq.Expressions.Expression.Constant(false);
                    var comparison = System.Linq.Expressions.Expression.Equal(property, falseConstant);
                    var lambda = System.Linq.Expressions.Expression.Lambda(comparison, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            return modelBuilder;
        }
    }
}

