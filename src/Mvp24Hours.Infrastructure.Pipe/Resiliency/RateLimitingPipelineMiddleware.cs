//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Exceptions;
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Middleware that implements rate limiting for pipeline operations
    /// using System.Threading.RateLimiting native .NET implementation.
    /// </summary>
    public class RateLimitingPipelineMiddleware : IPipelineMiddleware, IDisposable
    {
        private readonly IRateLimiterProvider _rateLimiterProvider;
        private readonly RateLimitingPipelineOptions _options;
        private readonly ILogger<RateLimitingPipelineMiddleware>? _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitingPipelineMiddleware"/>.
        /// </summary>
        public RateLimitingPipelineMiddleware(
            IRateLimiterProvider rateLimiterProvider,
            RateLimitingPipelineOptions? options = null,
            ILogger<RateLimitingPipelineMiddleware>? logger = null)
        {
            _rateLimiterProvider = rateLimiterProvider ?? throw new ArgumentNullException(nameof(rateLimiterProvider));
            _options = options ?? RateLimitingPipelineOptions.Default;
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -400; // Run early in the pipeline

        /// <inheritdoc />
        public async Task ExecuteAsync(
            IPipelineMessage message,
            Func<Task> next,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var rateLimitedOperation = GetRateLimitedOperation(message);
            var key = GetRateLimiterKey(message, rateLimitedOperation);
            var options = GetEffectiveOptions(rateLimitedOperation);

            _logger?.LogDebug(
                "Rate limiting check for key={Key}, algorithm={Algorithm}, permitLimit={PermitLimit}",
                key,
                options.Algorithm,
                options.PermitLimit);

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
                    "Rate limit exceeded for key={Key}. RetryAfter={RetryAfter}",
                    key,
                    retryAfter);

                // Notify the operation
                rateLimitedOperation?.OnRateLimited(retryAfter);
                _options.OnRateLimited?.Invoke(key, retryAfter);

                throw RateLimitExceededException.ForKey(key, retryAfter, options.PermitLimit);
            }

            _logger?.LogDebug("Rate limit permit acquired for key={Key}", key);

            await next();
        }

        /// <summary>
        /// Gets the rate limited operation from the message, if available.
        /// </summary>
        private static IRateLimitedOperation? GetRateLimitedOperation(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation is IRateLimitedOperation rateLimitedOp)
                {
                    return rateLimitedOp;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the rate limiter key for the operation.
        /// </summary>
        private string GetRateLimiterKey(IPipelineMessage message, IRateLimitedOperation? rateLimitedOperation)
        {
            // Priority: Operation key > Custom key generator > Pipeline key > Default
            if (!string.IsNullOrEmpty(rateLimitedOperation?.RateLimiterKey))
            {
                return rateLimitedOperation.RateLimiterKey;
            }

            if (_options.KeyGenerator != null)
            {
                return _options.KeyGenerator(message);
            }

            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                return $"pipeline_{operation?.GetType().Name ?? "unknown"}";
            }

            return _options.DefaultKey;
        }

        /// <summary>
        /// Gets effective options by merging operation-specific options with defaults.
        /// </summary>
        private NativeRateLimiterOptions GetEffectiveOptions(IRateLimitedOperation? operation)
        {
            if (operation == null)
            {
                return _options.DefaultRateLimiterOptions;
            }

            return new NativeRateLimiterOptions
            {
                Algorithm = operation.Algorithm,
                PermitLimit = operation.PermitLimit > 0 ? operation.PermitLimit : _options.DefaultRateLimiterOptions.PermitLimit,
                Window = operation.Window > TimeSpan.Zero ? operation.Window : _options.DefaultRateLimiterOptions.Window,
                SegmentsPerWindow = operation.SegmentsPerWindow > 0 ? operation.SegmentsPerWindow : _options.DefaultRateLimiterOptions.SegmentsPerWindow,
                ReplenishmentPeriod = operation.ReplenishmentPeriod > TimeSpan.Zero ? operation.ReplenishmentPeriod : _options.DefaultRateLimiterOptions.ReplenishmentPeriod,
                TokensPerPeriod = operation.TokensPerPeriod > 0 ? operation.TokensPerPeriod : _options.DefaultRateLimiterOptions.TokensPerPeriod,
                AutoReplenishment = operation.AutoReplenishment,
                QueueLimit = operation.QueueLimit,
                QueueProcessingOrder = operation.QueueProcessingOrder
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Options for configuring the rate limiting pipeline middleware.
    /// </summary>
    public class RateLimitingPipelineOptions
    {
        /// <summary>
        /// Gets or sets the default rate limiter key.
        /// </summary>
        public string DefaultKey { get; set; } = "pipeline_default";

        /// <summary>
        /// Gets or sets the default rate limiter options.
        /// </summary>
        public NativeRateLimiterOptions DefaultRateLimiterOptions { get; set; } = new NativeRateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.SlidingWindow,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4
        };

        /// <summary>
        /// Gets or sets a custom key generator function.
        /// </summary>
        public Func<IPipelineMessage, string>? KeyGenerator { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when rate limit is exceeded.
        /// </summary>
        public Action<string, TimeSpan?>? OnRateLimited { get; set; }

        /// <summary>
        /// Gets default options for the middleware.
        /// </summary>
        public static RateLimitingPipelineOptions Default => new();
    }
}

