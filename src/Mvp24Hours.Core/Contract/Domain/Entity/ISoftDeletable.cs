//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Interface for entities that support soft deletion (logical deletion).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Soft deletion allows marking records as deleted without physically removing them
    /// from the database. This is useful for:
    /// - Data recovery and audit trails
    /// - Maintaining referential integrity
    /// - Compliance with data retention policies
    /// - Analyzing deleted data
    /// </para>
    /// <para>
    /// Entities implementing this interface should be filtered by <see cref="IsDeleted"/>
    /// in queries by default using global query filters (e.g., in EF Core).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Product : ISoftDeletable
    /// {
    ///     public int Id { get; set; }
    ///     public string Name { get; set; }
    ///     public bool IsDeleted { get; set; }
    ///     public DateTime? DeletedAt { get; set; }
    ///     public string DeletedBy { get; set; }
    /// }
    /// 
    /// // In DbContext OnModelCreating:
    /// modelBuilder.Entity&lt;Product&gt;().HasQueryFilter(p => !p.IsDeleted);
    /// </code>
    /// </example>
    public interface ISoftDeletable
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity has been soft deleted.
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was deleted.
        /// Null if the entity has not been deleted.
        /// </summary>
        DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who deleted the entity.
        /// Null if the entity has not been deleted.
        /// </summary>
        string DeletedBy { get; set; }
    }

    /// <summary>
    /// Generic interface for soft-deletable entities with a typed user identifier.
    /// </summary>
    /// <typeparam name="TUserId">The type of the user identifier.</typeparam>
    public interface ISoftDeletable<TUserId>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity has been soft deleted.
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was deleted.
        /// </summary>
        DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who deleted the entity.
        /// </summary>
        TUserId DeletedBy { get; set; }
    }
}

