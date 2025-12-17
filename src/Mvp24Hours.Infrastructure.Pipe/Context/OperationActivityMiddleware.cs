//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Middleware that creates Activity/spans for each pipeline operation.
    /// This enables detailed tracing in OpenTelemetry-compatible systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Tracing Integration:</strong>
    /// Each operation gets its own Activity/span with:
    /// <list type="bullet">
    /// <item>Operation name and type</item>
    /// <item>Duration tracking</item>
    /// <item>Correlation and causation IDs</item>
    /// <item>Error information on failures</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class OperationActivityMiddleware : IPipelineMiddleware
    {
        private readonly IPipelineContextAccessor _contextAccessor;
        private readonly ILogger<OperationActivityMiddleware>? _logger;
        private readonly OperationActivityOptions _options;

        /// <summary>
        /// Gets the execution order. This middleware runs after context propagation.
        /// </summary>
        public int Order => _options.Order;

        /// <summary>
        /// Creates a new instance of the OperationActivityMiddleware.
        /// </summary>
        /// <param name="contextAccessor">The context accessor.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="options">Optional configuration options.</param>
        public OperationActivityMiddleware(
            IPipelineContextAccessor contextAccessor,
            ILogger<OperationActivityMiddleware>? logger = null,
            OperationActivityOptions? options = null)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger;
            _options = options ?? new OperationActivityOptions();
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken)
        {
            var context = _contextAccessor.Context;
            
            if (context == null || !_options.EnableOperationTracing)
            {
                await next();
                return;
            }

            var operationName = GetCurrentOperationName(message);
            Activity? activity = null;

            try
            {
                activity = context.StartActivity(
                    operationName,
                    _options.OperationActivityKind);

                if (activity != null)
                {
                    activity.SetTag("operation.type", "pipeline");
                    activity.SetTag("message.token", message.Token);
                    activity.SetTag("message.is_faulty", message.IsFaulty);
                    activity.SetTag("message.is_locked", message.IsLocked);

                    if (_options.IncludeContentCount)
                    {
                        activity.SetTag("message.content_count", message.GetContentAll()?.Count ?? 0);
                    }
                }

                await next();

                if (activity != null)
                {
                    activity.SetTag("message.is_faulty_after", message.IsFaulty);
                    activity.SetStatus(message.IsFaulty ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                activity?.SetTag("exception.message", ex.Message);
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        /// <summary>
        /// Gets the current operation name from the message or context.
        /// </summary>
        private string GetCurrentOperationName(IPipelineMessage message)
        {
            // Try to get operation name from context metadata
            var context = _contextAccessor.Context;
            if (context != null)
            {
                var operationName = context.GetMetadata<string>("__CurrentOperationName");
                if (!string.IsNullOrEmpty(operationName))
                {
                    return operationName;
                }
            }

            // Fall back to a generic name with message token
            return $"Operation.{message.Token[..Math.Min(8, message.Token.Length)]}";
        }
    }

    /// <summary>
    /// Configuration options for <see cref="OperationActivityMiddleware"/>.
    /// </summary>
    public sealed class OperationActivityOptions
    {
        /// <summary>
        /// Gets or sets the middleware execution order.
        /// Default is -900 (after context propagation but before most other middlewares).
        /// </summary>
        public int Order { get; set; } = -900;

        /// <summary>
        /// Gets or sets whether to enable operation-level tracing.
        /// Default is true.
        /// </summary>
        public bool EnableOperationTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets the ActivityKind for operation activities.
        /// Default is Internal.
        /// </summary>
        public ActivityKind OperationActivityKind { get; set; } = ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether to include content count in activity tags.
        /// Default is true.
        /// </summary>
        public bool IncludeContentCount { get; set; } = true;
    }
}

