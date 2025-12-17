//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
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
            var message = $"Publishing message: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Exchange={exchange}, RoutingKey={routingKey}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-logging-publishing", message);
            }
        }

        private void LogMessagePublished(string messageType, string messageId, string? correlationId, long elapsedMs)
        {
            var message = $"Message published successfully: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms";
            
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-filter-logging-published", message);
            }
        }

        private void LogMessageCancelled(string messageType, string messageId, string? correlationId, string? reason)
        {
            var message = $"Message publish cancelled: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Reason={reason}";
            
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-filter-logging-cancelled", message);
            }
        }

        private void LogMessagePublishFailed(string messageType, string messageId, string? correlationId, long elapsedMs, Exception ex)
        {
            var message = $"Message publish failed: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms, Error={ex.Message}";
            
            if (_logger != null)
            {
                _logger.LogError(ex, message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-filter-logging-publish-failed", ex);
            }
        }
    }
}

