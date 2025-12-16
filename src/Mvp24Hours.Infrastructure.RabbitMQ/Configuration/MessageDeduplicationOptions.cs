//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for message deduplication.
    /// </summary>
    [Serializable]
    public class MessageDeduplicationOptions
    {
        /// <summary>
        /// Gets or sets whether deduplication is enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the default expiration time in minutes for deduplication entries.
        /// Default is 60 minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum number of entries to keep in memory.
        /// Only applies to in-memory store. Default is 100,000.
        /// </summary>
        public int MaxEntries { get; set; } = 100_000;

        /// <summary>
        /// Gets or sets the header name used to store the message ID for deduplication.
        /// Default is "x-message-id".
        /// </summary>
        public string MessageIdHeaderName { get; set; } = "x-message-id";
    }
}

