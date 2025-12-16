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
    /// Handler interface for processing requests and generating responses.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message.</typeparam>
    /// <typeparam name="TResponse">The type of the response message.</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        /// <summary>
        /// Handles a request and returns a response.
        /// </summary>
        /// <param name="context">The consume context containing the request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response message.</returns>
        Task<TResponse> HandleAsync(IConsumeContext<TRequest> context, CancellationToken cancellationToken = default);
    }
}

