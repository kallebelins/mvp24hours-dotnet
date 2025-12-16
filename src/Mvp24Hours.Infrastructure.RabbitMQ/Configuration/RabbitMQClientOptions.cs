//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for RabbitMQ client.
    /// </summary>
    [Serializable]
    public class RabbitMQClientOptions : RabbitMQOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of redelivery attempts before sending to DLQ.
        /// Default is 3.
        /// </summary>
        public int MaxRedeliveredCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the dead letter queue configuration.
        /// </summary>
        public RabbitMQOptions? DeadLetter { get; set; }

        /// <summary>
        /// Gets or sets the message deduplication options.
        /// </summary>
        public MessageDeduplicationOptions Deduplication { get; set; } = new();

        /// <summary>
        /// Gets or sets the priority queue options.
        /// </summary>
        public PriorityQueueOptions PriorityQueue { get; set; } = new();

        /// <summary>
        /// Gets or sets the message TTL options.
        /// </summary>
        public MessageTtlOptions MessageTtl { get; set; } = new();

        /// <summary>
        /// Gets or sets the headers exchange options.
        /// </summary>
        public HeadersExchangeOptions HeadersExchange { get; set; } = new();

        /// <summary>
        /// Gets or sets the consumer prefetch/QoS options.
        /// </summary>
        public ConsumerPrefetchOptions ConsumerPrefetch { get; set; } = new();

        /// <summary>
        /// Gets or sets the publisher confirm options.
        /// </summary>
        public PublisherConfirmOptions PublisherConfirm { get; set; } = new();

        /// <summary>
        /// Gets or sets the batch publish options.
        /// </summary>
        public BatchPublishOptions BatchPublish { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable structured logging for messages.
        /// Default is false.
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable metrics collection.
        /// Default is false.
        /// </summary>
        public bool EnableMetrics { get; set; } = false;
    }
}
