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
    /// Base class for entities that support soft deletion with full audit tracking.
    /// Combines audit tracking with soft delete functionality.
    /// </summary>
    /// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
    /// <remarks>
    /// <para>
    /// This base class provides:
    /// - Identity-based equality (from EntityBase)
    /// - Creation/modification audit tracking (from AuditableEntity)
    /// - Soft delete with deletion audit tracking
    /// </para>
    /// <para>
    /// When using with EF Core, configure global query filters to exclude soft-deleted entities:
    /// <code>
    /// modelBuilder.Entity&lt;MyEntity&gt;().HasQueryFilter(e => !e.IsDeleted);
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Document : SoftDeletableEntity&lt;Guid&gt;
    /// {
    ///     public string Title { get; set; }
    ///     public string Content { get; set; }
    /// }
    /// </code>
    /// </example>
    public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>, ISoftDeletable
        where TId : IEquatable<TId>
    {
        /// <inheritdoc />
        public bool IsDeleted { get; set; }

        /// <inheritdoc />
        public DateTime? DeletedAt { get; set; }

        /// <inheritdoc />
        public string DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Marks the entity as deleted.
        /// </summary>
        /// <param name="deletedBy">The user who is deleting the entity.</param>
        public virtual void SoftDelete(string deletedBy)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
        }

        /// <summary>
        /// Restores a soft-deleted entity.
        /// </summary>
        public virtual void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = string.Empty;
        }
    }

    /// <summary>
    /// Base class for soft-deletable entities with GUID identifiers.
    /// </summary>
    public abstract class SoftDeletableGuidEntity : SoftDeletableEntity<Guid>
    {
        /// <summary>
        /// Creates a new soft-deletable entity with a new GUID.
        /// </summary>
        protected SoftDeletableGuidEntity()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a new soft-deletable entity with the specified GUID.
        /// </summary>
        protected SoftDeletableGuidEntity(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// Base class for soft-deletable entities with integer identifiers.
    /// </summary>
    public abstract class SoftDeletableIntEntity : SoftDeletableEntity<int>
    {
    }

    /// <summary>
    /// Base class for soft-deletable entities with long identifiers.
    /// </summary>
    public abstract class SoftDeletableLongEntity : SoftDeletableEntity<long>
    {
    }
}

