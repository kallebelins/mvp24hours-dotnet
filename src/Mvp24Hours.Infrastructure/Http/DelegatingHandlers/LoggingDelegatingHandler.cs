//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// A type for HTTP handlers that delegate the processing of HTTP response messages to another handler, called the inner handler.
    /// </summary>
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "logging-delegating-handler-sendasync-start", $"Sending request to {request.RequestUri}");

                var response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "logging-delegating-handler-sendasync-receive", $"Received a success response from {response.RequestMessage.RequestUri}");
                }
                else
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "logging-delegating-handler-sendasync-receive", $"Received a non-success status code {(int)response.StatusCode} from {response.RequestMessage.RequestUri}");
                }

                return response;
            }
            catch (HttpRequestException ex)
                when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
            {
                var hostWithPort = request.RequestUri.IsDefaultPort
                    ? request.RequestUri.DnsSafeHost
                    : $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";

                TelemetryHelper.Execute(TelemetryLevels.Error, "logging-delegating-handler-sendasync-failure", $"Unable to connect to {hostWithPort}. Please check the configuration to ensure the correct URL for the service has been configured.");
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "logging-delegating-handler-sendasync-end");
            }

            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                RequestMessage = request
            };
        }
    }
}
