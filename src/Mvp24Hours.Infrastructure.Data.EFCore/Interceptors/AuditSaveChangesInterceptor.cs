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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that automatically populates audit fields on entities implementing IAuditableEntity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor automatically sets CreatedAt/CreatedBy for new entities and
    /// ModifiedAt/ModifiedBy for modified entities during SaveChanges operations.
    /// </para>
    /// <para>
    /// The current user is obtained from <see cref="ICurrentUserProvider"/> if available,
    /// otherwise a default value ("System" or the configured default) is used.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DbContext configuration:
    /// services.AddDbContext&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider, systemClock));
    /// });
    /// 
    /// // Or register via DI:
    /// services.AddSingleton&lt;AuditSaveChangesInterceptor&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(sp.GetRequiredService&lt;AuditSaveChangesInterceptor&gt;());
    /// });
    /// </code>
    /// </example>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IClock _clock;
        private readonly string _defaultUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditSaveChangesInterceptor"/> class.
        /// </summary>
        /// <param name="currentUserProvider">Optional provider for the current user. If null, "System" is used.</param>
        /// <param name="clock">Optional clock for getting current time. If null, UTC now is used.</param>
        /// <param name="defaultUser">Default user identifier when no user is available. Defaults to "System".</param>
        public AuditSaveChangesInterceptor(
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
            ApplyAuditInformation(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditInformation(DbContext context)
        {
            if (context == null) return;

            var now = GetCurrentTime();
            var currentUser = GetCurrentUser();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IAuditableEntity auditableEntity)
                {
                    ApplyAuditForStringUser(entry, auditableEntity, now, currentUser);
                }
                else
                {
                    // Try to apply audit for generic IAuditableEntity<T>
                    TryApplyGenericAudit(entry, now, currentUser);
                }

                // Also handle legacy IEntityDateLog if present
                if (entry.Entity is IEntityDateLog entityDateLog)
                {
                    ApplyEntityDateLog(entry, entityDateLog, now);
                }
            }
        }

        private void ApplyAuditForStringUser(
            EntityEntry entry,
            IAuditableEntity entity,
            DateTime now,
            string currentUser)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = now;
                    entity.CreatedBy = currentUser;
                    entity.ModifiedAt = null;
                    entity.ModifiedBy = null;
                    // Audit information applied - no verbose logging needed in interceptor
                    break;

                case EntityState.Modified:
                    entity.ModifiedAt = now;
                    entity.ModifiedBy = currentUser;
                    // Prevent modification of creation audit fields
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
                    // Audit information applied - no verbose logging needed in interceptor
                    break;
            }
        }

        private void TryApplyGenericAudit(EntityEntry entry, DateTime now, string currentUser)
        {
            var entityType = entry.Entity.GetType();
            var interfaces = entityType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAuditableEntity<>))
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            SetPropertyValue(entry, "CreatedAt", now);
                            SetPropertyValue(entry, "CreatedBy", currentUser);
                            SetPropertyValue(entry, "ModifiedAt", null);
                            SetPropertyValue(entry, "ModifiedBy", null);
                            break;

                        case EntityState.Modified:
                            SetPropertyValue(entry, "ModifiedAt", now);
                            SetPropertyValue(entry, "ModifiedBy", currentUser);
                            // Prevent modification of creation audit fields
                            var createdAtProp = entry.Property("CreatedAt");
                            var createdByProp = entry.Property("CreatedBy");
                            if (createdAtProp != null) createdAtProp.IsModified = false;
                            if (createdByProp != null) createdByProp.IsModified = false;
                            break;
                    }
                    break;
                }
            }
        }

        private void ApplyEntityDateLog(EntityEntry entry, IEntityDateLog entity, DateTime now)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entity.Created = now;
                    entity.Modified = null;
                    break;

                case EntityState.Modified:
                    entity.Modified = now;
                    entry.Property(nameof(IEntityDateLog.Created)).IsModified = false;
                    break;
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
}
