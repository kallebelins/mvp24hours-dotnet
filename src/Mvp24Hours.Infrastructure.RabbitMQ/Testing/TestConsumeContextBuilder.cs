//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Builder for creating test consume contexts with custom configurations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public class TestConsumeContextBuilder<TMessage> where TMessage : class
    {
        private string? _messageId;
        private string? _correlationId;
        private string? _causationId;
        private string? _exchange;
        private string? _routingKey;
        private string? _queueName;
        private readonly Dictionary<string, object> _headers = new();
        private int _redeliveryCount;
        private DateTimeOffset? _sentAt;
        private CancellationToken _cancellationToken;
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Sets the message ID.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithMessageId(string messageId)
        {
            _messageId = messageId;
            return this;
        }

        /// <summary>
        /// Sets the correlation ID.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithCorrelationId(string correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        /// <summary>
        /// Sets the causation ID.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithCausationId(string causationId)
        {
            _causationId = causationId;
            return this;
        }

        /// <summary>
        /// Sets the exchange name.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithExchange(string exchange)
        {
            _exchange = exchange;
            return this;
        }

        /// <summary>
        /// Sets the routing key.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithRoutingKey(string routingKey)
        {
            _routingKey = routingKey;
            return this;
        }

        /// <summary>
        /// Sets the queue name.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithQueueName(string queueName)
        {
            _queueName = queueName;
            return this;
        }

        /// <summary>
        /// Adds a header to the context.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple headers to the context.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithHeaders(IDictionary<string, object> headers)
        {
            foreach (var header in headers)
            {
                _headers[header.Key] = header.Value;
            }
            return this;
        }

        /// <summary>
        /// Sets the redelivery count (simulates redelivery).
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithRedeliveryCount(int count)
        {
            _redeliveryCount = count;
            return this;
        }

        /// <summary>
        /// Simulates a redelivered message.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> AsRedelivered(int redeliveryCount = 1)
        {
            _redeliveryCount = Math.Max(1, redeliveryCount);
            return this;
        }

        /// <summary>
        /// Sets when the message was sent.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> SentAt(DateTimeOffset sentAt)
        {
            _sentAt = sentAt;
            return this;
        }

        /// <summary>
        /// Sets the cancellation token.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Sets the service provider.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> WithServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            return this;
        }

        /// <summary>
        /// Adds a tenant header.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> ForTenant(string tenantId)
        {
            _headers["x-tenant-id"] = tenantId;
            return this;
        }

        /// <summary>
        /// Adds a user header.
        /// </summary>
        public TestConsumeContextBuilder<TMessage> ForUser(string userId)
        {
            _headers["x-user-id"] = userId;
            return this;
        }

        /// <summary>
        /// Builds the test consume context.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A configured test consume context.</returns>
        public TestConsumeContext<TMessage> Build(TMessage message)
        {
            var serviceProvider = _serviceProvider ?? CreateDefaultServiceProvider();

            return new TestConsumeContext<TMessage>(
                message,
                serviceProvider,
                _messageId,
                _correlationId,
                _causationId,
                _exchange,
                _routingKey,
                _queueName,
                _headers.Count > 0 ? _headers : null,
                _redeliveryCount,
                _sentAt,
                _cancellationToken);
        }

        private static IServiceProvider CreateDefaultServiceProvider()
        {
            var services = new ServiceCollection();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        public static TestConsumeContextBuilder<TMessage> Create() => new();
    }
}

