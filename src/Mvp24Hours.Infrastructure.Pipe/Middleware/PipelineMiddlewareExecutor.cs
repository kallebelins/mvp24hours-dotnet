//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Middleware
{
    /// <summary>
    /// Executes pipeline middlewares in order, building a middleware chain.
    /// </summary>
    public static class PipelineMiddlewareExecutor
    {
        /// <summary>
        /// Executes all middlewares wrapping the core action.
        /// </summary>
        /// <param name="middlewares">Ordered list of middlewares to execute.</param>
        /// <param name="message">The pipeline message.</param>
        /// <param name="coreAction">The core action to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteAsync(
            IEnumerable<IPipelineMiddleware> middlewares,
            IPipelineMessage message,
            Func<Task> coreAction,
            CancellationToken cancellationToken = default)
        {
            if (middlewares == null || !middlewares.Any())
            {
                await coreAction();
                return;
            }

            var orderedMiddlewares = middlewares.OrderBy(m => m.Order).ToList();
            
            Func<Task> next = coreAction;
            
            // Build middleware chain from inside out
            for (int i = orderedMiddlewares.Count - 1; i >= 0; i--)
            {
                var middleware = orderedMiddlewares[i];
                var currentNext = next;
                next = () => ExecuteMiddlewareAsync(middleware, message, currentNext, cancellationToken);
            }

            await next();
        }

        private static async Task ExecuteMiddlewareAsync(
            IPipelineMiddleware middleware,
            IPipelineMessage message,
            Func<Task> next,
            CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-middleware-start", $"middleware:{middleware.GetType().Name}");
            try
            {
                await middleware.ExecuteAsync(message, next, cancellationToken);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-middleware-end", $"middleware:{middleware.GetType().Name}");
            }
        }

        /// <summary>
        /// Executes all sync middlewares wrapping the core action.
        /// </summary>
        /// <param name="middlewares">Ordered list of middlewares to execute.</param>
        /// <param name="message">The pipeline message.</param>
        /// <param name="coreAction">The core action to execute.</param>
        public static void Execute(
            IEnumerable<IPipelineMiddlewareSync> middlewares,
            IPipelineMessage message,
            Action coreAction)
        {
            if (middlewares == null || !middlewares.Any())
            {
                coreAction();
                return;
            }

            var orderedMiddlewares = middlewares.OrderBy(m => m.Order).ToList();
            
            Action next = coreAction;
            
            // Build middleware chain from inside out
            for (int i = orderedMiddlewares.Count - 1; i >= 0; i--)
            {
                var middleware = orderedMiddlewares[i];
                var currentNext = next;
                next = () => ExecuteMiddleware(middleware, message, currentNext);
            }

            next();
        }

        private static void ExecuteMiddleware(
            IPipelineMiddlewareSync middleware,
            IPipelineMessage message,
            Action next)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-middleware-sync-start", $"middleware:{middleware.GetType().Name}");
            try
            {
                middleware.Execute(message, next);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-middleware-sync-end", $"middleware:{middleware.GetType().Name}");
            }
        }
    }
}

