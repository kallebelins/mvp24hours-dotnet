//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Base class for strongly-typed entity identifiers.
    /// Provides type safety by preventing accidental mixing of IDs from different entity types.
    /// </summary>
    /// <typeparam name="TSelf">The derived ID type (CRTP pattern).</typeparam>
    /// <typeparam name="TValue">The underlying value type (typically Guid, int, or long).</typeparam>
    /// <example>
    /// <code>
    /// // Define your strongly-typed IDs
    /// public class CustomerId : EntityId&lt;CustomerId, Guid&gt;
    /// {
    ///     public CustomerId(Guid value) : base(value) { }
    ///     public static CustomerId New() => new(Guid.NewGuid());
    /// }
    /// 
    /// public class OrderId : EntityId&lt;OrderId, Guid&gt;
    /// {
    ///     public OrderId(Guid value) : base(value) { }
    ///     public static OrderId New() => new(Guid.NewGuid());
    /// }
    /// 
    /// // Compile-time safety - this won't compile:
    /// // void ProcessOrder(OrderId orderId) { }
    /// // ProcessOrder(customerId); // Error!
    /// </code>
    /// </example>
    public abstract class EntityId<TSelf, TValue> : BaseVO, IEquatable<TSelf>, IComparable<TSelf>
        where TSelf : EntityId<TSelf, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        /// <summary>
        /// Gets the underlying value of the identifier.
        /// </summary>
        public TValue Value { get; }

        /// <summary>
        /// Initializes a new instance of the strongly-typed ID.
        /// </summary>
        /// <param name="value">The underlying value.</param>
        protected EntityId(TValue value)
        {
            Value = value;
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <inheritdoc />
        public bool Equals(TSelf other)
        {
            if (other is null) return false;
            return Value.Equals(other.Value);
        }

        /// <inheritdoc />
        public int CompareTo(TSelf other)
        {
            if (other is null) return 1;
            return Value.CompareTo(other.Value);
        }

        /// <inheritdoc />
        public override string ToString() => Value?.ToString() ?? string.Empty;

        /// <summary>
        /// Implicit conversion to the underlying value type.
        /// </summary>
        public static implicit operator TValue(EntityId<TSelf, TValue> id) => id != null ? id.Value : default!;
    }

    /// <summary>
    /// Base class for strongly-typed entity identifiers using Guid as the underlying type.
    /// </summary>
    /// <typeparam name="TSelf">The derived ID type.</typeparam>
    /// <example>
    /// <code>
    /// public sealed class ProductId : GuidEntityId&lt;ProductId&gt;
    /// {
    ///     public ProductId(Guid value) : base(value) { }
    ///     public static ProductId New() => new(Guid.NewGuid());
    ///     public static ProductId Empty => new(Guid.Empty);
    /// }
    /// </code>
    /// </example>
    public abstract class GuidEntityId<TSelf> : EntityId<TSelf, Guid>
        where TSelf : GuidEntityId<TSelf>
    {
        /// <summary>
        /// Initializes a new instance with the specified Guid value.
        /// </summary>
        /// <param name="value">The Guid value.</param>
        protected GuidEntityId(Guid value) : base(value) { }

        /// <summary>
        /// Checks if this ID is empty (Guid.Empty).
        /// </summary>
        public bool IsEmpty => Value == Guid.Empty;
    }

    /// <summary>
    /// Base class for strongly-typed entity identifiers using int as the underlying type.
    /// </summary>
    /// <typeparam name="TSelf">The derived ID type.</typeparam>
    /// <example>
    /// <code>
    /// public sealed class SequenceId : IntEntityId&lt;SequenceId&gt;
    /// {
    ///     public SequenceId(int value) : base(value) { }
    /// }
    /// </code>
    /// </example>
    public abstract class IntEntityId<TSelf> : EntityId<TSelf, int>
        where TSelf : IntEntityId<TSelf>
    {
        /// <summary>
        /// Initializes a new instance with the specified int value.
        /// </summary>
        /// <param name="value">The int value.</param>
        protected IntEntityId(int value) : base(value) { }

        /// <summary>
        /// Checks if this ID is the default value (0).
        /// </summary>
        public bool IsDefault => Value == 0;
    }

    /// <summary>
    /// Base class for strongly-typed entity identifiers using long as the underlying type.
    /// </summary>
    /// <typeparam name="TSelf">The derived ID type.</typeparam>
    public abstract class LongEntityId<TSelf> : EntityId<TSelf, long>
        where TSelf : LongEntityId<TSelf>
    {
        /// <summary>
        /// Initializes a new instance with the specified long value.
        /// </summary>
        /// <param name="value">The long value.</param>
        protected LongEntityId(long value) : base(value) { }

        /// <summary>
        /// Checks if this ID is the default value (0).
        /// </summary>
        public bool IsDefault => Value == 0;
    }

    /// <summary>
    /// Base class for strongly-typed entity identifiers using string as the underlying type.
    /// </summary>
    /// <typeparam name="TSelf">The derived ID type.</typeparam>
    public abstract class StringEntityId<TSelf> : EntityId<TSelf, string>
        where TSelf : StringEntityId<TSelf>
    {
        /// <summary>
        /// Initializes a new instance with the specified string value.
        /// </summary>
        /// <param name="value">The string value.</param>
        protected StringEntityId(string value) : base(value ?? throw new ArgumentNullException(nameof(value))) { }

        /// <summary>
        /// Checks if this ID is null or empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);
    }
}

