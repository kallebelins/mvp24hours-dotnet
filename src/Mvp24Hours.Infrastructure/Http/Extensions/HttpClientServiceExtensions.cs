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
using System.Threading.Tasks;

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
            // Add telemetry handler (first to capture full request lifecycle)
            if (options.EnableTelemetry)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetService<ILogger<TelemetryDelegatingHandler>>();
                    return new TelemetryDelegatingHandler(logger, options.TelemetryOptions ?? new TelemetryHandlerOptions());
                });
            }

            // Add logging handler
            if (options.EnableLogging)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<LoggingDelegatingHandler>>();
                    return new LoggingDelegatingHandler(logger, options.LoggingOptions ?? new HttpLoggingOptions());
                });
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

            // Add authentication handler
            if (options.Authentication != null && options.Authentication.Scheme != AuthenticationScheme.None)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<AuthenticationDelegatingHandler>>();
                    return new AuthenticationDelegatingHandler(logger, options.Authentication);
                });
            }

            // Add custom header propagation
            if (options.PropagateHeaders.Count > 0)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    return new PropagationHeaderDelegatingHandler(sp, options.PropagateHeaders.ToArray());
                });
            }

            // Add compression handler
            if (options.Compression?.Enabled == true)
            {
                clientBuilder.AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<CompressionDelegatingHandler>>();
                    return new CompressionDelegatingHandler(logger, options.Compression);
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

        #region Handler Extension Methods

        /// <summary>
        /// Adds the logging delegating handler with custom options.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpLoggingHandler(
            this IHttpClientBuilder builder,
            Action<HttpLoggingOptions>? configure = null)
        {
            var options = new HttpLoggingOptions();
            configure?.Invoke(options);

            builder.Services.TryAddTransient<LoggingDelegatingHandler>();

            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LoggingDelegatingHandler>>();
                return new LoggingDelegatingHandler(logger, options);
            });
        }

        /// <summary>
        /// Adds the authentication delegating handler with Bearer token authentication.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="tokenProvider">Function that provides the authentication token.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpBearerAuthentication(
            this IHttpClientBuilder builder,
            Func<Task<string?>> tokenProvider)
        {
            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AuthenticationDelegatingHandler>>();
                return new AuthenticationDelegatingHandler(
                    logger,
                    AuthenticationScheme.Bearer,
                    tokenProvider: tokenProvider);
            });
        }

        /// <summary>
        /// Adds the authentication delegating handler with a static Bearer token.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="token">The static Bearer token.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpBearerAuthentication(
            this IHttpClientBuilder builder,
            string token)
        {
            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AuthenticationDelegatingHandler>>();
                return new AuthenticationDelegatingHandler(
                    logger,
                    new AuthenticationOptions
                    {
                        Scheme = AuthenticationScheme.Bearer,
                        StaticToken = token
                    });
            });
        }

        /// <summary>
        /// Adds the authentication delegating handler with API Key authentication.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="headerName">The header name. Default is "X-API-Key".</param>
        /// <param name="location">Where to place the API key. Default is Header.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpApiKeyAuthentication(
            this IHttpClientBuilder builder,
            string apiKey,
            string headerName = "X-API-Key",
            ApiKeyLocation location = ApiKeyLocation.Header)
        {
            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AuthenticationDelegatingHandler>>();
                return new AuthenticationDelegatingHandler(
                    logger,
                    new AuthenticationOptions
                    {
                        Scheme = AuthenticationScheme.ApiKey,
                        ApiKey = apiKey,
                        ApiKeyHeaderName = headerName,
                        ApiKeyLocation = location
                    });
            });
        }

        /// <summary>
        /// Adds the authentication delegating handler with Basic authentication.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpBasicAuthentication(
            this IHttpClientBuilder builder,
            string username,
            string password)
        {
            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AuthenticationDelegatingHandler>>();
                return new AuthenticationDelegatingHandler(
                    logger,
                    AuthenticationScheme.Basic,
                    username: username,
                    password: password);
            });
        }

        /// <summary>
        /// Adds the telemetry delegating handler for OpenTelemetry tracing.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpTelemetryHandler(
            this IHttpClientBuilder builder,
            Action<TelemetryHandlerOptions>? configure = null)
        {
            var options = new TelemetryHandlerOptions();
            configure?.Invoke(options);

            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetService<ILogger<TelemetryDelegatingHandler>>();
                return new TelemetryDelegatingHandler(logger, options);
            });
        }

        /// <summary>
        /// Adds the retry delegating handler with custom options.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpRetryHandler(
            this IHttpClientBuilder builder,
            Action<RetryPolicyOptions>? configure = null)
        {
            var options = new RetryPolicyOptions();
            configure?.Invoke(options);

            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RetryDelegatingHandler>>();
                return new RetryDelegatingHandler(logger, options);
            });
        }

        /// <summary>
        /// Adds the circuit breaker delegating handler with custom options.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="serviceName">The service name for logging.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpCircuitBreakerHandler(
            this IHttpClientBuilder builder,
            string? serviceName = null,
            Action<CircuitBreakerPolicyOptions>? configure = null)
        {
            var options = new CircuitBreakerPolicyOptions();
            configure?.Invoke(options);

            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<CircuitBreakerDelegatingHandler>>();
                return new CircuitBreakerDelegatingHandler(logger, options, serviceName ?? "HttpClient");
            });
        }

        /// <summary>
        /// Adds the timeout delegating handler.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="timeout">The default timeout for requests.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpTimeoutHandler(
            this IHttpClientBuilder builder,
            TimeSpan timeout)
        {
            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<TimeoutDelegatingHandler>>();
                return new TimeoutDelegatingHandler(logger, timeout);
            });
        }

        /// <summary>
        /// Adds the compression delegating handler for request body compression.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpCompressionHandler(
            this IHttpClientBuilder builder,
            Action<CompressionHandlerOptions>? configure = null)
        {
            var options = new CompressionHandlerOptions();
            configure?.Invoke(options);

            return builder.AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<CompressionDelegatingHandler>>();
                return new CompressionDelegatingHandler(logger, options);
            });
        }

        /// <summary>
        /// Adds all recommended handlers in the correct order:
        /// Telemetry -> Logging -> CorrelationId -> Authentication -> Compression.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <param name="configure">The configuration action for all handlers.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddMvpStandardHandlers(
            this IHttpClientBuilder builder,
            Action<StandardHandlersOptions>? configure = null)
        {
            var options = new StandardHandlersOptions();
            configure?.Invoke(options);

            // Add handlers in correct order (outer to inner)
            if (options.EnableTelemetry)
            {
                builder.AddMvpTelemetryHandler(opt =>
                {
                    opt.RecordFullUrl = options.TelemetryRecordFullUrl;
                    opt.RecordEvents = options.TelemetryRecordEvents;
                });
            }

            if (options.EnableLogging)
            {
                builder.AddMvpLoggingHandler(opt =>
                {
                    opt.LogRequestHeaders = options.LogRequestHeaders;
                    opt.LogRequestBody = options.LogRequestBody;
                    opt.LogResponseHeaders = options.LogResponseHeaders;
                    opt.LogResponseBody = options.LogResponseBody;
                });
            }

            if (options.PropagateCorrelationId)
            {
                builder.Services.TryAddTransient<PropagationCorrelationIdDelegatingHandler>();
                builder.AddHttpMessageHandler<PropagationCorrelationIdDelegatingHandler>();
            }

            if (options.EnableCompression)
            {
                builder.AddMvpCompressionHandler(opt =>
                {
                    opt.Algorithm = options.CompressionAlgorithm;
                    opt.MinimumSizeBytes = options.CompressionMinimumSizeBytes;
                });
            }

            return builder;
        }

        #endregion
    }

    /// <summary>
    /// Options for configuring standard handlers.
    /// </summary>
    public class StandardHandlersOptions
    {
        /// <summary>
        /// Gets or sets whether to enable telemetry. Default is true.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to record full URLs in telemetry. Default is false.
        /// </summary>
        public bool TelemetryRecordFullUrl { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to record events in telemetry. Default is true.
        /// </summary>
        public bool TelemetryRecordEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable logging. Default is true.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log request headers. Default is false.
        /// </summary>
        public bool LogRequestHeaders { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log request body. Default is false.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log response headers. Default is false.
        /// </summary>
        public bool LogResponseHeaders { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log response body. Default is false.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to propagate correlation ID. Default is true.
        /// </summary>
        public bool PropagateCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable request compression. Default is false.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the compression algorithm. Default is Gzip.
        /// </summary>
        public CompressionAlgorithm CompressionAlgorithm { get; set; } = CompressionAlgorithm.Gzip;

        /// <summary>
        /// Gets or sets the minimum size in bytes to trigger compression. Default is 1024.
        /// </summary>
        public int CompressionMinimumSizeBytes { get; set; } = 1024;
    }
}

