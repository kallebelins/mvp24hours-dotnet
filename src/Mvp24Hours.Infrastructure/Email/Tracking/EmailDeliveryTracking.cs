//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Tracking
{
    /// <summary>
    /// Interface for tracking email delivery status via webhooks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Email providers often support webhooks to notify your application about email delivery
    /// events such as sent, delivered, opened, clicked, bounced, or failed. This interface
    /// provides a unified way to handle these events.
    /// </para>
    /// <para>
    /// <strong>Common Delivery Events:</strong>
    /// <list type="bullet">
    /// <item><description><strong>Sent:</strong> Email was accepted by the provider</description></item>
    /// <item><description><strong>Delivered:</strong> Email was successfully delivered to recipient's mailbox</description></item>
    /// <item><description><strong>Opened:</strong> Recipient opened the email</description></item>
    /// <item><description><strong>Clicked:</strong> Recipient clicked a link in the email</description></item>
    /// <item><description><strong>Bounced:</strong> Email bounced (hard or soft bounce)</description></item>
    /// <item><description><strong>Failed:</strong> Email failed to send</description></item>
    /// <item><description><strong>Unsubscribed:</strong> Recipient unsubscribed</description></item>
    /// <item><description><strong>Complained:</strong> Recipient marked email as spam</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IEmailDeliveryTracking
    {
        /// <summary>
        /// Registers a webhook URL for receiving delivery events.
        /// </summary>
        /// <param name="webhookUrl">The URL to receive webhook events.</param>
        /// <param name="events">The events to subscribe to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Webhook registration result with webhook ID.</returns>
        Task<WebhookRegistrationResult> RegisterWebhookAsync(
            string webhookUrl,
            IEnumerable<EmailDeliveryEvent> events,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters a webhook.
        /// </summary>
        /// <param name="webhookId">The webhook ID to unregister.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        Task UnregisterWebhookAsync(
            string webhookId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a webhook event from the email provider.
        /// </summary>
        /// <param name="eventData">The webhook event data.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Task that completes when the event is processed.</returns>
        Task ProcessWebhookEventAsync(
            EmailDeliveryEventData eventData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the delivery status of an email.
        /// </summary>
        /// <param name="messageId">The message ID from the email provider.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The delivery status of the email.</returns>
        Task<EmailDeliveryStatus> GetDeliveryStatusAsync(
            string messageId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an email delivery event.
    /// </summary>
    public enum EmailDeliveryEvent
    {
        /// <summary>
        /// Email was sent (accepted by provider).
        /// </summary>
        Sent = 0,

        /// <summary>
        /// Email was delivered to recipient's mailbox.
        /// </summary>
        Delivered = 1,

        /// <summary>
        /// Recipient opened the email.
        /// </summary>
        Opened = 2,

        /// <summary>
        /// Recipient clicked a link in the email.
        /// </summary>
        Clicked = 3,

        /// <summary>
        /// Email bounced (hard bounce).
        /// </summary>
        Bounced = 4,

        /// <summary>
        /// Email failed to send.
        /// </summary>
        Failed = 5,

        /// <summary>
        /// Recipient unsubscribed.
        /// </summary>
        Unsubscribed = 6,

        /// <summary>
        /// Recipient marked email as spam.
        /// </summary>
        Complained = 7
    }

    /// <summary>
    /// Represents email delivery event data from a webhook.
    /// </summary>
    public class EmailDeliveryEventData
    {
        /// <summary>
        /// Gets or sets the message ID from the email provider.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the delivery event type.
        /// </summary>
        public EmailDeliveryEvent Event { get; set; }

        /// <summary>
        /// Gets or sets the recipient email address.
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional event metadata (provider-specific).
        /// </summary>
        public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Gets or sets the bounce reason (if event is Bounced).
        /// </summary>
        public string? BounceReason { get; set; }

        /// <summary>
        /// Gets or sets the click URL (if event is Clicked).
        /// </summary>
        public string? ClickUrl { get; set; }

        /// <summary>
        /// Gets or sets the user agent (if event is Opened or Clicked).
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the IP address (if available).
        /// </summary>
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// Represents the delivery status of an email.
    /// </summary>
    public class EmailDeliveryStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailDeliveryStatus"/> class.
        /// </summary>
        public EmailDeliveryStatus()
        {
            Events = new List<EmailDeliveryEventData>();
        }

        /// <summary>
        /// Gets or sets the message ID.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient email address.
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current delivery status.
        /// </summary>
        public EmailDeliveryStatusType Status { get; set; }

        /// <summary>
        /// Gets or sets the list of delivery events.
        /// </summary>
        public IList<EmailDeliveryEventData> Events { get; set; }

        /// <summary>
        /// Gets or sets when the email was sent.
        /// </summary>
        public DateTimeOffset? SentAt { get; set; }

        /// <summary>
        /// Gets or sets when the email was delivered (if delivered).
        /// </summary>
        public DateTimeOffset? DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets when the email was first opened (if opened).
        /// </summary>
        public DateTimeOffset? FirstOpenedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of times the email was opened.
        /// </summary>
        public int OpenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times links were clicked.
        /// </summary>
        public int ClickCount { get; set; }
    }

    /// <summary>
    /// Email delivery status type.
    /// </summary>
    public enum EmailDeliveryStatusType
    {
        /// <summary>
        /// Status unknown or pending.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Email was sent (accepted by provider).
        /// </summary>
        Sent = 1,

        /// <summary>
        /// Email was delivered.
        /// </summary>
        Delivered = 2,

        /// <summary>
        /// Email was opened.
        /// </summary>
        Opened = 3,

        /// <summary>
        /// Email bounced.
        /// </summary>
        Bounced = 4,

        /// <summary>
        /// Email failed.
        /// </summary>
        Failed = 5,

        /// <summary>
        /// Recipient unsubscribed.
        /// </summary>
        Unsubscribed = 6,

        /// <summary>
        /// Recipient complained (marked as spam).
        /// </summary>
        Complained = 7
    }

    /// <summary>
    /// Result of webhook registration.
    /// </summary>
    public class WebhookRegistrationResult
    {
        /// <summary>
        /// Gets or sets whether the registration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the webhook ID (if successful).
        /// </summary>
        public string? WebhookId { get; set; }

        /// <summary>
        /// Gets or sets error message (if failed).
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

