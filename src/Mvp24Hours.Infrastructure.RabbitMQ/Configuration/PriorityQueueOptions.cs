//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for priority queues.
    /// </summary>
    [Serializable]
    public class PriorityQueueOptions
    {
        /// <summary>
        /// Gets or sets whether priority queues are enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum priority level (0-255). Default is 10.
        /// Lower values are better for performance.
        /// </summary>
        public byte MaxPriority { get; set; } = 10;

        /// <summary>
        /// Gets or sets the default priority for messages (0 = lowest, MaxPriority = highest).
        /// Default is 0.
        /// </summary>
        public byte DefaultPriority { get; set; } = 0;
    }
}

