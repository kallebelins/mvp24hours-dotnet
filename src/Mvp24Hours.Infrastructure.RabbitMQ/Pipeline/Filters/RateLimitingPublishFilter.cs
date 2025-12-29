//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters
{
    /// <summary>
    /// Publish filter that implements rate limiting for message publishing
    /// using System.Threading.RateLimiting native .NET implementation.
    /// </summary>
    public class RateLimitingPublishFilter : IPublishFilter
    {
        private readonly IRateLimiterProvider _rateLimiterProvider;
        private readonly RateLimitingPublishFilterOptions _options;
        private readonly ILogger<RateLimitingPublishFilter>? _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitingPublishFilter"/>.
        /// </summary>
        public RateLimitingPublishFilter(
            IRateLimiterProvider rateLimiterProvider,
            RateLimitingPublishFilterOptions? options = null,
            ILogger<RateLimitingPublishFilter>? logger = null)
        {
            _rateLimiterProvider = rateLimiterProvider ?? throw new ArgumentNullException(nameof(rateLimiterProvider));
            _options = options ?? RateLimitingPublishFilterOptions.Default;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PublishAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var key = GetRateLimiterKey(context);
            var options = GetRateLimiterOptions<TMessage>();

            _logger?.LogDebug(
                "Rate limiting check for publish key={Key}, messageType={MessageType}, algorithm={Algorithm}",
                key,
                typeof(TMessage).Name,
                options.Algorithm);

            using var lease = await _rateLimiterProvider.AcquireAsync(
                key,
                options,
                1,
                cancellationToken);

            if (!lease.IsAcquired)
            {
                var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? retry
                    : (TimeSpan?)null;

                _logger?.LogWarning(
                    "Rate limit exceeded for publish key={Key}, messageType={MessageType}. RetryAfter={RetryAfter}",
                    key,
                    typeof(TMessage).Name,
                    retryAfter);

                _options.OnRateLimited?.Invoke(key, typeof(TMessage), retryAfter);

                // Wait if configured to do so
                if (_options.WaitWhenExceeded && retryAfter.HasValue)
                {
                    _logger?.LogDebug(
                        "Waiting {RetryAfter} before publishing due to rate limit for key={Key}",
                        retryAfter.Value,
                        key);

                    await Task.Delay(retryAfter.Value, cancellationToken);

                    // Retry acquire
                    using var retryLease = await _rateLimiterProvider.AcquireAsync(
                        key,
                        options,
                        1,
                        cancellationToken);

                    if (!retryLease.IsAcquired)
                    {
                        throw RateLimitExceededException.ForKey(key, retryAfter);
                    }

                    await next(context, cancellationToken);
                    return;
                }

                throw RateLimitExceededException.ForKey(key, retryAfter);
            }

            _logger?.LogDebug(
                "Rate limit permit acquired for publish key={Key}, messageType={MessageType}",
                key,
                typeof(TMessage).Name);

            await next(context, cancellationToken);
        }

        /// <summary>
        /// Gets the rate limiter key for the current context.
        /// </summary>
        private string GetRateLimiterKey<TMessage>(IPublishFilterContext<TMessage> context) where TMessage : class
        {
            if (_options.KeyGenerator != null)
            {
                return _options.KeyGenerator(context.Exchange, typeof(TMessage));
            }

            return _options.KeyMode switch
            {
                PublishRateLimitKeyMode.ByExchange => $"publish_exchange_{context.Exchange}",
                PublishRateLimitKeyMode.ByMessageType => $"publish_type_{typeof(TMessage).FullName}",
                PublishRateLimitKeyMode.ByRoutingKey => $"publish_routingkey_{context.RoutingKey}",
                PublishRateLimitKeyMode.Global => "publish_global",
                _ => "publish_global"
            };
        }

        /// <summary>
        /// Gets rate limiter options for the message type.
        /// </summary>
        private NativeRateLimiterOptions GetRateLimiterOptions<TMessage>()
        {
            // Check if there are type-specific options
            if (_options.TypeSpecificOptions.TryGetValue(typeof(TMessage), out var typeOptions))
            {
                return typeOptions;
            }

            return _options.DefaultRateLimiterOptions;
        }
    }

    /// <summary>
    /// Options for configuring the rate limiting publish filter.
    /// </summary>
    public class RateLimitingPublishFilterOptions
    {
        /// <summary>
        /// Gets or sets the key mode for rate limiting.
        /// </summary>
        public PublishRateLimitKeyMode KeyMode { get; set; } = PublishRateLimitKeyMode.Global;

        /// <summary>
        /// Gets or sets a custom key generator function.
        /// </summary>
        public Func<string, Type, string>? KeyGenerator { get; set; }

        /// <summary>
        /// Gets or sets the default rate limiter options.
        /// </summary>
        public NativeRateLimiterOptions DefaultRateLimiterOptions { get; set; } = new NativeRateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            PermitLimit = 1000,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 100,
            AutoReplenishment = true
        };

        /// <summary>
        /// Gets or sets type-specific rate limiter options.
        /// </summary>
        public System.Collections.Generic.Dictionary<Type, NativeRateLimiterOptions> TypeSpecificOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to wait when rate limit is exceeded.
        /// </summary>
        public bool WaitWhenExceeded { get; set; } = false;

        /// <summary>
        /// Gets or sets a callback invoked when rate limit is exceeded.
        /// </summary>
        public Action<string, Type, TimeSpan?>? OnRateLimited { get; set; }

        /// <summary>
        /// Gets default options.
        /// </summary>
        public static RateLimitingPublishFilterOptions Default => new();
    }

    /// <summary>
    /// Key mode for rate limiting publishers.
    /// </summary>
    public enum PublishRateLimitKeyMode
    {
        /// <summary>
        /// Rate limit per exchange.
        /// </summary>
        ByExchange,

        /// <summary>
        /// Rate limit per message type.
        /// </summary>
        ByMessageType,

        /// <summary>
        /// Rate limit per routing key.
        /// </summary>
        ByRoutingKey,

        /// <summary>
        /// Global rate limit for all publishers.
        /// </summary>
        Global
    }
}

