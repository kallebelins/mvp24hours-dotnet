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
    /// Consume filter that implements rate limiting for message consumers
    /// using System.Threading.RateLimiting native .NET implementation.
    /// </summary>
    public class RateLimitingConsumeFilter : IConsumeFilter
    {
        private readonly IRateLimiterProvider _rateLimiterProvider;
        private readonly RateLimitingConsumeFilterOptions _options;
        private readonly ILogger<RateLimitingConsumeFilter>? _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitingConsumeFilter"/>.
        /// </summary>
        public RateLimitingConsumeFilter(
            IRateLimiterProvider rateLimiterProvider,
            RateLimitingConsumeFilterOptions? options = null,
            ILogger<RateLimitingConsumeFilter>? logger = null)
        {
            _rateLimiterProvider = rateLimiterProvider ?? throw new ArgumentNullException(nameof(rateLimiterProvider));
            _options = options ?? RateLimitingConsumeFilterOptions.Default;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var key = GetRateLimiterKey(context);
            var options = GetRateLimiterOptions<TMessage>();

            _logger?.LogDebug(
                "Rate limiting check for consumer key={Key}, messageType={MessageType}, algorithm={Algorithm}",
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
                    "Rate limit exceeded for consumer key={Key}, messageType={MessageType}, messageId={MessageId}. RetryAfter={RetryAfter}",
                    key,
                    typeof(TMessage).Name,
                    context.MessageId,
                    retryAfter);

                _options.OnRateLimited?.Invoke(key, typeof(TMessage), retryAfter);

                // Handle rate limit exceeded based on configured behavior
                await HandleRateLimitExceededAsync(context, key, retryAfter);
                return;
            }

            _logger?.LogDebug(
                "Rate limit permit acquired for consumer key={Key}, messageType={MessageType}",
                key,
                typeof(TMessage).Name);

            await next(context, cancellationToken);
        }

        /// <summary>
        /// Gets the rate limiter key for the current context.
        /// </summary>
        private string GetRateLimiterKey<TMessage>(IConsumeFilterContext<TMessage> context) where TMessage : class
        {
            if (_options.KeyGenerator != null)
            {
                return _options.KeyGenerator(context.QueueName, typeof(TMessage));
            }

            return _options.KeyMode switch
            {
                RateLimitKeyMode.ByQueue => $"queue_{context.QueueName}",
                RateLimitKeyMode.ByMessageType => $"type_{typeof(TMessage).FullName}",
                RateLimitKeyMode.ByExchange => $"exchange_{context.Exchange}",
                RateLimitKeyMode.ByRoutingKey => $"routingkey_{context.RoutingKey}",
                RateLimitKeyMode.ByConsumerTag => $"consumer_{context.ConsumerTag}",
                RateLimitKeyMode.Global => "global_consumer",
                _ => "global_consumer"
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

        /// <summary>
        /// Handles rate limit exceeded based on configured behavior.
        /// </summary>
        private async Task HandleRateLimitExceededAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            string key,
            TimeSpan? retryAfter) where TMessage : class
        {
            switch (_options.ExceededBehavior)
            {
                case RateLimitExceededBehavior.Retry:
                    // Request retry with delay
                    context.SetRetry(retryAfter ?? _options.DefaultRetryDelay);
                    break;

                case RateLimitExceededBehavior.DeadLetter:
                    // Send to dead letter queue
                    context.SendToDeadLetter($"Rate limit exceeded for key '{key}'");
                    break;

                case RateLimitExceededBehavior.Skip:
                    // Skip remaining filters but don't throw
                    context.SkipRemainingFilters();
                    break;

                case RateLimitExceededBehavior.Throw:
                default:
                    // Throw exception (default behavior)
                    throw RateLimitExceededException.ForKey(key, retryAfter);
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Options for configuring the rate limiting consume filter.
    /// </summary>
    public class RateLimitingConsumeFilterOptions
    {
        /// <summary>
        /// Gets or sets the key mode for rate limiting.
        /// </summary>
        public RateLimitKeyMode KeyMode { get; set; } = RateLimitKeyMode.ByQueue;

        /// <summary>
        /// Gets or sets a custom key generator function.
        /// </summary>
        public Func<string, Type, string>? KeyGenerator { get; set; }

        /// <summary>
        /// Gets or sets the default rate limiter options.
        /// </summary>
        public NativeRateLimiterOptions DefaultRateLimiterOptions { get; set; } = new NativeRateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.SlidingWindow,
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 4
        };

        /// <summary>
        /// Gets or sets type-specific rate limiter options.
        /// </summary>
        public System.Collections.Generic.Dictionary<Type, NativeRateLimiterOptions> TypeSpecificOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the behavior when rate limit is exceeded.
        /// </summary>
        public RateLimitExceededBehavior ExceededBehavior { get; set; } = RateLimitExceededBehavior.Retry;

        /// <summary>
        /// Gets or sets the default retry delay when rate limited.
        /// </summary>
        public TimeSpan DefaultRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets a callback invoked when rate limit is exceeded.
        /// </summary>
        public Action<string, Type, TimeSpan?>? OnRateLimited { get; set; }

        /// <summary>
        /// Gets default options.
        /// </summary>
        public static RateLimitingConsumeFilterOptions Default => new();
    }

    /// <summary>
    /// Key mode for rate limiting consumers.
    /// </summary>
    public enum RateLimitKeyMode
    {
        /// <summary>
        /// Rate limit per queue.
        /// </summary>
        ByQueue,

        /// <summary>
        /// Rate limit per message type.
        /// </summary>
        ByMessageType,

        /// <summary>
        /// Rate limit per exchange.
        /// </summary>
        ByExchange,

        /// <summary>
        /// Rate limit per routing key.
        /// </summary>
        ByRoutingKey,

        /// <summary>
        /// Rate limit per consumer tag.
        /// </summary>
        ByConsumerTag,

        /// <summary>
        /// Global rate limit for all consumers.
        /// </summary>
        Global
    }

    /// <summary>
    /// Behavior when rate limit is exceeded.
    /// </summary>
    public enum RateLimitExceededBehavior
    {
        /// <summary>
        /// Throw an exception.
        /// </summary>
        Throw,

        /// <summary>
        /// Request a retry with delay.
        /// </summary>
        Retry,

        /// <summary>
        /// Send message to dead letter queue.
        /// </summary>
        DeadLetter,

        /// <summary>
        /// Skip the message (acknowledge without processing).
        /// </summary>
        Skip
    }
}

