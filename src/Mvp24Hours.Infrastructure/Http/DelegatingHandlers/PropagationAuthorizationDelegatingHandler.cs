//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers;

/// <summary>
/// Delegating handler that propagates the Authorization header from the current HTTP context
/// to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropagationAuthorizationDelegatingHandler"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider to resolve services from.</param>
/// <param name="logger">The logger instance.</param>
public class PropagationAuthorizationDelegatingHandler(
    IServiceProvider serviceProvider,
    ILogger<PropagationAuthorizationDelegatingHandler> logger) : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<PropagationAuthorizationDelegatingHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogDebug("Adding authorization header to request: {RequestUri}", request.RequestUri);
        try
        {
            request.PropagateHeaderKey(_serviceProvider, "Authorization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate authorization header to request: {RequestUri}", request.RequestUri);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
