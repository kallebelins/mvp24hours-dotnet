//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract
{
    /// <summary>
    /// Filter interface for message publishing pipeline.
    /// Filters are executed in order during message publishing.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being published.</typeparam>
    public interface IPublishFilter<TMessage> where TMessage : class
    {
        /// <summary>
        /// Executes the filter logic during message publishing.
        /// </summary>
        /// <param name="context">The publish filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Non-generic publish filter that applies to all message types.
    /// </summary>
    public interface IPublishFilter
    {
        /// <summary>
        /// Executes the filter logic during message publishing.
        /// </summary>
        /// <typeparam name="TMessage">The type of message being published.</typeparam>
        /// <param name="context">The publish filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class;
    }

    /// <summary>
    /// Delegate representing the next filter in the publish pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being published.</typeparam>
    /// <param name="context">The publish filter context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task PublishFilterDelegate<TMessage>(
        IPublishFilterContext<TMessage> context,
        CancellationToken cancellationToken = default) where TMessage : class;
}

