//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Sms.Results
{
    /// <summary>
    /// Represents the result of an SMS sending operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of sending an SMS, including success status,
    /// message ID (if provided by the SMS provider), delivery status, and any error information.
    /// </para>
    /// <para>
    /// The result is immutable and provides factory methods for creating success and failure results.
    /// </para>
    /// </remarks>
    public class SmsSendResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsSendResult"/> class.
        /// </summary>
        /// <param name="success">Whether the SMS was sent successfully.</param>
        /// <param name="messageId">The message ID provided by the SMS provider (if successful).</param>
        /// <param name="status">The delivery status of the SMS.</param>
        /// <param name="errors">List of error messages (if failed).</param>
        /// <param name="exception">The exception that occurred (if failed).</param>
        /// <param name="sentAt">When the SMS was sent (or attempted).</param>
        private SmsSendResult(
            bool success,
            string? messageId = null,
            SmsDeliveryStatus status = SmsDeliveryStatus.Unknown,
            IList<string>? errors = null,
            Exception? exception = null,
            DateTimeOffset? sentAt = null)
        {
            Success = success;
            MessageId = messageId;
            Status = status;
            Errors = errors ?? new List<string>();
            Exception = exception;
            SentAt = sentAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets whether the SMS was sent successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the message ID provided by the SMS provider (if successful).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The message ID is a unique identifier assigned by the SMS provider. It can be used
        /// to track the SMS, check delivery status, or retrieve logs.
        /// </para>
        /// <para>
        /// Not all providers return a message ID immediately. Some providers return a message ID
        /// only after the message is queued or sent. If the provider doesn't provide one, this
        /// will be <c>null</c> even if the SMS was sent successfully.
        /// </para>
        /// </remarks>
        public string? MessageId { get; }

        /// <summary>
        /// Gets the delivery status of the SMS.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The delivery status indicates the current state of the SMS message. Common statuses include:
        /// - <see cref="SmsDeliveryStatus.Queued"/>: Message is queued for sending
        /// - <see cref="SmsDeliveryStatus.Sent"/>: Message was sent to the carrier
        /// - <see cref="SmsDeliveryStatus.Delivered"/>: Message was delivered to the recipient
        /// - <see cref="SmsDeliveryStatus.Failed"/>: Message failed to send or deliver
        /// - <see cref="SmsDeliveryStatus.Unknown"/>: Status is not available or not yet determined
        /// </para>
        /// <para>
        /// The status may change over time as the provider receives delivery updates. Some providers
        /// support webhooks or polling to get updated status information.
        /// </para>
        /// </remarks>
        public SmsDeliveryStatus Status { get; }

        /// <summary>
        /// Gets the list of error messages (if failed).
        /// </summary>
        /// <remarks>
        /// This collection contains human-readable error messages describing why the SMS
        /// failed to send. Common errors include invalid phone numbers, provider errors,
        /// network issues, rate limiting, or insufficient credits/balance.
        /// </remarks>
        public IList<string> Errors { get; }

        /// <summary>
        /// Gets the exception that occurred during sending, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets when the SMS sending operation completed (or was attempted).
        /// </summary>
        public DateTimeOffset SentAt { get; }

        /// <summary>
        /// Gets whether the SMS sending failed.
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
        /// Creates a successful SMS send result.
        /// </summary>
        /// <param name="messageId">Optional message ID provided by the SMS provider.</param>
        /// <param name="status">The delivery status of the SMS.</param>
        /// <param name="sentAt">When the SMS was sent.</param>
        /// <returns>A result indicating successful SMS send.</returns>
        public static SmsSendResult Successful(
            string? messageId = null,
            SmsDeliveryStatus status = SmsDeliveryStatus.Queued,
            DateTimeOffset? sentAt = null)
        {
            return new SmsSendResult(
                success: true,
                messageId: messageId,
                status: status,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed SMS send result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the SMS failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="sentAt">When the SMS sending was attempted.</param>
        /// <returns>A result indicating failed SMS send.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static SmsSendResult Failed(
            string errorMessage,
            Exception? exception = null,
            DateTimeOffset? sentAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new SmsSendResult(
                success: false,
                status: SmsDeliveryStatus.Failed,
                errors: new List<string> { errorMessage },
                exception: exception,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed SMS send result with multiple errors.
        /// </summary>
        /// <param name="errors">The list of error messages.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="sentAt">When the SMS sending was attempted.</param>
        /// <returns>A result indicating failed SMS send.</returns>
        /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
        /// <exception cref="ArgumentException">Thrown when errors is empty.</exception>
        public static SmsSendResult Failed(
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

            return new SmsSendResult(
                success: false,
                status: SmsDeliveryStatus.Failed,
                errors: errors,
                exception: exception,
                sentAt: sentAt);
        }

        /// <summary>
        /// Creates a failed SMS send result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="sentAt">When the SMS sending was attempted.</param>
        /// <returns>A result indicating failed SMS send.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static SmsSendResult Failed(
            Exception exception,
            DateTimeOffset? sentAt = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new SmsSendResult(
                success: false,
                status: SmsDeliveryStatus.Failed,
                errors: new List<string> { exception.Message },
                exception: exception,
                sentAt: sentAt);
        }
    }

    /// <summary>
    /// Represents the delivery status of an SMS message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The delivery status indicates the current state of the SMS message in the delivery pipeline.
    /// Status updates may be received asynchronously via webhooks or polling, depending on the provider.
    /// </para>
    /// </remarks>
    public enum SmsDeliveryStatus
    {
        /// <summary>
        /// Status is unknown or not yet determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Message is queued for sending.
        /// </summary>
        Queued = 1,

        /// <summary>
        /// Message was sent to the carrier/provider.
        /// </summary>
        Sent = 2,

        /// <summary>
        /// Message was delivered to the recipient's device.
        /// </summary>
        Delivered = 3,

        /// <summary>
        /// Message delivery failed.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Message was undelivered (e.g., invalid number, carrier rejection).
        /// </summary>
        Undelivered = 5
    }
}

