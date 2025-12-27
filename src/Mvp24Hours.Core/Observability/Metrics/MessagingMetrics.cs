//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for RabbitMQ and messaging operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// message publishing, consumption, acknowledgment, and connection health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>published_total</c> - Counter for messages published</item>
/// <item><c>publish_duration_ms</c> - Histogram for publish duration</item>
/// <item><c>consumed_total</c> - Counter for messages consumed</item>
/// <item><c>consume_duration_ms</c> - Histogram for consume duration</item>
/// <item><c>acknowledged_total</c> / <c>rejected_total</c> - Ack/Nack counters</item>
/// <item><c>dead_lettered_total</c> - Counter for DLQ messages</item>
/// <item><c>queue_depth</c> - Gauge for queue depth</item>
/// <item><c>consumers_active</c> - Gauge for active consumers</item>
/// <item><c>connections_active</c> - Gauge for active connections</item>
/// <item><c>payload_size_bytes</c> - Histogram for message sizes</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class MyPublisher
/// {
///     private readonly MessagingMetrics _metrics;
///     
///     public async Task PublishAsync&lt;T&gt;(T message)
///     {
///         using var scope = _metrics.BeginPublish(typeof(T).Name, "my-exchange");
///         try
///         {
///             await _client.PublishAsync(message);
///             scope.Complete(payloadSize: messageBytes.Length);
///         }
///         catch
///         {
///             scope.Fail();
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
public sealed class MessagingMetrics
{
    #region Counters and Histograms

    private readonly Counter<long> _publishedTotal;
    private readonly Counter<long> _publishFailedTotal;
    private readonly Histogram<double> _publishDuration;
    private readonly Counter<long> _consumedTotal;
    private readonly Counter<long> _consumeFailedTotal;
    private readonly Histogram<double> _consumeDuration;
    private readonly Counter<long> _acknowledgedTotal;
    private readonly Counter<long> _rejectedTotal;
    private readonly Counter<long> _requeuedTotal;
    private readonly Counter<long> _deadLetteredTotal;
    private readonly UpDownCounter<int> _queueDepth;
    private readonly UpDownCounter<int> _consumersActive;
    private readonly Counter<long> _batchesTotal;
    private readonly Histogram<int> _batchSize;
    private readonly Histogram<int> _payloadSize;
    private readonly Counter<long> _connectionsTotal;
    private readonly UpDownCounter<int> _connectionsActive;
    private readonly Counter<long> _connectionFailuresTotal;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMetrics"/> class.
    /// </summary>
    public MessagingMetrics()
    {
        var meter = Mvp24HoursMeters.RabbitMQ.Meter;

        // Publishing
        _publishedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingPublishedTotal,
            unit: "{messages}",
            description: "Total number of messages published");

        _publishFailedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingPublishFailedTotal,
            unit: "{messages}",
            description: "Total number of failed message publications");

        _publishDuration = meter.CreateHistogram<double>(
            MetricNames.MessagingPublishDuration,
            unit: "ms",
            description: "Duration of message publishing in milliseconds");

        // Consuming
        _consumedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingConsumedTotal,
            unit: "{messages}",
            description: "Total number of messages consumed");

        _consumeFailedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingConsumeFailedTotal,
            unit: "{messages}",
            description: "Total number of failed message consumptions");

        _consumeDuration = meter.CreateHistogram<double>(
            MetricNames.MessagingConsumeDuration,
            unit: "ms",
            description: "Duration of message consumption in milliseconds");

        // Acknowledgments
        _acknowledgedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingAcknowledgedTotal,
            unit: "{messages}",
            description: "Total number of messages acknowledged");

        _rejectedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingRejectedTotal,
            unit: "{messages}",
            description: "Total number of messages rejected");

        _requeuedTotal = meter.CreateCounter<long>(
            MetricNames.MessagingRequeuedTotal,
            unit: "{messages}",
            description: "Total number of messages requeued");

        _deadLetteredTotal = meter.CreateCounter<long>(
            MetricNames.MessagingDeadLetteredTotal,
            unit: "{messages}",
            description: "Total number of messages sent to dead letter queue");

        // Queue metrics
        _queueDepth = meter.CreateUpDownCounter<int>(
            MetricNames.MessagingQueueDepth,
            unit: "{messages}",
            description: "Number of messages in queue");

        _consumersActive = meter.CreateUpDownCounter<int>(
            MetricNames.MessagingConsumersActive,
            unit: "{consumers}",
            description: "Number of active consumers");

        // Batch metrics
        _batchesTotal = meter.CreateCounter<long>(
            MetricNames.MessagingBatchesTotal,
            unit: "{batches}",
            description: "Total number of batch operations");

        _batchSize = meter.CreateHistogram<int>(
            MetricNames.MessagingBatchSize,
            unit: "{messages}",
            description: "Size of message batches");

        _payloadSize = meter.CreateHistogram<int>(
            MetricNames.MessagingPayloadSize,
            unit: "By",
            description: "Size of message payload in bytes");

        // Connection metrics
        _connectionsTotal = meter.CreateCounter<long>(
            MetricNames.MessagingConnectionsTotal,
            unit: "{connections}",
            description: "Total number of connection attempts");

        _connectionsActive = meter.CreateUpDownCounter<int>(
            MetricNames.MessagingConnectionsActive,
            unit: "{connections}",
            description: "Number of active connections");

        _connectionFailuresTotal = meter.CreateCounter<long>(
            MetricNames.MessagingConnectionFailuresTotal,
            unit: "{failures}",
            description: "Total number of connection failures");
    }

    #region Publish Methods

    /// <summary>
    /// Begins tracking a message publish operation.
    /// </summary>
    /// <param name="messageType">Type name of the message.</param>
    /// <param name="destination">Queue or exchange name.</param>
    /// <returns>A scope that should be disposed when publish completes.</returns>
    public PublishScope BeginPublish(string messageType, string destination)
    {
        return new PublishScope(this, messageType, destination);
    }

    /// <summary>
    /// Records a message publish operation.
    /// </summary>
    /// <param name="messageType">Type name of the message.</param>
    /// <param name="destination">Queue or exchange name.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the publish was successful.</param>
    /// <param name="payloadSize">Size of the message in bytes (optional).</param>
    public void RecordPublish(
        string messageType,
        string destination,
        double durationMs,
        bool success,
        int payloadSize = 0)
    {
        var tags = CreateMessageTags(messageType, destination, success);

        _publishedTotal.Add(1, tags);

        if (!success)
        {
            _publishFailedTotal.Add(1, tags);
        }

        _publishDuration.Record(durationMs, tags);

        if (payloadSize > 0)
        {
            _payloadSize.Record(payloadSize, tags);
        }
    }

    #endregion

    #region Consume Methods

    /// <summary>
    /// Begins tracking a message consume operation.
    /// </summary>
    /// <param name="messageType">Type name of the message.</param>
    /// <param name="queueName">Queue name.</param>
    /// <param name="consumerGroup">Consumer group name (optional).</param>
    /// <returns>A scope that should be disposed when consume completes.</returns>
    public ConsumeScope BeginConsume(string messageType, string queueName, string? consumerGroup = null)
    {
        return new ConsumeScope(this, messageType, queueName, consumerGroup);
    }

    /// <summary>
    /// Records a message consume operation.
    /// </summary>
    /// <param name="messageType">Type name of the message.</param>
    /// <param name="queueName">Queue name.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the consume was successful.</param>
    /// <param name="consumerGroup">Consumer group name (optional).</param>
    public void RecordConsume(
        string messageType,
        string queueName,
        double durationMs,
        bool success,
        string? consumerGroup = null)
    {
        var tags = new TagList
        {
            { MetricTags.MessageType, messageType },
            { MetricTags.QueueName, queueName },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        if (!string.IsNullOrEmpty(consumerGroup))
        {
            tags.Add(MetricTags.ConsumerGroup, consumerGroup);
        }

        _consumedTotal.Add(1, tags);

        if (!success)
        {
            _consumeFailedTotal.Add(1, tags);
        }

        _consumeDuration.Record(durationMs, tags);
    }

    #endregion

    #region Acknowledgment Methods

    /// <summary>
    /// Records a message acknowledgment.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    public void RecordAcknowledge(string queueName)
    {
        var tags = new TagList { { MetricTags.QueueName, queueName } };
        _acknowledgedTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a message rejection.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    /// <param name="requeue">Whether the message was requeued.</param>
    public void RecordReject(string queueName, bool requeue)
    {
        var tags = new TagList { { MetricTags.QueueName, queueName } };

        if (requeue)
        {
            _requeuedTotal.Add(1, tags);
        }
        else
        {
            _rejectedTotal.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a message sent to dead letter queue.
    /// </summary>
    /// <param name="queueName">Original queue name.</param>
    /// <param name="messageType">Type name of the message.</param>
    public void RecordDeadLetter(string queueName, string messageType)
    {
        var tags = new TagList
        {
            { MetricTags.QueueName, queueName },
            { MetricTags.MessageType, messageType }
        };
        _deadLetteredTotal.Add(1, tags);
    }

    #endregion

    #region Batch Methods

    /// <summary>
    /// Records a batch operation.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    /// <param name="batchSize">Number of messages in the batch.</param>
    public void RecordBatch(string queueName, int batchSize)
    {
        var tags = new TagList { { MetricTags.QueueName, queueName } };
        _batchesTotal.Add(1, tags);
        _batchSize.Record(batchSize, tags);
    }

    #endregion

    #region Queue Metrics

    /// <summary>
    /// Updates the queue depth gauge.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    /// <param name="delta">Change in queue depth.</param>
    public void UpdateQueueDepth(string queueName, int delta)
    {
        var tags = new TagList { { MetricTags.QueueName, queueName } };
        _queueDepth.Add(delta, tags);
    }

    /// <summary>
    /// Updates the active consumer count.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    /// <param name="delta">Change in consumer count.</param>
    public void UpdateActiveConsumers(string queueName, int delta)
    {
        var tags = new TagList { { MetricTags.QueueName, queueName } };
        _consumersActive.Add(delta, tags);
    }

    #endregion

    #region Connection Methods

    /// <summary>
    /// Records a connection attempt.
    /// </summary>
    /// <param name="success">Whether the connection was successful.</param>
    public void RecordConnectionAttempt(bool success)
    {
        _connectionsTotal.Add(1);

        if (!success)
        {
            _connectionFailuresTotal.Add(1);
        }
    }

    /// <summary>
    /// Updates the active connection count.
    /// </summary>
    /// <param name="delta">Change in connection count.</param>
    public void UpdateActiveConnections(int delta)
    {
        _connectionsActive.Add(delta);
    }

    /// <summary>
    /// Records a connection failure.
    /// </summary>
    /// <param name="errorType">Type of the error.</param>
    public void RecordConnectionFailure(string? errorType = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(MetricTags.ErrorType, errorType);
        }

        _connectionFailuresTotal.Add(1, tags);
    }

    #endregion

    #region Helper Methods

    private static TagList CreateMessageTags(string messageType, string destination, bool success)
    {
        return new TagList
        {
            { MetricTags.MessageType, messageType },
            { MetricTags.QueueName, destination },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };
    }

    #endregion

    #region Scope Structs

    /// <summary>
    /// Represents a scope for tracking publish operation duration.
    /// </summary>
    public readonly struct PublishScope : IDisposable
    {
        private readonly MessagingMetrics _metrics;
        private readonly string _messageType;
        private readonly string _destination;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the publish succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Gets or sets the payload size in bytes.
        /// </summary>
        public int PayloadSize { get; private set; }

        internal PublishScope(MessagingMetrics metrics, string messageType, string destination)
        {
            _metrics = metrics;
            _messageType = messageType;
            _destination = destination;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
            PayloadSize = 0;
        }

        /// <summary>
        /// Marks the publish as completed successfully.
        /// </summary>
        /// <param name="payloadSize">Size of the message in bytes.</param>
        public void Complete(int payloadSize = 0)
        {
            Succeeded = true;
            PayloadSize = payloadSize;
        }

        /// <summary>
        /// Marks the publish as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordPublish(_messageType, _destination, elapsed.TotalMilliseconds, Succeeded, PayloadSize);
        }
    }

    /// <summary>
    /// Represents a scope for tracking consume operation duration.
    /// </summary>
    public readonly struct ConsumeScope : IDisposable
    {
        private readonly MessagingMetrics _metrics;
        private readonly string _messageType;
        private readonly string _queueName;
        private readonly string? _consumerGroup;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the consume succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal ConsumeScope(MessagingMetrics metrics, string messageType, string queueName, string? consumerGroup)
        {
            _metrics = metrics;
            _messageType = messageType;
            _queueName = queueName;
            _consumerGroup = consumerGroup;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the consume as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the consume as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordConsume(_messageType, _queueName, elapsed.TotalMilliseconds, Succeeded, _consumerGroup);
        }
    }

    #endregion
}

