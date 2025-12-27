//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that propagates custom headers from the current context to HTTP requests.
    /// </summary>
    public class PropagationHeaderDelegatingHandler(IServiceProvider serviceProvider, ILogger<PropagationHeaderDelegatingHandler>? logger = null, params string[] keys) : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly string[] _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        private readonly ILogger<PropagationHeaderDelegatingHandler>? _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Starting header propagation for request {RequestUri}", request.RequestUri);
            try
            {
                foreach (string key in _keys.Where(x => x.HasValue()).ToList())
                {
                    _logger?.LogDebug("Adding header key '{HeaderKey}' to request {RequestUri}", key, request.RequestUri);
                    request.PropagateHeaderKey(_serviceProvider, key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to propagate headers to request {RequestUri}", request.RequestUri);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

