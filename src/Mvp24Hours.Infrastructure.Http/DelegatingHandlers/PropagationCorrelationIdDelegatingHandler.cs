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
    /// Delegating handler that propagates correlation ID from the current context to HTTP requests.
    /// </summary>
    public class PropagationCorrelationIdDelegatingHandler(IServiceProvider serviceProvider, ILogger<PropagationCorrelationIdDelegatingHandler>? logger = null) : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<PropagationCorrelationIdDelegatingHandler>? _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Adding correlation-id header to request {RequestUri}", request.RequestUri);
            try
            {
                request.PropagateHeaderKey(_serviceProvider, "X-Correlation-Id");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add correlation-id header to request {RequestUri}", request.RequestUri);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}