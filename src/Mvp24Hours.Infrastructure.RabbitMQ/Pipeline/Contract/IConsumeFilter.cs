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
    /// Filter interface for message consumption pipeline.
    /// Filters are executed in order during message consumption.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being consumed.</typeparam>
    public interface IConsumeFilter<TMessage> where TMessage : class
    {
        /// <summary>
        /// Executes the filter logic during message consumption.
        /// </summary>
        /// <param name="context">The consume filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConsumeAsync(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Non-generic consume filter that applies to all message types.
    /// </summary>
    public interface IConsumeFilter
    {
        /// <summary>
        /// Executes the filter logic during message consumption.
        /// </summary>
        /// <typeparam name="TMessage">The type of message being consumed.</typeparam>
        /// <param name="context">The consume filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class;
    }

    /// <summary>
    /// Delegate representing the next filter in the consume pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being consumed.</typeparam>
    /// <param name="context">The consume filter context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task ConsumeFilterDelegate<TMessage>(
        IConsumeFilterContext<TMessage> context,
        CancellationToken cancellationToken = default) where TMessage : class;
}

