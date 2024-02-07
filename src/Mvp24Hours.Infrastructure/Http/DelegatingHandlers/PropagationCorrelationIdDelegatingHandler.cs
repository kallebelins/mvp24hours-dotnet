//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class PropagationCorrelationIdDelegatingHandler(IServiceProvider serviceProvider) : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "correlation-id-delegating-handler", $"Add correlation-id to {request.RequestUri}");
            try
            {
                request.PropagateHeaderKey(_serviceProvider, "X-Correlation-Id");
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "correlation-id-delegating-handler-failure", ex);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
