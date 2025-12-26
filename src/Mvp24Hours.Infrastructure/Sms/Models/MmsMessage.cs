//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Sms.Models
{
    /// <summary>
    /// Represents an MMS (Multimedia Messaging Service) message to be sent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MMS allows sending multimedia content (images, videos, audio) along with text messages.
    /// This class extends the functionality of SMS to support media attachments.
    /// </para>
    /// <para>
    /// <strong>MMS vs SMS:</strong>
    /// - SMS: Text-only, limited to 160 characters (GSM 7-bit) or 70 characters (Unicode)
    /// - MMS: Supports text + media files (images, videos, audio)
    /// </para>
    /// <para>
    /// <strong>Size Limitations:</strong>
    /// MMS messages typically have size limits (e.g., 300KB-1MB depending on carrier).
    /// Check provider documentation for specific limits.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // MMS with image
    /// var mmsMessage = new MmsMessage
    /// {
    ///     To = "+5511999999999",
    ///     Body = "Check out this image!",
    ///     Attachments = new List&lt;MmsAttachment&gt;
    ///     {
    ///         new MmsAttachment(imageBytes, "image/jpeg", "photo.jpg")
    ///     }
    /// };
    /// var result = await smsService.SendMmsAsync(mmsMessage, cancellationToken);
    /// </code>
    /// </example>
    public class MmsMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MmsMessage"/> class.
        /// </summary>
        public MmsMessage()
        {
            Attachments = new List<MmsAttachment>();
        }

        /// <summary>
        /// Gets or sets the recipient phone number (To field).
        /// </summary>
        /// <remarks>
        /// This is the phone number of the recipient who should receive the MMS.
        /// The phone number should be in E.164 format (e.g., +5511999999999) for best compatibility.
        /// </remarks>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets the sender phone number (From field).
        /// </summary>
        /// <remarks>
        /// This is the phone number or alphanumeric sender ID that will appear as the sender.
        /// If not specified, the default From from <see cref="SmsOptions"/> is used.
        /// </remarks>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the message body (text content).
        /// </summary>
        /// <remarks>
        /// This is the text content that accompanies the media attachments.
        /// Unlike SMS, MMS can have longer text content (though limits still apply).
        /// </remarks>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the list of media attachments.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This collection contains the media files (images, videos, audio) to be sent with the MMS.
        /// At least one attachment is typically required for an MMS (though some providers allow
        /// text-only MMS for longer messages).
        /// </para>
        /// <para>
        /// <strong>Multiple Attachments:</strong>
        /// Some providers support multiple attachments in a single MMS, while others may limit to one.
        /// Check provider documentation for limits.
        /// </para>
        /// </remarks>
        public IList<MmsAttachment> Attachments { get; set; }

        /// <summary>
        /// Gets or sets custom metadata or tags associated with the message.
        /// </summary>
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Validates the MMS message.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(To))
            {
                errors.Add("Recipient phone number (To) is required.");
            }

            // MMS typically requires at least one attachment or a body
            if (string.IsNullOrWhiteSpace(Body) && (Attachments == null || Attachments.Count == 0))
            {
                errors.Add("MMS message must have either a body or at least one attachment.");
            }

            // Validate attachments
            if (Attachments != null)
            {
                foreach (var attachment in Attachments)
                {
                    var attachmentErrors = attachment.Validate();
                    errors.AddRange(attachmentErrors);
                }
            }

            return errors;
        }

        /// <summary>
        /// Gets whether the message has a recipient phone number.
        /// </summary>
        public bool HasRecipient => !string.IsNullOrWhiteSpace(To);

        /// <summary>
        /// Gets whether the message has attachments.
        /// </summary>
        public bool HasAttachments => Attachments != null && Attachments.Count > 0;

        /// <summary>
        /// Gets the total size of all attachments in bytes.
        /// </summary>
        public long TotalAttachmentSize => Attachments?.Sum(a => a.Size) ?? 0;
    }
}

