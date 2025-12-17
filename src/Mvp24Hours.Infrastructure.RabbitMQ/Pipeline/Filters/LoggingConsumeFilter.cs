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
    /// Consume filter that provides automatic logging for message consumption.
    /// Logs message receipt, processing time, success/failure status.
    /// </summary>
    public class LoggingConsumeFilter : IConsumeFilter
    {
        private readonly ILogger<LoggingConsumeFilter>? _logger;

        /// <summary>
        /// Creates a new logging consume filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public LoggingConsumeFilter(ILogger<LoggingConsumeFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var messageType = typeof(TMessage).Name;
            var messageId = context.MessageId;
            var correlationId = context.CorrelationId;
            var queueName = context.QueueName;
            var stopwatch = Stopwatch.StartNew();

            LogMessageReceived(messageType, messageId, correlationId, queueName, context.RedeliveryCount);

            try
            {
                await next(context, cancellationToken);
                
                stopwatch.Stop();
                LogMessageProcessed(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogMessageFailed(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds, ex);
                throw;
            }
        }

        private void LogMessageReceived(string messageType, string messageId, string? correlationId, string queueName, int redeliveryCount)
        {
            var message = $"Consuming message: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Queue={queueName}, RedeliveryCount={redeliveryCount}";
            
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-filter-logging-received", message);
            }
        }

        private void LogMessageProcessed(string messageType, string messageId, string? correlationId, long elapsedMs)
        {
            var message = $"Message processed successfully: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms";
            
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-filter-logging-processed", message);
            }
        }

        private void LogMessageFailed(string messageType, string messageId, string? correlationId, long elapsedMs, Exception ex)
        {
            var message = $"Message processing failed: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms, Error={ex.Message}";
            
            if (_logger != null)
            {
                _logger.LogError(ex, message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-filter-logging-failed", ex);
            }
        }
    }
}

