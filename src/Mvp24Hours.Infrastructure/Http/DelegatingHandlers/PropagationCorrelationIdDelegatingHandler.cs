//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that propagates the X-Correlation-Id header from the current HTTP context
    /// to outgoing HTTP requests for distributed tracing.
    /// </summary>
    public class PropagationCorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PropagationCorrelationIdDelegatingHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropagationCorrelationIdDelegatingHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve services from.</param>
        /// <param name="logger">The logger instance.</param>
        public PropagationCorrelationIdDelegatingHandler(
            IServiceProvider serviceProvider,
            ILogger<PropagationCorrelationIdDelegatingHandler> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _logger.LogDebug("Adding correlation-id header to request: {RequestUri}", request.RequestUri);
            try
            {
                request.PropagateHeaderKey(_serviceProvider, "X-Correlation-Id");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to propagate correlation-id header to request: {RequestUri}", request.RequestUri);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
