//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Functional programming extensions for <see cref="IBusinessResultWithStatus{T}"/>.
    /// Provides Match, Bind, Map, and other Railway Oriented Programming methods.
    /// </summary>
    public static class BusinessResultWithStatusExtensions
    {
        #region [ Match Methods ]

        /// <summary>
        /// Matches the result and executes the appropriate function.
        /// </summary>
        public static TResult Match<T, TResult>(
            this IBusinessResultWithStatus<T> result,
            Func<T, TResult> onSuccess,
            Func<ResultStatusCode, IReadOnlyCollection<IResultMessage>, TResult> onFailure)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.HasErrors
                ? onFailure(result.StatusCode, result.Errors)
                : onSuccess(result.Data!);
        }

        /// <summary>
        /// Matches the result based on status code.
        /// </summary>
        public static TResult MatchStatus<T, TResult>(
            this IBusinessResultWithStatus<T> result,
            Func<T, TResult> onSuccess,
            Func<TResult> onNotFound,
            Func<IReadOnlyCollection<IResultMessage>, TResult> onValidationFailed,
            Func<ResultStatusCode, IReadOnlyCollection<IResultMessage>, TResult> onOtherFailure)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.StatusCode switch
            {
                ResultStatusCode.Success => onSuccess(result.Data!),
                ResultStatusCode.NotFound => onNotFound(),
                ResultStatusCode.ValidationFailed => onValidationFailed(result.Errors),
                _ when result.HasErrors => onOtherFailure(result.StatusCode, result.Errors),
                _ => onSuccess(result.Data!)
            };
        }

        #endregion

        #region [ Map Methods ]

        /// <summary>
        /// Maps the data to a new type if successful.
        /// </summary>
        public static IBusinessResultWithStatus<TNew> Map<T, TNew>(
            this IBusinessResultWithStatus<T> result,
            Func<T, TNew> mapper)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                return new BusinessResultWithStatus<TNew>(
                    data: default,
                    statusCode: result.StatusCode,
                    messages: result.ExtendedMessages,
                    token: result.Token);
            }

            return new BusinessResultWithStatus<TNew>(
                data: mapper(result.Data!),
                statusCode: result.StatusCode,
                messages: result.ExtendedMessages,
                token: result.Token);
        }

        /// <summary>
        /// Async version of Map.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<TNew>> MapAsync<T, TNew>(
            this IBusinessResultWithStatus<T> result,
            Func<T, Task<TNew>> mapper)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                return new BusinessResultWithStatus<TNew>(
                    data: default,
                    statusCode: result.StatusCode,
                    messages: result.ExtendedMessages,
                    token: result.Token);
            }

            var mappedData = await mapper(result.Data!);

            return new BusinessResultWithStatus<TNew>(
                data: mappedData,
                statusCode: result.StatusCode,
                messages: result.ExtendedMessages,
                token: result.Token);
        }

        #endregion

        #region [ Bind Methods ]

        /// <summary>
        /// Binds the result to another operation that returns a result.
        /// </summary>
        public static IBusinessResultWithStatus<TNew> Bind<T, TNew>(
            this IBusinessResultWithStatus<T> result,
            Func<T, IBusinessResultWithStatus<TNew>> binder)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                return new BusinessResultWithStatus<TNew>(
                    data: default,
                    statusCode: result.StatusCode,
                    messages: result.ExtendedMessages,
                    token: result.Token);
            }

            var bindResult = binder(result.Data!);
            bindResult.SetToken(result.Token);

            return bindResult;
        }

        /// <summary>
        /// Async version of Bind.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<TNew>> BindAsync<T, TNew>(
            this IBusinessResultWithStatus<T> result,
            Func<T, Task<IBusinessResultWithStatus<TNew>>> binder)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                return new BusinessResultWithStatus<TNew>(
                    data: default,
                    statusCode: result.StatusCode,
                    messages: result.ExtendedMessages,
                    token: result.Token);
            }

            var bindResult = await binder(result.Data!);
            bindResult.SetToken(result.Token);

            return bindResult;
        }

        #endregion

        #region [ Tap Methods ]

        /// <summary>
        /// Executes a side effect if successful.
        /// </summary>
        public static IBusinessResultWithStatus<T> Tap<T>(
            this IBusinessResultWithStatus<T> result,
            Action<T> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (!result.HasErrors)
            {
                action(result.Data!);
            }

            return result;
        }

        /// <summary>
        /// Executes a side effect on errors.
        /// </summary>
        public static IBusinessResultWithStatus<T> TapError<T>(
            this IBusinessResultWithStatus<T> result,
            Action<ResultStatusCode, IReadOnlyCollection<IResultMessage>> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                action(result.StatusCode, result.Errors);
            }

            return result;
        }

        /// <summary>
        /// Executes a side effect on warnings.
        /// </summary>
        public static IBusinessResultWithStatus<T> TapWarning<T>(
            this IBusinessResultWithStatus<T> result,
            Action<IReadOnlyCollection<IResultMessage>> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasWarnings)
            {
                action(result.Warnings);
            }

            return result;
        }

        #endregion

        #region [ Ensure Methods ]

        /// <summary>
        /// Ensures a condition is met, returning a failure if not.
        /// </summary>
        public static IBusinessResultWithStatus<T> Ensure<T>(
            this IBusinessResultWithStatus<T> result,
            Func<T, bool> predicate,
            ResultStatusCode statusCode,
            string errorMessage,
            string? errorCode = null)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                return result;
            }

            if (!predicate(result.Data!))
            {
                return BusinessResultWithStatus.Failure<T>(
                    statusCode,
                    errorMessage,
                    errorCode,
                    token: result.Token);
            }

            return result;
        }

        #endregion

        #region [ Conversion Methods ]

        /// <summary>
        /// Converts to a standard IBusinessResult.
        /// </summary>
        public static IBusinessResult<T> ToBusinessResult<T>(
            this IBusinessResultWithStatus<T> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return new Core.ValueObjects.Logic.BusinessResult<T>(
                data: result.Data,
                messages: result.Messages,
                token: result.Token);
        }

        /// <summary>
        /// Gets the first error message or null.
        /// </summary>
        public static string? GetFirstError<T>(this IBusinessResultWithStatus<T> result)
        {
            return result?.Errors.Count > 0
                ? result.Errors.GetEnumerator().Current?.Message
                : null;
        }

        /// <summary>
        /// Gets the first warning message or null.
        /// </summary>
        public static string? GetFirstWarning<T>(this IBusinessResultWithStatus<T> result)
        {
            return result?.Warnings.Count > 0
                ? result.Warnings.GetEnumerator().Current?.Message
                : null;
        }

        #endregion

        #region [ Utility Methods ]

        /// <summary>
        /// Gets the data or throws an exception with details from the result.
        /// </summary>
        public static T GetValueOrThrow<T>(this IBusinessResultWithStatus<T> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.HasErrors)
            {
                var errorMessages = new List<string>();
                foreach (var error in result.Errors)
                {
                    errorMessages.Add(error.Message);
                }

                throw new InvalidOperationException(
                    $"Result has errors (StatusCode={result.StatusCode}): {string.Join("; ", errorMessages)}");
            }

            return result.Data!;
        }

        /// <summary>
        /// Gets the data or the default value.
        /// </summary>
        public static T GetValueOrDefault<T>(
            this IBusinessResultWithStatus<T> result,
            T defaultValue = default!)
        {
            return result != null && !result.HasErrors ? result.Data! : defaultValue;
        }

        /// <summary>
        /// Provides an alternative result if the current result has errors.
        /// </summary>
        public static IBusinessResultWithStatus<T> OrElse<T>(
            this IBusinessResultWithStatus<T> result,
            Func<IBusinessResultWithStatus<T>> fallback)
        {
            return result != null && !result.HasErrors ? result : fallback();
        }

        /// <summary>
        /// Async version of OrElse.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<T>> OrElseAsync<T>(
            this IBusinessResultWithStatus<T> result,
            Func<Task<IBusinessResultWithStatus<T>>> fallback)
        {
            return result != null && !result.HasErrors ? result : await fallback();
        }

        #endregion
    }
}

