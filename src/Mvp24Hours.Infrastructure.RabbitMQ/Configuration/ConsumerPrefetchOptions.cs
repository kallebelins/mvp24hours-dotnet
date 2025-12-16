//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for consumer prefetch/QoS settings.
    /// </summary>
    [Serializable]
    public class ConsumerPrefetchOptions
    {
        /// <summary>
        /// Gets or sets the prefetch count (number of unacknowledged messages per consumer).
        /// Default is 1 for fair dispatch. Increase for higher throughput.
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the prefetch size in bytes.
        /// 0 means no specific limit. Default is 0.
        /// </summary>
        public uint PrefetchSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the QoS settings apply globally to the channel.
        /// false = per consumer, true = per channel.
        /// Default is false.
        /// </summary>
        public bool Global { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of concurrent consumers for the same queue.
        /// Default is 1.
        /// </summary>
        public int ConcurrentConsumers { get; set; } = 1;
    }
}

