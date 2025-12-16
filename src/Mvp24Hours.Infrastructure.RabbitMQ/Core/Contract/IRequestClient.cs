//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Client interface for request/response pattern.
    /// Sends a request and waits for a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message.</typeparam>
    /// <typeparam name="TResponse">The type of the response message.</typeparam>
    public interface IRequestClient<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        /// <summary>
        /// Sends a request and waits for a response.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response wrapped in Response&lt;T&gt;.</returns>
        Task<Response<TResponse>> GetResponseAsync(TRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request and waits for a response with a specific timeout.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="timeout">The timeout for waiting for response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response wrapped in Response&lt;T&gt;.</returns>
        Task<Response<TResponse>> GetResponseAsync(TRequest request, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default timeout for requests.
        /// </summary>
        TimeSpan Timeout { get; }
    }
}

