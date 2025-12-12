//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Marker interface for all domain entities.
    /// Entities have identity and are distinguished by their unique identifier, not their attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Domain-Driven Design (DDD), an Entity is an object that is defined by its identity
    /// rather than its attributes. Two entities are considered equal if they have the same identity,
    /// regardless of their other properties.
    /// </para>
    /// <para>
    /// This interface extends <see cref="IEntityBase"/> for backward compatibility
    /// while providing additional type safety through the generic version.
    /// </para>
    /// </remarks>
    public interface IEntity : IEntityBase
    {
    }

    /// <summary>
    /// Generic interface for entities with a strongly-typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
    /// <example>
    /// <code>
    /// public class Customer : IEntity&lt;Guid&gt;
    /// {
    ///     public Guid Id { get; set; }
    ///     public string Name { get; set; }
    ///     
    ///     object IEntityBase.EntityKey => Id;
    /// }
    /// </code>
    /// </example>
    public interface IEntity<TId> : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for this entity.
        /// </summary>
        TId Id { get; set; }
    }
}

