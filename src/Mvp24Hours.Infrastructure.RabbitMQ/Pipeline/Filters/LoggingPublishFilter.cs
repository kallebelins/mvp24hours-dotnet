//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters
{
    /// <summary>
    /// Publish filter that provides automatic logging for message publishing.
    /// Logs message publish, processing time, success/failure status.
    /// </summary>
    public class LoggingPublishFilter : IPublishFilter
    {
        private readonly ILogger<LoggingPublishFilter>? _logger;

        /// <summary>
        /// Creates a new logging publish filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public LoggingPublishFilter(ILogger<LoggingPublishFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PublishAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var messageType = typeof(TMessage).Name;
            var messageId = context.MessageId;
            var correlationId = context.CorrelationId;
            var exchange = context.Exchange;
            var routingKey = context.RoutingKey;
            var stopwatch = Stopwatch.StartNew();

            LogMessagePublishing(messageType, messageId, correlationId, exchange, routingKey);

            try
            {
                await next(context, cancellationToken);
                
                stopwatch.Stop();
                
                if (context.ShouldCancelPublish)
                {
                    LogMessageCancelled(messageType, messageId, correlationId, context.CancellationReason);
                }
                else
                {
                    LogMessagePublished(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogMessagePublishFailed(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds, ex);
                throw;
            }
        }

        private void LogMessagePublishing(string messageType, string messageId, string? correlationId, string exchange, string routingKey)
        {
            _logger?.LogDebug(
                "Publishing message. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                messageType, messageId, correlationId, exchange, routingKey);
        }

        private void LogMessagePublished(string messageType, string messageId, string? correlationId, long elapsedMs)
        {
            _logger?.LogInformation(
                "Message published successfully. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                messageType, messageId, correlationId, elapsedMs);
        }

        private void LogMessageCancelled(string messageType, string messageId, string? correlationId, string? reason)
        {
            _logger?.LogWarning(
                "Message publish cancelled. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Reason={Reason}",
                messageType, messageId, correlationId, reason);
        }

        private void LogMessagePublishFailed(string messageType, string messageId, string? correlationId, long elapsedMs, Exception ex)
        {
            _logger?.LogError(ex,
                "Message publish failed. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                messageType, messageId, correlationId, elapsedMs);
        }
    }
}

