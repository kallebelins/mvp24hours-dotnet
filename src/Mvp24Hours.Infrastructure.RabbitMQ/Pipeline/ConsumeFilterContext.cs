//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline
{
    /// <summary>
    /// Implementation of consume filter context with message metadata and filter operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public class ConsumeFilterContext<TMessage> : IConsumeFilterContext<TMessage> where TMessage : class
    {
        private readonly IConsumeContext<TMessage> _consumeContext;

        /// <summary>
        /// Creates a new consume filter context.
        /// </summary>
        /// <param name="consumeContext">The underlying consume context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public ConsumeFilterContext(
            IConsumeContext<TMessage> consumeContext,
            CancellationToken cancellationToken = default)
        {
            _consumeContext = consumeContext ?? throw new ArgumentNullException(nameof(consumeContext));
            CancellationToken = cancellationToken;
            Items = new Dictionary<string, object?>();
        }

        /// <inheritdoc />
        public TMessage Message => _consumeContext.Message;

        /// <inheritdoc />
        public string MessageId => _consumeContext.MessageId;

        /// <inheritdoc />
        public string? CorrelationId => _consumeContext.CorrelationId;

        /// <inheritdoc />
        public string? CausationId => _consumeContext.CausationId;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers => _consumeContext.Headers;

        /// <inheritdoc />
        public string Exchange => _consumeContext.Exchange;

        /// <inheritdoc />
        public string RoutingKey => _consumeContext.RoutingKey;

        /// <inheritdoc />
        public string QueueName => _consumeContext.QueueName;

        /// <inheritdoc />
        public string ConsumerTag => _consumeContext.ConsumerTag;

        /// <inheritdoc />
        public ulong DeliveryTag => _consumeContext.DeliveryTag;

        /// <inheritdoc />
        public bool Redelivered => _consumeContext.Redelivered;

        /// <inheritdoc />
        public int RedeliveryCount => _consumeContext.RedeliveryCount;

        /// <inheritdoc />
        public DateTimeOffset? SentAt => _consumeContext.SentAt;

        /// <inheritdoc />
        public DateTimeOffset ReceivedAt => _consumeContext.ReceivedAt;

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _consumeContext.ServiceProvider;

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public IDictionary<string, object?> Items { get; }

        /// <inheritdoc />
        public IConsumeContext<TMessage> ConsumeContext => _consumeContext;

        /// <inheritdoc />
        public bool ShouldSkipRemainingFilters { get; private set; }

        /// <inheritdoc />
        public bool ShouldRetry { get; private set; }

        /// <inheritdoc />
        public TimeSpan? RetryDelay { get; private set; }

        /// <inheritdoc />
        public bool ShouldSendToDeadLetter { get; private set; }

        /// <inheritdoc />
        public string? DeadLetterReason { get; private set; }

        /// <inheritdoc />
        public Exception? Exception { get; private set; }

        /// <inheritdoc />
        public T? GetHeader<T>(string key) => _consumeContext.GetHeader<T>(key);

        /// <inheritdoc />
        public void SkipRemainingFilters()
        {
            ShouldSkipRemainingFilters = true;
        }

        /// <inheritdoc />
        public void SetRetry(TimeSpan? retryDelay = null)
        {
            ShouldRetry = true;
            RetryDelay = retryDelay;
        }

        /// <inheritdoc />
        public void SendToDeadLetter(string reason)
        {
            ShouldSendToDeadLetter = true;
            DeadLetterReason = reason;
        }

        /// <inheritdoc />
        public void SetException(Exception exception)
        {
            Exception = exception;
        }

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            return _consumeContext.PublishAsync(message, routingKey, cancellationToken);
        }

        /// <summary>
        /// Resets the retry flag.
        /// </summary>
        public void ResetRetry()
        {
            ShouldRetry = false;
            RetryDelay = null;
        }

        /// <summary>
        /// Resets the dead letter flag.
        /// </summary>
        public void ResetDeadLetter()
        {
            ShouldSendToDeadLetter = false;
            DeadLetterReason = null;
        }

        /// <summary>
        /// Resets the skip flag.
        /// </summary>
        public void ResetSkip()
        {
            ShouldSkipRemainingFilters = false;
        }

        /// <summary>
        /// Resets the exception.
        /// </summary>
        public void ResetException()
        {
            Exception = null;
        }
    }
}

