//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Builder for configuring HTTP resilience using the native .NET 9 API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder provides a fluent API for configuring resilience strategies
    /// using <c>Microsoft.Extensions.Http.Resilience</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHttpClient("MyApi")
    ///     .AddMvpResilience(builder => builder
    ///         .WithOptions(NativeResilienceOptions.HighAvailability)
    ///         .OnRetry((args, delay) => logger.LogWarning("Retry attempt"))
    ///         .OnCircuitBreak(args => logger.LogError("Circuit opened")));
    /// </code>
    /// </example>
    public class NativeResilienceBuilder
    {
        private readonly IHttpClientBuilder _httpClientBuilder;
        private NativeResilienceOptions _options = new();
        private Action<RetryStrategyOptions<HttpResponseMessage>>? _configureRetry;
        private Action<CircuitBreakerStrategyOptions<HttpResponseMessage>>? _configureCircuitBreaker;
        private Action<TimeoutStrategyOptions>? _configureAttemptTimeout;
        private Action<TimeoutStrategyOptions>? _configureTotalTimeout;
        private Action<OnRetryArguments<HttpResponseMessage>, TimeSpan>? _onRetry;
        private Action<OnCircuitOpenedArguments<HttpResponseMessage>>? _onCircuitOpen;
        private Action<OnCircuitClosedArguments<HttpResponseMessage>>? _onCircuitClose;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeResilienceBuilder"/> class.
        /// </summary>
        /// <param name="httpClientBuilder">The HTTP client builder.</param>
        public NativeResilienceBuilder(IHttpClientBuilder httpClientBuilder)
        {
            _httpClientBuilder = httpClientBuilder ?? throw new ArgumentNullException(nameof(httpClientBuilder));
        }

        /// <summary>
        /// Configures the resilience options using a preset or custom options.
        /// </summary>
        /// <param name="options">The resilience options.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder WithOptions(NativeResilienceOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Configures the resilience options using an action.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder ConfigureOptions(Action<NativeResilienceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(_options);
            return this;
        }

        /// <summary>
        /// Adds a callback for retry attempts.
        /// </summary>
        /// <param name="onRetry">The callback to execute on retry.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder OnRetry(Action<OnRetryArguments<HttpResponseMessage>, TimeSpan> onRetry)
        {
            _onRetry = onRetry;
            return this;
        }

        /// <summary>
        /// Adds a callback for when the circuit breaker opens.
        /// </summary>
        /// <param name="onCircuitOpen">The callback to execute when circuit opens.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder OnCircuitBreak(Action<OnCircuitOpenedArguments<HttpResponseMessage>> onCircuitOpen)
        {
            _onCircuitOpen = onCircuitOpen;
            return this;
        }

        /// <summary>
        /// Adds a callback for when the circuit breaker closes.
        /// </summary>
        /// <param name="onCircuitClose">The callback to execute when circuit closes.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder OnCircuitReset(Action<OnCircuitClosedArguments<HttpResponseMessage>> onCircuitClose)
        {
            _onCircuitClose = onCircuitClose;
            return this;
        }

        /// <summary>
        /// Allows custom configuration of the retry strategy.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder ConfigureRetry(Action<RetryStrategyOptions<HttpResponseMessage>> configure)
        {
            _configureRetry = configure;
            return this;
        }

        /// <summary>
        /// Allows custom configuration of the circuit breaker strategy.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder ConfigureCircuitBreaker(Action<CircuitBreakerStrategyOptions<HttpResponseMessage>> configure)
        {
            _configureCircuitBreaker = configure;
            return this;
        }

        /// <summary>
        /// Allows custom configuration of the attempt timeout strategy.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder ConfigureAttemptTimeout(Action<TimeoutStrategyOptions> configure)
        {
            _configureAttemptTimeout = configure;
            return this;
        }

        /// <summary>
        /// Allows custom configuration of the total timeout strategy.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public NativeResilienceBuilder ConfigureTotalTimeout(Action<TimeoutStrategyOptions> configure)
        {
            _configureTotalTimeout = configure;
            return this;
        }

        /// <summary>
        /// Builds and applies the resilience configuration to the HTTP client.
        /// </summary>
        /// <returns>The HTTP client builder for further configuration.</returns>
        public IHttpClientBuilder Build()
        {
            if (!_options.EnableRetry && !_options.EnableCircuitBreaker &&
                !_options.EnableAttemptTimeout && !_options.EnableTotalTimeout)
            {
                // No resilience enabled, return as-is
                return _httpClientBuilder;
            }

            // Use the standard resilience handler with custom configuration
            _httpClientBuilder.AddStandardResilienceHandler(options =>
            {
                // Configure total timeout
                if (_options.EnableTotalTimeout)
                {
                    options.TotalRequestTimeout.Timeout = _options.TotalRequestTimeout;
                    _configureTotalTimeout?.Invoke(options.TotalRequestTimeout);
                }

                // Configure retry
                if (_options.EnableRetry)
                {
                    options.Retry.MaxRetryAttempts = _options.MaxRetryAttempts;
                    options.Retry.Delay = _options.RetryDelay;
                    options.Retry.UseJitter = _options.UseJitter;
                    options.Retry.BackoffType = DelayBackoffType.Exponential;

                    if (_onRetry != null)
                    {
                        options.Retry.OnRetry = args =>
                        {
                            _onRetry(args, args.RetryDelay);
                            return ValueTask.CompletedTask;
                        };
                    }

                    _configureRetry?.Invoke(options.Retry);
                }

                // Configure circuit breaker
                if (_options.EnableCircuitBreaker)
                {
                    options.CircuitBreaker.FailureRatio = _options.CircuitBreakerFailureRatio;
                    options.CircuitBreaker.SamplingDuration = _options.CircuitBreakerSamplingDuration;
                    options.CircuitBreaker.MinimumThroughput = _options.CircuitBreakerMinimumThroughput;
                    options.CircuitBreaker.BreakDuration = _options.CircuitBreakerBreakDuration;

                    if (_onCircuitOpen != null)
                    {
                        options.CircuitBreaker.OnOpened = args =>
                        {
                            _onCircuitOpen(args);
                            return ValueTask.CompletedTask;
                        };
                    }

                    if (_onCircuitClose != null)
                    {
                        options.CircuitBreaker.OnClosed = args =>
                        {
                            _onCircuitClose(args);
                            return ValueTask.CompletedTask;
                        };
                    }

                    _configureCircuitBreaker?.Invoke(options.CircuitBreaker);
                }

                // Configure attempt timeout
                if (_options.EnableAttemptTimeout)
                {
                    options.AttemptTimeout.Timeout = _options.AttemptTimeout;
                    _configureAttemptTimeout?.Invoke(options.AttemptTimeout);
                }
            });

            return _httpClientBuilder;
        }
    }

    /// <summary>
    /// Extension methods for the native resilience builder.
    /// </summary>
    public static class NativeResilienceBuilderExtensions
    {
        /// <summary>
        /// Adds Mvp24Hours resilience configuration using the native .NET 9 API.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddHttpClient("MyApi")
        ///     .AddMvpResilience(r => r
        ///         .WithOptions(NativeResilienceOptions.HighAvailability)
        ///         .OnRetry((args, delay) => Console.WriteLine($"Retry after {delay}"))
        ///         .OnCircuitBreak(args => Console.WriteLine("Circuit opened!")));
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMvpResilience(
            this IHttpClientBuilder builder,
            Action<NativeResilienceBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var resilienceBuilder = new NativeResilienceBuilder(builder);
            configure(resilienceBuilder);

            return resilienceBuilder.Build();
        }

        /// <summary>
        /// Adds Mvp24Hours resilience configuration with preset options.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="options">The preset resilience options.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddHttpClient("MyApi")
        ///     .AddMvpResilience(NativeResilienceOptions.LowLatency);
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMvpResilience(
            this IHttpClientBuilder builder,
            NativeResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            return builder.AddMvpResilience(r => r.WithOptions(options));
        }

        /// <summary>
        /// Adds standard Mvp24Hours resilience with default options.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        /// <remarks>
        /// This is equivalent to calling <c>AddStandardResilienceHandler()</c> from
        /// Microsoft.Extensions.Http.Resilience.
        /// </remarks>
        public static IHttpClientBuilder AddMvpStandardResilience(this IHttpClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddStandardResilienceHandler();
            return builder;
        }
    }
}

