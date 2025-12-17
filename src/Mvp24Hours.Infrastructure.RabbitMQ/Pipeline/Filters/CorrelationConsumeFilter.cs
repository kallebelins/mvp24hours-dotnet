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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters
{
    /// <summary>
    /// Consume filter that propagates correlation and causation IDs for distributed tracing.
    /// Stores correlation context for use in downstream operations.
    /// </summary>
    public class CorrelationConsumeFilter : IConsumeFilter
    {
        private readonly ILogger<CorrelationConsumeFilter>? _logger;
        private static readonly AsyncLocal<CorrelationContext?> _currentContext = new();

        /// <summary>
        /// Gets the current correlation context.
        /// </summary>
        public static CorrelationContext? Current => _currentContext.Value;

        /// <summary>
        /// Creates a new correlation consume filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public CorrelationConsumeFilter(ILogger<CorrelationConsumeFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            // Extract or generate correlation ID
            var correlationId = context.CorrelationId ?? context.MessageId;
            var causationId = context.CausationId;
            var messageId = context.MessageId;

            // Create correlation context
            var correlationContext = new CorrelationContext(
                correlationId,
                causationId,
                messageId);

            // Store in AsyncLocal for downstream access
            var previousContext = _currentContext.Value;
            _currentContext.Value = correlationContext;

            // Store in Items for filter chain access
            context.Items["CorrelationId"] = correlationId;
            context.Items["CausationId"] = causationId;
            context.Items["MessageId"] = messageId;
            context.Items["CorrelationContext"] = correlationContext;

            LogCorrelationStarted(correlationId, causationId, messageId);

            try
            {
                await next(context, cancellationToken);
            }
            finally
            {
                // Restore previous context
                _currentContext.Value = previousContext;
                
                LogCorrelationEnded(correlationId);
            }
        }

        private void LogCorrelationStarted(string correlationId, string? causationId, string messageId)
        {
            var message = $"Correlation context started: CorrelationId={correlationId}, CausationId={causationId}, MessageId={messageId}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-correlation-started", message);
            }
        }

        private void LogCorrelationEnded(string correlationId)
        {
            var message = $"Correlation context ended: CorrelationId={correlationId}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-correlation-ended", message);
            }
        }
    }

    /// <summary>
    /// Publish filter that propagates correlation and causation IDs.
    /// </summary>
    public class CorrelationPublishFilter : IPublishFilter
    {
        private readonly ILogger<CorrelationPublishFilter>? _logger;

        /// <summary>
        /// Creates a new correlation publish filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public CorrelationPublishFilter(ILogger<CorrelationPublishFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PublishAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            // Get current correlation context
            var currentContext = CorrelationConsumeFilter.Current;

            // Set correlation ID (preserve existing or use from context or generate new)
            var correlationId = context.CorrelationId 
                ?? currentContext?.CorrelationId 
                ?? Guid.NewGuid().ToString();
            
            context.SetCorrelationId(correlationId);

            // Set causation ID to the current message ID (the message that caused this publish)
            var causationId = currentContext?.MessageId ?? context.CausationId;
            if (!string.IsNullOrEmpty(causationId))
            {
                context.SetCausationId(causationId);
            }

            LogCorrelationPropagated(correlationId, causationId, context.MessageId);

            await next(context, cancellationToken);
        }

        private void LogCorrelationPropagated(string correlationId, string? causationId, string messageId)
        {
            var message = $"Correlation propagated to publish: CorrelationId={correlationId}, CausationId={causationId}, MessageId={messageId}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-correlation-propagated", message);
            }
        }
    }

    /// <summary>
    /// Send filter that propagates correlation and causation IDs.
    /// </summary>
    public class CorrelationSendFilter : ISendFilter
    {
        private readonly ILogger<CorrelationSendFilter>? _logger;

        /// <summary>
        /// Creates a new correlation send filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public CorrelationSendFilter(ILogger<CorrelationSendFilter>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            SendFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            // Get current correlation context
            var currentContext = CorrelationConsumeFilter.Current;

            // Set correlation ID (preserve existing or use from context or generate new)
            var correlationId = context.CorrelationId 
                ?? currentContext?.CorrelationId 
                ?? Guid.NewGuid().ToString();
            
            context.SetCorrelationId(correlationId);

            // Set causation ID to the current message ID (the message that caused this send)
            var causationId = currentContext?.MessageId ?? context.CausationId;
            if (!string.IsNullOrEmpty(causationId))
            {
                context.SetCausationId(causationId);
            }

            LogCorrelationPropagated(correlationId, causationId, context.MessageId);

            await next(context, cancellationToken);
        }

        private void LogCorrelationPropagated(string correlationId, string? causationId, string messageId)
        {
            var message = $"Correlation propagated to send: CorrelationId={correlationId}, CausationId={causationId}, MessageId={messageId}";
            
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-correlation-propagated-send", message);
            }
        }
    }

    /// <summary>
    /// Represents the correlation context for distributed tracing.
    /// </summary>
    public class CorrelationContext
    {
        /// <summary>
        /// Creates a new correlation context.
        /// </summary>
        /// <param name="correlationId">The correlation ID.</param>
        /// <param name="causationId">The causation ID.</param>
        /// <param name="messageId">The message ID.</param>
        public CorrelationContext(string correlationId, string? causationId, string messageId)
        {
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            CausationId = causationId;
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
            StartedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the correlation ID for distributed tracing.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// Gets the causation ID linking to the parent operation.
        /// </summary>
        public string? CausationId { get; }

        /// <summary>
        /// Gets the current message ID.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets when this correlation context was created.
        /// </summary>
        public DateTimeOffset StartedAt { get; }
    }
}

