//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Contract
{
    /// <summary>
    /// Unified interface for SMS sending operations across different providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a consistent API for sending SMS messages regardless of the
    /// underlying SMS provider (Twilio, Azure Communication Services, etc.).
    /// All operations are asynchronous and support cancellation tokens for proper resource management.
    /// </para>
    /// <para>
    /// <strong>SMS Format Support:</strong>
    /// SMS messages are plain text and typically limited to 160 characters per message (GSM 7-bit)
    /// or 70 characters (UCS-2/Unicode). Longer messages are automatically split into multiple
    /// segments by most providers. The actual character limit depends on the provider and encoding.
    /// </para>
    /// <para>
    /// <strong>Default Options:</strong>
    /// Default values from <see cref="SmsOptions"/> are applied when not explicitly
    /// specified in the SMS message (e.g., From number, Country code).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Send a simple SMS
    /// var message = new SmsMessage
    /// {
    ///     To = "+5511999999999",
    ///     Body = "Hello, this is a test SMS message."
    /// };
    /// var result = await smsService.SendAsync(message, cancellationToken);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"SMS sent: {result.MessageId}");
    /// }
    /// 
    /// // Send SMS with explicit sender
    /// var messageWithSender = new SmsMessage
    /// {
    ///     To = "+5511999999999",
    ///     From = "+5511888888888",
    ///     Body = "Message from specific number"
    /// };
    /// var result2 = await smsService.SendAsync(messageWithSender, cancellationToken);
    /// </code>
    /// </example>
    public interface ISmsService
    {
        /// <summary>
        /// Sends an SMS message.
        /// </summary>
        /// <param name="message">The SMS message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        /// <remarks>
        /// <para>
        /// This method sends a single SMS message. The message must include a recipient phone number
        /// and message body. The sender (From) can be specified or will use the default from options.
        /// </para>
        /// <para>
        /// <strong>Validation:</strong>
        /// The message is validated before sending. Required fields include:
        /// - Recipient phone number (To) in E.164 format or national format
        /// - Message body (Body) - cannot be empty
        /// </para>
        /// <para>
        /// <strong>Default Values:</strong>
        /// If the message doesn't specify a From number, the default From from <see cref="SmsOptions"/>
        /// is used. If a country code is needed and not provided, the default country code is used.
        /// </para>
        /// <para>
        /// <strong>Error Handling:</strong>
        /// If the SMS fails to send, the result will contain error information. Common failures
        /// include invalid phone numbers, provider errors, network issues, rate limiting, or
        /// insufficient credits/balance.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        Task<SmsSendResult> SendAsync(
            SmsMessage message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple SMS messages in batch.
        /// </summary>
        /// <param name="messages">The SMS messages to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A collection of results, one for each message sent.</returns>
        /// <remarks>
        /// <para>
        /// This method sends multiple SMS messages. The implementation may optimize batch sending
        /// depending on the provider (e.g., using bulk send APIs when available).
        /// </para>
        /// <para>
        /// <strong>Partial Success:</strong>
        /// If some SMS messages succeed and others fail, the results collection will contain both
        /// successful and failed results. Check each result individually to determine success.
        /// </para>
        /// <para>
        /// <strong>Rate Limiting:</strong>
        /// Some providers may rate limit batch operations. The implementation should handle
        /// rate limiting gracefully, potentially splitting the batch or retrying with backoff.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
        Task<IList<SmsSendResult>> SendBatchAsync(
            IEnumerable<SmsMessage> messages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an MMS (Multimedia Messaging Service) message.
        /// </summary>
        /// <param name="message">The MMS message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        /// <remarks>
        /// <para>
        /// This method sends an MMS message with multimedia attachments (images, videos, audio).
        /// MMS allows sending media files along with text messages, unlike SMS which is text-only.
        /// </para>
        /// <para>
        /// <strong>MMS Support:</strong>
        /// Not all SMS providers support MMS. Check provider documentation to ensure MMS is supported.
        /// If MMS is not supported, this method may throw <see cref="NotSupportedException"/> or
        /// return a failed result.
        /// </para>
        /// <para>
        /// <strong>Size Limitations:</strong>
        /// MMS messages typically have size limits (e.g., 300KB-1MB depending on carrier).
        /// Check provider documentation for specific limits.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when MMS is not supported by the provider.</exception>
        Task<SmsSendResult> SendMmsAsync(
            Models.MmsMessage message,
            CancellationToken cancellationToken = default);
    }
}

