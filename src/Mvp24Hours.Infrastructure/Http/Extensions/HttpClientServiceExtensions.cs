//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Builders;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Http.Helpers;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Http.Serializers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace Mvp24Hours.Infrastructure.Http.Extensions
{
    /// <summary>
    /// Extension methods for configuring HTTP clients with IServiceCollection.
    /// </summary>
    public static class HttpClientServiceExtensions
    {
        /// <summary>
        /// Adds a typed HTTP client to the service collection.
        /// </summary>
        /// <typeparam name="TApi">The marker type that identifies the API.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpTypedHttpClient<TApi>(
            this IServiceCollection services,
            Action<HttpClientOptions> configure)
            where TApi : class
        {
            var options = new HttpClientOptions { Name = typeof(TApi).Name };
            configure(options);

            return services.AddMvpTypedHttpClient<TApi>(options);
        }

        /// <summary>
        /// Adds a typed HTTP client to the service collection using fluent builder.
        /// </summary>
        /// <typeparam name="TApi">The marker type that identifies the API.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The fluent builder configuration action.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpTypedHttpClient<TApi>(
            this IServiceCollection services,
            Action<HttpClientBuilder> configure)
            where TApi : class
        {
            var builder = HttpClientBuilder.Create(typeof(TApi).Name);
            configure(builder);

            return services.AddMvpTypedHttpClient<TApi>(builder.Build());
        }

        /// <summary>
        /// Adds a typed HTTP client to the service collection with options.
        /// </summary>
        /// <typeparam name="TApi">The marker type that identifies the API.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The HTTP client options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpTypedHttpClient<TApi>(
            this IServiceCollection services,
            HttpClientOptions options)
            where TApi : class
        {
            // Ensure serializer is registered
            services.TryAddSingleton<IHttpClientSerializer, JsonHttpClientSerializer>();

            // Configure the named HTTP client
            var clientBuilder = services.AddHttpClient<ITypedHttpClient<TApi>, TypedHttpClient<TApi>>(
                options.Name ?? typeof(TApi).Name,
                client => ConfigureHttpClient(client, options));

            // Configure primary handler
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options));

            // Configure handler lifetime
            clientBuilder.SetHandlerLifetime(options.HandlerLifetime);

            // Add delegating handlers
            AddDelegatingHandlers(clientBuilder, options, services);

            // Add resilience policies
            AddResiliencePolicies(clientBuilder, options);

            return clientBuilder;
        }

        /// <summary>
        /// Adds a named HTTP client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpHttpClient(
            this IServiceCollection services,
            string name,
            Action<HttpClientOptions> configure)
        {
            var options = new HttpClientOptions { Name = name };
            configure(options);

            return services.AddMvpHttpClient(name, options);
        }

        /// <summary>
        /// Adds a named HTTP client to the service collection using fluent builder.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="configure">The fluent builder configuration action.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpHttpClient(
            this IServiceCollection services,
            string name,
            Action<HttpClientBuilder> configure)
        {
            var builder = HttpClientBuilder.Create(name);
            configure(builder);

            return services.AddMvpHttpClient(name, builder.Build());
        }

        /// <summary>
        /// Adds a named HTTP client to the service collection with options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="options">The HTTP client options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMvpHttpClient(
            this IServiceCollection services,
            string name,
            HttpClientOptions options)
        {
            // Configure the named HTTP client
            var clientBuilder = services.AddHttpClient(name, client => ConfigureHttpClient(client, options));

            // Configure primary handler
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options));

            // Configure handler lifetime
            clientBuilder.SetHandlerLifetime(options.HandlerLifetime);

            // Add delegating handlers
            AddDelegatingHandlers(clientBuilder, options, services);

            // Add resilience policies
            AddResiliencePolicies(clientBuilder, options);

            return clientBuilder;
        }

        /// <summary>
        /// Configures the HTTP client with the provided options.
        /// </summary>
        private static void ConfigureHttpClient(HttpClient client, HttpClientOptions options)
        {
            // Set base address
            if (options.BaseAddress != null)
            {
                client.BaseAddress = options.BaseAddress;
            }

            // Set timeout
            client.Timeout = options.Timeout;

            // Set max response buffer size
            client.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;

            // Set default headers
            foreach (var header in options.DefaultHeaders)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Set Accept header
            if (!string.IsNullOrWhiteSpace(options.AcceptHeader))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd(options.AcceptHeader);
            }

            // Set User-Agent header
            if (!string.IsNullOrWhiteSpace(options.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            }
        }

        /// <summary>
        /// Creates the primary HTTP message handler with the provided options.
        /// </summary>
        private static HttpMessageHandler CreatePrimaryHandler(HttpClientOptions options)
        {
            var handler = new HttpClientHandler();

            // Configure decompression
            if (options.EnableDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
            }

            // Configure redirects
            handler.AllowAutoRedirect = options.FollowRedirects;
            handler.MaxAutomaticRedirections = options.MaxRedirects;

            // Configure cookies
            handler.UseCookies = options.UseCookies;

            // Configure SSL/TLS
            handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

            // Configure server certificate validation
            if (!options.ValidateServerCertificate)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            // Configure client certificate
            var certificate = CertificateHelper.LoadCertificate(options.Certificate);
            if (certificate != null)
            {
                handler.ClientCertificates.Add(certificate);
            }

            // Configure proxy
            if (options.Proxy?.Enabled == true && !string.IsNullOrWhiteSpace(options.Proxy.Address))
            {
                var proxy = new WebProxy(options.Proxy.Address)
                {
                    BypassProxyOnLocal = options.Proxy.BypassOnLocal,
                    UseDefaultCredentials = options.Proxy.UseDefaultCredentials
                };

                if (options.Proxy.BypassList.Count > 0)
                {
                    proxy.BypassList = options.Proxy.BypassList.ToArray();
                }

                if (!string.IsNullOrWhiteSpace(options.Proxy.Username) && !string.IsNullOrWhiteSpace(options.Proxy.Password))
                {
                    proxy.Credentials = new NetworkCredential(options.Proxy.Username, options.Proxy.Password);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return handler;
        }

        /// <summary>
        /// Adds delegating handlers based on options.
        /// </summary>
        private static void AddDelegatingHandlers(IHttpClientBuilder clientBuilder, HttpClientOptions options, IServiceCollection services)
        {
            // Add logging handler
            if (options.EnableLogging)
            {
                clientBuilder.AddHttpMessageHandler<LoggingDelegatingHandler>();
                services.TryAddTransient<LoggingDelegatingHandler>();
            }

            // Add correlation ID propagation
            if (options.PropagateCorrelationId)
            {
                clientBuilder.AddHttpMessageHandler<PropagationCorrelationIdDelegatingHandler>();
                services.TryAddTransient<PropagationCorrelationIdDelegatingHandler>();
            }

            // Add authorization propagation
            if (options.PropagateAuthorization)
            {
                clientBuilder.AddHttpMessageHandler<PropagationAuthorizationDelegatingHandler>();
                services.TryAddTransient<PropagationAuthorizationDelegatingHandler>();
            }

            // Add custom header propagation
            if (options.PropagateHeaders.Count > 0)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    return new PropagationHeaderDelegatingHandler(sp, options.PropagateHeaders.ToArray());
                });
            }
        }

        /// <summary>
        /// Adds resilience policies (retry, circuit breaker, timeout) to the HTTP client.
        /// </summary>
        private static void AddResiliencePolicies(IHttpClientBuilder clientBuilder, HttpClientOptions options)
        {
            // Add timeout policy (innermost, wraps around the request)
            if (options.TimeoutPolicy?.Enabled == true)
            {
                var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(options.TimeoutPolicy.Timeout);
                clientBuilder.AddPolicyHandler(timeoutPolicy);
            }

            // Add circuit breaker policy
            if (options.CircuitBreakerPolicy?.Enabled == true)
            {
                var circuitBreakerPolicy = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .AdvancedCircuitBreakerAsync(
                        failureThreshold: options.CircuitBreakerPolicy.FailureRatio,
                        samplingDuration: options.CircuitBreakerPolicy.SamplingDuration,
                        minimumThroughput: options.CircuitBreakerPolicy.MinimumThroughput,
                        durationOfBreak: options.CircuitBreakerPolicy.BreakDuration,
                        onBreak: (result, breakDuration) =>
                        {
                            // Log circuit breaker open
                        },
                        onReset: () =>
                        {
                            // Log circuit breaker reset
                        },
                        onHalfOpen: () =>
                        {
                            // Log circuit breaker half-open
                        });

                clientBuilder.AddPolicyHandler(circuitBreakerPolicy);
            }

            // Add retry policy (outermost)
            if (options.RetryPolicy?.Enabled == true)
            {
                var retryPolicy = CreateRetryPolicy(options.RetryPolicy);
                clientBuilder.AddPolicyHandler(retryPolicy);
            }
        }

        /// <summary>
        /// Creates a retry policy based on options.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(RetryPolicyOptions options)
        {
            var jitterer = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .OrResult(response => options.RetryStatusCodes.Contains((int)response.StatusCode))
                .WaitAndRetryAsync(
                    options.MaxRetries,
                    retryAttempt =>
                    {
                        var delay = options.BackoffType switch
                        {
                            BackoffType.Constant => options.InitialDelay,
                            BackoffType.Linear => TimeSpan.FromMilliseconds(options.InitialDelay.TotalMilliseconds * retryAttempt),
                            BackoffType.Exponential => TimeSpan.FromMilliseconds(options.InitialDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1)),
                            BackoffType.DecorrelatedJitter => TimeSpan.FromMilliseconds(
                                options.InitialDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1) *
                                (1.0 + jitterer.NextDouble() * options.JitterFactor)),
                            _ => options.InitialDelay
                        };

                        // Ensure delay doesn't exceed max delay
                        if (delay > options.MaxDelay)
                        {
                            delay = options.MaxDelay;
                        }

                        // Add jitter for non-decorrelated types
                        if (options.BackoffType != BackoffType.DecorrelatedJitter && options.JitterFactor > 0)
                        {
                            var jitter = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * options.JitterFactor * jitterer.NextDouble());
                            delay = delay.Add(jitter);
                        }

                        return delay;
                    },
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log retry attempt
                    });
        }

        /// <summary>
        /// Adds the default HTTP client serializer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddMvpHttpClientSerializer(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpClientSerializer, JsonHttpClientSerializer>();
            return services;
        }

        /// <summary>
        /// Adds a custom HTTP client serializer to the service collection.
        /// </summary>
        /// <typeparam name="TSerializer">The serializer type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddMvpHttpClientSerializer<TSerializer>(this IServiceCollection services)
            where TSerializer : class, IHttpClientSerializer
        {
            services.AddSingleton<IHttpClientSerializer, TSerializer>();
            return services;
        }
    }
}

