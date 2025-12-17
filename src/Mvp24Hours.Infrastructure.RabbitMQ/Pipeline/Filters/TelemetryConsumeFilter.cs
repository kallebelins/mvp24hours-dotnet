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
    /// Consume filter that provides OpenTelemetry integration with distributed tracing.
    /// Creates spans for message consumption with rich metadata.
    /// </summary>
    public class TelemetryConsumeFilter : IConsumeFilter
    {
        private readonly ILogger<TelemetryConsumeFilter>? _logger;
        
        /// <summary>
        /// The ActivitySource for creating spans.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new("Mvp24Hours.RabbitMQ.Consumer", "1.0.0");

        /// <summary>
        /// Creates a new telemetry consume filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public TelemetryConsumeFilter(ILogger<TelemetryConsumeFilter>? logger = null)
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
            var activityName = $"RabbitMQ Consume {messageType}";

            // Try to extract parent context from headers
            var parentContext = ExtractParentContext(context);

            using var activity = ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer,
                parentContext);

            if (activity != null)
            {
                // Set standard messaging attributes
                activity.SetTag("messaging.system", "rabbitmq");
                activity.SetTag("messaging.operation", "receive");
                activity.SetTag("messaging.destination", context.Exchange);
                activity.SetTag("messaging.destination_kind", "topic");
                activity.SetTag("messaging.rabbitmq.routing_key", context.RoutingKey);
                activity.SetTag("messaging.message_id", context.MessageId);
                activity.SetTag("messaging.message_type", messageType);
                
                // Set correlation IDs
                if (!string.IsNullOrEmpty(context.CorrelationId))
                {
                    activity.SetTag("messaging.correlation_id", context.CorrelationId);
                }
                if (!string.IsNullOrEmpty(context.CausationId))
                {
                    activity.SetTag("messaging.causation_id", context.CausationId);
                }

                // Set queue info
                activity.SetTag("messaging.consumer.queue", context.QueueName);
                activity.SetTag("messaging.consumer.tag", context.ConsumerTag);
                activity.SetTag("messaging.redelivered", context.Redelivered);
                activity.SetTag("messaging.redelivery_count", context.RedeliveryCount);

                // Store activity in Items for downstream access
                context.Items["Activity"] = activity;
                context.Items["TraceId"] = activity.TraceId.ToString();
                context.Items["SpanId"] = activity.SpanId.ToString();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-telemetry-started", 
                    $"TraceId={activity.TraceId}, SpanId={activity.SpanId}");
            }

            try
            {
                await next(context, cancellationToken);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().FullName);
                activity?.SetTag("error.message", ex.Message);
                activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                }));
                
                throw;
            }
        }

        private static ActivityContext ExtractParentContext<TMessage>(IConsumeFilterContext<TMessage> context) 
            where TMessage : class
        {
            // Try to extract traceparent from headers (W3C Trace Context)
            var traceparent = context.GetHeader<string>("traceparent");
            if (!string.IsNullOrEmpty(traceparent))
            {
                if (ActivityContext.TryParse(traceparent, null, out var activityContext))
                {
                    return activityContext;
                }
            }

            return default;
        }
    }

    /// <summary>
    /// Publish filter that provides OpenTelemetry integration with distributed tracing.
    /// Creates spans for message publishing with rich metadata.
    /// </summary>
    public class TelemetryPublishFilter : IPublishFilter
    {
        private readonly ILogger<TelemetryPublishFilter>? _logger;
        
        /// <summary>
        /// The ActivitySource for creating spans.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new("Mvp24Hours.RabbitMQ.Publisher", "1.0.0");

        /// <summary>
        /// Creates a new telemetry publish filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public TelemetryPublishFilter(ILogger<TelemetryPublishFilter>? logger = null)
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
            var activityName = $"RabbitMQ Publish {messageType}";

            using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer);

            if (activity != null)
            {
                // Set standard messaging attributes
                activity.SetTag("messaging.system", "rabbitmq");
                activity.SetTag("messaging.operation", "send");
                activity.SetTag("messaging.destination", context.Exchange);
                activity.SetTag("messaging.destination_kind", "topic");
                activity.SetTag("messaging.rabbitmq.routing_key", context.RoutingKey);
                activity.SetTag("messaging.message_id", context.MessageId);
                activity.SetTag("messaging.message_type", messageType);
                
                // Set correlation IDs
                if (!string.IsNullOrEmpty(context.CorrelationId))
                {
                    activity.SetTag("messaging.correlation_id", context.CorrelationId);
                }
                if (!string.IsNullOrEmpty(context.CausationId))
                {
                    activity.SetTag("messaging.causation_id", context.CausationId);
                }

                // Inject trace context into headers (W3C Trace Context)
                context.Headers["traceparent"] = activity.Id ?? $"00-{activity.TraceId}-{activity.SpanId}-00";
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    context.Headers["tracestate"] = activity.TraceStateString;
                }

                // Store activity in Items for downstream access
                context.Items["Activity"] = activity;
                context.Items["TraceId"] = activity.TraceId.ToString();
                context.Items["SpanId"] = activity.SpanId.ToString();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-telemetry-publish-started", 
                    $"TraceId={activity.TraceId}, SpanId={activity.SpanId}");
            }

            try
            {
                await next(context, cancellationToken);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().FullName);
                activity?.SetTag("error.message", ex.Message);
                
                throw;
            }
        }
    }

    /// <summary>
    /// Send filter that provides OpenTelemetry integration with distributed tracing.
    /// Creates spans for message sending with rich metadata.
    /// </summary>
    public class TelemetrySendFilter : ISendFilter
    {
        private readonly ILogger<TelemetrySendFilter>? _logger;
        
        /// <summary>
        /// The ActivitySource for creating spans.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new("Mvp24Hours.RabbitMQ.Sender", "1.0.0");

        /// <summary>
        /// Creates a new telemetry send filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public TelemetrySendFilter(ILogger<TelemetrySendFilter>? logger = null)
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
            var activityName = $"RabbitMQ Send {messageType}";

            using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer);

            if (activity != null)
            {
                // Set standard messaging attributes
                activity.SetTag("messaging.system", "rabbitmq");
                activity.SetTag("messaging.operation", "send");
                activity.SetTag("messaging.destination", context.DestinationQueue);
                activity.SetTag("messaging.destination_kind", "queue");
                activity.SetTag("messaging.message_id", context.MessageId);
                activity.SetTag("messaging.message_type", messageType);
                
                // Set correlation IDs
                if (!string.IsNullOrEmpty(context.CorrelationId))
                {
                    activity.SetTag("messaging.correlation_id", context.CorrelationId);
                }
                if (!string.IsNullOrEmpty(context.CausationId))
                {
                    activity.SetTag("messaging.causation_id", context.CausationId);
                }

                // Inject trace context into headers (W3C Trace Context)
                context.Headers["traceparent"] = activity.Id ?? $"00-{activity.TraceId}-{activity.SpanId}-00";
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    context.Headers["tracestate"] = activity.TraceStateString;
                }

                // Store activity in Items for downstream access
                context.Items["Activity"] = activity;
                context.Items["TraceId"] = activity.TraceId.ToString();
                context.Items["SpanId"] = activity.SpanId.ToString();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-filter-telemetry-send-started", 
                    $"TraceId={activity.TraceId}, SpanId={activity.SpanId}");
            }

            try
            {
                await next(context, cancellationToken);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().FullName);
                activity?.SetTag("error.message", ex.Message);
                
                throw;
            }
        }
    }
}

