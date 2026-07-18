//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

/// <summary>
/// Publish filter that provides automatic logging for message publishing.
/// Logs message publish, processing time, success/failure status.
/// </summary>
/// <remarks>
/// Creates a new logging publish filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
public class LoggingPublishFilter(ILogger<LoggingPublishFilter>? logger = null) : IPublishFilter
{
    private readonly ILogger<LoggingPublishFilter>? _logger = logger;

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        IPublishFilterContext<TMessage> context,
        PublishFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        string messageType = typeof(TMessage).Name;
        string messageId = context.MessageId;
        string? correlationId = context.CorrelationId;
        string exchange = context.Exchange;
        string routingKey = context.RoutingKey;
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

