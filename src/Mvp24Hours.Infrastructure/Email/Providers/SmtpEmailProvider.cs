//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Providers
{
    /// <summary>
    /// SMTP email provider implementation using System.Net.Mail.SmtpClient.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider sends emails using the standard SMTP protocol via System.Net.Mail.SmtpClient.
    /// It supports SSL/TLS encryption, STARTTLS, authentication, and attachments.
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// Configure SMTP settings via <see cref="SmtpEmailOptions"/> when registering the service.
    /// </para>
    /// <para>
    /// <strong>Security Note:</strong>
    /// For production environments, consider using MailKit instead of System.Net.Mail.SmtpClient
    /// for better security and modern protocol support. This implementation uses the built-in
    /// SmtpClient which is functional but may have limitations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register SMTP email service
    /// services.AddSmtpEmailService(options =>
    /// {
    ///     options.DefaultFrom = "noreply@example.com";
    /// }, smtpOptions =>
    /// {
    ///     smtpOptions.Host = "smtp.gmail.com";
    ///     smtpOptions.Port = 587;
    ///     smtpOptions.Username = "your-email@gmail.com";
    ///     smtpOptions.Password = "your-app-password";
    ///     smtpOptions.EnableStartTls = true;
    /// });
    /// </code>
    /// </example>
    public class SmtpEmailProvider : BaseEmailProvider
    {
        private readonly SmtpEmailOptions _smtpOptions;
        private readonly ILogger<SmtpEmailProvider>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailProvider"/> class.
        /// </summary>
        /// <param name="emailOptions">The email options.</param>
        /// <param name="smtpOptions">The SMTP-specific options.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when emailOptions or smtpOptions is null.</exception>
        public SmtpEmailProvider(
            IOptions<EmailOptions> emailOptions,
            IOptions<SmtpEmailOptions> smtpOptions,
            ILogger<SmtpEmailProvider>? logger = null)
            : base(emailOptions)
        {
            _smtpOptions = smtpOptions?.Value ?? throw new ArgumentNullException(nameof(smtpOptions));
            _logger = logger;

            // Validate SMTP options
            var validationErrors = _smtpOptions.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                throw new InvalidOperationException($"Invalid SMTP configuration: {errorMessage}");
            }
        }

        /// <summary>
        /// Sends the email message using SMTP.
        /// </summary>
        /// <param name="message">The email message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and error information.</returns>
        protected override async Task<EmailSendResult> SendEmailAsync(
            EmailMessage message,
            CancellationToken cancellationToken)
        {
            try
            {
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = CreateMailMessage(message);

                // Send email
                await smtpClient.SendMailAsync(mailMessage);

                // Generate a message ID (SMTP doesn't provide one, so we create one)
                var messageId = Guid.NewGuid().ToString("N");

                _logger?.LogDebug("Email sent successfully via SMTP. MessageId: {MessageId}, To: {To}, Subject: {Subject}",
                    messageId, string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Successful(messageId);
            }
            catch (SmtpException ex)
            {
                _logger?.LogError(ex, "SMTP error while sending email. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"SMTP error: {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while sending email via SMTP. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"Failed to send email: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Creates and configures an SMTP client.
        /// </summary>
        /// <returns>A configured SMTP client.</returns>
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                Timeout = _smtpOptions.Timeout,
                UseDefaultCredentials = _smtpOptions.UseDefaultCredentials
            };

            // Configure authentication
            if (!_smtpOptions.UseDefaultCredentials &&
                !string.IsNullOrWhiteSpace(_smtpOptions.Username) &&
                !string.IsNullOrWhiteSpace(_smtpOptions.Password))
            {
                client.Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password);
            }

            // Configure certificate validation callback
            if (_smtpOptions.ServerCertificateValidationCallback != null)
            {
                // Note: System.Net.Mail.SmtpClient doesn't directly support certificate validation callback
                // This would require using ServicePointManager.ServerCertificateValidationCallback
                // which is global. For better control, consider using MailKit instead.
                ServicePointManager.ServerCertificateValidationCallback = _smtpOptions.ServerCertificateValidationCallback;
            }

            // Configure STARTTLS (if enabled and SSL is not enabled)
            if (_smtpOptions.EnableStartTls && !_smtpOptions.EnableSsl)
            {
                // STARTTLS is handled automatically by SmtpClient when EnableSsl is false
                // and the server supports STARTTLS. The EnableStartTls flag is informational.
            }

            return client;
        }

        /// <summary>
        /// Creates a MailMessage from an EmailMessage.
        /// </summary>
        /// <param name="message">The email message to convert.</param>
        /// <returns>A MailMessage instance.</returns>
        private MailMessage CreateMailMessage(EmailMessage message)
        {
            var mailMessage = new MailMessage();

            // Set From
            if (!string.IsNullOrWhiteSpace(message.From))
            {
                mailMessage.From = ParseMailAddress(message.From);
            }

            // Set Reply-To
            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                mailMessage.ReplyToList.Add(ParseMailAddress(message.ReplyTo));
            }

            // Set To recipients
            foreach (var to in message.To ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(to))
                {
                    mailMessage.To.Add(ParseMailAddress(to));
                }
            }

            // Set CC recipients
            foreach (var cc in message.Cc ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    mailMessage.CC.Add(ParseMailAddress(cc));
                }
            }

            // Set BCC recipients
            foreach (var bcc in message.Bcc ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(bcc))
                {
                    mailMessage.Bcc.Add(ParseMailAddress(bcc));
                }
            }

            // Set Subject
            mailMessage.Subject = message.Subject ?? string.Empty;
            mailMessage.SubjectEncoding = Encoding.UTF8;

            // Set Body
            if (!string.IsNullOrWhiteSpace(message.HtmlBody) && !string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                // Both HTML and plain text - use alternate views
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = message.HtmlBody;
                mailMessage.BodyEncoding = Encoding.UTF8;

                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody,
                    Encoding.UTF8,
                    "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }
            else if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                // HTML only
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = message.HtmlBody;
                mailMessage.BodyEncoding = Encoding.UTF8;
            }
            else if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                // Plain text only
                mailMessage.IsBodyHtml = false;
                mailMessage.Body = message.PlainTextBody;
                mailMessage.BodyEncoding = Encoding.UTF8;
            }

            // Set Priority
            mailMessage.Priority = message.Priority switch
            {
                EmailPriority.High => MailPriority.High,
                EmailPriority.Low => MailPriority.Low,
                _ => MailPriority.Normal
            };

            // Set Request Read Receipt
            if (message.RequestReadReceipt)
            {
                mailMessage.Headers.Add("Disposition-Notification-To", message.From ?? Options.DefaultFrom ?? string.Empty);
            }

            // Add Attachments
            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var mailAttachment = CreateAttachment(attachment);
                    if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
                    {
                        mailAttachment.ContentId = attachment.ContentId;
                        mailAttachment.ContentDisposition!.Inline = true;
                    }
                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            // Add Custom Headers
            foreach (var header in message.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                {
                    mailMessage.Headers.Add(header.Key, header.Value);
                }
            }

            return mailMessage;
        }

        /// <summary>
        /// Parses an email address string into a MailAddress.
        /// </summary>
        /// <param name="address">The email address string (can include display name).</param>
        /// <returns>A MailAddress instance.</returns>
        private MailAddress ParseMailAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(address));
            }

            // Try to parse as "Display Name <email@domain.com>" format
            if (address.Contains('<') && address.Contains('>'))
            {
                var startIndex = address.IndexOf('<');
                var endIndex = address.IndexOf('>');
                var displayName = address.Substring(0, startIndex).Trim().Trim('"');
                var emailAddress = address.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                return new MailAddress(emailAddress, displayName);
            }

            // Simple email address
            return new MailAddress(address);
        }

        /// <summary>
        /// Creates an Attachment from an IEmailAttachment.
        /// </summary>
        /// <param name="attachment">The email attachment to convert.</param>
        /// <returns>An Attachment instance.</returns>
        private Attachment CreateAttachment(Contract.IEmailAttachment attachment)
        {
            if (attachment == null)
            {
                throw new ArgumentNullException(nameof(attachment));
            }

            Stream? contentStream = null;
            try
            {
                contentStream = attachment.GetContentStream();
                if (contentStream == null)
                {
                    throw new InvalidOperationException($"Cannot get content stream for attachment '{attachment.FileName}'.");
                }

                var mailAttachment = new Attachment(contentStream, attachment.FileName, attachment.ContentType);
                return mailAttachment;
            }
            catch
            {
                // If attachment creation fails, dispose the stream if we created it
                contentStream?.Dispose();
                throw;
            }
        }
    }
}

