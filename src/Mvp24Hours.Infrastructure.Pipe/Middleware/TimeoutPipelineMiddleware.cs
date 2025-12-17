//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Middleware
{
    /// <summary>
    /// Exception thrown when a pipeline operation times out.
    /// </summary>
    public class PipelineTimeoutException : TimeoutException
    {
        /// <summary>
        /// The configured timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; }

        public PipelineTimeoutException(TimeSpan timeout)
            : base($"Pipeline operation timed out after {timeout.TotalMilliseconds}ms")
        {
            Timeout = timeout;
        }

        public PipelineTimeoutException(TimeSpan timeout, Exception innerException)
            : base($"Pipeline operation timed out after {timeout.TotalMilliseconds}ms", innerException)
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Middleware that enforces timeout on operations.
    /// </summary>
    public class TimeoutPipelineMiddleware : IPipelineMiddleware
    {
        private readonly TimeSpan _defaultTimeout;

        /// <summary>
        /// Creates a new instance with the specified default timeout.
        /// </summary>
        /// <param name="defaultTimeout">Default timeout for operations.</param>
        public TimeoutPipelineMiddleware(TimeSpan defaultTimeout)
        {
            if (defaultTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(defaultTimeout), "Timeout must be positive");
            
            _defaultTimeout = defaultTimeout;
        }

        /// <inheritdoc />
        public int Order => -500; // Run after logging, before most other middlewares

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_defaultTimeout);

            try
            {
                // Create a task that completes when next() completes or timeout occurs
                var nextTask = next();
                var timeoutTask = Task.Delay(Timeout.Infinite, timeoutCts.Token);

                var completedTask = await Task.WhenAny(nextTask, timeoutTask);

                if (completedTask == timeoutTask && timeoutCts.IsCancellationRequested)
                {
                    // Original cancellation was requested
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                    // Timeout occurred
                    throw new PipelineTimeoutException(_defaultTimeout);
                }

                // Await the next task to propagate any exceptions
                await nextTask;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new PipelineTimeoutException(_defaultTimeout);
            }
        }
    }
}

