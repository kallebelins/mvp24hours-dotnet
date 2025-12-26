//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Results;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Sms.Models
{
    /// <summary>
    /// Represents a delivery report for an SMS/MMS message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delivery reports provide status updates about SMS/MMS messages, including delivery confirmation,
    /// failure reasons, and timestamps. These reports are typically received via webhooks from the SMS provider.
    /// </para>
    /// <para>
    /// <strong>Status Updates:</strong>
    /// Delivery status can change over time:
    /// - Initially: Queued or Sent
    /// - Later: Delivered, Failed, or Undelivered
    /// </para>
    /// </remarks>
    public class DeliveryReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryReport"/> class.
        /// </summary>
        public DeliveryReport()
        {
        }

        /// <summary>
        /// Gets or sets the message ID from the SMS provider.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier assigned by the SMS provider when the message was sent.
        /// It is used to correlate the delivery report with the original message.
        /// </remarks>
        public string? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the recipient phone number.
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets the sender phone number or sender ID.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the delivery status.
        /// </summary>
        /// <remarks>
        /// This indicates the current state of the message delivery.
        /// </remarks>
        public SmsDeliveryStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error code (if failed).
        /// </summary>
        /// <remarks>
        /// Provider-specific error code that indicates why the message failed (if applicable).
        /// </remarks>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message (if failed).
        /// </summary>
        /// <remarks>
        /// Human-readable error message describing why the message failed (if applicable).
        /// </remarks>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets when the message was sent (or queued).
        /// </summary>
        public DateTimeOffset? SentAt { get; set; }

        /// <summary>
        /// Gets or sets when the message was delivered (if delivered).
        /// </summary>
        public DateTimeOffset? DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets when the delivery report was received.
        /// </summary>
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets provider-specific metadata.
        /// </summary>
        /// <remarks>
        /// Additional information provided by the SMS provider that may be useful for debugging
        /// or analytics.
        /// </remarks>
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Gets whether the message was successfully delivered.
        /// </summary>
        public bool IsDelivered => Status == SmsDeliveryStatus.Delivered;

        /// <summary>
        /// Gets whether the message delivery failed.
        /// </summary>
        public bool IsFailed => Status == SmsDeliveryStatus.Failed || Status == SmsDeliveryStatus.Undelivered;
    }
}

