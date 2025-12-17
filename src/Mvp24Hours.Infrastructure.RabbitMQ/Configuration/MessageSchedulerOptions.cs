//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for the message scheduler.
    /// </summary>
    public class MessageSchedulerOptions
    {
        /// <summary>
        /// Gets or sets whether the message scheduler is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use the RabbitMQ delayed message exchange plugin.
        /// When false, uses a retry queue approach. Default is false.
        /// </summary>
        public bool UseDelayedMessagePlugin { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the delayed exchange when using the plugin.
        /// Default is "mvp.delayed.exchange".
        /// </summary>
        public string DelayedExchangeName { get; set; } = "mvp.delayed.exchange";

        /// <summary>
        /// Gets or sets the name of the scheduled messages queue.
        /// Default is "mvp.scheduled.messages".
        /// </summary>
        public string ScheduledQueueName { get; set; } = "mvp.scheduled.messages";

        /// <summary>
        /// Gets or sets the polling interval for checking scheduled messages.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the batch size for processing scheduled messages.
        /// Default is 100.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to persist scheduled messages to storage.
        /// Default is true.
        /// </summary>
        public bool PersistMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum retry count for failed scheduled messages.
        /// Default is 3.
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry delay multiplier (exponential backoff).
        /// Default is 2.0.
        /// </summary>
        public double RetryDelayMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the base retry delay in milliseconds.
        /// Default is 1000 (1 second).
        /// </summary>
        public int BaseRetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the time-to-live for completed scheduled messages in storage.
        /// Default is 24 hours.
        /// </summary>
        public TimeSpan CompletedMessageTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets whether to enable recurring/periodic message support.
        /// Default is true.
        /// </summary>
        public bool EnableRecurringMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum interval for recurring messages.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan MinimumRecurringInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
}

