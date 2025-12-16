//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Implementation of consume context with message metadata and operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public class ConsumeContext<TMessage> : IConsumeContext<TMessage> where TMessage : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMvpRabbitMQClient? _rabbitMQClient;
        private readonly BasicDeliverEventArgs _deliverEventArgs;

        /// <summary>
        /// Creates a new consume context.
        /// </summary>
        public ConsumeContext(
            TMessage message,
            BasicDeliverEventArgs deliverEventArgs,
            IServiceProvider serviceProvider,
            IMvpRabbitMQClient? rabbitMQClient = null,
            string? queueName = null,
            CancellationToken cancellationToken = default)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _deliverEventArgs = deliverEventArgs ?? throw new ArgumentNullException(nameof(deliverEventArgs));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _rabbitMQClient = rabbitMQClient;
            QueueName = queueName ?? string.Empty;
            CancellationToken = cancellationToken;
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
        public string Exchange => _deliverEventArgs.Exchange;

        /// <inheritdoc />
        public string RoutingKey => _deliverEventArgs.RoutingKey;

        /// <inheritdoc />
        public string QueueName { get; }

        /// <inheritdoc />
        public string ConsumerTag => _deliverEventArgs.ConsumerTag;

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
        public IServiceProvider ServiceProvider => _serviceProvider;

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

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

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_rabbitMQClient == null)
                throw new InvalidOperationException("RabbitMQ client is not available in this context.");

            var headers = new Dictionary<string, object>();
            
            // Propagate correlation ID
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                headers["x-correlation-id"] = CorrelationId;
            }
            
            // Set causation ID to current message ID
            headers["x-causation-id"] = MessageId;

            _rabbitMQClient.Publish(message, routingKey ?? RoutingKey, headers);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RespondAsync<T>(T response, CancellationToken cancellationToken = default) where T : class
        {
            var replyTo = _deliverEventArgs.BasicProperties?.ReplyTo;
            if (string.IsNullOrEmpty(replyTo))
                throw new InvalidOperationException("Cannot respond: no reply-to address was specified.");

            if (_rabbitMQClient == null)
                throw new InvalidOperationException("RabbitMQ client is not available in this context.");

            var headers = new Dictionary<string, object>
            {
                ["x-correlation-id"] = CorrelationId ?? MessageId
            };

            _rabbitMQClient.Publish(response, replyTo, headers);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Core.Contract.IServiceScope CreateScope()
        {
            var scope = _serviceProvider.CreateScope();
            return new ServiceScopeWrapper(scope);
        }

        private class ServiceScopeWrapper : Core.Contract.IServiceScope
        {
            private readonly Microsoft.Extensions.DependencyInjection.IServiceScope _scope;

            public ServiceScopeWrapper(Microsoft.Extensions.DependencyInjection.IServiceScope scope)
            {
                _scope = scope;
            }

            public IServiceProvider ServiceProvider => _scope.ServiceProvider;

            public void Dispose()
            {
                _scope.Dispose();
            }
        }
    }
}

