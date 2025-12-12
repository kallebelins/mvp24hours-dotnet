//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Domain.Entities
{
    /// <summary>
    /// Base class for all domain entities with a strongly-typed identifier.
    /// Provides equality comparison based on identity (Id).
    /// </summary>
    /// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
    /// <remarks>
    /// <para>
    /// In Domain-Driven Design, entities are defined by their identity, not their attributes.
    /// Two entities with the same Id are considered equal, even if their other properties differ.
    /// </para>
    /// <para>
    /// This base class provides:
    /// - Identity-based equality comparison
    /// - Proper GetHashCode implementation
    /// - Domain events support (optional)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Customer : EntityBase&lt;Guid&gt;
    /// {
    ///     public string Name { get; private set; }
    ///     public Email Email { get; private set; }
    ///     
    ///     public Customer(Guid id, string name, Email email)
    ///     {
    ///         Id = id;
    ///         Name = name;
    ///         Email = email;
    ///     }
    ///     
    ///     public void ChangeName(string newName)
    ///     {
    ///         Name = newName;
    ///         // Could add domain event: AddDomainEvent(new CustomerNameChanged(Id, newName));
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class EntityBase<TId> : IEntity<TId>, IEquatable<EntityBase<TId>>
        where TId : IEquatable<TId>
    {
        private int? _cachedHashCode;

        /// <inheritdoc />
        public virtual TId Id { get; set; } = default!;

        /// <inheritdoc />
        object IEntityBase.EntityKey => Id!;

        /// <summary>
        /// Determines whether this entity is transient (not yet persisted).
        /// </summary>
        /// <returns>True if the entity has not been persisted; otherwise, false.</returns>
        public bool IsTransient()
        {
            return Id == null || Id.Equals(default!);
        }

        #region Equality

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as EntityBase<TId>);
        }

        /// <inheritdoc />
        public bool Equals(EntityBase<TId> other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            // Transient entities are never equal
            if (IsTransient() || other.IsTransient())
            {
                return false;
            }

            return Id.Equals(other.Id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Cache hash code to avoid recomputation
            // This is safe because Id should not change after entity creation
            if (!_cachedHashCode.HasValue)
            {
                _cachedHashCode = IsTransient()
                    ? base.GetHashCode()
                    : HashCode.Combine(GetType(), Id);
            }

            return _cachedHashCode.Value;
        }

        /// <summary>
        /// Equality operator for entities.
        /// </summary>
        public static bool operator ==(EntityBase<TId> left, EntityBase<TId> right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for entities.
        /// </summary>
        public static bool operator !=(EntityBase<TId> left, EntityBase<TId> right)
        {
            return !(left == right);
        }

        #endregion
    }

    /// <summary>
    /// Base class for entities with GUID identifiers.
    /// </summary>
    /// <example>
    /// <code>
    /// public class Order : GuidEntityBase
    /// {
    ///     public DateTime OrderDate { get; set; }
    ///     public decimal Total { get; set; }
    /// }
    /// </code>
    /// </example>
    public abstract class GuidEntityBase : EntityBase<Guid>
    {
        /// <summary>
        /// Creates a new entity with a new GUID.
        /// </summary>
        protected GuidEntityBase()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a new entity with the specified GUID.
        /// </summary>
        protected GuidEntityBase(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// Base class for entities with integer identifiers.
    /// </summary>
    public abstract class IntEntityBase : EntityBase<int>
    {
    }

    /// <summary>
    /// Base class for entities with long identifiers.
    /// </summary>
    public abstract class LongEntityBase : EntityBase<long>
    {
    }
}

