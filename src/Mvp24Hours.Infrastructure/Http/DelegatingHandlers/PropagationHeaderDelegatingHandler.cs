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
    /// Delegating handler that propagates multiple headers from the current HTTP context
    /// to outgoing HTTP requests based on the provided header keys.
    /// </summary>
    public class PropagationHeaderDelegatingHandler : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string[] _keys;
        private readonly ILogger<PropagationHeaderDelegatingHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropagationHeaderDelegatingHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve services from.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="keys">The header keys to propagate.</param>
        public PropagationHeaderDelegatingHandler(
            IServiceProvider serviceProvider,
            ILogger<PropagationHeaderDelegatingHandler> logger,
            params string[] keys)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _logger.LogDebug("Starting header propagation for request: {RequestUri}", request.RequestUri);
            try
            {
                foreach (string key in _keys.Where(x => x.HasValue()).ToList())
                {
                    _logger.LogDebug("Adding header key '{HeaderKey}' to request: {RequestUri}", key, request.RequestUri);
                    request.PropagateHeaderKey(_serviceProvider, key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to propagate headers to request: {RequestUri}", request.RequestUri);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
