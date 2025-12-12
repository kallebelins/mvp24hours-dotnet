//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Core.Domain.Enumerations
{
    /// <summary>
    /// Base class for Smart Enums (Enumeration Pattern).
    /// Provides type-safe, behavior-rich alternatives to standard enums.
    /// </summary>
    /// <typeparam name="TEnum">The derived enumeration type.</typeparam>
    /// <remarks>
    /// <para>
    /// Smart Enums (also called Enumeration classes) extend the standard C# enum concept
    /// by allowing:
    /// - Associated behavior (methods) with each value
    /// - Rich domain logic encapsulation
    /// - Polymorphism and inheritance
    /// - Better support for ORM mapping
    /// </para>
    /// <para>
    /// Unlike standard enums, Smart Enums are reference types and can contain
    /// arbitrary data and methods.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderStatus : Enumeration&lt;OrderStatus&gt;
    /// {
    ///     public static readonly OrderStatus Pending = new(1, nameof(Pending));
    ///     public static readonly OrderStatus Processing = new(2, nameof(Processing));
    ///     public static readonly OrderStatus Shipped = new(3, nameof(Shipped));
    ///     public static readonly OrderStatus Delivered = new(4, nameof(Delivered));
    ///     public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled));
    ///     
    ///     private OrderStatus(int value, string name) : base(value, name) { }
    ///     
    ///     public virtual bool CanCancel => this == Pending || this == Processing;
    /// }
    /// 
    /// // Usage
    /// var status = OrderStatus.FromValue(1);
    /// if (status.CanCancel)
    /// {
    ///     // Cancel order
    /// }
    /// </code>
    /// </example>
    public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
        where TEnum : Enumeration<TEnum>
    {
        private static readonly Lazy<Dictionary<int, TEnum>> _byValue = 
            new(() => GetAllValues().ToDictionary(e => e.Value));
        
        private static readonly Lazy<Dictionary<string, TEnum>> _byName = 
            new(() => GetAllValues().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the numeric value of this enumeration.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Gets the name of this enumeration.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new enumeration instance.
        /// </summary>
        /// <param name="value">The numeric value.</param>
        /// <param name="name">The name.</param>
        protected Enumeration(int value, string name)
        {
            Value = value;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets all values of this enumeration type.
        /// </summary>
        public static IReadOnlyCollection<TEnum> GetAll() => _byValue.Value.Values.ToList();

        /// <summary>
        /// Gets an enumeration by its numeric value.
        /// </summary>
        /// <param name="value">The value to look up.</param>
        /// <returns>The enumeration with the specified value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no enumeration has the specified value.</exception>
        public static TEnum FromValue(int value)
        {
            if (!_byValue.Value.TryGetValue(value, out var result))
            {
                throw new InvalidOperationException(
                    $"'{value}' is not a valid value for {typeof(TEnum).Name}. " +
                    $"Valid values are: {string.Join(", ", _byValue.Value.Keys)}");
            }
            return result;
        }

        /// <summary>
        /// Gets an enumeration by its name (case-insensitive).
        /// </summary>
        /// <param name="name">The name to look up.</param>
        /// <returns>The enumeration with the specified name.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no enumeration has the specified name.</exception>
        public static TEnum FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_byName.Value.TryGetValue(name, out var result))
            {
                throw new InvalidOperationException(
                    $"'{name}' is not a valid name for {typeof(TEnum).Name}. " +
                    $"Valid names are: {string.Join(", ", _byName.Value.Keys)}");
            }
            return result;
        }

        /// <summary>
        /// Tries to get an enumeration by its numeric value.
        /// </summary>
        /// <param name="value">The value to look up.</param>
        /// <param name="result">The found enumeration, or null if not found.</param>
        /// <returns>True if found; otherwise, false.</returns>
        public static bool TryFromValue(int value, out TEnum result)
        {
            return _byValue.Value.TryGetValue(value, out result!);
        }

        /// <summary>
        /// Tries to get an enumeration by its name (case-insensitive).
        /// </summary>
        /// <param name="name">The name to look up.</param>
        /// <param name="result">The found enumeration, or null if not found.</param>
        /// <returns>True if found; otherwise, false.</returns>
        public static bool TryFromName(string name, out TEnum result)
        {
            result = null!;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return _byName.Value.TryGetValue(name, out result!);
        }

        /// <summary>
        /// Checks if a value is defined in this enumeration.
        /// </summary>
        public static bool IsDefined(int value) => _byValue.Value.ContainsKey(value);

        /// <summary>
        /// Checks if a name is defined in this enumeration.
        /// </summary>
        public static bool IsDefined(string name) => 
            !string.IsNullOrWhiteSpace(name) && _byName.Value.ContainsKey(name);

        private static IEnumerable<TEnum> GetAllValues()
        {
            var enumType = typeof(TEnum);
            var fields = enumType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == enumType);

            foreach (var field in fields)
            {
                if (field.GetValue(null) is TEnum value)
                {
                    yield return value;
                }
            }
        }

        #region Equality

        /// <inheritdoc />
        public bool Equals(Enumeration<TEnum> other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Enumeration<TEnum> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            return !(left == right);
        }

        #endregion

        #region Comparison

        /// <inheritdoc />
        public int CompareTo(Enumeration<TEnum> other)
        {
            if (other is null) return 1;
            return Value.CompareTo(other.Value);
        }

        /// <summary>
        /// Less than operator.
        /// </summary>
        public static bool operator <(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Less than or equal operator.
        /// </summary>
        public static bool operator <=(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Greater than operator.
        /// </summary>
        public static bool operator >(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Greater than or equal operator.
        /// </summary>
        public static bool operator >=(Enumeration<TEnum> left, Enumeration<TEnum> right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Implicit conversion to int.
        /// </summary>
        public static implicit operator int(Enumeration<TEnum> enumeration)
        {
            return enumeration?.Value ?? 0;
        }

        /// <summary>
        /// Implicit conversion to string.
        /// </summary>
        public static implicit operator string(Enumeration<TEnum> enumeration)
        {
            return enumeration?.Name ?? string.Empty;
        }

        #endregion

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}

