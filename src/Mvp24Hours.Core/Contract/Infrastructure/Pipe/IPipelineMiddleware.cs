//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines a middleware that wraps operation execution with pre/post processing.
    /// Follows the middleware pattern for unified cross-cutting concerns.
    /// </summary>
    /// <example>
    /// <code>
    /// public class LoggingMiddleware : IPipelineMiddleware
    /// {
    ///     public async Task ExecuteAsync(IPipelineMessage message, Func&lt;Task&gt; next, CancellationToken cancellationToken)
    ///     {
    ///         Console.WriteLine("Before operation");
    ///         await next();
    ///         Console.WriteLine("After operation");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IPipelineMiddleware
    {
        /// <summary>
        /// Gets the order in which this middleware should be executed.
        /// Lower values execute first (outer middleware), higher values execute later (inner middleware).
        /// Default is 0.
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Executes the middleware logic, wrapping the next delegate in the pipeline.
        /// </summary>
        /// <param name="message">The pipeline message being processed.</param>
        /// <param name="next">The next delegate in the pipeline to invoke.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Synchronous version of pipeline middleware.
    /// </summary>
    public interface IPipelineMiddlewareSync
    {
        /// <summary>
        /// Gets the order in which this middleware should be executed.
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Executes the middleware logic, wrapping the next delegate in the pipeline.
        /// </summary>
        /// <param name="message">The pipeline message being processed.</param>
        /// <param name="next">The next delegate in the pipeline to invoke.</param>
        void Execute(IPipelineMessage message, Action next);
    }

    /// <summary>
    /// Generic middleware that can access operation metadata.
    /// </summary>
    /// <typeparam name="TOperation">Type of operation being executed.</typeparam>
    public interface IPipelineMiddleware<TOperation> where TOperation : class
    {
        /// <summary>
        /// Gets the order in which this middleware should be executed.
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Executes the middleware logic with access to the operation type.
        /// </summary>
        /// <param name="message">The pipeline message being processed.</param>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="next">The next delegate in the pipeline to invoke.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(IPipelineMessage message, TOperation operation, Func<Task> next, CancellationToken cancellationToken = default);
    }
}

