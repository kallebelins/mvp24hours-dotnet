//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Contract
{
    /// <summary>
    /// Unified interface for email sending operations across different providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a consistent API for sending emails regardless of the
    /// underlying email provider (SMTP, SendGrid, Azure Communication Services, etc.).
    /// All operations are asynchronous and support cancellation tokens for proper resource management.
    /// </para>
    /// <para>
    /// <strong>Email Format Support:</strong>
    /// The interface supports both HTML and plain text email bodies. You can provide either
    /// or both formats. If both are provided, the provider will typically send a multipart
    /// email with both formats, allowing email clients to choose the appropriate format.
    /// </para>
    /// <para>
    /// <strong>Attachments:</strong>
    /// Email attachments are supported via the <see cref="IEmailAttachment"/> interface.
    /// Attachments can be provided as byte arrays, streams, or file paths (provider-dependent).
    /// </para>
    /// <para>
    /// <strong>Default Options:</strong>
    /// Default values from <see cref="EmailOptions"/> are applied when not explicitly
    /// specified in the email message (e.g., From address, Reply-To).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Send a simple text email
    /// var message = new EmailMessage
    /// {
    ///     To = new[] { "recipient@example.com" },
    ///     Subject = "Hello",
    ///     PlainTextBody = "This is a plain text email."
    /// };
    /// var result = await emailService.SendAsync(message, cancellationToken);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Email sent: {result.MessageId}");
    /// }
    /// 
    /// // Send an HTML email with attachment
    /// var htmlMessage = new EmailMessage
    /// {
    ///     To = new[] { "recipient@example.com" },
    ///     Subject = "Report",
    ///     HtmlBody = "&lt;h1&gt;Monthly Report&lt;/h1&gt;&lt;p&gt;Please find attached.&lt;/p&gt;",
    ///     Attachments = new[]
    ///     {
    ///         new EmailAttachment("report.pdf", pdfBytes, "application/pdf")
    ///     }
    /// };
    /// var htmlResult = await emailService.SendAsync(htmlMessage, cancellationToken);
    /// </code>
    /// </example>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and error information.</returns>
        /// <remarks>
        /// <para>
        /// This method sends a single email message. The message can include HTML and/or plain text
        /// body, attachments, CC/BCC recipients, and custom headers.
        /// </para>
        /// <para>
        /// <strong>Validation:</strong>
        /// The message is validated before sending. Required fields include:
        /// - At least one recipient (To, CC, or BCC)
        /// - Subject
        /// - At least one body format (HtmlBody or PlainTextBody)
        /// </para>
        /// <para>
        /// <strong>Default Values:</strong>
        /// If the message doesn't specify a From address, Reply-To, or other optional fields,
        /// the values from <see cref="EmailOptions"/> are used.
        /// </para>
        /// <para>
        /// <strong>Error Handling:</strong>
        /// If the email fails to send, the result will contain error information. Common failures
        /// include invalid email addresses, provider errors, network issues, or rate limiting.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        Task<EmailSendResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple email messages in batch.
        /// </summary>
        /// <param name="messages">The email messages to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A collection of results, one for each message sent.</returns>
        /// <remarks>
        /// <para>
        /// This method sends multiple email messages. The implementation may optimize batch sending
        /// depending on the provider (e.g., using bulk send APIs when available).
        /// </para>
        /// <para>
        /// <strong>Partial Success:</strong>
        /// If some emails succeed and others fail, the results collection will contain both
        /// successful and failed results. Check each result individually to determine success.
        /// </para>
        /// <para>
        /// <strong>Rate Limiting:</strong>
        /// Some providers may rate limit batch operations. The implementation should handle
        /// rate limiting gracefully, potentially splitting the batch or retrying with backoff.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
        Task<IList<EmailSendResult>> SendBatchAsync(
            IEnumerable<EmailMessage> messages,
            CancellationToken cancellationToken = default);
    }
}

