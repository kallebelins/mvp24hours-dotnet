//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Interface for entities that support optimistic concurrency control through versioning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optimistic concurrency assumes that conflicts are rare and checks for conflicts
    /// only when committing changes. The version property is used to detect if the
    /// entity has been modified by another process since it was loaded.
    /// </para>
    /// <para>
    /// Common implementations:
    /// - SQL Server: Use ROWVERSION/TIMESTAMP column
    /// - PostgreSQL: Use xmin system column or a version counter
    /// - Other databases: Use a version counter (int/long) incremented on each update
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Product : IVersionedEntity
    /// {
    ///     public int Id { get; set; }
    ///     public string Name { get; set; }
    ///     public byte[] RowVersion { get; set; }
    /// }
    /// 
    /// // In DbContext OnModelCreating:
    /// modelBuilder.Entity&lt;Product&gt;()
    ///     .Property(p => p.RowVersion)
    ///     .IsRowVersion();
    /// </code>
    /// </example>
    public interface IVersionedEntity
    {
        /// <summary>
        /// Gets or sets the row version for optimistic concurrency control.
        /// This is typically a timestamp/rowversion column in SQL Server
        /// or a byte array representing the version.
        /// </summary>
        byte[] RowVersion { get; set; }
    }

    /// <summary>
    /// Interface for entities that use a numeric version counter for concurrency control.
    /// </summary>
    /// <remarks>
    /// This is useful when the database doesn't support automatic row versioning
    /// or when you need explicit control over the version number.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Document : IVersionedEntityWithCounter
    /// {
    ///     public int Id { get; set; }
    ///     public string Content { get; set; }
    ///     public long Version { get; set; }
    /// }
    /// 
    /// // In DbContext OnModelCreating:
    /// modelBuilder.Entity&lt;Document&gt;()
    ///     .Property(d => d.Version)
    ///     .IsConcurrencyToken();
    /// </code>
    /// </example>
    public interface IVersionedEntityWithCounter
    {
        /// <summary>
        /// Gets or sets the version number.
        /// This should be incremented on each update to the entity.
        /// </summary>
        long Version { get; set; }
    }
}

