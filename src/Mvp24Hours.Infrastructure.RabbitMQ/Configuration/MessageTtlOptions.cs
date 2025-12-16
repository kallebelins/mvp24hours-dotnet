//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for message TTL (Time-To-Live).
    /// </summary>
    [Serializable]
    public class MessageTtlOptions
    {
        /// <summary>
        /// Gets or sets whether message TTL is enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the default message TTL in milliseconds.
        /// Messages older than this will be discarded or dead-lettered.
        /// Default is 0 (no TTL).
        /// </summary>
        public int DefaultTtlMilliseconds { get; set; } = 0;

        /// <summary>
        /// Gets or sets the queue message TTL in milliseconds.
        /// This applies to all messages in the queue.
        /// Default is 0 (no TTL).
        /// </summary>
        public int QueueTtlMilliseconds { get; set; } = 0;

        /// <summary>
        /// Gets or sets the queue expiration in milliseconds.
        /// The queue will be deleted after this time if unused.
        /// Default is 0 (no expiration).
        /// </summary>
        public int QueueExpiresMilliseconds { get; set; } = 0;
    }
}

