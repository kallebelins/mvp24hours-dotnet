//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.ValueObjects.Functional
{
    /// <summary>
    /// Represents a value that can be one of two types: Left (typically error) or Right (typically success).
    /// Used for Railway Oriented Programming and explicit error handling.
    /// </summary>
    /// <typeparam name="TLeft">The type for the Left value (typically error).</typeparam>
    /// <typeparam name="TRight">The type for the Right value (typically success).</typeparam>
    /// <remarks>
    /// <para>
    /// By convention:
    /// - Left represents failure/error
    /// - Right represents success
    /// </para>
    /// <para>
    /// Either is similar to Result but more flexible because both sides can be any type,
    /// while Result typically has a fixed error type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating Either values
    /// Either&lt;string, int&gt; success = Either&lt;string, int&gt;.Right(42);
    /// Either&lt;string, int&gt; failure = Either&lt;string, int&gt;.Left("Error: Division by zero");
    /// 
    /// // Pattern matching
    /// string result = success.Match(
    ///     left: error => $"Failed: {error}",
    ///     right: value => $"Success: {value}"
    /// );
    /// 
    /// // Chaining operations (only on Right value)
    /// var doubled = success
    ///     .Map(x => x * 2)
    ///     .Map(x => $"Result: {x}");
    /// </code>
    /// </example>
    public readonly struct Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>
    {
        private readonly TLeft _left;
        private readonly TRight _right;
        private readonly bool _isRight;

        private Either(TLeft left, TRight right, bool isRight)
        {
            _left = left;
            _right = right;
            _isRight = isRight;
        }

        /// <summary>
        /// Gets a value indicating whether this Either contains a Right value (success).
        /// </summary>
        public bool IsRight => _isRight;

        /// <summary>
        /// Gets a value indicating whether this Either contains a Left value (error).
        /// </summary>
        public bool IsLeft => !_isRight;

        /// <summary>
        /// Gets the Left value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when Either is Right.</exception>
        public TLeft LeftValue
        {
            get
            {
                if (_isRight)
                {
                    throw new InvalidOperationException("Either is Right, not Left.");
                }
                return _left;
            }
        }

        /// <summary>
        /// Gets the Right value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when Either is Left.</exception>
        public TRight RightValue
        {
            get
            {
                if (!_isRight)
                {
                    throw new InvalidOperationException("Either is Left, not Right.");
                }
                return _right;
            }
        }

        /// <summary>
        /// Creates an Either with a Left value (typically error).
        /// </summary>
        public static Either<TLeft, TRight> Left(TLeft value)
        {
            return new Either<TLeft, TRight>(value, default!, false);
        }

        /// <summary>
        /// Creates an Either with a Right value (typically success).
        /// </summary>
        public static Either<TLeft, TRight> Right(TRight value)
        {
            return new Either<TLeft, TRight>(default!, value, true);
        }

        /// <summary>
        /// Pattern matches on the Either, executing one of two functions.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="left">The function to execute if Either is Left.</param>
        /// <param name="right">The function to execute if Either is Right.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(Func<TLeft, TResult> left, Func<TRight, TResult> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            return _isRight ? right(_right) : left(_left);
        }

        /// <summary>
        /// Pattern matches on the Either, executing an action.
        /// </summary>
        public void Match(Action<TLeft> left, Action<TRight> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            if (_isRight)
            {
                right(_right);
            }
            else
            {
                left(_left);
            }
        }

        /// <summary>
        /// Transforms the Right value using the specified function.
        /// Left values are passed through unchanged.
        /// </summary>
        public Either<TLeft, TNewRight> Map<TNewRight>(Func<TRight, TNewRight> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return _isRight
                ? Either<TLeft, TNewRight>.Right(selector(_right))
                : Either<TLeft, TNewRight>.Left(_left);
        }

        /// <summary>
        /// Transforms the Left value using the specified function.
        /// Right values are passed through unchanged.
        /// </summary>
        public Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return _isRight
                ? Either<TNewLeft, TRight>.Right(_right)
                : Either<TNewLeft, TRight>.Left(selector(_left));
        }

        /// <summary>
        /// Transforms the Right value using a function that returns an Either.
        /// </summary>
        public Either<TLeft, TNewRight> Bind<TNewRight>(Func<TRight, Either<TLeft, TNewRight>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return _isRight ? selector(_right) : Either<TLeft, TNewRight>.Left(_left);
        }

        /// <summary>
        /// Gets the Right value or a default if Left.
        /// </summary>
        public TRight RightOr(TRight defaultValue)
        {
            return _isRight ? _right : defaultValue;
        }

        /// <summary>
        /// Gets the Right value or the result of a factory function if Left.
        /// </summary>
        public TRight RightOr(Func<TLeft, TRight> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return _isRight ? _right : factory(_left);
        }

        /// <summary>
        /// Gets the Left value or a default if Right.
        /// </summary>
        public TLeft LeftOr(TLeft defaultValue)
        {
            return _isRight ? defaultValue : _left;
        }

        /// <summary>
        /// Converts this Either to a Maybe, discarding the Left value.
        /// </summary>
        public Maybe<TRight> ToMaybe()
        {
            return _isRight ? Maybe<TRight>.Some(_right) : Maybe<TRight>.None;
        }

        #region Equality

        /// <inheritdoc />
        public bool Equals(Either<TLeft, TRight> other)
        {
            if (_isRight != other._isRight)
            {
                return false;
            }

            if (_isRight)
            {
                return Equals(_right, other._right);
            }

            return Equals(_left, other._left);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Either<TLeft, TRight> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _isRight
                ? HashCode.Combine(true, _right)
                : HashCode.Combine(false, _left);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Either<TLeft, TRight> left, Either<TLeft, TRight> right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Either<TLeft, TRight> left, Either<TLeft, TRight> right) => !left.Equals(right);

        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return _isRight ? $"Right({_right})" : $"Left({_left})";
        }
    }

    /// <summary>
    /// Static helper class for creating Either instances.
    /// </summary>
    public static class Either
    {
        /// <summary>
        /// Creates an Either with a Left value.
        /// </summary>
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft value)
        {
            return Either<TLeft, TRight>.Left(value);
        }

        /// <summary>
        /// Creates an Either with a Right value.
        /// </summary>
        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight value)
        {
            return Either<TLeft, TRight>.Right(value);
        }
    }
}

