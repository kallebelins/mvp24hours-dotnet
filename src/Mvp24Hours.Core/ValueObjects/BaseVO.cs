//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Base class for Value Objects following DDD patterns.
    /// Value Objects are immutable and compared by their values, not by identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Value Object is a small object that represents a simple entity whose equality is not
    /// based on identity. Two Value Objects are equal if all their properties are equal.
    /// </para>
    /// <para>
    /// Value Objects should be immutable - once created, their state should not change.
    /// If you need different values, create a new Value Object instance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Email : BaseVO
    /// {
    ///     public string Value { get; }
    ///     
    ///     public Email(string value)
    ///     {
    ///         Value = value?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(value));
    ///     }
    ///     
    ///     protected override IEnumerable&lt;object&gt; GetEqualityComponents()
    ///     {
    ///         yield return Value;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class BaseVO : IEquatable<BaseVO>
    {
        #region [ Equality ]

        /// <summary>
        /// Gets the components that define equality for this Value Object.
        /// Override this method to return all properties that should be compared for equality.
        /// </summary>
        /// <returns>An enumerable of objects representing the equality components.</returns>
        /// <remarks>
        /// The order and type of components matter for equality comparison.
        /// Components are compared using SequenceEqual.
        /// </remarks>
        protected abstract IEnumerable<object> GetEqualityComponents();

        #endregion

        #region [ IEquatable<BaseVO> ]

        /// <summary>
        /// Determines whether the specified Value Object is equal to the current Value Object.
        /// </summary>
        /// <param name="other">The Value Object to compare with the current Value Object.</param>
        /// <returns>true if the specified Value Object is equal to the current Value Object; otherwise, false.</returns>
        public bool Equals(BaseVO other)
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

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        #endregion

        #region [ Overrides ]

        /// <summary>
        /// Determines whether the specified object is equal to the current Value Object.
        /// </summary>
        /// <param name="obj">The object to compare with the current Value Object.</param>
        /// <returns>true if the specified object is equal to the current Value Object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BaseVO);
        }

        /// <summary>
        /// Serves as the hash function for the Value Object.
        /// Uses a better distribution algorithm combining all equality components.
        /// </summary>
        /// <returns>A hash code for the current Value Object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // Use a prime number to start and for multiplication
                // This provides better hash distribution
                const int prime = 397;
                var hash = 17;

                foreach (var component in GetEqualityComponents())
                {
                    hash = hash * prime ^ (component?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns a string representation of the Value Object.
        /// Override this method in derived classes to provide meaningful string representation.
        /// </summary>
        /// <returns>A string that represents the current Value Object.</returns>
        public override string ToString()
        {
            var components = GetEqualityComponents().ToList();
            if (components.Count == 0)
            {
                return GetType().Name;
            }

            if (components.Count == 1)
            {
                return components[0]?.ToString() ?? string.Empty;
            }

            return $"{GetType().Name}({string.Join(", ", components.Select(c => c?.ToString() ?? "null"))})";
        }

        /// <summary>
        /// Equality operator for Value Objects.
        /// </summary>
        /// <param name="a">The first Value Object to compare.</param>
        /// <param name="b">The second Value Object to compare.</param>
        /// <returns>true if the Value Objects are equal; otherwise, false.</returns>
#pragma warning disable S3875 // "operator==" should not be overloaded on reference types
        public static bool operator ==(BaseVO a, BaseVO b)
#pragma warning restore S3875 // "operator==" should not be overloaded on reference types
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Inequality operator for Value Objects.
        /// </summary>
        /// <param name="a">The first Value Object to compare.</param>
        /// <param name="b">The second Value Object to compare.</param>
        /// <returns>true if the Value Objects are not equal; otherwise, false.</returns>
        public static bool operator !=(BaseVO a, BaseVO b)
        {
            return !(a == b);
        }

        #endregion

        #region [ Copy ]

        /// <summary>
        /// Creates a shallow copy of the current Value Object.
        /// Since Value Objects should be immutable, this typically returns a new instance with the same values.
        /// </summary>
        /// <returns>A shallow copy of the current Value Object.</returns>
        /// <remarks>
        /// This method uses MemberwiseClone which creates a shallow copy.
        /// For Value Objects containing reference types, consider overriding this method
        /// to create deep copies if necessary.
        /// </remarks>
        [JsonIgnore]
        protected BaseVO ShallowCopy => (BaseVO)MemberwiseClone();

        #endregion
    }
}
