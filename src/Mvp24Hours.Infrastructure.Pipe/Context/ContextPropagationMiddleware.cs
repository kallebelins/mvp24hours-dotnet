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
    /// Middleware that propagates pipeline context across operations.
    /// This middleware ensures that context information (correlation ID, user, tenant, etc.)
    /// is available throughout the pipeline execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Execution Order:</strong>
    /// This middleware should run early in the pipeline (low Order value) to ensure
    /// context is available for all subsequent operations.
    /// </para>
    /// <para>
    /// <strong>Activity Integration:</strong>
    /// When enabled, creates a root Activity/span for the pipeline execution
    /// that can be used for distributed tracing with OpenTelemetry.
    /// </para>
    /// </remarks>
    public sealed class ContextPropagationMiddleware : IPipelineMiddleware
    {
        private readonly IPipelineContextAccessor _contextAccessor;
        private readonly ILogger<ContextPropagationMiddleware>? _logger;
        private readonly ContextPropagationOptions _options;

        /// <summary>
        /// Gets the execution order. This middleware runs early to ensure context is available.
        /// </summary>
        public int Order => _options.Order;

        /// <summary>
        /// Creates a new instance of the ContextPropagationMiddleware.
        /// </summary>
        /// <param name="contextAccessor">The context accessor for storing/retrieving context.</param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <param name="options">Optional configuration options.</param>
        public ContextPropagationMiddleware(
            IPipelineContextAccessor contextAccessor,
            ILogger<ContextPropagationMiddleware>? logger = null,
            ContextPropagationOptions? options = null)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger;
            _options = options ?? new ContextPropagationOptions();
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken)
        {
            // Try to get existing context from message or create new one
            var context = GetOrCreateContext(message);
            
            Activity? activity = null;

            try
            {
                // Set context in accessor for operations to access
                using var scope = _contextAccessor.BeginScope(context);

                // Start activity for tracing if enabled
                if (_options.EnableActivityTracing)
                {
                    activity = context.StartActivity(
                        _options.RootActivityName ?? "Pipeline.Execute",
                        ActivityKind.Internal);

                    if (activity != null)
                    {
                        activity.SetTag("pipeline.token", message.Token);
                        activity.SetTag("pipeline.context_type", context.GetType().Name);
                    }
                }

                _logger?.LogDebug(
                    "Pipeline context established. CorrelationId: {CorrelationId}, Token: {Token}",
                    context.CorrelationId,
                    message.Token);

                // Capture initial state snapshot if enabled
                if (_options.CaptureInitialSnapshot)
                {
                    context.CaptureSnapshot(
                        "Pipeline.Start",
                        new
                        {
                            Token = message.Token,
                            IsFaulty = message.IsFaulty,
                            IsLocked = message.IsLocked,
                            ContentCount = message.GetContentAll()?.Count ?? 0
                        },
                        "Initial pipeline state");
                }

                // Execute the rest of the pipeline
                await next();

                // Capture final state snapshot if enabled
                if (_options.CaptureFinalSnapshot)
                {
                    context.CaptureSnapshot(
                        "Pipeline.End",
                        new
                        {
                            Token = message.Token,
                            IsFaulty = message.IsFaulty,
                            IsLocked = message.IsLocked,
                            ContentCount = message.GetContentAll()?.Count ?? 0,
                            MessageCount = message.Messages?.Count ?? 0
                        },
                        "Final pipeline state");
                }

                // Store context back in message for downstream access
                if (_options.StoreContextInMessage)
                {
                    message.AddContent(PipelineContextKey, context);
                }

                _logger?.LogDebug(
                    "Pipeline execution completed. CorrelationId: {CorrelationId}, IsFaulty: {IsFaulty}",
                    context.CorrelationId,
                    message.IsFaulty);

                activity?.SetStatus(message.IsFaulty ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Pipeline execution failed. CorrelationId: {CorrelationId}, Error: {Error}",
                    context.CorrelationId,
                    ex.Message);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                activity?.SetTag("exception.message", ex.Message);
                activity?.SetTag("exception.stacktrace", ex.StackTrace);

                // Capture error snapshot if enabled
                if (_options.CaptureErrorSnapshot)
                {
                    context.CaptureSnapshot(
                        "Pipeline.Error",
                        new
                        {
                            ExceptionType = ex.GetType().Name,
                            ExceptionMessage = ex.Message,
                            Token = message.Token,
                            IsFaulty = message.IsFaulty
                        },
                        $"Error: {ex.Message}");
                }

                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        /// <summary>
        /// Gets an existing context from the message or creates a new one.
        /// </summary>
        private IPipelineContext GetOrCreateContext(IPipelineMessage message)
        {
            // First, try to get context from message
            var existingContext = message.GetContent<IPipelineContext>(PipelineContextKey);
            if (existingContext != null)
            {
                return existingContext;
            }

            // Second, check if there's a context already in the accessor (nested pipeline)
            if (_contextAccessor.HasContext && _contextAccessor.Context != null)
            {
                // Create a child context for nested execution
                return _contextAccessor.Context.CreateChildContext();
            }

            // Third, create a new context using the message token as correlation ID
            var newContext = new PipelineContext(message.Token);

            // Apply any default values from options
            if (!string.IsNullOrEmpty(_options.DefaultTenantId))
            {
                newContext.TenantId = _options.DefaultTenantId;
            }

            return newContext;
        }

        /// <summary>
        /// The key used to store the pipeline context in the message.
        /// </summary>
        public const string PipelineContextKey = "__PipelineContext";
    }

    /// <summary>
    /// Configuration options for <see cref="ContextPropagationMiddleware"/>.
    /// </summary>
    public sealed class ContextPropagationOptions
    {
        /// <summary>
        /// Gets or sets the middleware execution order. Lower values execute first.
        /// Default is -1000 to ensure context is available early.
        /// </summary>
        public int Order { get; set; } = -1000;

        /// <summary>
        /// Gets or sets whether to enable Activity tracing for OpenTelemetry integration.
        /// Default is true.
        /// </summary>
        public bool EnableActivityTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the root activity. Default is "Pipeline.Execute".
        /// </summary>
        public string? RootActivityName { get; set; }

        /// <summary>
        /// Gets or sets whether to store the context in the message after execution.
        /// Default is true.
        /// </summary>
        public bool StoreContextInMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture an initial state snapshot.
        /// Default is false.
        /// </summary>
        public bool CaptureInitialSnapshot { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to capture a final state snapshot.
        /// Default is false.
        /// </summary>
        public bool CaptureFinalSnapshot { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to capture an error snapshot on exception.
        /// Default is true.
        /// </summary>
        public bool CaptureErrorSnapshot { get; set; } = true;

        /// <summary>
        /// Gets or sets a default tenant ID to apply when creating new contexts.
        /// Default is null.
        /// </summary>
        public string? DefaultTenantId { get; set; }
    }
}

