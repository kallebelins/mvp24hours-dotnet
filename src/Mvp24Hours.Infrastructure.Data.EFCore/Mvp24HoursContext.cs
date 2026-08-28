//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Internal;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
/// A Mvp24HoursContext instance represents a session with the database and can be used to query and save instances of your entities.
/// </summary>
public abstract class Mvp24HoursContext : DbContext
{
    #region [ Ctor ]

    protected Mvp24HoursContext()
        : base()
    {
    }

    protected Mvp24HoursContext(DbContextOptions options)
        : base(options)
    {
    }

    #endregion

    #region [ Configs ]

    /// <summary>
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.OnModelCreating(ModelBuilder)"/>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (CanApplyEntityLog)
        {
            modelBuilder.ApplyGlobalFilters<IEntityDateLog>(e => e.Removed == null);
        }
    }
    /// <summary>
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChanges"/>
    /// </summary>
    public override int SaveChanges()
    {
#pragma warning disable CS0618 // intentional: legacy IEntityDateLog path kept until removal in v12
        ApplyLogRules();
#pragma warning restore CS0618
        return base.SaveChanges();
    }
    /// <summary>
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable CS0618 // intentional: legacy IEntityDateLog path kept until removal in v12
        ApplyLogRules();
#pragma warning restore CS0618
        return base.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(bool, CancellationToken)"/>
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
#pragma warning disable CS0618 // intentional: legacy IEntityDateLog path kept until removal in v12
        ApplyLogRules();
#pragma warning restore CS0618
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    /// <summary>
    /// Apply log rules
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stamps <c>Created</c>/<c>Modified</c> (and <c>CreatedBy</c>/<c>ModifiedBy</c> for
    /// <c>IEntityLog&lt;T&gt;</c>) on entities implementing
    /// <see cref="IEntityDateLog"/>, gated by <see cref="CanApplyEntityLog"/>. It takes no action on
    /// <c>EntityState.Deleted</c>: the legacy soft delete is performed by <c>Repository.Remove</c>,
    /// which sets <c>Removed</c> and calls <c>Modify</c>.
    /// </para>
    /// <para>
    /// The recommended replacement is <c>SoftDeleteInterceptor</c> (registered with
    /// <c>AddMvp24HoursEFCoreSoftDeleteInterceptor</c>) together with <c>AuditSaveChangesInterceptor</c>,
    /// which use <c>ISoftDeletable</c>/<c>IAuditableEntity</c>, <c>ICurrentUserProvider</c> and
    /// <c>IClock</c> instead of <see cref="EntityLogBy"/> and <c>TimeZoneHelper</c>. The two mechanisms
    /// target different interfaces and are not interchangeable — see the "Soft delete (EF Core)" section
    /// of the migration guide for the entity migration path.
    /// </para>
    /// </remarks>
    [Obsolete("Use SoftDeleteInterceptor + AuditInterceptor (see AddMvp24HoursEFCoreSoftDeleteInterceptor). Will be removed in v12.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
    protected void ApplyLogRules()
    {
        if (!CanApplyEntityLog)
        {
            return;
        }

        // entity log and guid
        foreach (EntityEntry? entry in ChangeTracker
            .Entries()
            .Where(e =>
                e.Entity is IEntityDateLog
                && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)))
        {
            if (entry.Entity is not IEntityDateLog dateLog)
            {
                continue;
            }

            bool hasUserBy = EntityLogAccessor.HasEntityLog(entry.Entity);

            if (entry.State == EntityState.Added)
            {
                dateLog.Created = TimeZoneHelper.GetTimeZoneNow();
                dateLog.Modified = null;
                dateLog.Removed = null;

                if (hasUserBy)
                {
                    EntityLogAccessor.TrySetPropertyValue(entry.Entity, "CreatedBy", EntityLogBy);
                    EntityLogAccessor.TrySetPropertyValue(entry.Entity, "ModifiedBy", null);
                    EntityLogAccessor.TrySetPropertyValue(entry.Entity, "RemovedBy", null);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (dateLog.Removed == null)
                {
                    dateLog.Modified = TimeZoneHelper.GetTimeZoneNow();

                    if (hasUserBy)
                    {
                        EntityLogAccessor.TrySetPropertyValue(entry.Entity, "ModifiedBy", EntityLogBy);
                    }
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // no action
            }
        }
    }

    #endregion

    #region [ Props ]

    /// <summary>
    /// Indicates whether log control can be performed by the base context of Mvp24Hours.
    /// </summary>
    public virtual bool CanApplyEntityLog { get; }
    /// <summary>
    /// Gets the value of the user logged in the context or logged into the database
    /// </summary>
    public virtual object? EntityLogBy { get; }

    #endregion
}
