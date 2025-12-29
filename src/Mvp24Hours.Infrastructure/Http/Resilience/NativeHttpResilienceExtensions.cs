//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Extension methods for configuring HTTP clients with native .NET 9 resilience APIs.
    /// </summary>
    /// <remarks>
    /// <para>This is the modern, recommended API for HTTP resilience in .NET 9+.</para>
    /// <para>Uses <c>Microsoft.Extensions.Http.Resilience</c> which internally uses Polly v8.</para>
    /// <para>
    /// Benefits over the legacy approach:
    /// <list type="bullet">
    ///     <item>Simplified configuration via IOptions pattern</item>
    ///     <item>Built-in OpenTelemetry integration</item>
    ///     <item>Native metrics support</item>
    ///     <item>Better performance with Polly v8</item>
    ///     <item>Reduced boilerplate code</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class NativeHttpResilienceExtensions
    {
        /// <summary>
        /// Adds an HTTP client with standard resilience handler using the native .NET 9 API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="configureClient">Optional configuration for the HTTP client.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <remarks>
        /// <para>
        /// The standard resilience handler includes:
        /// <list type="bullet">
        ///     <item>Total request timeout (30 seconds default)</item>
        ///     <item>Retry (3 attempts with exponential backoff)</item>
        ///     <item>Circuit breaker (failure ratio based)</item>
        ///     <item>Attempt timeout (10 seconds per attempt)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHttpClientWithStandardResilience("MyApi", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://api.example.com");
        /// });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddHttpClientWithStandardResilience(
            this IServiceCollection services,
            string name,
            Action<HttpClient>? configureClient = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var builder = services.AddHttpClient(name);

            if (configureClient != null)
            {
                builder.ConfigureHttpClient(configureClient);
            }

            builder.AddStandardResilienceHandler();

            return builder;
        }

        /// <summary>
        /// Adds an HTTP client with standard resilience handler and custom options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="configureClient">Configuration for the HTTP client.</param>
        /// <param name="configureResilience">Configuration for the resilience options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddHttpClientWithStandardResilience("MyApi",
        ///     client => client.BaseAddress = new Uri("https://api.example.com"),
        ///     options =>
        ///     {
        ///         options.Retry.MaxRetryAttempts = 5;
        ///         options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        ///         options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        ///         options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        ///     });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddHttpClientWithStandardResilience(
            this IServiceCollection services,
            string name,
            Action<HttpClient> configureClient,
            Action<HttpStandardResilienceOptions> configureResilience)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(configureClient);
            ArgumentNullException.ThrowIfNull(configureResilience);

            var builder = services.AddHttpClient(name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddStandardResilienceHandler(configureResilience);

            return builder;
        }

        /// <summary>
        /// Adds an HTTP client with custom resilience handler for advanced scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HTTP client.</param>
        /// <param name="pipelineName">The name of the resilience pipeline.</param>
        /// <param name="configureClient">Configuration for the HTTP client.</param>
        /// <param name="configurePipeline">Configuration for the resilience pipeline.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <remarks>
        /// Use this method when you need fine-grained control over the resilience pipeline.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHttpClientWithCustomResilience("MyApi", "custom-pipeline",
        ///     client => client.BaseAddress = new Uri("https://api.example.com"),
        ///     builder =>
        ///     {
        ///         builder.AddRetry(new HttpRetryStrategyOptions
        ///         {
        ///             MaxRetryAttempts = 3,
        ///             Delay = TimeSpan.FromMilliseconds(200),
        ///             BackoffType = DelayBackoffType.Exponential,
        ///             UseJitter = true
        ///         });
        ///         
        ///         builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        ///         {
        ///             FailureRatio = 0.5,
        ///             SamplingDuration = TimeSpan.FromSeconds(30),
        ///             MinimumThroughput = 10,
        ///             BreakDuration = TimeSpan.FromSeconds(60)
        ///         });
        ///         
        ///         builder.AddTimeout(TimeSpan.FromSeconds(30));
        ///     });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddHttpClientWithCustomResilience(
            this IServiceCollection services,
            string name,
            string pipelineName,
            Action<HttpClient> configureClient,
            Action<ResiliencePipelineBuilder<HttpResponseMessage>> configurePipeline)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
            ArgumentNullException.ThrowIfNull(configureClient);
            ArgumentNullException.ThrowIfNull(configurePipeline);

            var builder = services.AddHttpClient(name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddResilienceHandler(pipelineName, configurePipeline);

            return builder;
        }

        /// <summary>
        /// Adds a typed HTTP client with standard resilience handler.
        /// </summary>
        /// <typeparam name="TClient">The typed HTTP client type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureClient">Optional configuration for the HTTP client.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddTypedHttpClientWithStandardResilience&lt;IMyApiClient&gt;(client =>
        /// {
        ///     client.BaseAddress = new Uri("https://api.example.com");
        /// });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddTypedHttpClientWithStandardResilience<TClient>(
            this IServiceCollection services,
            Action<HttpClient>? configureClient = null)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(services);

            var builder = services.AddHttpClient<TClient>();

            if (configureClient != null)
            {
                builder.ConfigureHttpClient(configureClient);
            }

            builder.AddStandardResilienceHandler();

            return builder;
        }

        /// <summary>
        /// Adds a typed HTTP client with standard resilience handler and custom options.
        /// </summary>
        /// <typeparam name="TClient">The typed HTTP client type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureClient">Configuration for the HTTP client.</param>
        /// <param name="configureResilience">Configuration for the resilience options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddTypedHttpClientWithStandardResilience<TClient>(
            this IServiceCollection services,
            Action<HttpClient> configureClient,
            Action<HttpStandardResilienceOptions> configureResilience)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureClient);
            ArgumentNullException.ThrowIfNull(configureResilience);

            var builder = services.AddHttpClient<TClient>();
            builder.ConfigureHttpClient(configureClient);
            builder.AddStandardResilienceHandler(configureResilience);

            return builder;
        }

        /// <summary>
        /// Adds a typed HTTP client with custom resilience handler.
        /// </summary>
        /// <typeparam name="TClient">The typed HTTP client type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="pipelineName">The name of the resilience pipeline.</param>
        /// <param name="configureClient">Configuration for the HTTP client.</param>
        /// <param name="configurePipeline">Configuration for the resilience pipeline.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddTypedHttpClientWithCustomResilience<TClient>(
            this IServiceCollection services,
            string pipelineName,
            Action<HttpClient> configureClient,
            Action<ResiliencePipelineBuilder<HttpResponseMessage>> configurePipeline)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
            ArgumentNullException.ThrowIfNull(configureClient);
            ArgumentNullException.ThrowIfNull(configurePipeline);

            var builder = services.AddHttpClient<TClient>();
            builder.ConfigureHttpClient(configureClient);
            builder.AddResilienceHandler(pipelineName, configurePipeline);

            return builder;
        }
    }
}

