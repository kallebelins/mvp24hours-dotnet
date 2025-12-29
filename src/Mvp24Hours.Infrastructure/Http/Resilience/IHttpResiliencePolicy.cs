//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Interface for HTTP resilience policies that wrap HTTP requests with resilience patterns.
    /// </summary>
    /// <remarks>
    /// This interface provides a unified abstraction for different resilience patterns
    /// (retry, circuit breaker, timeout, bulkhead, fallback) that can be applied to HTTP requests.
    /// </remarks>
    public interface IHttpResiliencePolicy
    {
        /// <summary>
        /// Gets the name of the policy for identification and logging.
        /// </summary>
        string PolicyName { get; }

        /// <summary>
        /// Executes an HTTP request through the resilience policy.
        /// </summary>
        /// <param name="requestFactory">Factory function that creates the HTTP request message.</param>
        /// <param name="sendAsync">Function that sends the HTTP request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the underlying Polly policy for advanced composition scenarios.
        /// </summary>
        /// <returns>The Polly policy instance.</returns>
        IAsyncPolicy<HttpResponseMessage> GetPollyPolicy();
    }
}

