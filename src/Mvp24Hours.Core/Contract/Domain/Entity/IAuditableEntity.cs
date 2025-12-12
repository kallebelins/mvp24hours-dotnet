//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Interface for entities that track creation and modification audit information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Auditable entities automatically track when they were created and modified,
    /// as well as who performed these operations. This is useful for compliance,
    /// debugging, and data integrity purposes.
    /// </para>
    /// <para>
    /// This interface is similar to <see cref="IEntityDateLog"/> but uses more
    /// standardized naming conventions and adds user tracking with string identifiers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Order : IAuditableEntity
    /// {
    ///     public int Id { get; set; }
    ///     public DateTime CreatedAt { get; set; }
    ///     public string CreatedBy { get; set; }
    ///     public DateTime? ModifiedAt { get; set; }
    ///     public string ModifiedBy { get; set; }
    /// }
    /// </code>
    /// </example>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created the entity.
        /// This could be a user ID, username, or system identifier.
        /// </summary>
        string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// Null if the entity has never been modified after creation.
        /// </summary>
        DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified the entity.
        /// Null if the entity has never been modified after creation.
        /// </summary>
        string ModifiedBy { get; set; }
    }

    /// <summary>
    /// Generic interface for auditable entities with a typed user identifier.
    /// </summary>
    /// <typeparam name="TUserId">The type of the user identifier (e.g., Guid, int, string).</typeparam>
    public interface IAuditableEntity<TUserId>
    {
        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created the entity.
        /// </summary>
        TUserId CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified the entity.
        /// </summary>
        TUserId ModifiedBy { get; set; }
    }
}

