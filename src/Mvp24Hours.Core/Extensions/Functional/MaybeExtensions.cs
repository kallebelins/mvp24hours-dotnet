//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Functional;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Core.Extensions.Functional
{
    /// <summary>
    /// Extension methods for Maybe&lt;T&gt; providing additional functional operations
    /// and conversions to/from other types.
    /// </summary>
    public static class MaybeExtensions
    {
        #region Nullable Conversions

        /// <summary>
        /// Converts a nullable value to Maybe.
        /// </summary>
        public static Maybe<T> ToMaybe<T>(this T value)
        {
            return Maybe<T>.From(value);
        }

        /// <summary>
        /// Converts a nullable struct to Maybe.
        /// </summary>
        public static Maybe<T> ToMaybe<T>(this T? value) where T : struct
        {
            return value.HasValue ? Maybe<T>.Some(value.Value) : Maybe<T>.None;
        }

        #endregion

        #region Collection Operations

        /// <summary>
        /// Returns the first element wrapped in Maybe, or None if the collection is empty.
        /// </summary>
        public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            foreach (var item in source)
            {
                return Maybe<T>.Some(item);
            }
            return Maybe<T>.None;
        }

        /// <summary>
        /// Returns the first element that matches the predicate wrapped in Maybe, or None if not found.
        /// </summary>
        public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return Maybe<T>.Some(item);
                }
            }
            return Maybe<T>.None;
        }

        /// <summary>
        /// Returns the single element wrapped in Maybe, or None if the collection is empty.
        /// Throws if there are multiple elements.
        /// </summary>
        public static Maybe<T> SingleOrNone<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return Maybe<T>.None;
            }

            var result = enumerator.Current;
            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains more than one element.");
            }

            return Maybe<T>.Some(result);
        }

        /// <summary>
        /// Returns the last element wrapped in Maybe, or None if the collection is empty.
        /// </summary>
        public static Maybe<T> LastOrNone<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is IList<T> list)
            {
                return list.Count > 0 ? Maybe<T>.Some(list[list.Count - 1]) : Maybe<T>.None;
            }

            var result = Maybe<T>.None;
            foreach (var item in source)
            {
                result = Maybe<T>.Some(item);
            }
            return result;
        }

        /// <summary>
        /// Filters out None values from a sequence of Maybe.
        /// </summary>
        public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            foreach (var maybe in source)
            {
                if (maybe.HasValue)
                {
                    yield return maybe.Value;
                }
            }
        }

        /// <summary>
        /// Gets a value from a dictionary wrapped in Maybe, or None if the key doesn't exist.
        /// </summary>
        public static Maybe<TValue> GetValueOrNone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            return dictionary.TryGetValue(key, out var value)
                ? Maybe<TValue>.Some(value)
                : Maybe<TValue>.None;
        }

        #endregion

        #region BusinessResult Conversions

        /// <summary>
        /// Converts a BusinessResult to Maybe.
        /// Returns Some if successful with data; otherwise, None.
        /// </summary>
        public static Maybe<T> ToMaybe<T>(this IBusinessResult<T> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors || result.Data == null)
            {
                return Maybe<T>.None;
            }

            return Maybe<T>.Some(result.Data);
        }

        /// <summary>
        /// Converts a Maybe to a BusinessResult.
        /// Returns success if has value; otherwise, returns failure with the provided error message.
        /// </summary>
        public static IBusinessResult<T> ToBusinessResult<T>(this Maybe<T> maybe, string errorMessage = "Value not found")
        {
            if (maybe.HasValue)
            {
                return BusinessResult.Success(maybe.Value);
            }

            return BusinessResult.Failure<T>(errorMessage);
        }

        #endregion

        #region Either Conversions

        /// <summary>
        /// Converts an Either to Maybe, discarding the Left value.
        /// </summary>
        public static Maybe<TRight> ToMaybe<TLeft, TRight>(this Either<TLeft, TRight> either)
        {
            return either.IsRight ? Maybe<TRight>.Some(either.RightValue) : Maybe<TRight>.None;
        }

        /// <summary>
        /// Converts a Maybe to Either with a default Left value for None.
        /// </summary>
        public static Either<TLeft, T> ToEither<TLeft, T>(this Maybe<T> maybe, TLeft leftValue)
        {
            return maybe.HasValue
                ? Either<TLeft, T>.Right(maybe.Value)
                : Either<TLeft, T>.Left(leftValue);
        }

        /// <summary>
        /// Converts a Maybe to Either with a factory for the Left value.
        /// </summary>
        public static Either<TLeft, T> ToEither<TLeft, T>(this Maybe<T> maybe, Func<TLeft> leftFactory)
        {
            if (leftFactory == null) throw new ArgumentNullException(nameof(leftFactory));

            return maybe.HasValue
                ? Either<TLeft, T>.Right(maybe.Value)
                : Either<TLeft, T>.Left(leftFactory());
        }

        #endregion

        #region Async Operations

        /// <summary>
        /// Asynchronously transforms the Maybe value.
        /// </summary>
        public static async System.Threading.Tasks.Task<Maybe<TResult>> MapAsync<T, TResult>(
            this Maybe<T> maybe,
            Func<T, System.Threading.Tasks.Task<TResult>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            if (!maybe.HasValue)
            {
                return Maybe<TResult>.None;
            }

            var result = await selector(maybe.Value);
            return Maybe<TResult>.Some(result);
        }

        /// <summary>
        /// Asynchronously binds the Maybe to another Maybe.
        /// </summary>
        public static async System.Threading.Tasks.Task<Maybe<TResult>> BindAsync<T, TResult>(
            this Maybe<T> maybe,
            Func<T, System.Threading.Tasks.Task<Maybe<TResult>>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            if (!maybe.HasValue)
            {
                return Maybe<TResult>.None;
            }

            return await selector(maybe.Value);
        }

        #endregion

        #region Combination

        /// <summary>
        /// Combines two Maybe values into a tuple if both have values.
        /// </summary>
        public static Maybe<(T1, T2)> Combine<T1, T2>(this Maybe<T1> first, Maybe<T2> second)
        {
            if (first.HasValue && second.HasValue)
            {
                return Maybe<(T1, T2)>.Some((first.Value, second.Value));
            }
            return Maybe<(T1, T2)>.None;
        }

        /// <summary>
        /// Combines three Maybe values into a tuple if all have values.
        /// </summary>
        public static Maybe<(T1, T2, T3)> Combine<T1, T2, T3>(
            this Maybe<T1> first,
            Maybe<T2> second,
            Maybe<T3> third)
        {
            if (first.HasValue && second.HasValue && third.HasValue)
            {
                return Maybe<(T1, T2, T3)>.Some((first.Value, second.Value, third.Value));
            }
            return Maybe<(T1, T2, T3)>.None;
        }

        #endregion
    }
}

