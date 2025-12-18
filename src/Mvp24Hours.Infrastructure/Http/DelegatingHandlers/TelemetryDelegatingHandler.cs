//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler for OpenTelemetry tracing integration.
    /// Creates spans for outgoing HTTP requests with rich metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler creates OpenTelemetry spans for each HTTP request, providing
    /// distributed tracing capabilities. It captures request/response metadata
    /// and propagates trace context to downstream services.
    /// </para>
    /// <para>
    /// <strong>Captured Metadata:</strong>
    /// <list type="bullet">
    /// <item>HTTP method and URL</item>
    /// <item>HTTP status code</item>
    /// <item>Request/response size (when available)</item>
    /// <item>Duration in milliseconds</item>
    /// <item>Exception details on failure</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler&lt;TelemetryDelegatingHandler&gt;();
    /// </code>
    /// </example>
    public class TelemetryDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<TelemetryDelegatingHandler>? _logger;
        private readonly TelemetryHandlerOptions _options;

        // Activity source for OpenTelemetry integration
        private static readonly ActivitySource ActivitySource = new("Mvp24Hours.Infrastructure.Http", "1.0.0");

        // OpenTelemetry semantic convention attribute names
        private const string HttpRequestMethodTag = "http.request.method";
        private const string HttpResponseStatusCodeTag = "http.response.status_code";
        private const string UrlFullTag = "url.full";
        private const string UrlSchemeTag = "url.scheme";
        private const string ServerAddressTag = "server.address";
        private const string ServerPortTag = "server.port";
        private const string HttpRequestBodySizeTag = "http.request.body.size";
        private const string HttpResponseBodySizeTag = "http.response.body.size";
        private const string NetworkProtocolVersionTag = "network.protocol.version";
        private const string ErrorTypeTag = "error.type";
        private const string UserAgentOriginalTag = "user_agent.original";

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDelegatingHandler"/> class.
        /// </summary>
        public TelemetryDelegatingHandler()
            : this(null, new TelemetryHandlerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TelemetryDelegatingHandler(ILogger<TelemetryDelegatingHandler>? logger)
            : this(logger, new TelemetryHandlerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The telemetry handler options.</param>
        public TelemetryDelegatingHandler(
            ILogger<TelemetryDelegatingHandler>? logger,
            TelemetryHandlerOptions options)
        {
            _logger = logger;
            _options = options ?? new TelemetryHandlerOptions();
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

            // Start an activity (span) for this HTTP request
            using var activity = ActivitySource.StartActivity(
                $"HTTP {request.Method}",
                ActivityKind.Client);

            if (activity is null)
            {
                // If there's no listener, just execute the request
                return await base.SendAsync(request, cancellationToken);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Set request attributes
                SetRequestAttributes(activity, request);

                // Execute the request
                var response = await base.SendAsync(request, cancellationToken);

                stopwatch.Stop();

                // Set response attributes
                SetResponseAttributes(activity, response, stopwatch.Elapsed);

                // Set status based on HTTP status code
                if (!response.IsSuccessStatusCode)
                {
                    activity.SetStatus(ActivityStatusCode.Error,
                        $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Record exception
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordExceptionOnActivity(activity, ex);
                activity.SetTag(ErrorTypeTag, ex.GetType().FullName);
                activity.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

                _logger?.LogError(ex,
                    "HTTP request to {Method} {Uri} failed after {ElapsedMs}ms",
                    request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private void SetRequestAttributes(Activity activity, HttpRequestMessage request)
        {
            // Required attributes (OpenTelemetry semantic conventions)
            activity.SetTag(HttpRequestMethodTag, request.Method.Method);
            
            if (request.RequestUri != null)
            {
                activity.SetTag(UrlFullTag, _options.RecordFullUrl
                    ? request.RequestUri.ToString()
                    : request.RequestUri.GetLeftPart(UriPartial.Path));
                
                activity.SetTag(UrlSchemeTag, request.RequestUri.Scheme);
                activity.SetTag(ServerAddressTag, request.RequestUri.Host);

                if (!request.RequestUri.IsDefaultPort)
                {
                    activity.SetTag(ServerPortTag, request.RequestUri.Port);
                }
            }

            // Request content length
            if (request.Content?.Headers.ContentLength.HasValue == true)
            {
                activity.SetTag(HttpRequestBodySizeTag, request.Content.Headers.ContentLength.Value);
            }

            // User agent
            if (_options.RecordUserAgent && request.Headers.UserAgent.Count > 0)
            {
                activity.SetTag(UserAgentOriginalTag, request.Headers.UserAgent.ToString());
            }

            // HTTP version
            activity.SetTag(NetworkProtocolVersionTag, request.Version.ToString());

            // Custom tags from options
            if (_options.CustomTags != null)
            {
                foreach (var tag in _options.CustomTags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }

            // Add event for request start
            if (_options.RecordEvents)
            {
                activity.AddEvent(new ActivityEvent("HTTP request started"));
            }
        }

        private void SetResponseAttributes(
            Activity activity,
            HttpResponseMessage response,
            TimeSpan elapsed)
        {
            // Response status code
            activity.SetTag(HttpResponseStatusCodeTag, (int)response.StatusCode);

            // Response content length
            if (response.Content?.Headers.ContentLength.HasValue == true)
            {
                activity.SetTag(HttpResponseBodySizeTag, response.Content.Headers.ContentLength.Value);
            }

            // Duration
            activity.SetTag("duration_ms", elapsed.TotalMilliseconds);

            // Add event for response received
            if (_options.RecordEvents)
            {
                activity.AddEvent(new ActivityEvent("HTTP response received",
                    tags: new ActivityTagsCollection
                    {
                        { "status_code", (int)response.StatusCode },
                        { "duration_ms", elapsed.TotalMilliseconds }
                    }));
            }
        }

        /// <summary>
        /// Records an exception on the activity using AddEvent (compatible with all .NET versions).
        /// </summary>
        private static void RecordExceptionOnActivity(Activity activity, Exception ex)
        {
            var tags = new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName ?? ex.GetType().Name },
                { "exception.message", ex.Message }
            };

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                tags.Add("exception.stacktrace", ex.StackTrace);
            }

            activity.AddEvent(new ActivityEvent("exception", tags: tags));
        }
    }

    /// <summary>
    /// Configuration options for telemetry handler.
    /// </summary>
    public class TelemetryHandlerOptions
    {
        /// <summary>
        /// Gets or sets whether to record the full URL including query string.
        /// Default is false (only path is recorded for security).
        /// </summary>
        public bool RecordFullUrl { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to record the User-Agent header.
        /// Default is true.
        /// </summary>
        public bool RecordUserAgent { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to record events (request start, response received).
        /// Default is true.
        /// </summary>
        public bool RecordEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets custom tags to add to all spans.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object>? CustomTags { get; set; }
    }
}

