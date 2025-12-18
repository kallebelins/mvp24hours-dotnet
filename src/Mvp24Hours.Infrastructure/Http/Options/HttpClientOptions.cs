//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Http.Options
{
    /// <summary>
    /// Configuration options for HTTP clients.
    /// </summary>
    public class HttpClientOptions
    {
        /// <summary>
        /// Gets or sets the name of the HTTP client.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base address of the HTTP client.
        /// </summary>
        public Uri? BaseAddress { get; set; }

        /// <summary>
        /// Gets or sets the timeout for requests. Default is 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the default request headers.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; } = new();

        /// <summary>
        /// Gets or sets the maximum response content buffer size. Default is 2GB.
        /// </summary>
        public long MaxResponseContentBufferSize { get; set; } = 2147483647L;

        /// <summary>
        /// Gets or sets whether to enable automatic decompression. Default is true.
        /// </summary>
        public bool EnableDecompression { get; set; } = true;

        /// <summary>
        /// Gets or sets the SSL/TLS certificate options.
        /// </summary>
        public CertificateOptions? Certificate { get; set; }

        /// <summary>
        /// Gets or sets the retry policy options.
        /// </summary>
        public RetryPolicyOptions? RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the circuit breaker policy options.
        /// </summary>
        public CircuitBreakerPolicyOptions? CircuitBreakerPolicy { get; set; }

        /// <summary>
        /// Gets or sets the timeout policy options.
        /// </summary>
        public TimeoutPolicyOptions? TimeoutPolicy { get; set; }

        /// <summary>
        /// Gets or sets the handler lifetime. Default is 2 minutes.
        /// </summary>
        public TimeSpan HandlerLifetime { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets whether to propagate correlation ID headers. Default is true.
        /// </summary>
        public bool PropagateCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to propagate authorization headers. Default is false.
        /// </summary>
        public bool PropagateAuthorization { get; set; } = false;

        /// <summary>
        /// Gets or sets custom headers to propagate.
        /// </summary>
        public List<string> PropagateHeaders { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable request/response logging. Default is true.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the logging options.
        /// </summary>
        public HttpLoggingOptions? LoggingOptions { get; set; }

        /// <summary>
        /// Gets or sets whether to enable telemetry. Default is true.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the user agent string.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the accept header value. Default is "application/json".
        /// </summary>
        public string AcceptHeader { get; set; } = "application/json";

        /// <summary>
        /// Gets or sets whether to follow redirects. Default is true.
        /// </summary>
        public bool FollowRedirects { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of redirects. Default is 50.
        /// </summary>
        public int MaxRedirects { get; set; } = 50;

        /// <summary>
        /// Gets or sets whether to use cookies. Default is true.
        /// </summary>
        public bool UseCookies { get; set; } = true;

        /// <summary>
        /// Gets or sets the HTTP version to use.
        /// </summary>
        public Version? HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets whether to validate the server certificate. Default is true.
        /// </summary>
        public bool ValidateServerCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the proxy settings.
        /// </summary>
        public ProxyOptions? Proxy { get; set; }

        /// <summary>
        /// Gets or sets the authentication options.
        /// </summary>
        public DelegatingHandlers.AuthenticationOptions? Authentication { get; set; }

        /// <summary>
        /// Gets or sets the request compression options.
        /// </summary>
        public DelegatingHandlers.CompressionHandlerOptions? Compression { get; set; }

        /// <summary>
        /// Gets or sets the telemetry handler options.
        /// </summary>
        public DelegatingHandlers.TelemetryHandlerOptions? TelemetryOptions { get; set; }
    }

    /// <summary>
    /// SSL/TLS certificate options for HTTP clients.
    /// </summary>
    public class CertificateOptions
    {
        /// <summary>
        /// Gets or sets the certificate file path.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the certificate password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint for certificate store lookup.
        /// </summary>
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Gets or sets the certificate store location. Default is CurrentUser.
        /// </summary>
        public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;

        /// <summary>
        /// Gets or sets the certificate store name. Default is My.
        /// </summary>
        public StoreName StoreName { get; set; } = StoreName.My;

        /// <summary>
        /// Gets or sets the certificate subject name for certificate store lookup.
        /// </summary>
        public string? SubjectName { get; set; }

        /// <summary>
        /// Gets or sets the certificate as a base64-encoded string.
        /// </summary>
        public string? Base64Certificate { get; set; }

        /// <summary>
        /// Gets or sets the X509KeyStorageFlags. Default is DefaultKeySet.
        /// </summary>
        public X509KeyStorageFlags KeyStorageFlags { get; set; } = X509KeyStorageFlags.DefaultKeySet;
    }

    /// <summary>
    /// Retry policy options for HTTP clients.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets whether the retry policy is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts. Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retries. Default is 1 second.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum delay between retries. Default is 30 seconds.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the backoff type. Default is Exponential.
        /// </summary>
        public BackoffType BackoffType { get; set; } = BackoffType.Exponential;

        /// <summary>
        /// Gets or sets the jitter factor for delay randomization (0.0 to 1.0). Default is 0.1.
        /// </summary>
        public double JitterFactor { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the HTTP status codes that should trigger a retry.
        /// Default includes: 408, 429, 500, 502, 503, 504.
        /// </summary>
        public List<int> RetryStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };

        /// <summary>
        /// Gets or sets whether to retry on timeout exceptions. Default is true.
        /// </summary>
        public bool RetryOnTimeout { get; set; } = true;
    }

    /// <summary>
    /// Circuit breaker policy options for HTTP clients.
    /// </summary>
    public class CircuitBreakerPolicyOptions
    {
        /// <summary>
        /// Gets or sets whether the circuit breaker is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of consecutive failures before opening the circuit. Default is 5.
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the sampling duration for failure counting. Default is 30 seconds.
        /// </summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the minimum throughput before the circuit breaker can activate. Default is 10.
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Gets or sets the duration the circuit remains open. Default is 30 seconds.
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the failure ratio threshold (0.0 to 1.0). Default is 0.5.
        /// </summary>
        public double FailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Callback invoked when the circuit breaker opens.
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnBreak { get; set; }

        /// <summary>
        /// Callback invoked when the circuit breaker closes (resets).
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnReset { get; set; }

        /// <summary>
        /// Callback invoked when the circuit breaker enters half-open state.
        /// </summary>
        public Action<CircuitBreakerStateChangeInfo>? OnHalfOpen { get; set; }
    }

    /// <summary>
    /// Information about a circuit breaker state change.
    /// </summary>
    public class CircuitBreakerStateChangeInfo
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new circuit state.
        /// </summary>
        public Polly.CircuitBreaker.CircuitState NewState { get; set; }

        /// <summary>
        /// Gets or sets the break duration (only applicable when opening).
        /// </summary>
        public TimeSpan? BreakDuration { get; set; }

        /// <summary>
        /// Gets or sets the reason for the state change.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the state change.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Timeout policy options for HTTP clients.
    /// </summary>
    public class TimeoutPolicyOptions
    {
        /// <summary>
        /// Gets or sets whether the timeout policy is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the request timeout. Default is 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// HTTP logging options.
    /// </summary>
    public class HttpLoggingOptions
    {
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
        /// Gets or sets the maximum body size to log. Default is 4KB.
        /// </summary>
        public int MaxBodyLogSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the headers to exclude from logging.
        /// Default includes: Authorization, Cookie, Set-Cookie.
        /// </summary>
        public List<string> SensitiveHeaders { get; set; } = new() { "Authorization", "Cookie", "Set-Cookie", "X-Api-Key" };

        /// <summary>
        /// Gets or sets the mask value for sensitive headers. Default is "***".
        /// </summary>
        public string MaskValue { get; set; } = "***";
    }

    /// <summary>
    /// Proxy options for HTTP clients.
    /// </summary>
    public class ProxyOptions
    {
        /// <summary>
        /// Gets or sets whether proxy is enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the proxy address.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets whether to bypass the proxy for local addresses. Default is true.
        /// </summary>
        public bool BypassOnLocal { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of addresses to bypass the proxy for.
        /// </summary>
        public List<string> BypassList { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to use default credentials. Default is false.
        /// </summary>
        public bool UseDefaultCredentials { get; set; } = false;

        /// <summary>
        /// Gets or sets the proxy username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the proxy password.
        /// </summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// Defines the backoff strategy type for retry policies.
    /// </summary>
    public enum BackoffType
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        Constant,

        /// <summary>
        /// Linear increase in delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay between retries.
        /// </summary>
        Exponential,

        /// <summary>
        /// Exponential with decorrelated jitter for better distribution.
        /// </summary>
        DecorrelatedJitter
    }
}

