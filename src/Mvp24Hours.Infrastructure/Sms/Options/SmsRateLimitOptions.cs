//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Sms.Options
{
    /// <summary>
    /// Configuration options for SMS rate limiting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rate limiting prevents sending too many messages to the same recipient within a time period.
    /// This helps prevent spam, reduces costs, and ensures compliance with carrier regulations.
    /// </para>
    /// </remarks>
    public class SmsRateLimitOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsRateLimitOptions"/> class.
        /// </summary>
        public SmsRateLimitOptions()
        {
        }

        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, rate limiting is disabled and all messages are allowed.
        /// Default is <c>true</c>.
        /// </remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of messages allowed per destination in the time window.
        /// </summary>
        /// <remarks>
        /// This is the maximum number of messages that can be sent to the same phone number
        /// within the <see cref="TimeWindow"/> period.
        /// </remarks>
        /// <example>
        /// If <see cref="MaxMessagesPerDestination"/> is 5 and <see cref="TimeWindow"/> is 1 hour,
        /// then at most 5 messages can be sent to the same phone number per hour.
        /// </example>
        public int MaxMessagesPerDestination { get; set; } = 10;

        /// <summary>
        /// Gets or sets the time window for rate limiting.
        /// </summary>
        /// <remarks>
        /// This is the time period over which the rate limit is applied. For example, if set to
        /// 1 hour, the rate limit applies per hour.
        /// </remarks>
        public TimeSpan TimeWindow { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to throw an exception when rate limit is exceeded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, an exception is thrown when attempting to send a message that would
        /// exceed the rate limit. When <c>false</c>, the send operation returns a failed result
        /// without throwing.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (return failed result instead of throwing).
        /// </para>
        /// </remarks>
        public bool ThrowOnExceeded { get; set; } = false;
    }
}

