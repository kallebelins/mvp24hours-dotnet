//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Implementation of a single message item within a batch.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public class BatchMessageItem<TMessage> : IBatchMessageItem<TMessage> where TMessage : class
    {
        private readonly BasicDeliverEventArgs _deliverEventArgs;

        /// <summary>
        /// Creates a new batch message item.
        /// </summary>
        /// <param name="message">The deserialized message.</param>
        /// <param name="deliverEventArgs">The delivery event args from RabbitMQ.</param>
        public BatchMessageItem(TMessage message, BasicDeliverEventArgs deliverEventArgs)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _deliverEventArgs = deliverEventArgs ?? throw new ArgumentNullException(nameof(deliverEventArgs));
            ReceivedAt = DateTimeOffset.UtcNow;

            // Extract message metadata
            var props = deliverEventArgs.BasicProperties;
            MessageId = props?.MessageId ?? props?.CorrelationId ?? Guid.NewGuid().ToString();
            CorrelationId = props?.CorrelationId;

            // Parse headers
            var headers = new Dictionary<string, object>();
            if (props?.Headers != null)
            {
                foreach (var header in props.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }
            Headers = headers;

            // Extract additional metadata
            CausationId = GetHeader<string>("x-causation-id");
            RedeliveryCount = GetHeader<int?>("x-redelivered-count") ?? (deliverEventArgs.Redelivered ? 1 : 0);

            if (props?.Timestamp.UnixTime > 0)
            {
                SentAt = DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime);
            }
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public string? CausationId { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <inheritdoc />
        public string RoutingKey => _deliverEventArgs.RoutingKey;

        /// <inheritdoc />
        public ulong DeliveryTag => _deliverEventArgs.DeliveryTag;

        /// <inheritdoc />
        public bool Redelivered => _deliverEventArgs.Redelivered;

        /// <inheritdoc />
        public int RedeliveryCount { get; }

        /// <inheritdoc />
        public DateTimeOffset? SentAt { get; }

        /// <inheritdoc />
        public DateTimeOffset ReceivedAt { get; }

        /// <inheritdoc />
        public T? GetHeader<T>(string key)
        {
            if (Headers.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                if (value is byte[] bytes)
                {
                    var stringValue = System.Text.Encoding.UTF8.GetString(bytes);
                    if (typeof(T) == typeof(string))
                        return (T)(object)stringValue;

                    try
                    {
                        return (T)Convert.ChangeType(stringValue, typeof(T));
                    }
                    catch
                    {
                        return default;
                    }
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// Gets the original delivery event args.
        /// </summary>
        internal BasicDeliverEventArgs DeliverEventArgs => _deliverEventArgs;
    }
}

