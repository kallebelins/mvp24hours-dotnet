//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Functional programming extensions for IBusinessResult.
    /// Provides Match, Bind, Map, and other Railway Oriented Programming methods.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions enable functional programming patterns with business results,
    /// allowing clean composition of operations that may fail.
    /// </para>
    /// <para>
    /// <strong>Railway Oriented Programming:</strong>
    /// <list type="bullet">
    /// <item><c>Map</c> - Transform success data, bypass on failure</item>
    /// <item><c>Bind</c> - Chain operations that return results</item>
    /// <item><c>Match</c> - Handle both success and failure cases</item>
    /// <item><c>Tap</c> - Execute side effects without changing the result</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class BusinessResultFunctionalExtensions
    {
        #region [ Match Methods ]

        /// <summary>
        /// Matches the result and executes the appropriate function.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <typeparam name="TResult">The type of the match result.</typeparam>
        /// <param name="result">The business result to match.</param>
        /// <param name="onSuccess">Function to execute if successful.</param>
        /// <param name="onFailure">Function to execute if failed.</param>
        /// <returns>The result of the matched function.</returns>
        /// <example>
        /// <code>
        /// var message = result.Match(
        ///     onSuccess: order => $"Order {order.Id} created",
        ///     onFailure: errors => $"Failed: {string.Join(", ", errors.Select(e => e.Message))}"
        /// );
        /// </code>
        /// </example>
        public static TResult Match<T, TResult>(
            this IBusinessResult<T> result,
            Func<T, TResult> onSuccess,
            Func<IReadOnlyCollection<IMessageResult>, TResult> onFailure)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return result.HasErrors
                ? onFailure(result.Messages ?? new List<IMessageResult>().AsReadOnly())
                : onSuccess(result.Data);
        }

        /// <summary>
        /// Matches the result and executes the appropriate action.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result to match.</param>
        /// <param name="onSuccess">Action to execute if successful.</param>
        /// <param name="onFailure">Action to execute if failed.</param>
        /// <returns>The original result for chaining.</returns>
        public static IBusinessResult<T> Match<T>(
            this IBusinessResult<T> result,
            Action<T> onSuccess,
            Action<IReadOnlyCollection<IMessageResult>> onFailure)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                onFailure(result.Messages ?? new List<IMessageResult>().AsReadOnly());
            }
            else
            {
                onSuccess(result.Data);
            }

            return result;
        }

        /// <summary>
        /// Async version of Match that executes the appropriate function.
        /// </summary>
        public static async Task<TResult> MatchAsync<T, TResult>(
            this IBusinessResult<T> result,
            Func<T, Task<TResult>> onSuccess,
            Func<IReadOnlyCollection<IMessageResult>, Task<TResult>> onFailure)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return result.HasErrors
                ? await onFailure(result.Messages ?? new List<IMessageResult>().AsReadOnly())
                : await onSuccess(result.Data);
        }

        #endregion

        #region [ Map Methods ]

        /// <summary>
        /// Maps the data to a new type if the result is successful.
        /// If the result has errors, the errors are passed through.
        /// </summary>
        /// <typeparam name="T">The source type.</typeparam>
        /// <typeparam name="TNew">The target type.</typeparam>
        /// <param name="result">The business result to map.</param>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new result with the mapped data or the original errors.</returns>
        /// <example>
        /// <code>
        /// var orderDto = result.Map(order => new OrderDto
        /// {
        ///     Id = order.Id,
        ///     CustomerName = order.Customer.Name
        /// });
        /// </code>
        /// </example>
        public static IBusinessResult<TNew> Map<T, TNew>(
            this IBusinessResult<T> result,
            Func<T, TNew> mapper)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                return new BusinessResult<TNew>(
                    data: default,
                    messages: result.Messages,
                    token: result.Token);
            }

            return new BusinessResult<TNew>(
                data: mapper(result.Data),
                messages: result.Messages,
                token: result.Token);
        }

        /// <summary>
        /// Async version of Map.
        /// </summary>
        public static async Task<IBusinessResult<TNew>> MapAsync<T, TNew>(
            this IBusinessResult<T> result,
            Func<T, Task<TNew>> mapper)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                return new BusinessResult<TNew>(
                    data: default,
                    messages: result.Messages,
                    token: result.Token);
            }

            var mappedData = await mapper(result.Data);

            return new BusinessResult<TNew>(
                data: mappedData,
                messages: result.Messages,
                token: result.Token);
        }

        /// <summary>
        /// Async version of Map for Task results.
        /// </summary>
        public static async Task<IBusinessResult<TNew>> MapAsync<T, TNew>(
            this Task<IBusinessResult<T>> resultTask,
            Func<T, TNew> mapper)
        {
            var result = await resultTask;
            return result.Map(mapper);
        }

        #endregion

        #region [ Bind Methods ]

        /// <summary>
        /// Binds the result to another operation that returns a result.
        /// If the current result has errors, the next operation is not executed.
        /// </summary>
        /// <typeparam name="T">The source type.</typeparam>
        /// <typeparam name="TNew">The target type.</typeparam>
        /// <param name="result">The business result to bind.</param>
        /// <param name="binder">The function that returns a new result.</param>
        /// <returns>The result of the binder function or the original errors.</returns>
        /// <example>
        /// <code>
        /// var result = GetOrder(orderId)
        ///     .Bind(order => ValidateOrder(order))
        ///     .Bind(order => ProcessOrder(order));
        /// </code>
        /// </example>
        public static IBusinessResult<TNew> Bind<T, TNew>(
            this IBusinessResult<T> result,
            Func<T, IBusinessResult<TNew>> binder)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                return new BusinessResult<TNew>(
                    data: default,
                    messages: result.Messages,
                    token: result.Token);
            }

            var bindResult = binder(result.Data);

            // Preserve token from original result if new result doesn't have one
            if (string.IsNullOrEmpty(bindResult.Token) && !string.IsNullOrEmpty(result.Token))
            {
                bindResult.SetToken(result.Token);
            }

            return bindResult;
        }

        /// <summary>
        /// Async version of Bind.
        /// </summary>
        public static async Task<IBusinessResult<TNew>> BindAsync<T, TNew>(
            this IBusinessResult<T> result,
            Func<T, Task<IBusinessResult<TNew>>> binder)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                return new BusinessResult<TNew>(
                    data: default,
                    messages: result.Messages,
                    token: result.Token);
            }

            var bindResult = await binder(result.Data);

            if (string.IsNullOrEmpty(bindResult.Token) && !string.IsNullOrEmpty(result.Token))
            {
                bindResult.SetToken(result.Token);
            }

            return bindResult;
        }

        /// <summary>
        /// Async version of Bind for Task results.
        /// </summary>
        public static async Task<IBusinessResult<TNew>> BindAsync<T, TNew>(
            this Task<IBusinessResult<T>> resultTask,
            Func<T, IBusinessResult<TNew>> binder)
        {
            var result = await resultTask;
            return result.Bind(binder);
        }

        /// <summary>
        /// Async version of Bind for Task results with async binder.
        /// </summary>
        public static async Task<IBusinessResult<TNew>> BindAsync<T, TNew>(
            this Task<IBusinessResult<T>> resultTask,
            Func<T, Task<IBusinessResult<TNew>>> binder)
        {
            var result = await resultTask;
            return await result.BindAsync(binder);
        }

        #endregion

        #region [ Tap Methods ]

        /// <summary>
        /// Executes a side effect action if the result is successful.
        /// Returns the original result unchanged.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>The original result.</returns>
        /// <example>
        /// <code>
        /// var result = GetOrder(orderId)
        ///     .Tap(order => logger.LogInformation($"Processing order {order.Id}"))
        ///     .Bind(order => ProcessOrder(order));
        /// </code>
        /// </example>
        public static IBusinessResult<T> Tap<T>(
            this IBusinessResult<T> result,
            Action<T> action)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.HasErrors)
            {
                action(result.Data);
            }

            return result;
        }

        /// <summary>
        /// Executes a side effect action if the result has errors.
        /// Returns the original result unchanged.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <param name="action">The action to execute on failure.</param>
        /// <returns>The original result.</returns>
        public static IBusinessResult<T> TapError<T>(
            this IBusinessResult<T> result,
            Action<IReadOnlyCollection<IMessageResult>> action)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.HasErrors)
            {
                action(result.Messages ?? new List<IMessageResult>().AsReadOnly());
            }

            return result;
        }

        /// <summary>
        /// Async version of Tap.
        /// </summary>
        public static async Task<IBusinessResult<T>> TapAsync<T>(
            this IBusinessResult<T> result,
            Func<T, Task> action)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.HasErrors)
            {
                await action(result.Data);
            }

            return result;
        }

        #endregion

        #region [ Utility Methods ]

        /// <summary>
        /// Checks if the result is successful (no errors).
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool IsSuccess<T>(this IBusinessResult<T> result)
        {
            return result != null && !result.HasErrors;
        }

        /// <summary>
        /// Checks if the result has failed (has errors).
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <returns>True if failed, false otherwise.</returns>
        public static bool IsFailure<T>(this IBusinessResult<T> result)
        {
            return result == null || result.HasErrors;
        }

        /// <summary>
        /// Gets the data if successful, or the default value if failed.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The data if successful, or the default value.</returns>
        public static T GetValueOrDefault<T>(
            this IBusinessResult<T> result,
            T defaultValue = default!)
        {
            return result.IsSuccess() ? result.Data : defaultValue;
        }

        /// <summary>
        /// Gets the data if successful, or throws an exception if failed.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <returns>The data if successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the result has errors.</exception>
        public static T GetValueOrThrow<T>(this IBusinessResult<T> result)
        {
            if (result.IsFailure())
            {
                var errorMessages = result.Messages != null
                    ? string.Join("; ", System.Linq.Enumerable.Select(result.Messages, m => m.Message))
                    : "Unknown error";

                throw new InvalidOperationException($"Result has errors: {errorMessages}");
            }

            return result.Data;
        }

        /// <summary>
        /// Provides an alternative result if the current result has errors.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <param name="fallback">The fallback function.</param>
        /// <returns>The original result if successful, or the fallback result.</returns>
        public static IBusinessResult<T> OrElse<T>(
            this IBusinessResult<T> result,
            Func<IBusinessResult<T>> fallback)
        {
            return result.IsSuccess() ? result : fallback();
        }

        /// <summary>
        /// Async version of OrElse.
        /// </summary>
        public static async Task<IBusinessResult<T>> OrElseAsync<T>(
            this IBusinessResult<T> result,
            Func<Task<IBusinessResult<T>>> fallback)
        {
            return result.IsSuccess() ? result : await fallback();
        }

        /// <summary>
        /// Ensures a condition is met, returning a failure if not.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The business result.</param>
        /// <param name="predicate">The condition to check.</param>
        /// <param name="errorMessage">The error message if condition fails.</param>
        /// <param name="errorKey">Optional error key.</param>
        /// <returns>The original result if condition passes, or a failure.</returns>
        public static IBusinessResult<T> Ensure<T>(
            this IBusinessResult<T> result,
            Func<T, bool> predicate,
            string errorMessage,
            string? errorKey = null)
        {
            if (result.IsFailure())
            {
                return result;
            }

            if (!predicate(result.Data))
            {
                return BusinessResult.Failure<T>(errorMessage, errorKey ?? "VALIDATION_ERROR", result.Token);
            }

            return result;
        }

        #endregion
    }
}

