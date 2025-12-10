//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// Static factory class for creating BusinessResult instances.
    /// Provides a fluent API for creating success and failure results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory provides static methods for creating business results following
    /// the Result Pattern, making it easier to work with success/failure outcomes.
    /// </para>
    /// <para>
    /// <strong>Railway Oriented Programming:</strong>
    /// Use the <c>Bind</c> and <c>Map</c> extension methods to chain operations
    /// that may succeed or fail.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a success result
    /// var result = BusinessResult.Success(order);
    /// 
    /// // Create a failure result
    /// var failure = BusinessResult.Failure&lt;Order&gt;("Order not found", "NOT_FOUND");
    /// 
    /// // From data
    /// var fromData = BusinessResult.From(existingOrder);
    /// 
    /// // Check result
    /// if (result.IsSuccess)
    /// {
    ///     // Process order
    /// }
    /// </code>
    /// </example>
    public static class BusinessResult
    {
        #region [ Success Methods ]

        /// <summary>
        /// Creates a successful result with the specified data.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="data">The data to wrap in the result.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A successful business result containing the data.</returns>
        public static IBusinessResult<T> Success<T>(T data, string? token = null)
        {
            return new BusinessResult<T>(
                data: data,
                messages: null,
                token: token);
        }

        /// <summary>
        /// Creates a successful result with the specified data and an info message.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="data">The data to wrap in the result.</param>
        /// <param name="message">An informational message.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A successful business result containing the data and message.</returns>
        public static IBusinessResult<T> Success<T>(T data, string message, string? token = null)
        {
            var messages = new List<IMessageResult>
            {
                new MessageResult(message, MessageType.Info)
            };

            return new BusinessResult<T>(
                data: data,
                messages: new ReadOnlyCollection<IMessageResult>(messages),
                token: token);
        }

        /// <summary>
        /// Creates a successful result with no data (for void operations).
        /// </summary>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A successful business result with default data.</returns>
        public static IBusinessResult<T> Success<T>(string? token = null)
        {
            return new BusinessResult<T>(
                data: default,
                messages: null,
                token: token);
        }

        #endregion

        #region [ Failure Methods ]

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <typeparam name="T">The type of data (will be default).</typeparam>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorKey">Optional error key for categorization.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A failed business result containing the error.</returns>
        public static IBusinessResult<T> Failure<T>(string errorMessage, string? errorKey = null, string? token = null)
        {
            var messages = new List<IMessageResult>
            {
                new MessageResult(errorKey ?? "Error", errorMessage, MessageType.Error)
            };

            return new BusinessResult<T>(
                data: default,
                messages: new ReadOnlyCollection<IMessageResult>(messages),
                token: token);
        }

        /// <summary>
        /// Creates a failed result with multiple error messages.
        /// </summary>
        /// <typeparam name="T">The type of data (will be default).</typeparam>
        /// <param name="errors">The collection of error messages.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A failed business result containing the errors.</returns>
        public static IBusinessResult<T> Failure<T>(IEnumerable<IMessageResult> errors, string? token = null)
        {
            var messages = new List<IMessageResult>();
            if (errors != null)
            {
                messages.AddRange(errors);
            }

            return new BusinessResult<T>(
                data: default,
                messages: new ReadOnlyCollection<IMessageResult>(messages),
                token: token);
        }

        /// <summary>
        /// Creates a failed result with multiple error messages.
        /// </summary>
        /// <typeparam name="T">The type of data (will be default).</typeparam>
        /// <param name="errors">The collection of error key/message pairs.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A failed business result containing the errors.</returns>
        public static IBusinessResult<T> Failure<T>(IEnumerable<(string key, string message)> errors, string? token = null)
        {
            var messages = new List<IMessageResult>();
            if (errors != null)
            {
                foreach (var (key, message) in errors)
                {
                    messages.Add(new MessageResult(key, message, MessageType.Error));
                }
            }

            return new BusinessResult<T>(
                data: default,
                messages: new ReadOnlyCollection<IMessageResult>(messages),
                token: token);
        }

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        /// <typeparam name="T">The type of data (will be default).</typeparam>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A failed business result containing the exception message.</returns>
        public static IBusinessResult<T> Failure<T>(Exception exception, string? token = null)
        {
            var errorKey = exception.GetType().Name;
            var errorMessage = exception.Message;

            return Failure<T>(errorMessage, errorKey, token);
        }

        #endregion

        #region [ From Methods ]

        /// <summary>
        /// Creates a result from the specified data.
        /// Returns success if data is not null, failure otherwise.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="data">The data to wrap in the result.</param>
        /// <param name="notFoundMessage">Message to use if data is null.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A business result based on the data.</returns>
        public static IBusinessResult<T> From<T>(T? data, string notFoundMessage = "Resource not found", string? token = null)
            where T : class
        {
            if (data == null)
            {
                return Failure<T>(notFoundMessage, "NOT_FOUND", token);
            }

            return Success(data, token);
        }

        /// <summary>
        /// Creates a result from a nullable value type.
        /// Returns success if value has a value, failure otherwise.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="data">The nullable value to wrap in the result.</param>
        /// <param name="notFoundMessage">Message to use if value is null.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A business result based on the value.</returns>
        public static IBusinessResult<T> FromValue<T>(T? data, string notFoundMessage = "Resource not found", string? token = null)
            where T : struct
        {
            if (!data.HasValue)
            {
                return Failure<T>(notFoundMessage, "NOT_FOUND", token);
            }

            return Success(data.Value, token);
        }

        /// <summary>
        /// Creates a result based on a condition.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="successData">Data to return if condition is true.</param>
        /// <param name="failureMessage">Message to return if condition is false.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A business result based on the condition.</returns>
        public static IBusinessResult<T> FromCondition<T>(
            bool condition,
            T successData,
            string failureMessage = "Condition not met",
            string? token = null)
        {
            return condition
                ? Success(successData, token)
                : Failure<T>(failureMessage, "CONDITION_FAILED", token);
        }

        #endregion

        #region [ Combine Methods ]

        /// <summary>
        /// Combines multiple results. If any result has errors, returns a failure with all errors.
        /// </summary>
        /// <param name="results">The results to combine.</param>
        /// <returns>A combined business result.</returns>
        public static IBusinessResult<bool> Combine(params object[] results)
        {
            var allMessages = new List<IMessageResult>();
            var hasErrors = false;

            foreach (var result in results)
            {
                var resultType = result.GetType();
                if (resultType.IsGenericType)
                {
                    var messagesProperty = resultType.GetProperty("Messages");
                    var hasErrorsProperty = resultType.GetProperty("HasErrors");

                    if (messagesProperty != null)
                    {
                        var messages = messagesProperty.GetValue(result) as IReadOnlyCollection<IMessageResult>;
                        if (messages != null)
                        {
                            allMessages.AddRange(messages);
                        }
                    }

                    if (hasErrorsProperty != null)
                    {
                        var resultHasErrors = (bool)(hasErrorsProperty.GetValue(result) ?? false);
                        if (resultHasErrors)
                        {
                            hasErrors = true;
                        }
                    }
                }
            }

            return new BusinessResult<bool>(
                data: !hasErrors,
                messages: new ReadOnlyCollection<IMessageResult>(allMessages));
        }

        #endregion
    }
}

