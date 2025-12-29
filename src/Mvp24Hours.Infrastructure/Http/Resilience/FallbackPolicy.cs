//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Options for configuring a Fallback policy.
    /// </summary>
    public class FallbackPolicyOptions
    {
        /// <summary>
        /// Gets or sets whether the fallback policy is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the fallback action that returns a default response when the primary action fails.
        /// </summary>
        public Func<Exception?, HttpResponseMessage?, CancellationToken, Task<HttpResponseMessage>>? FallbackAction { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when fallback is executed.
        /// </summary>
        public Action<Exception?, HttpResponseMessage?>? OnFallback { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status codes that should trigger fallback.
        /// Default includes: 408, 429, 500, 502, 503, 504.
        /// </summary>
        public List<int> FallbackStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };

        /// <summary>
        /// Gets or sets whether to fallback on timeout exceptions. Default is true.
        /// </summary>
        public bool FallbackOnTimeout { get; set; } = true;
    }

    /// <summary>
    /// Fallback policy implementation that provides a default response when the primary action fails.
    /// Useful for graceful degradation and providing default values when services are unavailable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fallback pattern provides an alternative response when the primary action fails,
    /// allowing the application to continue operating with reduced functionality rather than failing completely.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Configurable fallback action for failed requests</item>
    /// <item>Automatic fallback on transient HTTP errors</item>
    /// <item>Configurable fallback conditions based on HTTP status codes</item>
    /// <item>Callback notification when fallback is executed</item>
    /// <item>Detailed logging of fallback events</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This implementation uses Polly 8.x API with ResiliencePipeline.
    /// </para>
    /// </remarks>
    public class FallbackPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<FallbackPolicy>? _logger;
        private readonly FallbackPolicyOptions _options;
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackPolicy"/> class.
        /// </summary>
        /// <param name="options">The fallback policy options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public FallbackPolicy(FallbackPolicyOptions options, ILogger<FallbackPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _pipeline = CreatePipeline();
        }

        /// <inheritdoc/>
        public string PolicyName => "FallbackPolicy";

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken = default)
        {
            if (requestFactory == null)
            {
                throw new ArgumentNullException(nameof(requestFactory));
            }

            if (sendAsync == null)
            {
                throw new ArgumentNullException(nameof(sendAsync));
            }

            if (!_options.Enabled)
            {
                var request = requestFactory();
                return await sendAsync(request, cancellationToken);
            }

            return await _pipeline.ExecuteAsync(
                async (ct) =>
                {
                    var request = requestFactory();
                    return await sendAsync(request, ct);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy()
        {
            // Note: Polly 8.x uses ResiliencePipeline, not IAsyncPolicy.
            // This method is kept for interface compatibility but returns a no-op policy.
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        private ResiliencePipeline<HttpResponseMessage> CreatePipeline()
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            builder.AddFallback(new Polly.Fallback.FallbackStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response =>
                    {
                        if (response == null) return true;
                        var statusCode = (int)response.StatusCode;
                        return _options.FallbackStatusCodes.Contains(statusCode);
                    }),

                FallbackAction = async args =>
                {
                    _options.OnFallback?.Invoke(args.Outcome.Exception, args.Outcome.Result);

                    _logger?.LogWarning(
                        "Fallback action executed. Exception: {Exception}, StatusCode: {StatusCode}",
                        args.Outcome.Exception?.Message,
                        (int?)args.Outcome.Result?.StatusCode);

                    if (_options.FallbackAction != null)
                    {
                        return Outcome.FromResult(await _options.FallbackAction(
                            args.Outcome.Exception,
                            args.Outcome.Result,
                            args.Context.CancellationToken));
                    }

                    // Return a default response indicating service unavailable
                    var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("Service temporarily unavailable. Fallback response.")
                    };

                    return Outcome.FromResult(response);
                }
            });

            return builder.Build();
        }
    }
}
