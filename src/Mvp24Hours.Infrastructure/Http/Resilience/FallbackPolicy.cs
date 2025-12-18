//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
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
        public Func<DelegateResult<HttpResponseMessage>, Context, CancellationToken, Task<HttpResponseMessage>>? FallbackAction { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when fallback is executed.
        /// </summary>
        public Action<DelegateResult<HttpResponseMessage>, Context>? OnFallback { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status codes that should trigger fallback.
        /// Default includes: 408, 429, 500, 502, 503, 504.
        /// </summary>
        public System.Collections.Generic.List<int> FallbackStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };

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
    /// </remarks>
    public class FallbackPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<FallbackPolicy>? _logger;
        private readonly FallbackPolicyOptions _options;
        private readonly AsyncFallbackPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackPolicy"/> class.
        /// </summary>
        /// <param name="options">The fallback policy options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public FallbackPolicy(FallbackPolicyOptions options, ILogger<FallbackPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _policy = CreatePolicy();
        }

        /// <inheritdoc/>
        public string PolicyName => "FallbackPolicy";

        /// <inheritdoc/>
        public Task<HttpResponseMessage> ExecuteAsync(
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

            if (!_options.Enabled || _options.FallbackAction == null)
            {
                var request = requestFactory();
                return sendAsync(request, cancellationToken);
            }

            return _policy.ExecuteAsync(
                async (ct) =>
                {
                    var request = requestFactory();
                    return await sendAsync(request, ct);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _policy;

        private AsyncFallbackPolicy<HttpResponseMessage> CreatePolicy()
        {
            var policyBuilder = HttpPolicyExtensions
                .HandleTransientHttpError();

            // Add custom status codes to fallback on
            if (_options.FallbackStatusCodes.Count > 0)
            {
                foreach (var statusCode in _options.FallbackStatusCodes)
                {
                    policyBuilder = policyBuilder.OrResult(response => (int)response.StatusCode == statusCode);
                }
            }

            if (_options.FallbackOnTimeout)
            {
                policyBuilder = policyBuilder.Or<TimeoutRejectedException>();
            }

            var fallbackAction = _options.FallbackAction ?? DefaultFallbackAction;
            var onFallback = _options.OnFallback != null
                ? (Func<DelegateResult<HttpResponseMessage>, Context, Task>)((outcome, context) =>
                {
                    _options.OnFallback(outcome, context);
                    return Task.CompletedTask;
                })
                : null;

            if (onFallback != null)
            {
                return policyBuilder.FallbackAsync(fallbackAction, onFallback);
            }
            else
            {
                return policyBuilder.FallbackAsync(fallbackAction);
            }
        }

        private Task<HttpResponseMessage> DefaultFallbackAction(
            DelegateResult<HttpResponseMessage> outcome,
            Context context,
            CancellationToken cancellationToken)
        {
            _logger?.LogWarning(
                "Fallback action executed. " +
                "Exception: {Exception}, " +
                "StatusCode: {StatusCode}",
                outcome.Exception?.Message,
                (int?)outcome.Result?.StatusCode);

            // Return a default response indicating service unavailable
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service temporarily unavailable. Fallback response.")
            };

            return Task.FromResult(response);
        }
    }
}

