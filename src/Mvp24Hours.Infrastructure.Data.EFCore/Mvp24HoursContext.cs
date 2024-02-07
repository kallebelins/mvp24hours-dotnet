//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechanges-start");
            try
            {
                this.ApplyLogRules();
                return base.SaveChanges();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechanges-end"); }
        }
        /// <summary>
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechangesasync-start");
            try
            {
                ApplyLogRules();
                return base.SaveChangesAsync(cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechangesasync-end"); }
        }
        /// <summary>
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(bool, CancellationToken)"/>
        /// </summary>
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechangesasync-acceptallchangesonsuccess-start");
            try
            {
                ApplyLogRules();
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-dbcontext-savechangesasync-acceptallchangesonsuccess-end"); }
        }
        /// <summary>
        /// Apply log rules
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        protected void ApplyLogRules()
        {
            if (!CanApplyEntityLog)
            {
                return;
            }

            // entity log and guid
            foreach (var entry in this.ChangeTracker
                .Entries()
                .Where(e =>
                    (e.Entity.GetType().InheritsOrImplements(typeof(IEntityLog<>))
                        || e.Entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>))
                        || e.Entity.GetType().InheritsOrImplements(typeof(IEntityDateLog)))
                    && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)))
            {
                bool hasUserBy = (entry.Entity.GetType().InheritsOrImplements(typeof(IEntityLog<>))
                        || entry.Entity.GetType().InheritsOrImplements(typeof(EntityBaseLog<,>)));

                var e = (dynamic)entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    e.Created = TimeZoneHelper.GetTimeZoneNow();
                    e.Modified = null;
                    e.Removed = null;

                    if (hasUserBy)
                    {
                        e.CreatedBy = (dynamic)EntityLogBy;
                        e.ModifiedBy = null;
                        e.RemovedBy = null;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (e.Removed == null)
                    {
                        e.Modified = TimeZoneHelper.GetTimeZoneNow();

                        if (hasUserBy)
                        {
                            e.ModifiedBy = (dynamic)EntityLogBy;
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
        public virtual object EntityLogBy { get; }

        #endregion
    }
}