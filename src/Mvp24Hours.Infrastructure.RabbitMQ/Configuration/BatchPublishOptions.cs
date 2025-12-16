//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for batch publishing.
    /// </summary>
    [Serializable]
    public class BatchPublishOptions
    {
        /// <summary>
        /// Gets or sets whether batch publishing is enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum batch size (number of messages).
        /// Default is 100.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum time in milliseconds to wait before sending a batch.
        /// Default is 100ms.
        /// </summary>
        public int MaxBatchDelayMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum batch size in bytes.
        /// Default is 1MB (1048576 bytes).
        /// </summary>
        public long MaxBatchSizeBytes { get; set; } = 1_048_576;
    }
}

