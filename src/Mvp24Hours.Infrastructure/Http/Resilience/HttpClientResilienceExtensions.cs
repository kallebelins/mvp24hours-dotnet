//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Options;
using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Extension methods for configuring HTTP clients with Polly resilience policies.
    /// </summary>
    public static class HttpClientResilienceExtensions
    {
        /// <summary>
        /// Adds an HTTP client with Polly resilience policies configured.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="configure">The configuration action for resilience policies.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddHttpClientWithPolly("MyApi", builder =>
        /// {
        ///     builder.AddRetryPolicy(options =>
        ///     {
        ///         options.MaxRetries = 3;
        ///         options.BackoffType = BackoffType.Exponential;
        ///     });
        ///     
        ///     builder.AddCircuitBreakerPolicy(options =>
        ///     {
        ///         options.FailureRatio = 0.5;
        ///         options.BreakDuration = TimeSpan.FromSeconds(30);
        ///     });
        ///     
        ///     builder.AddTimeoutPolicy(options =>
        ///     {
        ///         options.Timeout = TimeSpan.FromSeconds(30);
        ///     });
        /// });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddHttpClientWithPolly(
            this IServiceCollection services,
            string name,
            Action<HttpResiliencePolicyBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("HTTP client name cannot be null or empty.", nameof(name));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new HttpResiliencePolicyBuilder(name);
            configure(builder);

            var clientBuilder = services.AddHttpClient(name);
            builder.ApplyTo(clientBuilder);

            return clientBuilder;
        }

        /// <summary>
        /// Adds a typed HTTP client with Polly resilience policies configured.
        /// </summary>
        /// <typeparam name="TClient">The typed HTTP client type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action for resilience policies.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddHttpClientWithPolly<TClient>(
            this IServiceCollection services,
            Action<HttpResiliencePolicyBuilder> configure)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var name = typeof(TClient).Name;
            var builder = new HttpResiliencePolicyBuilder(name);
            configure(builder);

            var clientBuilder = services.AddHttpClient<TClient>();
            builder.ApplyTo(clientBuilder);

            return clientBuilder;
        }

        /// <summary>
        /// Adds a resilience policy to an HTTP client builder.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="policy">The resilience policy to add.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddResiliencePolicy(
            this IHttpClientBuilder builder,
            IHttpResiliencePolicy policy)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            return builder.AddPolicyHandler(policy.GetPollyPolicy());
        }
    }

    /// <summary>
    /// Builder for configuring HTTP resilience policies.
    /// </summary>
    public class HttpResiliencePolicyBuilder
    {
        private readonly string _clientName;
        private readonly List<IHttpResiliencePolicy> _policies = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResiliencePolicyBuilder"/> class.
        /// </summary>
        /// <param name="clientName">The name of the HTTP client.</param>
        public HttpResiliencePolicyBuilder(string clientName)
        {
            _clientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
        }

        /// <summary>
        /// Adds a retry policy with the specified configuration.
        /// </summary>
        /// <param name="configure">The configuration action for retry options.</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddRetryPolicy(Action<RetryPolicyOptions>? configure = null)
        {
            var options = new RetryPolicyOptions();
            configure?.Invoke(options);
            _policies.Add(new RetryPolicy(options));
            return this;
        }

        /// <summary>
        /// Adds a circuit breaker policy with the specified configuration.
        /// </summary>
        /// <param name="configure">The configuration action for circuit breaker options.</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddCircuitBreakerPolicy(Action<CircuitBreakerPolicyOptions>? configure = null)
        {
            var options = new CircuitBreakerPolicyOptions();
            configure?.Invoke(options);
            _policies.Add(new CircuitBreakerPolicy(options, _clientName));
            return this;
        }

        /// <summary>
        /// Adds a timeout policy with the specified configuration.
        /// </summary>
        /// <param name="configure">The configuration action for timeout options.</param>
        /// <param name="strategy">The timeout strategy (Optimistic or Pessimistic).</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddTimeoutPolicy(
            Action<TimeoutPolicyOptions>? configure = null,
            TimeoutStrategy strategy = TimeoutStrategy.Optimistic)
        {
            var options = new TimeoutPolicyOptions();
            configure?.Invoke(options);
            _policies.Add(new TimeoutPolicy(options, strategy));
            return this;
        }

        /// <summary>
        /// Adds a bulkhead policy with the specified configuration.
        /// </summary>
        /// <param name="configure">The configuration action for bulkhead options.</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddBulkheadPolicy(Action<BulkheadPolicyOptions>? configure = null)
        {
            var options = new BulkheadPolicyOptions();
            configure?.Invoke(options);
            _policies.Add(new BulkheadPolicy(options));
            return this;
        }

        /// <summary>
        /// Adds a fallback policy with the specified configuration.
        /// </summary>
        /// <param name="configure">The configuration action for fallback options.</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddFallbackPolicy(Action<FallbackPolicyOptions>? configure = null)
        {
            var options = new FallbackPolicyOptions();
            configure?.Invoke(options);
            _policies.Add(new FallbackPolicy(options));
            return this;
        }

        /// <summary>
        /// Adds a custom resilience policy.
        /// </summary>
        /// <param name="policy">The custom policy to add.</param>
        /// <returns>The builder for chaining.</returns>
        public HttpResiliencePolicyBuilder AddPolicy(IHttpResiliencePolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            _policies.Add(policy);
            return this;
        }

        /// <summary>
        /// Applies all configured policies to the HTTP client builder.
        /// Policies are applied in order: Timeout → Bulkhead → CircuitBreaker → Retry → Fallback
        /// </summary>
        /// <param name="clientBuilder">The HTTP client builder.</param>
        internal void ApplyTo(IHttpClientBuilder clientBuilder)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            // Apply policies in reverse order (innermost to outermost)
            // This ensures proper policy wrapping: Timeout → Bulkhead → CircuitBreaker → Retry → Fallback
            for (int i = _policies.Count - 1; i >= 0; i--)
            {
                clientBuilder.AddPolicyHandler(_policies[i].GetPollyPolicy());
            }
        }
    }
}

