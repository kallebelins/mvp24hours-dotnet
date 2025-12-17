//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Extension methods for accessing pipeline context from messages.
    /// </summary>
    public static class PipelineMessageContextExtensions
    {
        /// <summary>
        /// Gets the pipeline context from the message, or null if not available.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The pipeline context, or null.</returns>
        public static IPipelineContext? GetPipelineContext(this IPipelineMessage message)
        {
            if (message == null)
            {
                return null;
            }

            return message.GetContent<IPipelineContext>(ContextPropagationMiddleware.PipelineContextKey);
        }

        /// <summary>
        /// Gets the pipeline context from the message, creating a new one if not available.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The pipeline context.</returns>
        public static IPipelineContext GetOrCreatePipelineContext(this IPipelineMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var context = message.GetPipelineContext();
            if (context != null)
            {
                return context;
            }

            context = new PipelineContext(message.Token);
            message.SetPipelineContext(context);
            return context;
        }

        /// <summary>
        /// Sets the pipeline context in the message.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <param name="context">The context to set.</param>
        public static void SetPipelineContext(this IPipelineMessage message, IPipelineContext context)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            message.AddContent(ContextPropagationMiddleware.PipelineContextKey, context);
        }

        /// <summary>
        /// Gets the correlation ID from the message context, or the message token if no context.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The correlation ID.</returns>
        public static string GetCorrelationId(this IPipelineMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.GetPipelineContext()?.CorrelationId ?? message.Token;
        }

        /// <summary>
        /// Gets the user ID from the message context, or null if not available.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The user ID, or null.</returns>
        public static string? GetUserId(this IPipelineMessage message)
        {
            return message?.GetPipelineContext()?.UserId;
        }

        /// <summary>
        /// Gets the tenant ID from the message context, or null if not available.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The tenant ID, or null.</returns>
        public static string? GetTenantId(this IPipelineMessage message)
        {
            return message?.GetPipelineContext()?.TenantId;
        }

        /// <summary>
        /// Captures a state snapshot in the message context.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="state">The state to capture.</param>
        /// <param name="description">Optional description.</param>
        public static void CaptureSnapshot(
            this IPipelineMessage message,
            string operationName,
            object? state,
            string? description = null)
        {
            message?.GetPipelineContext()?.CaptureSnapshot(operationName, state, description);
        }

        /// <summary>
        /// Sets a metadata value in the message context.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="message">The pipeline message.</param>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The value to set.</param>
        public static void SetContextMetadata<T>(
            this IPipelineMessage message,
            string key,
            T value)
        {
            message?.GetPipelineContext()?.SetMetadata(key, value);
        }

        /// <summary>
        /// Gets a metadata value from the message context.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="message">The pipeline message.</param>
        /// <param name="key">The metadata key.</param>
        /// <returns>The value, or default if not found.</returns>
        public static T? GetContextMetadata<T>(this IPipelineMessage message, string key) where T : class
        {
            return message?.GetPipelineContext()?.GetMetadata<T>(key);
        }

        /// <summary>
        /// Gets a struct metadata value from the message context.
        /// </summary>
        /// <typeparam name="T">The struct type of the value.</typeparam>
        /// <param name="message">The pipeline message.</param>
        /// <param name="key">The metadata key.</param>
        /// <returns>The value, or default if not found.</returns>
        public static T GetContextMetadataValue<T>(this IPipelineMessage message, string key) where T : struct
        {
            return message?.GetPipelineContext()?.GetMetadata<T>(key) ?? default;
        }

        /// <summary>
        /// Checks if the message has a pipeline context.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>True if the message has a context; otherwise, false.</returns>
        public static bool HasPipelineContext(this IPipelineMessage message)
        {
            return message?.GetPipelineContext() != null;
        }

        /// <summary>
        /// Creates a child context from the message's current context.
        /// Useful for nested pipeline executions.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>A new child context, or a new context if no parent exists.</returns>
        public static IPipelineContext CreateChildContext(this IPipelineMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var parentContext = message.GetPipelineContext();
            if (parentContext != null)
            {
                return parentContext.CreateChildContext();
            }

            return new PipelineContext(message.Token);
        }
    }
}

