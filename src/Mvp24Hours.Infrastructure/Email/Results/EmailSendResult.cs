//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Email.Results
{
    /// <summary>
    /// Represents the result of an email sending operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of sending an email, including success status,
    /// message ID (if provided by the email provider), and any error information.
    /// </remarks>
    /// <para>
    /// The result is immutable and provides factory methods for creating success and failure results.
    /// </para>
    public class EmailSendResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSendResult"/> class.
        /// </summary>
        /// <param name="success">Whether the email was sent successfully.</param>
        /// <param name="messageId">The message ID provided by the email provider (if successful).</param>
        /// <param name="errors">List of error messages (if failed).</param>
        /// <param name="exception">The exception that occurred (if failed).</param>
        /// <param name="sentAt">When the email was sent (or attempted).</param>
        private EmailSendResult(
            bool success,
            string? messageId = null,
            IList<string>? errors = null,
            Exception? exception = null,
            DateTimeOffset? sentAt = null)
        {
            Success = success;
            MessageId = messageId;
            Errors = errors ?? new List<string>();
            Exception = exception;
            SentAt = sentAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets whether the email was sent successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the message ID provided by the email provider (if successful).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The message ID is a unique identifier assigned by the email provider. It can be used
        /// to track the email, check delivery status, or retrieve logs.
        /// </para>
        /// <para>
        /// Not all providers return a message ID. If the provider doesn't provide one, this
        /// will be <c>null</c> even if the email was sent successfully.
        /// </para>
        /// </remarks>
        public string? MessageId { get; }

        /// <summary>
        /// Gets the list of error messages (if failed).
        /// </summary>
        /// <remarks>
        /// This collection contains human-readable error messages describing why the email
        /// failed to send. Common errors include invalid email addresses, provider errors,
        /// network issues, or rate limiting.
        /// </remarks>
        public IList<string> Errors { get; }

        /// <summary>
        /// Gets the exception that occurred during sending, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets when the email sending operation completed (or was attempted).
        /// </summary>
        public DateTimeOffset SentAt { get; }

        /// <summary>
        /// Gets whether the email sending failed.
        /// </summary>
        public bool IsFailure => !Success;

        /// <summary>
        /// Gets the first error message, if any; otherwise, <c>null</c>.
        /// </summary>
        /// <remarks>
        /// This is a convenience property for accessing the first error when you only need
        /// a single error message. If multiple errors occurred, use the <see cref="Errors"/>
        /// collection to access all errors.
        /// </remarks>
        public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

        /// <summary>
        /// Creates a successful email send result.
        /// </summary>
        /// <param name="messageId">Optional message ID provided by the email provider.</param>
        /// <param name="sentAt">When the email was sent.</param>
        /// <returns>A result indicating successful email send.</returns>
        public static EmailSendResult Successful(
            string? messageId = null,
            DateTimeOffset? sentAt = null)
        {
            return new EmailSendResult(
                success: true,
                messageId: messageId,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed email send result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the email failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="sentAt">When the email sending was attempted.</param>
        /// <returns>A result indicating failed email send.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static EmailSendResult Failed(
            string errorMessage,
            Exception? exception = null,
            DateTimeOffset? sentAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new EmailSendResult(
                success: false,
                errors: new List<string> { errorMessage },
                exception: exception,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed email send result with multiple errors.
        /// </summary>
        /// <param name="errors">The list of error messages.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="sentAt">When the email sending was attempted.</param>
        /// <returns>A result indicating failed email send.</returns>
        /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
        /// <exception cref="ArgumentException">Thrown when errors is empty.</exception>
        public static EmailSendResult Failed(
            IList<string> errors,
            Exception? exception = null,
            DateTimeOffset? sentAt = null)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            if (errors.Count == 0)
            {
                throw new ArgumentException("Errors list cannot be empty.", nameof(errors));
            }

            return new EmailSendResult(
                success: false,
                errors: errors,
                exception: exception,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed email send result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="sentAt">When the email sending was attempted.</param>
        /// <returns>A result indicating failed email send.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static EmailSendResult Failed(
            Exception exception,
            DateTimeOffset? sentAt = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new EmailSendResult(
                success: false,
                errors: new List<string> { exception.Message },
                exception: exception,
                sentAt: sentAt);
        }
    }
}

