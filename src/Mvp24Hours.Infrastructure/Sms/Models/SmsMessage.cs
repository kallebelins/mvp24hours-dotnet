//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Sms.Models
{
    /// <summary>
    /// Represents an SMS message to be sent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates all information needed to send an SMS, including recipient phone number,
    /// message body, and optional sender number.
    /// </para>
    /// <para>
    /// <strong>Phone Number Format:</strong>
    /// Phone numbers should be provided in E.164 format (e.g., +5511999999999) for international
    /// compatibility. Some providers also support national format (e.g., (11) 99999-9999), but
    /// E.164 is recommended for best compatibility.
    /// </para>
    /// <para>
    /// <strong>Message Length:</strong>
    /// SMS messages are typically limited to 160 characters per message (GSM 7-bit encoding) or
    /// 70 characters (UCS-2/Unicode). Longer messages are automatically split into multiple
    /// segments by most providers. The actual character limit depends on the provider and encoding.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// If From or other optional fields are not specified, they will be taken from
    /// <see cref="SmsOptions"/> configured for the SMS service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple SMS
    /// var message = new SmsMessage
    /// {
    ///     To = "+5511999999999",
    ///     Body = "Hello, this is a test SMS message."
    /// };
    /// 
    /// // SMS with explicit sender
    /// var messageWithSender = new SmsMessage
    /// {
    ///     To = "+5511999999999",
    ///     From = "+5511888888888",
    ///     Body = "Message from specific number"
    /// };
    /// </code>
    /// </example>
    public class SmsMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsMessage"/> class.
        /// </summary>
        public SmsMessage()
        {
        }

        /// <summary>
        /// Gets or sets the recipient phone number (To field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the phone number of the recipient who should receive the SMS.
        /// The phone number should be in E.164 format (e.g., +5511999999999) for best compatibility,
        /// though some providers also support national format.
        /// </para>
        /// <para>
        /// <strong>E.164 Format:</strong>
        /// E.164 format includes the country code and starts with a plus sign (+).
        /// Example: +5511999999999 (Brazil, area code 11, number 99999-9999)
        /// </para>
        /// <para>
        /// This field is required and must be a valid phone number.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// message.To = "+5511999999999"; // E.164 format (recommended)
        /// message.To = "11999999999"; // National format (provider-dependent)
        /// </code>
        /// </example>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets the sender phone number (From field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the phone number or alphanumeric sender ID that will appear as the sender
        /// of the SMS. The format depends on the provider:
        /// - Phone number: E.164 format (e.g., +5511888888888)
        /// - Alphanumeric sender ID: Short text (e.g., "MyCompany", "BRAND")
        /// </para>
        /// <para>
        /// If not specified, the default From number or sender ID from <see cref="SmsOptions"/>
        /// is used. Some providers require verification or registration of sender IDs before use.
        /// </para>
        /// <para>
        /// <strong>Provider Restrictions:</strong>
        /// Different providers have different rules for sender IDs:
        /// - Some allow only verified phone numbers
        /// - Some allow alphanumeric sender IDs (with restrictions on length and characters)
        /// - Some require country-specific sender ID registration
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// message.From = "+5511888888888"; // Phone number
        /// message.From = "MyCompany"; // Alphanumeric sender ID
        /// </code>
        /// </example>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the message body (text content).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the text content of the SMS message. The message body is required and cannot
        /// be empty. Most providers automatically split long messages into multiple segments.
        /// </para>
        /// <para>
        /// <strong>Character Limits:</strong>
        /// - GSM 7-bit encoding: 160 characters per segment
        /// - UCS-2/Unicode encoding: 70 characters per segment
        /// Longer messages are automatically split into multiple segments, and the recipient
        /// may be charged for multiple messages depending on their carrier plan.
        /// </para>
        /// <para>
        /// <strong>Special Characters:</strong>
        /// Some characters (like emojis, accented characters, or special symbols) may force
        /// Unicode encoding, reducing the character limit per segment.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// message.Body = "Hello, this is a test SMS message.";
        /// </code>
        /// </example>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets custom metadata or tags associated with the message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Custom metadata can be used for tracking, analytics, or provider-specific features.
        /// The exact usage depends on the SMS provider. Common use cases include:
        /// - Tracking campaign IDs
        /// - Associating messages with user accounts
        /// - Adding custom tags for filtering or reporting
        /// </para>
        /// <para>
        /// Not all providers support custom metadata. Check provider documentation for details.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// message.Metadata = new Dictionary&lt;string, string&gt;
        /// {
        ///     { "CampaignId", "12345" },
        ///     { "UserId", "user-123" }
        /// };
        /// </code>
        /// </example>
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Validates the SMS message.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks that the message has all required fields:
        /// - Recipient phone number (To)
        /// - Message body (Body)
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(To))
            {
                errors.Add("Recipient phone number (To) is required.");
            }

            if (string.IsNullOrWhiteSpace(Body))
            {
                errors.Add("Message body (Body) is required.");
            }

            return errors;
        }

        /// <summary>
        /// Gets whether the message has a recipient phone number.
        /// </summary>
        public bool HasRecipient => !string.IsNullOrWhiteSpace(To);

        /// <summary>
        /// Gets whether the message has a body.
        /// </summary>
        public bool HasBody => !string.IsNullOrWhiteSpace(Body);

        /// <summary>
        /// Gets whether the message is valid (has recipient and body).
        /// </summary>
        public bool IsValid => HasRecipient && HasBody;
    }
}

