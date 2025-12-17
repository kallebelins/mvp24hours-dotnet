//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// Represents the result of a typed operation execution.
    /// </summary>
    /// <typeparam name="T">The type of the operation output.</typeparam>
    public sealed class OperationResult<T> : IOperationResult<T>
    {
        private readonly List<IMessageResult> _messages;

        private OperationResult(T? value, bool isSuccess, IEnumerable<IMessageResult>? messages = null)
        {
            Value = value;
            IsSuccess = isSuccess;
            _messages = messages?.ToList() ?? [];
        }

        /// <inheritdoc/>
        public bool IsSuccess { get; }

        /// <inheritdoc/>
        public bool IsFailure => !IsSuccess;

        /// <inheritdoc/>
        public T? Value { get; }

        /// <inheritdoc/>
        public IReadOnlyList<IMessageResult> Messages => _messages.AsReadOnly();

        /// <inheritdoc/>
        public string? ErrorMessage => IsFailure
            ? string.Join("; ", _messages.Where(m => m.Type == MessageType.Error).Select(m => m.Message))
            : null;

        /// <summary>
        /// Creates a successful operation result with the given value.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <returns>A successful operation result.</returns>
        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>(value, true);
        }

        /// <summary>
        /// Creates a successful operation result with the given value and messages.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <param name="messages">Optional info/warning messages.</param>
        /// <returns>A successful operation result.</returns>
        public static OperationResult<T> Success(T value, IEnumerable<IMessageResult> messages)
        {
            return new OperationResult<T>(value, true, messages);
        }

        /// <summary>
        /// Creates a failed operation result with the given error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed operation result.</returns>
        public static OperationResult<T> Failure(string errorMessage)
        {
            return new OperationResult<T>(default, false, [new MessageResult(errorMessage, MessageType.Error)]);
        }

        /// <summary>
        /// Creates a failed operation result with the given messages.
        /// </summary>
        /// <param name="messages">The error messages.</param>
        /// <returns>A failed operation result.</returns>
        public static OperationResult<T> Failure(IEnumerable<IMessageResult> messages)
        {
            return new OperationResult<T>(default, false, messages);
        }

        /// <summary>
        /// Creates a failed operation result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <returns>A failed operation result.</returns>
        public static OperationResult<T> Failure(Exception exception)
        {
            return new OperationResult<T>(default, false, [new MessageResult(exception.Message, MessageType.Error)]);
        }

        /// <summary>
        /// Creates an operation result from a value and success flag.
        /// </summary>
        /// <param name="value">The result value (can be default for failures).</param>
        /// <param name="isSuccess">Whether the operation succeeded.</param>
        /// <param name="messages">Optional messages.</param>
        /// <returns>An operation result.</returns>
        public static OperationResult<T> Create(T? value, bool isSuccess, IEnumerable<IMessageResult>? messages = null)
        {
            return new OperationResult<T>(value, isSuccess, messages);
        }

        /// <summary>
        /// Converts this result to a different type using a transformation function.
        /// </summary>
        /// <typeparam name="TNew">The new result type.</typeparam>
        /// <param name="transform">The transformation function.</param>
        /// <returns>A new operation result with the transformed value.</returns>
        public OperationResult<TNew> Map<TNew>(Func<T, TNew> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (IsFailure)
                return OperationResult<TNew>.Failure(_messages);

            try
            {
                var newValue = transform(Value!);
                return OperationResult<TNew>.Success(newValue, _messages);
            }
            catch (Exception ex)
            {
                var messages = _messages.ToList();
                messages.Add(new MessageResult(ex.Message, MessageType.Error));
                return OperationResult<TNew>.Failure(messages);
            }
        }

        /// <summary>
        /// Chains this result with another operation that returns a result.
        /// </summary>
        /// <typeparam name="TNew">The new result type.</typeparam>
        /// <param name="bind">The function to chain.</param>
        /// <returns>The chained operation result.</returns>
        public OperationResult<TNew> Bind<TNew>(Func<T, OperationResult<TNew>> bind)
        {
            if (bind == null)
                throw new ArgumentNullException(nameof(bind));

            if (IsFailure)
                return OperationResult<TNew>.Failure(_messages);

            try
            {
                var result = bind(Value!);
                // Combine messages from both operations
                var combinedMessages = _messages.Concat(result.Messages).ToList();
                return OperationResult<TNew>.Create(result.Value, result.IsSuccess, combinedMessages);
            }
            catch (Exception ex)
            {
                var messages = _messages.ToList();
                messages.Add(new MessageResult(ex.Message, MessageType.Error));
                return OperationResult<TNew>.Failure(messages);
            }
        }

        /// <summary>
        /// Matches the result to execute different functions based on success or failure.
        /// </summary>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <param name="onSuccess">Function to execute on success.</param>
        /// <param name="onFailure">Function to execute on failure.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<IMessageResult>, TResult> onFailure)
        {
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

            return IsSuccess ? onSuccess(Value!) : onFailure(Messages);
        }

        /// <summary>
        /// Implicitly converts a value to a successful operation result.
        /// </summary>
        public static implicit operator OperationResult<T>(T value) => Success(value);
    }

    /// <summary>
    /// Non-generic operation result helper methods.
    /// </summary>
    public static class OperationResult
    {
        /// <summary>
        /// Creates a successful operation result with the given value.
        /// </summary>
        public static OperationResult<T> Success<T>(T value) => OperationResult<T>.Success(value);

        /// <summary>
        /// Creates a failed operation result with the given error message.
        /// </summary>
        public static OperationResult<T> Failure<T>(string errorMessage) => OperationResult<T>.Failure(errorMessage);

        /// <summary>
        /// Creates a failed operation result from an exception.
        /// </summary>
        public static OperationResult<T> Failure<T>(Exception exception) => OperationResult<T>.Failure(exception);

        /// <summary>
        /// Creates a successful operation result without a value (for void-like operations).
        /// </summary>
        public static OperationResult<object> Success() => OperationResult<object>.Success(new object());

        /// <summary>
        /// Creates a failed operation result without type information.
        /// </summary>
        public static OperationResult<object> Failure(string errorMessage) => OperationResult<object>.Failure(errorMessage);
    }
}

