//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Prometheus-compatible metrics for RabbitMQ operations using System.Diagnostics.Metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class provides metrics that are compatible with Prometheus/OpenMetrics format
/// and can be exported via OpenTelemetry metrics exporter. The metrics follow
/// Prometheus naming conventions and include relevant labels/tags.
/// </para>
/// <para>
/// <strong>Available Metrics:</strong>
/// <list type="bullet">
/// <item>mvp_rabbitmq_messages_published_total - Total messages published</item>
/// <item>mvp_rabbitmq_messages_consumed_total - Total messages consumed</item>
/// <item>mvp_rabbitmq_messages_failed_total - Total messages that failed processing</item>
/// <item>mvp_rabbitmq_message_processing_duration_seconds - Message processing duration histogram</item>
/// <item>mvp_rabbitmq_message_publish_duration_seconds - Message publish duration histogram</item>
/// <item>mvp_rabbitmq_messages_redelivered_total - Total redelivered messages</item>
/// <item>mvp_rabbitmq_connections_total - Total connection events</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to export metrics
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(metrics =>
///     {
///         metrics
///             .AddMeter(RabbitMQPrometheusMetrics.MeterName)
///             .AddPrometheusExporter();
///     });
/// </code>
/// </example>
public sealed class RabbitMQPrometheusMetrics : IRabbitMQMetrics, IConsumeObserver, IPublishObserver, IDisposable
{
    /// <summary>
    /// The meter name for OpenTelemetry configuration.
    /// </summary>
    public const string MeterName = "Mvp24Hours.RabbitMQ";

    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _messagesPublished;
    private readonly Counter<long> _messagesConsumed;
    private readonly Counter<long> _messagesFailed;
    private readonly Counter<long> _messagesRedelivered;
    private readonly Counter<long> _messagesAcked;
    private readonly Counter<long> _messagesNacked;
    private readonly Counter<long> _messagesRejected;
    private readonly Counter<long> _publisherConfirms;
    private readonly Counter<long> _publisherNacks;
    private readonly Counter<long> _connectionEvents;
    private readonly Counter<long> _duplicateMessagesSkipped;

    // Histograms
    private readonly Histogram<double> _processingDuration;
    private readonly Histogram<double> _publishDuration;
    private readonly Histogram<double> _messageSize;

    // Internal counters for IRabbitMQMetrics compatibility
    private long _messagesSentCount;
    private long _messagesReceivedCount;
    private long _messagesAckedCount;
    private long _messagesNackedCount;
    private long _messagesRejectedCount;
    private long _messagesRedeliveredCount;
    private long _publisherConfirmsCount;
    private long _publisherNacksCount;
    private long _connectionFailuresCount;
    private long _channelCreationsCount;
    private long _duplicateMessagesSkippedCount;

    private readonly ConcurrentDictionary<string, long> _messagesByQueue = new();
    private readonly ConcurrentDictionary<string, long> _messagesByExchange = new();
    private readonly ConcurrentDictionary<string, long> _errorsByType = new();

    /// <summary>
    /// Creates a new instance of RabbitMQPrometheusMetrics.
    /// </summary>
    public RabbitMQPrometheusMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        // Counters
        _messagesPublished = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_published_total",
            "messages",
            "Total number of messages published to RabbitMQ");

        _messagesConsumed = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_consumed_total",
            "messages",
            "Total number of messages consumed from RabbitMQ");

        _messagesFailed = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_failed_total",
            "messages",
            "Total number of messages that failed processing");

        _messagesRedelivered = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_redelivered_total",
            "messages",
            "Total number of redelivered messages");

        _messagesAcked = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_acked_total",
            "messages",
            "Total number of acknowledged messages");

        _messagesNacked = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_nacked_total",
            "messages",
            "Total number of negatively acknowledged messages");

        _messagesRejected = _meter.CreateCounter<long>(
            "mvp_rabbitmq_messages_rejected_total",
            "messages",
            "Total number of rejected messages");

        _publisherConfirms = _meter.CreateCounter<long>(
            "mvp_rabbitmq_publisher_confirms_total",
            "confirms",
            "Total number of publisher confirms received");

        _publisherNacks = _meter.CreateCounter<long>(
            "mvp_rabbitmq_publisher_nacks_total",
            "nacks",
            "Total number of publisher nacks received");

        _connectionEvents = _meter.CreateCounter<long>(
            "mvp_rabbitmq_connection_events_total",
            "events",
            "Total number of connection events");

        _duplicateMessagesSkipped = _meter.CreateCounter<long>(
            "mvp_rabbitmq_duplicate_messages_skipped_total",
            "messages",
            "Total number of duplicate messages skipped");

        // Histograms
        _processingDuration = _meter.CreateHistogram<double>(
            "mvp_rabbitmq_message_processing_duration_seconds",
            "seconds",
            "Duration of message processing in seconds");

        _publishDuration = _meter.CreateHistogram<double>(
            "mvp_rabbitmq_message_publish_duration_seconds",
            "seconds",
            "Duration of message publishing in seconds");

        _messageSize = _meter.CreateHistogram<double>(
            "mvp_rabbitmq_message_size_bytes",
            "bytes",
            "Size of messages in bytes");

        // Create observable gauges for current state
        _meter.CreateObservableGauge(
            "mvp_rabbitmq_messages_in_progress",
            () => new Measurement<long>(_messagesReceivedCount - _messagesAckedCount - _messagesNackedCount - _messagesRejectedCount),
            "messages",
            "Current number of messages being processed");
    }

    #region IRabbitMQMetrics Implementation

    /// <inheritdoc />
    public long MessagesSent => Interlocked.Read(ref _messagesSentCount);

    /// <inheritdoc />
    public long MessagesReceived => Interlocked.Read(ref _messagesReceivedCount);

    /// <inheritdoc />
    public long MessagesAcked => Interlocked.Read(ref _messagesAckedCount);

    /// <inheritdoc />
    public long MessagesNacked => Interlocked.Read(ref _messagesNackedCount);

    /// <inheritdoc />
    public long MessagesRejected => Interlocked.Read(ref _messagesRejectedCount);

    /// <inheritdoc />
    public long MessagesRedelivered => Interlocked.Read(ref _messagesRedeliveredCount);

    /// <inheritdoc />
    public long PublisherConfirms => Interlocked.Read(ref _publisherConfirmsCount);

    /// <inheritdoc />
    public long PublisherNacks => Interlocked.Read(ref _publisherNacksCount);

    /// <inheritdoc />
    public long ConnectionFailures => Interlocked.Read(ref _connectionFailuresCount);

    /// <inheritdoc />
    public long ChannelCreations => Interlocked.Read(ref _channelCreationsCount);

    /// <inheritdoc />
    public long DuplicateMessagesSkipped => Interlocked.Read(ref _duplicateMessagesSkippedCount);

    /// <inheritdoc />
    public void IncrementMessagesSent(string? exchange = null)
    {
        Interlocked.Increment(ref _messagesSentCount);
        _messagesPublished.Add(1, new KeyValuePair<string, object?>("exchange", exchange ?? "default"));
        
        if (!string.IsNullOrEmpty(exchange))
        {
            _messagesByExchange.AddOrUpdate(exchange, 1, (_, count) => count + 1);
        }
    }

    /// <inheritdoc />
    public void IncrementMessagesReceived(string? queue = null)
    {
        Interlocked.Increment(ref _messagesReceivedCount);
        _messagesConsumed.Add(1, new KeyValuePair<string, object?>("queue", queue ?? "default"));
        
        if (!string.IsNullOrEmpty(queue))
        {
            _messagesByQueue.AddOrUpdate(queue, 1, (_, count) => count + 1);
        }
    }

    /// <inheritdoc />
    public void IncrementMessagesAcked()
    {
        Interlocked.Increment(ref _messagesAckedCount);
        _messagesAcked.Add(1);
    }

    /// <inheritdoc />
    public void IncrementMessagesNacked()
    {
        Interlocked.Increment(ref _messagesNackedCount);
        _messagesNacked.Add(1);
    }

    /// <inheritdoc />
    public void IncrementMessagesRejected()
    {
        Interlocked.Increment(ref _messagesRejectedCount);
        _messagesRejected.Add(1);
    }

    /// <inheritdoc />
    public void IncrementMessagesRedelivered()
    {
        Interlocked.Increment(ref _messagesRedeliveredCount);
        _messagesRedelivered.Add(1);
    }

    /// <inheritdoc />
    public void IncrementPublisherConfirms()
    {
        Interlocked.Increment(ref _publisherConfirmsCount);
        _publisherConfirms.Add(1);
    }

    /// <inheritdoc />
    public void IncrementPublisherNacks()
    {
        Interlocked.Increment(ref _publisherNacksCount);
        _publisherNacks.Add(1);
    }

    /// <inheritdoc />
    public void IncrementConnectionFailures()
    {
        Interlocked.Increment(ref _connectionFailuresCount);
        _connectionEvents.Add(1, new KeyValuePair<string, object?>("event_type", "failure"));
    }

    /// <inheritdoc />
    public void IncrementChannelCreations()
    {
        Interlocked.Increment(ref _channelCreationsCount);
    }

    /// <inheritdoc />
    public void IncrementDuplicateMessagesSkipped()
    {
        Interlocked.Increment(ref _duplicateMessagesSkippedCount);
        _duplicateMessagesSkipped.Add(1);
    }

    /// <inheritdoc />
    public void IncrementError(string errorType)
    {
        _errorsByType.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        _messagesFailed.Add(1, new KeyValuePair<string, object?>("error_type", errorType));
    }

    /// <inheritdoc />
    public RabbitMQMetricsSnapshot GetSnapshot()
    {
        return new RabbitMQMetricsSnapshot
        {
            MessagesSent = MessagesSent,
            MessagesReceived = MessagesReceived,
            MessagesAcked = MessagesAcked,
            MessagesNacked = MessagesNacked,
            MessagesRejected = MessagesRejected,
            MessagesRedelivered = MessagesRedelivered,
            PublisherConfirms = PublisherConfirms,
            PublisherNacks = PublisherNacks,
            ConnectionFailures = ConnectionFailures,
            ChannelCreations = ChannelCreations,
            DuplicateMessagesSkipped = DuplicateMessagesSkipped,
            MessagesByQueue = new ConcurrentDictionary<string, long>(_messagesByQueue),
            MessagesByExchange = new ConcurrentDictionary<string, long>(_messagesByExchange),
            ErrorsByType = new ConcurrentDictionary<string, long>(_errorsByType),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _messagesSentCount, 0);
        Interlocked.Exchange(ref _messagesReceivedCount, 0);
        Interlocked.Exchange(ref _messagesAckedCount, 0);
        Interlocked.Exchange(ref _messagesNackedCount, 0);
        Interlocked.Exchange(ref _messagesRejectedCount, 0);
        Interlocked.Exchange(ref _messagesRedeliveredCount, 0);
        Interlocked.Exchange(ref _publisherConfirmsCount, 0);
        Interlocked.Exchange(ref _publisherNacksCount, 0);
        Interlocked.Exchange(ref _connectionFailuresCount, 0);
        Interlocked.Exchange(ref _channelCreationsCount, 0);
        Interlocked.Exchange(ref _duplicateMessagesSkippedCount, 0);
        _messagesByQueue.Clear();
        _messagesByExchange.Clear();
        _errorsByType.Clear();
    }

    #endregion

    #region IConsumeObserver Implementation

    /// <inheritdoc />
    public Task PreConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
    {
        IncrementMessagesReceived(context.QueueName);
        
        if (context.Redelivered)
        {
            IncrementMessagesRedelivered();
        }

        _messageSize.Record(context.PayloadSize,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("queue", context.QueueName));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PostConsumeAsync(ConsumeObserverContext context, CancellationToken cancellationToken = default)
    {
        IncrementMessagesAcked();

        _processingDuration.Record(context.Duration.TotalSeconds,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("queue", context.QueueName),
            new KeyValuePair<string, object?>("status", "success"));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ConsumeFaultAsync(ConsumeObserverContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        IncrementError(exception.GetType().Name);

        _processingDuration.Record(context.Duration.TotalSeconds,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("queue", context.QueueName),
            new KeyValuePair<string, object?>("status", "error"),
            new KeyValuePair<string, object?>("error_type", exception.GetType().Name));

        return Task.CompletedTask;
    }

    #endregion

    #region IPublishObserver Implementation

    /// <inheritdoc />
    public Task PrePublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
    {
        _messageSize.Record(context.PayloadSize,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("exchange", context.Exchange));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PostPublishAsync(PublishObserverContext context, CancellationToken cancellationToken = default)
    {
        IncrementMessagesSent(context.Exchange);

        if (context.Confirmed)
        {
            IncrementPublisherConfirms();
        }

        _publishDuration.Record(context.Duration.TotalSeconds,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("exchange", context.Exchange),
            new KeyValuePair<string, object?>("status", "success"));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishFaultAsync(PublishObserverContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        IncrementError(exception.GetType().Name);
        IncrementPublisherNacks();

        _publishDuration.Record(context.Duration.TotalSeconds,
            new KeyValuePair<string, object?>("message_type", context.MessageType),
            new KeyValuePair<string, object?>("exchange", context.Exchange),
            new KeyValuePair<string, object?>("status", "error"),
            new KeyValuePair<string, object?>("error_type", exception.GetType().Name));

        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Records a connection event.
    /// </summary>
    /// <param name="eventType">The type of connection event (connected, disconnected, reconnecting, etc.).</param>
    /// <param name="hostName">The host name.</param>
    public void RecordConnectionEvent(string eventType, string hostName)
    {
        _connectionEvents.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("host", hostName));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}

