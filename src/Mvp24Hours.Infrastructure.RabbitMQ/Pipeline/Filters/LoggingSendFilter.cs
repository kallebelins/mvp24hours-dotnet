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
            _logger?.LogDebug(
                "Sending message. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Destination={Destination}",
                messageType, messageId, correlationId, destination);
        }

        private void LogMessageSent(string messageType, string messageId, string? correlationId, long elapsedMs)
        {
            _logger?.LogInformation(
                "Message sent successfully. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                messageType, messageId, correlationId, elapsedMs);
        }

        private void LogMessageCancelled(string messageType, string messageId, string? correlationId, string? reason)
        {
            _logger?.LogWarning(
                "Message send cancelled. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Reason={Reason}",
                messageType, messageId, correlationId, reason);
        }

        private void LogMessageSendFailed(string messageType, string messageId, string? correlationId, long elapsedMs, Exception ex)
        {
            _logger?.LogError(ex,
                "Message send failed. Type={MessageType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                messageType, messageId, correlationId, elapsedMs);
        }
    }
}

