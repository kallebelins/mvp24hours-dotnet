//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.RabbitMQ.Metrics
{
    /// <summary>
    /// Interface for RabbitMQ metrics collection.
    /// </summary>
    public interface IRabbitMQMetrics
    {
        /// <summary>
        /// Gets the total number of messages sent.
        /// </summary>
        long MessagesSent { get; }

        /// <summary>
        /// Gets the total number of messages received.
        /// </summary>
        long MessagesReceived { get; }

        /// <summary>
        /// Gets the total number of messages acknowledged.
        /// </summary>
        long MessagesAcked { get; }

        /// <summary>
        /// Gets the total number of messages negatively acknowledged.
        /// </summary>
        long MessagesNacked { get; }

        /// <summary>
        /// Gets the total number of messages rejected.
        /// </summary>
        long MessagesRejected { get; }

        /// <summary>
        /// Gets the total number of messages redelivered.
        /// </summary>
        long MessagesRedelivered { get; }

        /// <summary>
        /// Gets the total number of publisher confirms received.
        /// </summary>
        long PublisherConfirms { get; }

        /// <summary>
        /// Gets the total number of publisher nacks received.
        /// </summary>
        long PublisherNacks { get; }

        /// <summary>
        /// Gets the total number of connection failures.
        /// </summary>
        long ConnectionFailures { get; }

        /// <summary>
        /// Gets the total number of channel creations.
        /// </summary>
        long ChannelCreations { get; }

        /// <summary>
        /// Gets the total number of duplicate messages skipped.
        /// </summary>
        long DuplicateMessagesSkipped { get; }

        /// <summary>
        /// Increments the messages sent counter.
        /// </summary>
        /// <param name="exchange">Optional exchange name for per-exchange tracking.</param>
        void IncrementMessagesSent(string? exchange = null);

        /// <summary>
        /// Increments the messages received counter.
        /// </summary>
        /// <param name="queue">Optional queue name for per-queue tracking.</param>
        void IncrementMessagesReceived(string? queue = null);

        /// <summary>
        /// Increments the messages acked counter.
        /// </summary>
        void IncrementMessagesAcked();

        /// <summary>
        /// Increments the messages nacked counter.
        /// </summary>
        void IncrementMessagesNacked();

        /// <summary>
        /// Increments the messages rejected counter.
        /// </summary>
        void IncrementMessagesRejected();

        /// <summary>
        /// Increments the messages redelivered counter.
        /// </summary>
        void IncrementMessagesRedelivered();

        /// <summary>
        /// Increments the publisher confirms counter.
        /// </summary>
        void IncrementPublisherConfirms();

        /// <summary>
        /// Increments the publisher nacks counter.
        /// </summary>
        void IncrementPublisherNacks();

        /// <summary>
        /// Increments the connection failures counter.
        /// </summary>
        void IncrementConnectionFailures();

        /// <summary>
        /// Increments the channel creations counter.
        /// </summary>
        void IncrementChannelCreations();

        /// <summary>
        /// Increments the duplicate messages skipped counter.
        /// </summary>
        void IncrementDuplicateMessagesSkipped();

        /// <summary>
        /// Increments the error counter for a specific error type.
        /// </summary>
        /// <param name="errorType">The type of error.</param>
        void IncrementError(string errorType);

        /// <summary>
        /// Gets a snapshot of current metrics.
        /// </summary>
        RabbitMQMetricsSnapshot GetSnapshot();

        /// <summary>
        /// Resets all metrics to zero.
        /// </summary>
        void Reset();
    }
}

