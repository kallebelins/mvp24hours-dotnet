//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Events
{
    /// <summary>
    /// Interface for dispatching CronJob lifecycle events to handlers.
    /// </summary>
    public interface ICronJobEventDispatcher
    {
        /// <summary>
        /// Dispatches the job starting event to all registered handlers.
        /// </summary>
        Task DispatchStartingAsync(ICronJobContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches the job completed event to all registered handlers.
        /// </summary>
        Task DispatchCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches the job failed event to all registered handlers.
        /// </summary>
        Task DispatchFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches the job cancelled event to all registered handlers.
        /// </summary>
        Task DispatchCancelledAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches the job retry event to all registered handlers.
        /// </summary>
        Task DispatchRetryAsync(ICronJobContext context, Exception exception, TimeSpan delay, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches the job skipped event to all registered handlers.
        /// </summary>
        Task DispatchSkippedAsync(string jobName, SkipReason reason, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Default implementation of <see cref="ICronJobEventDispatcher"/>.
    /// Resolves handlers from DI and dispatches events in order.
    /// </summary>
    public sealed class CronJobEventDispatcher : ICronJobEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CronJobEventDispatcher> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="CronJobEventDispatcher"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving handlers.</param>
        /// <param name="logger">The logger.</param>
        public CronJobEventDispatcher(IServiceProvider serviceProvider, ILogger<CronJobEventDispatcher> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task DispatchStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobStartingHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobStartingAsync(context, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob starting handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, context.JobName);
                }
            }
        }

        /// <inheritdoc />
        public async Task DispatchCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobCompletedHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobCompletedAsync(context, duration, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob completed handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, context.JobName);
                }
            }
        }

        /// <inheritdoc />
        public async Task DispatchFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobFailedHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobFailedAsync(context, exception, duration, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob failed handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, context.JobName);
                }
            }
        }

        /// <inheritdoc />
        public async Task DispatchCancelledAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobCancelledHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobCancelledAsync(context, duration, CancellationToken.None); // Use new token since we're cancelled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob cancelled handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, context.JobName);
                }
            }
        }

        /// <inheritdoc />
        public async Task DispatchRetryAsync(ICronJobContext context, Exception exception, TimeSpan delay, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobRetryHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobRetryAsync(context, exception, delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob retry handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, context.JobName);
                }
            }
        }

        /// <inheritdoc />
        public async Task DispatchSkippedAsync(string jobName, SkipReason reason, CancellationToken cancellationToken)
        {
            var handlers = GetOrderedHandlers<ICronJobSkippedHandler>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.OnJobSkippedAsync(jobName, reason, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CronJob skipped handler {HandlerType} for job {JobName}",
                        handler.GetType().Name, jobName);
                }
            }
        }

        private IEnumerable<T> GetOrderedHandlers<T>() where T : ICronJobEventHandler
        {
            return _serviceProvider.GetServices<T>()
                .OrderBy(h => h.Order);
        }
    }
}

