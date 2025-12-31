//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Events
{
    /// <summary>
    /// Base interface for CronJob event handlers.
    /// Implement this interface to handle job lifecycle events.
    /// </summary>
    public interface ICronJobEventHandler
    {
        /// <summary>
        /// Gets the order in which this handler should be executed.
        /// Lower values execute first. Default is 0.
        /// </summary>
        int Order => 0;
    }

    /// <summary>
    /// Handler for job starting events.
    /// Called before the DoWork method is executed.
    /// </summary>
    public interface ICronJobStartingHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job is about to start execution.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler for job completed events.
    /// Called after the DoWork method completes successfully.
    /// </summary>
    public interface ICronJobCompletedHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job completes successfully.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        /// <param name="duration">The duration of the job execution.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler for job failed events.
    /// Called when the DoWork method throws an exception.
    /// </summary>
    public interface ICronJobFailedHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job fails with an exception.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">The duration of the job execution until failure.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler for job cancelled events.
    /// Called when the job is cancelled via the cancellation token.
    /// </summary>
    public interface ICronJobCancelledHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job is cancelled.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        /// <param name="duration">The duration of the job execution until cancellation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobCancelledAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler for job retry events.
    /// Called when a job is about to be retried after a failure.
    /// </summary>
    public interface ICronJobRetryHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job is about to be retried.
        /// </summary>
        /// <param name="context">The job execution context (with updated attempt number).</param>
        /// <param name="exception">The exception that caused the retry.</param>
        /// <param name="delay">The delay before the retry.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobRetryAsync(ICronJobContext context, Exception exception, TimeSpan delay, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler for job skipped events.
    /// Called when a job execution is skipped (e.g., due to overlapping or circuit breaker).
    /// </summary>
    public interface ICronJobSkippedHandler : ICronJobEventHandler
    {
        /// <summary>
        /// Called when a job execution is skipped.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="reason">The reason for skipping.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task OnJobSkippedAsync(string jobName, SkipReason reason, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the reason for skipping a job execution.
    /// </summary>
    public enum SkipReason
    {
        /// <summary>
        /// Job was skipped because another execution is still running.
        /// </summary>
        Overlapping,

        /// <summary>
        /// Job was skipped because the circuit breaker is open.
        /// </summary>
        CircuitBreakerOpen,

        /// <summary>
        /// Job was skipped because it is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Job was skipped because a dependency job has not completed.
        /// </summary>
        DependencyNotMet
    }

    /// <summary>
    /// Composite handler that implements all job lifecycle events.
    /// Extend this class for convenience when handling multiple event types.
    /// </summary>
    public abstract class CronJobEventHandlerBase :
        ICronJobStartingHandler,
        ICronJobCompletedHandler,
        ICronJobFailedHandler,
        ICronJobCancelledHandler,
        ICronJobRetryHandler,
        ICronJobSkippedHandler
    {
        /// <inheritdoc />
        public virtual int Order => 0;

        /// <inheritdoc />
        public virtual Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public virtual Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public virtual Task OnJobFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public virtual Task OnJobCancelledAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public virtual Task OnJobRetryAsync(ICronJobContext context, Exception exception, TimeSpan delay, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public virtual Task OnJobSkippedAsync(string jobName, SkipReason reason, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}

