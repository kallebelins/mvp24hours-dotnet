//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Providers
{
    /// <summary>
    /// Base class for email providers that implements common validation and default application logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract class provides a foundation for implementing email providers by handling
    /// common tasks such as message validation, applying default values, and error handling.
    /// Derived classes only need to implement the actual sending logic.
    /// </para>
    /// </remarks>
    public abstract class BaseEmailProvider : IEmailService
    {
        /// <summary>
        /// Gets the email options.
        /// </summary>
        protected EmailOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEmailProvider"/> class.
        /// </summary>
        /// <param name="options">The email options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        protected BaseEmailProvider(IOptions<EmailOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEmailProvider"/> class.
        /// </summary>
        /// <param name="options">The email options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        protected BaseEmailProvider(EmailOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and error information.</returns>
        public async Task<EmailSendResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Validate message
            var validationErrors = ValidateMessage(message);
            if (validationErrors.Count > 0)
            {
                return EmailSendResult.Failed(validationErrors);
            }

            // Apply defaults
            var emailToSend = ApplyDefaults(message);

            try
            {
                // Delegate to derived class for actual sending
                var result = await SendEmailAsync(emailToSend, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                return EmailSendResult.Failed(ex);
            }
        }

        /// <summary>
        /// Sends multiple email messages in batch.
        /// </summary>
        /// <param name="messages">The email messages to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A collection of results, one for each message sent.</returns>
        public async Task<IList<EmailSendResult>> SendBatchAsync(
            IEnumerable<EmailMessage> messages,
            CancellationToken cancellationToken = default)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var results = new List<EmailSendResult>();
            var messagesList = messages.ToList();

            foreach (var message in messagesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await SendAsync(message, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Sends the email message using the provider-specific implementation.
        /// </summary>
        /// <param name="message">The email message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and error information.</returns>
        protected abstract Task<EmailSendResult> SendEmailAsync(
            EmailMessage message,
            CancellationToken cancellationToken);

        /// <summary>
        /// Validates the email message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        protected virtual IList<string> ValidateMessage(EmailMessage message)
        {
            var errors = new List<string>();

            // Validate using EmailMessage.Validate()
            var validationErrors = message.Validate();
            errors.AddRange(validationErrors);

            // Validate recipient limits
            if (Options.MaxRecipientsPerEmail.HasValue)
            {
                var totalRecipients = (message.To?.Count ?? 0) +
                                     (message.Cc?.Count ?? 0) +
                                     (message.Bcc?.Count ?? 0);

                if (totalRecipients > Options.MaxRecipientsPerEmail.Value)
                {
                    errors.Add($"Number of recipients ({totalRecipients}) exceeds maximum allowed ({Options.MaxRecipientsPerEmail.Value}).");
                }
            }

            // Validate attachment limits
            if (Options.MaxAttachmentsPerEmail.HasValue && message.Attachments != null)
            {
                if (message.Attachments.Count > Options.MaxAttachmentsPerEmail.Value)
                {
                    errors.Add($"Number of attachments ({message.Attachments.Count}) exceeds maximum allowed ({Options.MaxAttachmentsPerEmail.Value}).");
                }
            }

            // Validate attachment sizes
            if (Options.MaxAttachmentSize.HasValue && message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    if (attachment.ContentLength.HasValue &&
                        attachment.ContentLength.Value > Options.MaxAttachmentSize.Value)
                    {
                        errors.Add($"Attachment '{attachment.FileName}' size ({attachment.ContentLength.Value} bytes) exceeds maximum allowed ({Options.MaxAttachmentSize.Value} bytes).");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Applies default values from options to the email message.
        /// </summary>
        /// <param name="message">The message to apply defaults to.</param>
        /// <returns>A new email message with defaults applied.</returns>
        protected virtual EmailMessage ApplyDefaults(EmailMessage message)
        {
            // Create a copy to avoid modifying the original
            var emailCopy = new EmailMessage
            {
                To = new List<string>(message.To ?? new List<string>()),
                Cc = new List<string>(message.Cc ?? new List<string>()),
                Bcc = new List<string>(message.Bcc ?? new List<string>()),
                From = message.From ?? Options.DefaultFrom,
                ReplyTo = message.ReplyTo ?? Options.DefaultReplyTo,
                Subject = !string.IsNullOrWhiteSpace(Options.DefaultSubjectPrefix)
                    ? Options.DefaultSubjectPrefix + message.Subject
                    : message.Subject,
                HtmlBody = message.HtmlBody,
                PlainTextBody = message.PlainTextBody,
                Priority = message.Priority != EmailPriority.Normal ? message.Priority : Options.DefaultPriority,
                RequestReadReceipt = message.RequestReadReceipt || Options.DefaultRequestReadReceipt,
                Attachments = new List<IEmailAttachment>(message.Attachments ?? new List<IEmailAttachment>())
            };

            // Apply default headers
            foreach (var header in Options.DefaultHeaders)
            {
                if (!emailCopy.Headers.ContainsKey(header.Key))
                {
                    emailCopy.Headers[header.Key] = header.Value;
                }
            }

            // Copy custom headers from original message
            foreach (var header in message.Headers)
            {
                emailCopy.Headers[header.Key] = header.Value;
            }

            return emailCopy;
        }
    }
}

