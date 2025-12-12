//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;

namespace Mvp24Hours.Core.Domain.Entities
{
    /// <summary>
    /// Base class for entities that track audit information (creation and modification).
    /// </summary>
    /// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
    /// <remarks>
    /// <para>
    /// This base class combines entity identity with audit tracking.
    /// The audit fields should typically be set automatically by the persistence layer
    /// (e.g., using EF Core interceptors or SaveChanges override).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Product : AuditableEntity&lt;Guid&gt;
    /// {
    ///     public string Name { get; set; }
    ///     public decimal Price { get; set; }
    /// }
    /// 
    /// // Usage with automatic audit tracking in DbContext:
    /// public override Task&lt;int&gt; SaveChangesAsync(CancellationToken ct = default)
    /// {
    ///     foreach (var entry in ChangeTracker.Entries&lt;IAuditableEntity&gt;())
    ///     {
    ///         if (entry.State == EntityState.Added)
    ///         {
    ///             entry.Entity.CreatedAt = DateTime.UtcNow;
    ///             entry.Entity.CreatedBy = _currentUser.UserId;
    ///         }
    ///         else if (entry.State == EntityState.Modified)
    ///         {
    ///             entry.Entity.ModifiedAt = DateTime.UtcNow;
    ///             entry.Entity.ModifiedBy = _currentUser.UserId;
    ///         }
    ///     }
    ///     return base.SaveChangesAsync(ct);
    /// }
    /// </code>
    /// </example>
    public abstract class AuditableEntity<TId> : EntityBase<TId>, IAuditableEntity
        where TId : IEquatable<TId>
    {
        /// <inheritdoc />
        public DateTime CreatedAt { get; set; }

        /// <inheritdoc />
        public string CreatedBy { get; set; } = string.Empty;

        /// <inheritdoc />
        public DateTime? ModifiedAt { get; set; }

        /// <inheritdoc />
        public string ModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base class for auditable entities with GUID identifiers.
    /// </summary>
    public abstract class AuditableGuidEntity : AuditableEntity<Guid>
    {
        /// <summary>
        /// Creates a new auditable entity with a new GUID.
        /// </summary>
        protected AuditableGuidEntity()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a new auditable entity with the specified GUID.
        /// </summary>
        protected AuditableGuidEntity(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// Base class for auditable entities with integer identifiers.
    /// </summary>
    public abstract class AuditableIntEntity : AuditableEntity<int>
    {
    }

    /// <summary>
    /// Base class for auditable entities with long identifiers.
    /// </summary>
    public abstract class AuditableLongEntity : AuditableEntity<long>
    {
    }
}

