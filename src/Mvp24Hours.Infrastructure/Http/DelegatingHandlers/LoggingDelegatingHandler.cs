//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler for logging HTTP requests and responses with sensitive data masking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler logs HTTP requests and responses with configurable verbosity levels.
    /// It automatically masks sensitive data in headers based on the configured sensitive headers list.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Logs request URI, method, and optional headers/body</item>
    /// <item>Logs response status code and optional headers/body</item>
    /// <item>Masks sensitive headers (Authorization, API Keys, etc.)</item>
    /// <item>Configurable body logging with size limits</item>
    /// <item>Duration tracking for performance monitoring</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler&lt;LoggingDelegatingHandler&gt;();
    /// </code>
    /// </example>
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;
        private readonly HttpLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
            : this(logger, new HttpLoggingOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The logging options.</param>
        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger, HttpLoggingOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new HttpLoggingOptions();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // Log request
                await LogRequestAsync(request, requestId, cancellationToken);

                // Execute request
                var response = await base.SendAsync(request, cancellationToken);

                stopwatch.Stop();

                // Log response
                await LogResponseAsync(response, requestId, stopwatch.Elapsed, cancellationToken);

                return response;
            }
            catch (HttpRequestException ex)
                when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
            {
                stopwatch.Stop();
                var hostWithPort = GetHostWithPort(request.RequestUri!);

                _logger.LogError(
                    ex,
                    "[{RequestId}] Connection refused to {Host} after {ElapsedMs}ms. " +
                    "Please check the configuration to ensure the correct URL has been configured.",
                    requestId, hostWithPort, stopwatch.ElapsedMilliseconds);

                return new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    RequestMessage = request,
                    ReasonPhrase = $"Connection refused to {hostWithPort}"
                };
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    ex,
                    "[{RequestId}] Request to {Method} {Uri} timed out after {ElapsedMs}ms",
                    requestId, request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "[{RequestId}] Request to {Method} {Uri} failed after {ElapsedMs}ms with error: {ErrorMessage}",
                    requestId, request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        private async Task LogRequestAsync(
            HttpRequestMessage request,
            string requestId,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{requestId}] HTTP Request: {request.Method} {request.RequestUri}");

            if (_options.LogRequestHeaders && request.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in request.Headers)
                {
                    var value = MaskHeaderValue(header.Key, string.Join(", ", header.Value));
                    sb.AppendLine($"  {header.Key}: {value}");
                }

                if (request.Content?.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        var value = MaskHeaderValue(header.Key, string.Join(", ", header.Value));
                        sb.AppendLine($"  {header.Key}: {value}");
                    }
                }
            }

            if (_options.LogRequestBody && request.Content != null)
            {
                var body = await ReadContentAsync(request.Content, cancellationToken);
                if (!string.IsNullOrEmpty(body))
                {
                    sb.AppendLine($"Body: {body}");
                }
            }

            _logger.LogDebug("{RequestLog}", sb.ToString().TrimEnd());
        }

        private async Task LogResponseAsync(
            HttpResponseMessage response,
            string requestId,
            TimeSpan elapsed,
            CancellationToken cancellationToken)
        {
            var statusCode = (int)response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error
                : statusCode >= 400 ? LogLevel.Warning
                : LogLevel.Information;

            var sb = new StringBuilder();
            sb.AppendLine($"[{requestId}] HTTP Response: {statusCode} {response.ReasonPhrase} " +
                         $"from {response.RequestMessage?.RequestUri} in {elapsed.TotalMilliseconds:F1}ms");

            if (_options.LogResponseHeaders && response.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in response.Headers)
                {
                    var value = MaskHeaderValue(header.Key, string.Join(", ", header.Value));
                    sb.AppendLine($"  {header.Key}: {value}");
                }

                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        var value = MaskHeaderValue(header.Key, string.Join(", ", header.Value));
                        sb.AppendLine($"  {header.Key}: {value}");
                    }
                }
            }

            if (_options.LogResponseBody && response.Content != null)
            {
                var body = await ReadContentAsync(response.Content, cancellationToken);
                if (!string.IsNullOrEmpty(body))
                {
                    sb.AppendLine($"Body: {body}");
                }
            }

            _logger.Log(logLevel, "{ResponseLog}", sb.ToString().TrimEnd());
        }

        private string MaskHeaderValue(string headerName, string headerValue)
        {
            if (_options.SensitiveHeaders.Any(h =>
                string.Equals(h, headerName, StringComparison.OrdinalIgnoreCase)))
            {
                return _options.MaskValue;
            }

            return headerValue;
        }

        private async Task<string> ReadContentAsync(HttpContent content, CancellationToken cancellationToken)
        {
            try
            {
                // Buffer the content to allow multiple reads
                await content.LoadIntoBufferAsync();
                var body = await content.ReadAsStringAsync(cancellationToken);

                if (body.Length > _options.MaxBodyLogSize)
                {
                    return body[.._options.MaxBodyLogSize] + $"... [truncated, total {body.Length} chars]";
                }

                return body;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read HTTP content for logging");
                return "[Unable to read content]";
            }
        }

        private static string GetHostWithPort(Uri uri)
        {
            return uri.IsDefaultPort
                ? uri.DnsSafeHost
                : $"{uri.DnsSafeHost}:{uri.Port}";
        }
    }
}
