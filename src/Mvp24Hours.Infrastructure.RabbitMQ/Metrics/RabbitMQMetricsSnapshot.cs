//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Metrics
{
    /// <summary>
    /// Snapshot of RabbitMQ metrics at a point in time.
    /// </summary>
    public class RabbitMQMetricsSnapshot
    {
        /// <summary>
        /// Gets or sets the total number of messages sent.
        /// </summary>
        public long MessagesSent { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages received.
        /// </summary>
        public long MessagesReceived { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages acknowledged.
        /// </summary>
        public long MessagesAcked { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages negatively acknowledged.
        /// </summary>
        public long MessagesNacked { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages rejected.
        /// </summary>
        public long MessagesRejected { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages redelivered.
        /// </summary>
        public long MessagesRedelivered { get; init; }

        /// <summary>
        /// Gets or sets the total number of publisher confirms received.
        /// </summary>
        public long PublisherConfirms { get; init; }

        /// <summary>
        /// Gets or sets the total number of publisher nacks received.
        /// </summary>
        public long PublisherNacks { get; init; }

        /// <summary>
        /// Gets or sets the total number of connection failures.
        /// </summary>
        public long ConnectionFailures { get; init; }

        /// <summary>
        /// Gets or sets the total number of channel creations.
        /// </summary>
        public long ChannelCreations { get; init; }

        /// <summary>
        /// Gets or sets the total number of duplicate messages skipped.
        /// </summary>
        public long DuplicateMessagesSkipped { get; init; }

        /// <summary>
        /// Gets or sets the messages count by queue.
        /// </summary>
        public ConcurrentDictionary<string, long> MessagesByQueue { get; init; } = new();

        /// <summary>
        /// Gets or sets the messages count by exchange.
        /// </summary>
        public ConcurrentDictionary<string, long> MessagesByExchange { get; init; } = new();

        /// <summary>
        /// Gets or sets the errors count by type.
        /// </summary>
        public ConcurrentDictionary<string, long> ErrorsByType { get; init; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the snapshot was taken.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Calculates the success rate (acked / received).
        /// </summary>
        public double SuccessRate => MessagesReceived > 0
            ? (double)MessagesAcked / MessagesReceived * 100
            : 0;

        /// <summary>
        /// Calculates the failure rate ((nacked + rejected) / received).
        /// </summary>
        public double FailureRate => MessagesReceived > 0
            ? (double)(MessagesNacked + MessagesRejected) / MessagesReceived * 100
            : 0;

        /// <summary>
        /// Calculates the redelivery rate (redelivered / received).
        /// </summary>
        public double RedeliveryRate => MessagesReceived > 0
            ? (double)MessagesRedelivered / MessagesReceived * 100
            : 0;

        /// <summary>
        /// Calculates the publisher confirm rate (confirms / sent).
        /// </summary>
        public double PublisherConfirmRate => MessagesSent > 0
            ? (double)PublisherConfirms / MessagesSent * 100
            : 0;
    }
}

