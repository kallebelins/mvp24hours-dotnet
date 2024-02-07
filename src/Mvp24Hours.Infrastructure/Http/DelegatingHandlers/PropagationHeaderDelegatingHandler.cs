//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class PropagationHeaderDelegatingHandler(IServiceProvider serviceProvider, params string[] keys) : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly string[] _keys = keys ?? throw new ArgumentNullException(nameof(keys));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "propagation-header-delegating-handler-start", $"Start to request '{request.RequestUri}'");
            try
            {
                foreach (string key in _keys.Where(x => x.HasValue()).ToList())
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "propagation-header-delegating-handler", $"Add header key '{key}' to '{request.RequestUri}'");
                    request.PropagateHeaderKey(_serviceProvider, key);
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "propagation-header-delegating-handler-failure", ex);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
