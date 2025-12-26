//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Email.Models
{
    /// <summary>
    /// Represents an email message to be sent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates all information needed to send an email, including recipients,
    /// subject, body (HTML and/or plain text), attachments, and optional headers.
    /// </para>
    /// <para>
    /// <strong>Recipients:</strong>
    /// At least one recipient must be specified (To, CC, or BCC). Multiple recipients can be
    /// provided as an array or collection.
    /// </para>
    /// <para>
    /// <strong>Body Format:</strong>
    /// You can provide either HTML body, plain text body, or both. If both are provided, the
    /// email will be sent as a multipart message, allowing email clients to choose the
    /// appropriate format. If only one format is provided, the email will be sent in that format.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// If From, ReplyTo, or other optional fields are not specified, they will be taken from
    /// <see cref="EmailOptions"/> configured for the email service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple text email
    /// var message = new EmailMessage
    /// {
    ///     To = new[] { "user@example.com" },
    ///     Subject = "Welcome",
    ///     PlainTextBody = "Welcome to our service!"
    /// };
    /// 
    /// // HTML email with attachments
    /// var htmlMessage = new EmailMessage
    /// {
    ///     To = new[] { "user@example.com" },
    ///     Cc = new[] { "manager@example.com" },
    ///     Subject = "Monthly Report",
    ///     HtmlBody = "&lt;h1&gt;Report&lt;/h1&gt;&lt;p&gt;Please review the attached report.&lt;/p&gt;",
    ///     PlainTextBody = "Report\n\nPlease review the attached report.",
    ///     Attachments = new[]
    ///     {
    ///         new EmailAttachment("report.pdf", pdfBytes, "application/pdf")
    ///     }
    /// };
    /// </code>
    /// </example>
    public class EmailMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailMessage"/> class.
        /// </summary>
        public EmailMessage()
        {
            To = new List<string>();
            Cc = new List<string>();
            Bcc = new List<string>();
            Attachments = new List<IEmailAttachment>();
            EmbeddedImages = new List<EmbeddedImage>();
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the primary recipients (To field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the list of primary recipients who should receive the email.
        /// At least one recipient must be specified across To, Cc, or Bcc fields.
        /// </para>
        /// <para>
        /// Email addresses should be in valid format (e.g., "user@example.com" or
        /// "Display Name &lt;user@example.com&gt;").
        /// </para>
        /// </remarks>
        public IList<string> To { get; set; }

        /// <summary>
        /// Gets or sets the carbon copy recipients (CC field).
        /// </summary>
        /// <remarks>
        /// CC recipients receive a copy of the email and are visible to all recipients.
        /// </remarks>
        public IList<string> Cc { get; set; }

        /// <summary>
        /// Gets or sets the blind carbon copy recipients (BCC field).
        /// </summary>
        /// <remarks>
        /// BCC recipients receive a copy of the email but are not visible to other recipients.
        /// </remarks>
        public IList<string> Bcc { get; set; }

        /// <summary>
        /// Gets or sets the sender email address (From field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// If not specified, the default From address from <see cref="EmailOptions"/> is used.
        /// </para>
        /// <para>
        /// Can be in format "user@example.com" or "Display Name &lt;user@example.com&gt;".
        /// </para>
        /// </remarks>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the reply-to email address (Reply-To field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// If not specified, the default ReplyTo address from <see cref="EmailOptions"/> is used.
        /// If no default is configured, replies will go to the From address.
        /// </para>
        /// <para>
        /// Can be in format "user@example.com" or "Display Name &lt;user@example.com&gt;".
        /// </para>
        /// </remarks>
        public string? ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the email subject.
        /// </summary>
        /// <remarks>
        /// The subject is required and should be concise and descriptive.
        /// </remarks>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the HTML body of the email.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the HTML-formatted body of the email. HTML emails provide rich formatting
        /// capabilities including images, links, tables, and styled content.
        /// </para>
        /// <para>
        /// If both <see cref="HtmlBody"/> and <see cref="PlainTextBody"/> are provided, the
        /// email will be sent as a multipart message with both formats.
        /// </para>
        /// <para>
        /// At least one body format (HTML or plain text) must be provided.
        /// </para>
        /// </remarks>
        public string? HtmlBody { get; set; }

        /// <summary>
        /// Gets or sets the plain text body of the email.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the plain text version of the email body. Plain text emails are simpler
        /// and more compatible across email clients, but lack formatting capabilities.
        /// </para>
        /// <para>
        /// If both <see cref="HtmlBody"/> and <see cref="PlainTextBody"/> are provided, the
        /// email will be sent as a multipart message with both formats.
        /// </para>
        /// <para>
        /// At least one body format (HTML or plain text) must be provided.
        /// </para>
        /// </remarks>
        public string? PlainTextBody { get; set; }

        /// <summary>
        /// Gets or sets the list of attachments to include in the email.
        /// </summary>
        /// <remarks>
        /// Attachments can include files, images, documents, etc. Each attachment must
        /// implement <see cref="IEmailAttachment"/>.
        /// </remarks>
        public IList<IEmailAttachment> Attachments { get; set; }

        /// <summary>
        /// Gets or sets the list of embedded images (inline images) to include in the email.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Embedded images are displayed inline within the email body rather than as attachments.
        /// They are referenced in HTML using a Content-ID (CID) URL, e.g., <c>&lt;img src="cid:logo" /&gt;</c>.
        /// </para>
        /// <para>
        /// Each embedded image must have a unique Content-ID that is referenced in the HTML body.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Add embedded image
        /// message.EmbeddedImages.Add(new EmbeddedImage("logo", logoBytes, "image/png"));
        /// 
        /// // Reference in HTML
        /// message.HtmlBody = "&lt;img src=\"cid:logo\" alt=\"Logo\" /&gt;";
        /// </code>
        /// </example>
        public IList<EmbeddedImage> EmbeddedImages { get; set; }

        /// <summary>
        /// Gets or sets custom email headers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Custom headers allow adding additional metadata to the email (e.g., X-Custom-Header).
        /// Standard headers like From, To, Subject, etc., should be set via their respective
        /// properties rather than custom headers.
        /// </para>
        /// <para>
        /// Some providers may restrict which headers can be set. Check provider documentation
        /// for details.
        /// </para>
        /// </remarks>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the priority of the email (Normal, High, Low).
        /// </summary>
        /// <remarks>
        /// Email priority is typically displayed by email clients to help users prioritize
        /// their inbox. Not all providers support priority settings.
        /// </remarks>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        /// <summary>
        /// Gets or sets whether to request a read receipt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, a read receipt may be requested. However, read receipts are
        /// not guaranteed - recipients can choose to ignore the request, and not all email
        /// clients support read receipts.
        /// </para>
        /// <para>
        /// Not all email providers support read receipts. Check provider documentation.
        /// </para>
        /// </remarks>
        public bool RequestReadReceipt { get; set; } = false;

        /// <summary>
        /// Validates the email message.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks that the message has all required fields:
        /// - At least one recipient (To, Cc, or Bcc)
        /// - Subject
        /// - At least one body format (HtmlBody or PlainTextBody)
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            var hasRecipients = (To != null && To.Count > 0) ||
                               (Cc != null && Cc.Count > 0) ||
                               (Bcc != null && Bcc.Count > 0);

            if (!hasRecipients)
            {
                errors.Add("At least one recipient (To, Cc, or Bcc) must be specified.");
            }

            if (string.IsNullOrWhiteSpace(Subject))
            {
                errors.Add("Subject is required.");
            }

            var hasBody = !string.IsNullOrWhiteSpace(HtmlBody) ||
                          !string.IsNullOrWhiteSpace(PlainTextBody);

            if (!hasBody)
            {
                errors.Add("At least one body format (HtmlBody or PlainTextBody) must be specified.");
            }

            return errors;
        }

        /// <summary>
        /// Gets whether the message has any recipients.
        /// </summary>
        public bool HasRecipients =>
            (To != null && To.Count > 0) ||
            (Cc != null && Cc.Count > 0) ||
            (Bcc != null && Bcc.Count > 0);

        /// <summary>
        /// Gets whether the message has a body (HTML or plain text).
        /// </summary>
        public bool HasBody =>
            !string.IsNullOrWhiteSpace(HtmlBody) ||
            !string.IsNullOrWhiteSpace(PlainTextBody);

        /// <summary>
        /// Gets whether the message has attachments.
        /// </summary>
        public bool HasAttachments =>
            Attachments != null && Attachments.Count > 0;
    }
}

