//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.ValueObjects.Functional
{
    /// <summary>
    /// Represents an optional value that may or may not exist.
    /// Use Maybe to avoid null reference exceptions and make the absence of a value explicit.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <remarks>
    /// <para>
    /// Maybe (also known as Option) is a functional programming pattern that explicitly
    /// represents the presence or absence of a value. This is safer than using null because:
    /// - The type system forces you to handle both cases
    /// - It makes the API contract explicit about optional values
    /// - It enables functional composition with Map, Bind, and Match
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating Maybe values
    /// var some = Maybe&lt;int&gt;.Some(42);
    /// var none = Maybe&lt;int&gt;.None;
    /// 
    /// // Pattern matching
    /// string result = some.Match(
    ///     some: value => $"Found: {value}",
    ///     none: () => "Not found"
    /// );
    /// 
    /// // Chaining operations
    /// var doubled = some
    ///     .Map(x => x * 2)
    ///     .Map(x => $"Result: {x}");
    /// 
    /// // Safe value extraction
    /// int value = some.ValueOr(0); // Returns 42
    /// int noneValue = none.ValueOr(0); // Returns 0
    /// </code>
    /// </example>
    public readonly struct Maybe<T> : IEquatable<Maybe<T>>
    {
        private readonly T _value;
        private readonly bool _hasValue;

        private Maybe(T value, bool hasValue)
        {
            _value = value;
            _hasValue = hasValue;
        }

        /// <summary>
        /// Gets a value indicating whether this Maybe contains a value.
        /// </summary>
        public bool HasValue => _hasValue;

        /// <summary>
        /// Gets a value indicating whether this Maybe is empty (no value).
        /// </summary>
        public bool HasNoValue => !_hasValue;

        /// <summary>
        /// Gets the contained value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when Maybe has no value.</exception>
        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    throw new InvalidOperationException("Maybe has no value. Check HasValue before accessing Value.");
                }
                return _value;
            }
        }

        /// <summary>
        /// Creates a Maybe containing a value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A Maybe containing the value, or None if value is null.</returns>
        public static Maybe<T> Some(T value)
        {
            if (value == null)
            {
                return None;
            }
            return new Maybe<T>(value, true);
        }

        /// <summary>
        /// Creates an empty Maybe.
        /// </summary>
        public static Maybe<T> None => new Maybe<T>(default!, false);

        /// <summary>
        /// Creates a Maybe from a nullable value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>Some if value is not null; otherwise, None.</returns>
        public static Maybe<T> From(T value)
        {
            return value == null ? None : Some(value);
        }

        /// <summary>
        /// Returns the contained value or a default value if empty.
        /// </summary>
        /// <param name="defaultValue">The value to return if Maybe is empty.</param>
        /// <returns>The contained value or the default.</returns>
        public T ValueOr(T defaultValue)
        {
            return _hasValue ? _value : defaultValue;
        }

        /// <summary>
        /// Returns the contained value or the result of a factory function if empty.
        /// </summary>
        /// <param name="defaultValueFactory">A function that produces a default value.</param>
        /// <returns>The contained value or the factory result.</returns>
        public T ValueOr(Func<T> defaultValueFactory)
        {
            if (defaultValueFactory == null) throw new ArgumentNullException(nameof(defaultValueFactory));
            return _hasValue ? _value : defaultValueFactory();
        }

        /// <summary>
        /// Transforms the contained value using the specified function.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The transformation function.</param>
        /// <returns>A Maybe containing the transformed value, or None if this Maybe is empty.</returns>
        public Maybe<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return _hasValue ? Maybe<TResult>.Some(selector(_value)) : Maybe<TResult>.None;
        }

        /// <summary>
        /// Transforms the contained value using a function that returns a Maybe.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The transformation function that returns a Maybe.</param>
        /// <returns>The result of the transformation, or None if this Maybe is empty.</returns>
        public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return _hasValue ? selector(_value) : Maybe<TResult>.None;
        }

        /// <summary>
        /// Pattern matches on the Maybe, executing one of two functions.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="some">The function to execute if Maybe has a value.</param>
        /// <param name="none">The function to execute if Maybe is empty.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));
            return _hasValue ? some(_value) : none();
        }

        /// <summary>
        /// Pattern matches on the Maybe, executing an action.
        /// </summary>
        /// <param name="some">The action to execute if Maybe has a value.</param>
        /// <param name="none">The action to execute if Maybe is empty.</param>
        public void Match(Action<T> some, Action none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));

            if (_hasValue)
            {
                some(_value);
            }
            else
            {
                none();
            }
        }

        /// <summary>
        /// Executes an action if Maybe has a value.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This Maybe for chaining.</returns>
        public Maybe<T> Tap(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (_hasValue)
            {
                action(_value);
            }
            return this;
        }

        /// <summary>
        /// Filters the Maybe using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to test.</param>
        /// <returns>This Maybe if predicate is true; otherwise, None.</returns>
        public Maybe<T> Where(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (!_hasValue) return None;
            return predicate(_value) ? this : None;
        }

        /// <summary>
        /// Converts this Maybe to a nullable reference.
        /// </summary>
        /// <returns>The value or null.</returns>
        public T ToNullable()
        {
            return _hasValue ? _value : default!;
        }

        #region Equality

        /// <inheritdoc />
        public bool Equals(Maybe<T> other)
        {
            if (!_hasValue && !other._hasValue)
            {
                return true;
            }

            if (_hasValue != other._hasValue)
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Maybe<T> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _hasValue ? HashCode.Combine(true, _value) : HashCode.Combine(false);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

        #endregion

        #region Conversions

        /// <summary>
        /// Implicit conversion from value to Maybe.
        /// </summary>
        public static implicit operator Maybe<T>(T value) => From(value);

        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return _hasValue ? $"Some({_value})" : "None";
        }
    }

    /// <summary>
    /// Static helper class for creating Maybe instances.
    /// </summary>
    public static class Maybe
    {
        /// <summary>
        /// Creates a Maybe containing a value.
        /// </summary>
        public static Maybe<T> Some<T>(T value) => Maybe<T>.Some(value);

        /// <summary>
        /// Creates an empty Maybe.
        /// </summary>
        public static Maybe<T> None<T>() => Maybe<T>.None;

        /// <summary>
        /// Creates a Maybe from a nullable value.
        /// </summary>
        public static Maybe<T> From<T>(T value) => Maybe<T>.From(value);

        /// <summary>
        /// Creates a Maybe from a nullable struct.
        /// </summary>
        public static Maybe<T> FromNullable<T>(T? value) where T : struct
        {
            return value.HasValue ? Maybe<T>.Some(value.Value) : Maybe<T>.None;
        }
    }
}

