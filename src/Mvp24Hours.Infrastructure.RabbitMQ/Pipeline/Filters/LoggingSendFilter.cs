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
    /// Send filter that provides automatic logging for message sending.
    /// Logs message send, processing time, success/failure status.
    /// </summary>
    public class LoggingSendFilter : ISendFilter
    {
        private readonly ILogger<LoggingSendFilter>? _logger;

        /// <summary>
        /// Creates a new logging send filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public LoggingSendFilter(ILogger<LoggingSendFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            SendFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var messageType = typeof(TMessage).Name;
            var messageId = context.MessageId;
            var correlationId = context.CorrelationId;
            var destination = context.DestinationQueue;
            var stopwatch = Stopwatch.StartNew();

            LogMessageSending(messageType, messageId, correlationId, destination);

            try
            {
                await next(context, cancellationToken);
                
                stopwatch.Stop();
                
                if (context.ShouldCancelSend)
                {
                    LogMessageCancelled(messageType, messageId, correlationId, context.CancellationReason);
                }
                else
                {
                    LogMessageSent(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogMessageSendFailed(messageType, messageId, correlationId, stopwatch.ElapsedMilliseconds, ex);
                throw;
            }
        }

        private void LogMessageSending(string messageType, string messageId, string? correlationId, string destination)
        {
            var message = $"Sending message: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Destination={destination}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-logging-sending", message);
            }
        }

        private void LogMessageSent(string messageType, string messageId, string? correlationId, long elapsedMs)
        {
            var message = $"Message sent successfully: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms";
            
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-filter-logging-sent", message);
            }
        }

        private void LogMessageCancelled(string messageType, string messageId, string? correlationId, string? reason)
        {
            var message = $"Message send cancelled: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Reason={reason}";
            
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-filter-logging-send-cancelled", message);
            }
        }

        private void LogMessageSendFailed(string messageType, string messageId, string? correlationId, long elapsedMs, Exception ex)
        {
            var message = $"Message send failed: Type={messageType}, MessageId={messageId}, CorrelationId={correlationId}, Duration={elapsedMs}ms, Error={ex.Message}";
            
            if (_logger != null)
            {
                _logger.LogError(ex, message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-filter-logging-send-failed", ex);
            }
        }
    }
}

