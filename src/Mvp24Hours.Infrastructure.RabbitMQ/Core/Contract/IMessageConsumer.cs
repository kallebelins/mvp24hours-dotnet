//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Strongly-typed message consumer interface.
    /// Implement this interface to consume messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to consume.</typeparam>
    public interface IMessageConsumer<in TMessage> where TMessage : class
    {
        /// <summary>
        /// Handles the consumption of a message.
        /// </summary>
        /// <param name="context">The consume context containing the message and metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConsumeAsync(IConsumeContext<TMessage> context, CancellationToken cancellationToken = default);
    }
}

