//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Metrics
{
    /// <summary>
    /// Thread-safe metrics collector for RabbitMQ operations.
    /// </summary>
    public class RabbitMQMetrics : IRabbitMQMetrics
    {
        private long _messagesSent;
        private long _messagesReceived;
        private long _messagesAcked;
        private long _messagesNacked;
        private long _messagesRejected;
        private long _messagesRedelivered;
        private long _publisherConfirms;
        private long _publisherNacks;
        private long _connectionFailures;
        private long _channelCreations;
        private long _duplicateMessagesSkipped;
        
        private readonly ConcurrentDictionary<string, long> _messagesByQueue = new();
        private readonly ConcurrentDictionary<string, long> _messagesByExchange = new();
        private readonly ConcurrentDictionary<string, long> _errorsByType = new();

        /// <inheritdoc />
        public long MessagesSent => Interlocked.Read(ref _messagesSent);

        /// <inheritdoc />
        public long MessagesReceived => Interlocked.Read(ref _messagesReceived);

        /// <inheritdoc />
        public long MessagesAcked => Interlocked.Read(ref _messagesAcked);

        /// <inheritdoc />
        public long MessagesNacked => Interlocked.Read(ref _messagesNacked);

        /// <inheritdoc />
        public long MessagesRejected => Interlocked.Read(ref _messagesRejected);

        /// <inheritdoc />
        public long MessagesRedelivered => Interlocked.Read(ref _messagesRedelivered);

        /// <inheritdoc />
        public long PublisherConfirms => Interlocked.Read(ref _publisherConfirms);

        /// <inheritdoc />
        public long PublisherNacks => Interlocked.Read(ref _publisherNacks);

        /// <inheritdoc />
        public long ConnectionFailures => Interlocked.Read(ref _connectionFailures);

        /// <inheritdoc />
        public long ChannelCreations => Interlocked.Read(ref _channelCreations);

        /// <inheritdoc />
        public long DuplicateMessagesSkipped => Interlocked.Read(ref _duplicateMessagesSkipped);

        /// <inheritdoc />
        public void IncrementMessagesSent(string? exchange = null)
        {
            Interlocked.Increment(ref _messagesSent);
            if (!string.IsNullOrEmpty(exchange))
            {
                _messagesByExchange.AddOrUpdate(exchange, 1, (_, count) => count + 1);
            }
        }

        /// <inheritdoc />
        public void IncrementMessagesReceived(string? queue = null)
        {
            Interlocked.Increment(ref _messagesReceived);
            if (!string.IsNullOrEmpty(queue))
            {
                _messagesByQueue.AddOrUpdate(queue, 1, (_, count) => count + 1);
            }
        }

        /// <inheritdoc />
        public void IncrementMessagesAcked() => Interlocked.Increment(ref _messagesAcked);

        /// <inheritdoc />
        public void IncrementMessagesNacked() => Interlocked.Increment(ref _messagesNacked);

        /// <inheritdoc />
        public void IncrementMessagesRejected() => Interlocked.Increment(ref _messagesRejected);

        /// <inheritdoc />
        public void IncrementMessagesRedelivered() => Interlocked.Increment(ref _messagesRedelivered);

        /// <inheritdoc />
        public void IncrementPublisherConfirms() => Interlocked.Increment(ref _publisherConfirms);

        /// <inheritdoc />
        public void IncrementPublisherNacks() => Interlocked.Increment(ref _publisherNacks);

        /// <inheritdoc />
        public void IncrementConnectionFailures() => Interlocked.Increment(ref _connectionFailures);

        /// <inheritdoc />
        public void IncrementChannelCreations() => Interlocked.Increment(ref _channelCreations);

        /// <inheritdoc />
        public void IncrementDuplicateMessagesSkipped() => Interlocked.Increment(ref _duplicateMessagesSkipped);

        /// <inheritdoc />
        public void IncrementError(string errorType)
        {
            _errorsByType.AddOrUpdate(errorType, 1, (_, count) => count + 1);
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
            Interlocked.Exchange(ref _messagesSent, 0);
            Interlocked.Exchange(ref _messagesReceived, 0);
            Interlocked.Exchange(ref _messagesAcked, 0);
            Interlocked.Exchange(ref _messagesNacked, 0);
            Interlocked.Exchange(ref _messagesRejected, 0);
            Interlocked.Exchange(ref _messagesRedelivered, 0);
            Interlocked.Exchange(ref _publisherConfirms, 0);
            Interlocked.Exchange(ref _publisherNacks, 0);
            Interlocked.Exchange(ref _connectionFailures, 0);
            Interlocked.Exchange(ref _channelCreations, 0);
            Interlocked.Exchange(ref _duplicateMessagesSkipped, 0);
            _messagesByQueue.Clear();
            _messagesByExchange.Clear();
            _errorsByType.Clear();
        }
    }
}

