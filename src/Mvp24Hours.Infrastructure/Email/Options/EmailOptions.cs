//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Email.Options
{
    /// <summary>
    /// Configuration options for email service operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control various aspects of email sending behavior, including default
    /// sender addresses, reply-to addresses, and provider-specific settings.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// Default values specified here are used when not explicitly provided in individual
    /// email messages. For example, if an email message doesn't specify a From address,
    /// the <see cref="DefaultFrom"/> value is used.
    /// </para>
    /// </remarks>
    public class EmailOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailOptions"/> class.
        /// </summary>
        public EmailOptions()
        {
            DefaultHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the default sender email address (From field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This address is used as the sender when an email message doesn't explicitly
        /// specify a From address. It should be a valid email address that your email
        /// provider allows you to send from.
        /// </para>
        /// <para>
        /// Can be in format "user@example.com" or "Display Name &lt;user@example.com&gt;".
        /// </para>
        /// <para>
        /// This is typically required and should be configured when setting up the email service.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.DefaultFrom = "noreply@example.com";
        /// // or
        /// options.DefaultFrom = "My Company &lt;noreply@example.com&gt;";
        /// </code>
        /// </example>
        public string? DefaultFrom { get; set; }

        /// <summary>
        /// Gets or sets the default reply-to email address (Reply-To field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This address is used as the reply-to address when an email message doesn't
        /// explicitly specify a ReplyTo address. Replies to emails will be sent to this address.
        /// </para>
        /// <para>
        /// If not specified, replies will go to the From address (or DefaultFrom).
        /// </para>
        /// <para>
        /// Can be in format "user@example.com" or "Display Name &lt;user@example.com&gt;".
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.DefaultReplyTo = "support@example.com";
        /// </code>
        /// </example>
        public string? DefaultReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the default email subject prefix.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This prefix is prepended to all email subjects. Useful for adding environment
        /// indicators (e.g., "[DEV]", "[TEST]") or branding.
        /// </para>
        /// <para>
        /// The prefix is added automatically, so don't include brackets or separators unless
        /// you want them in every subject.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.DefaultSubjectPrefix = "[DEV] "; // Results in "[DEV] Your Subject"
        /// </code>
        /// </example>
        public string? DefaultSubjectPrefix { get; set; }

        /// <summary>
        /// Gets or sets the default email priority.
        /// </summary>
        /// <remarks>
        /// This priority is used when an email message doesn't explicitly specify a priority.
        /// </remarks>
        public EmailPriority DefaultPriority { get; set; } = EmailPriority.Normal;

        /// <summary>
        /// Gets or sets whether to request read receipts by default.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, read receipts are requested for all emails unless explicitly
        /// disabled in the email message. Note that read receipts are not guaranteed and
        /// depend on recipient email client support.
        /// </remarks>
        public bool DefaultRequestReadReceipt { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of recipients allowed per email.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some email providers limit the number of recipients per email. This setting
        /// allows you to enforce a limit and get validation errors before attempting to send.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable recipient limit validation.
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no limit enforced).
        /// </para>
        /// </remarks>
        public int? MaxRecipientsPerEmail { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum attachment size in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Attachments larger than this size will be rejected during validation. This helps
        /// prevent sending emails that exceed provider limits.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable attachment size validation.
        /// </para>
        /// <para>
        /// Default is 25MB (26,214,400 bytes), which is a common limit for many email providers.
        /// </para>
        /// </remarks>
        public long? MaxAttachmentSize { get; set; } = 25 * 1024 * 1024; // 25MB

        /// <summary>
        /// Gets or sets the maximum number of attachments allowed per email.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some email providers limit the number of attachments per email. This setting
        /// allows you to enforce a limit and get validation errors before attempting to send.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable attachment count validation.
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no limit enforced).
        /// </para>
        /// </remarks>
        public int? MaxAttachmentsPerEmail { get; set; } = null;

        /// <summary>
        /// Gets or sets custom default headers to include in all emails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// These headers are added to all emails sent through this service. Headers specified
        /// in individual email messages will override these defaults.
        /// </para>
        /// <para>
        /// Common use cases include:
        /// - X-Environment header for environment identification
        /// - X-Application header for application identification
        /// - Custom tracking headers
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.DefaultHeaders["X-Environment"] = "Production";
        /// options.DefaultHeaders["X-Application"] = "MyApp";
        /// </code>
        /// </example>
        public IDictionary<string, string> DefaultHeaders { get; set; }

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks for logical inconsistencies in the configuration (e.g., negative
        /// limits, invalid email addresses).
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (MaxRecipientsPerEmail.HasValue && MaxRecipientsPerEmail.Value <= 0)
            {
                errors.Add("Maximum recipients per email must be greater than zero.");
            }

            if (MaxAttachmentSize.HasValue && MaxAttachmentSize.Value <= 0)
            {
                errors.Add("Maximum attachment size must be greater than zero.");
            }

            if (MaxAttachmentsPerEmail.HasValue && MaxAttachmentsPerEmail.Value <= 0)
            {
                errors.Add("Maximum attachments per email must be greater than zero.");
            }

            return errors;
        }

        /// <summary>
        /// Creates default email options suitable for most scenarios.
        /// </summary>
        /// <returns>Default options instance.</returns>
        public static EmailOptions Default => new();
    }

    /// <summary>
    /// Represents the priority of an email message.
    /// </summary>
    public enum EmailPriority
    {
        /// <summary>
        /// Normal priority (default).
        /// </summary>
        Normal = 0,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 1,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 2
    }
}

