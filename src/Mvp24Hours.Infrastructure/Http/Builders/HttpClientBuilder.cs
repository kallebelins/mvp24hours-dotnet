//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Http.Builders
{
    /// <summary>
    /// Fluent builder for configuring HTTP client options.
    /// </summary>
    public class HttpClientBuilder
    {
        private readonly HttpClientOptions _options = new();

        /// <summary>
        /// Creates a new instance of the HTTP client builder.
        /// </summary>
        public static HttpClientBuilder Create() => new();

        /// <summary>
        /// Creates a new instance of the HTTP client builder with a name.
        /// </summary>
        public static HttpClientBuilder Create(string name) => new HttpClientBuilder().WithName(name);

        /// <summary>
        /// Sets the name of the HTTP client.
        /// </summary>
        public HttpClientBuilder WithName(string name)
        {
            _options.Name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        /// <summary>
        /// Sets the base address of the HTTP client.
        /// </summary>
        public HttpClientBuilder WithBaseAddress(string baseAddress)
        {
            _options.BaseAddress = new Uri(baseAddress ?? throw new ArgumentNullException(nameof(baseAddress)));
            return this;
        }

        /// <summary>
        /// Sets the base address of the HTTP client.
        /// </summary>
        public HttpClientBuilder WithBaseAddress(Uri baseAddress)
        {
            _options.BaseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            return this;
        }

        /// <summary>
        /// Sets the timeout for requests.
        /// </summary>
        public HttpClientBuilder WithTimeout(TimeSpan timeout)
        {
            _options.Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the timeout for requests in seconds.
        /// </summary>
        public HttpClientBuilder WithTimeout(int seconds)
        {
            _options.Timeout = TimeSpan.FromSeconds(seconds);
            return this;
        }

        /// <summary>
        /// Adds a default header to all requests.
        /// </summary>
        public HttpClientBuilder WithDefaultHeader(string name, string value)
        {
            _options.DefaultHeaders[name] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple default headers to all requests.
        /// </summary>
        public HttpClientBuilder WithDefaultHeaders(Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                _options.DefaultHeaders[header.Key] = header.Value;
            }
            return this;
        }

        /// <summary>
        /// Sets the Accept header value.
        /// </summary>
        public HttpClientBuilder WithAcceptHeader(string accept)
        {
            _options.AcceptHeader = accept;
            return this;
        }

        /// <summary>
        /// Sets the User-Agent header value.
        /// </summary>
        public HttpClientBuilder WithUserAgent(string userAgent)
        {
            _options.UserAgent = userAgent;
            return this;
        }

        /// <summary>
        /// Adds a Bearer token authorization header.
        /// </summary>
        public HttpClientBuilder WithBearerToken(string token)
        {
            _options.DefaultHeaders["Authorization"] = $"Bearer {token}";
            return this;
        }

        /// <summary>
        /// Adds an API key header.
        /// </summary>
        public HttpClientBuilder WithApiKey(string apiKey, string headerName = "X-Api-Key")
        {
            _options.DefaultHeaders[headerName] = apiKey;
            return this;
        }

        /// <summary>
        /// Adds Basic authentication.
        /// </summary>
        public HttpClientBuilder WithBasicAuth(string username, string password)
        {
            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
            _options.DefaultHeaders["Authorization"] = $"Basic {credentials}";
            return this;
        }

        #region Certificate Configuration

        /// <summary>
        /// Configures the client certificate from a file.
        /// </summary>
        public HttpClientBuilder WithCertificateFromFile(string filePath, string? password = null)
        {
            _options.Certificate = new CertificateOptions
            {
                FilePath = filePath,
                Password = password
            };
            return this;
        }

        /// <summary>
        /// Configures the client certificate from a base64 string.
        /// </summary>
        public HttpClientBuilder WithCertificateFromBase64(string base64Certificate, string? password = null)
        {
            _options.Certificate = new CertificateOptions
            {
                Base64Certificate = base64Certificate,
                Password = password
            };
            return this;
        }

        /// <summary>
        /// Configures the client certificate from the certificate store by thumbprint.
        /// </summary>
        public HttpClientBuilder WithCertificateFromStore(
            string thumbprint,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            StoreName storeName = StoreName.My)
        {
            _options.Certificate = new CertificateOptions
            {
                Thumbprint = thumbprint,
                StoreLocation = storeLocation,
                StoreName = storeName
            };
            return this;
        }

        /// <summary>
        /// Configures the client certificate from the certificate store by subject name.
        /// </summary>
        public HttpClientBuilder WithCertificateFromStoreBySubject(
            string subjectName,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            StoreName storeName = StoreName.My)
        {
            _options.Certificate = new CertificateOptions
            {
                SubjectName = subjectName,
                StoreLocation = storeLocation,
                StoreName = storeName
            };
            return this;
        }

        /// <summary>
        /// Disables server certificate validation (use with caution in development only).
        /// </summary>
        public HttpClientBuilder DisableServerCertificateValidation()
        {
            _options.ValidateServerCertificate = false;
            return this;
        }

        #endregion

        #region Retry Policy Configuration

        /// <summary>
        /// Configures the retry policy.
        /// </summary>
        public HttpClientBuilder WithRetry(Action<RetryPolicyOptions> configure)
        {
            _options.RetryPolicy ??= new RetryPolicyOptions();
            configure(_options.RetryPolicy);
            return this;
        }

        /// <summary>
        /// Configures the retry policy with default options.
        /// </summary>
        public HttpClientBuilder WithRetry(int maxRetries = 3, TimeSpan? initialDelay = null)
        {
            _options.RetryPolicy = new RetryPolicyOptions
            {
                Enabled = true,
                MaxRetries = maxRetries,
                InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1)
            };
            return this;
        }

        /// <summary>
        /// Configures exponential backoff retry policy.
        /// </summary>
        public HttpClientBuilder WithExponentialRetry(int maxRetries = 3, TimeSpan? initialDelay = null, TimeSpan? maxDelay = null)
        {
            _options.RetryPolicy = new RetryPolicyOptions
            {
                Enabled = true,
                MaxRetries = maxRetries,
                InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1),
                MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30),
                BackoffType = BackoffType.Exponential
            };
            return this;
        }

        /// <summary>
        /// Disables the retry policy.
        /// </summary>
        public HttpClientBuilder WithoutRetry()
        {
            _options.RetryPolicy = new RetryPolicyOptions { Enabled = false };
            return this;
        }

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Configures the circuit breaker policy.
        /// </summary>
        public HttpClientBuilder WithCircuitBreaker(Action<CircuitBreakerPolicyOptions> configure)
        {
            _options.CircuitBreakerPolicy ??= new CircuitBreakerPolicyOptions();
            configure(_options.CircuitBreakerPolicy);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker policy with default options.
        /// </summary>
        public HttpClientBuilder WithCircuitBreaker(int failureThreshold = 5, TimeSpan? breakDuration = null)
        {
            _options.CircuitBreakerPolicy = new CircuitBreakerPolicyOptions
            {
                Enabled = true,
                FailureThreshold = failureThreshold,
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30)
            };
            return this;
        }

        /// <summary>
        /// Disables the circuit breaker policy.
        /// </summary>
        public HttpClientBuilder WithoutCircuitBreaker()
        {
            _options.CircuitBreakerPolicy = new CircuitBreakerPolicyOptions { Enabled = false };
            return this;
        }

        #endregion

        #region Timeout Policy Configuration

        /// <summary>
        /// Configures the timeout policy.
        /// </summary>
        public HttpClientBuilder WithTimeoutPolicy(TimeSpan timeout)
        {
            _options.TimeoutPolicy = new TimeoutPolicyOptions
            {
                Enabled = true,
                Timeout = timeout
            };
            return this;
        }

        /// <summary>
        /// Disables the timeout policy.
        /// </summary>
        public HttpClientBuilder WithoutTimeoutPolicy()
        {
            _options.TimeoutPolicy = new TimeoutPolicyOptions { Enabled = false };
            return this;
        }

        #endregion

        #region Handler Lifetime Configuration

        /// <summary>
        /// Sets the handler lifetime.
        /// </summary>
        public HttpClientBuilder WithHandlerLifetime(TimeSpan lifetime)
        {
            _options.HandlerLifetime = lifetime;
            return this;
        }

        #endregion

        #region Header Propagation Configuration

        /// <summary>
        /// Enables correlation ID header propagation.
        /// </summary>
        public HttpClientBuilder WithCorrelationIdPropagation(bool enabled = true)
        {
            _options.PropagateCorrelationId = enabled;
            return this;
        }

        /// <summary>
        /// Enables authorization header propagation.
        /// </summary>
        public HttpClientBuilder WithAuthorizationPropagation(bool enabled = true)
        {
            _options.PropagateAuthorization = enabled;
            return this;
        }

        /// <summary>
        /// Adds headers to propagate to downstream services.
        /// </summary>
        public HttpClientBuilder WithHeaderPropagation(params string[] headers)
        {
            _options.PropagateHeaders.AddRange(headers);
            return this;
        }

        #endregion

        #region Logging Configuration

        /// <summary>
        /// Enables request/response logging.
        /// </summary>
        public HttpClientBuilder WithLogging(bool enabled = true)
        {
            _options.EnableLogging = enabled;
            return this;
        }

        /// <summary>
        /// Configures logging options.
        /// </summary>
        public HttpClientBuilder WithLogging(Action<HttpLoggingOptions> configure)
        {
            _options.EnableLogging = true;
            _options.LoggingOptions ??= new HttpLoggingOptions();
            configure(_options.LoggingOptions);
            return this;
        }

        /// <summary>
        /// Enables detailed logging (headers and body).
        /// </summary>
        public HttpClientBuilder WithDetailedLogging()
        {
            _options.EnableLogging = true;
            _options.LoggingOptions = new HttpLoggingOptions
            {
                LogRequestHeaders = true,
                LogRequestBody = true,
                LogResponseHeaders = true,
                LogResponseBody = true
            };
            return this;
        }

        /// <summary>
        /// Disables logging.
        /// </summary>
        public HttpClientBuilder WithoutLogging()
        {
            _options.EnableLogging = false;
            return this;
        }

        #endregion

        #region Telemetry Configuration

        /// <summary>
        /// Enables telemetry.
        /// </summary>
        public HttpClientBuilder WithTelemetry(bool enabled = true)
        {
            _options.EnableTelemetry = enabled;
            return this;
        }

        /// <summary>
        /// Disables telemetry.
        /// </summary>
        public HttpClientBuilder WithoutTelemetry()
        {
            _options.EnableTelemetry = false;
            return this;
        }

        #endregion

        #region Proxy Configuration

        /// <summary>
        /// Configures a proxy for the HTTP client.
        /// </summary>
        public HttpClientBuilder WithProxy(string address, string? username = null, string? password = null)
        {
            _options.Proxy = new ProxyOptions
            {
                Enabled = true,
                Address = address,
                Username = username,
                Password = password
            };
            return this;
        }

        /// <summary>
        /// Configures proxy options.
        /// </summary>
        public HttpClientBuilder WithProxy(Action<ProxyOptions> configure)
        {
            _options.Proxy ??= new ProxyOptions { Enabled = true };
            configure(_options.Proxy);
            return this;
        }

        #endregion

        #region HTTP Version Configuration

        /// <summary>
        /// Sets the HTTP version to use.
        /// </summary>
        public HttpClientBuilder WithHttpVersion(Version version)
        {
            _options.HttpVersion = version;
            return this;
        }

        /// <summary>
        /// Uses HTTP/2.
        /// </summary>
        public HttpClientBuilder UseHttp2()
        {
            _options.HttpVersion = new Version(2, 0);
            return this;
        }

        /// <summary>
        /// Uses HTTP/1.1.
        /// </summary>
        public HttpClientBuilder UseHttp11()
        {
            _options.HttpVersion = new Version(1, 1);
            return this;
        }

        #endregion

        #region Redirect Configuration

        /// <summary>
        /// Configures redirect following behavior.
        /// </summary>
        public HttpClientBuilder WithRedirects(bool follow = true, int maxRedirects = 50)
        {
            _options.FollowRedirects = follow;
            _options.MaxRedirects = maxRedirects;
            return this;
        }

        /// <summary>
        /// Disables automatic redirect following.
        /// </summary>
        public HttpClientBuilder WithoutRedirects()
        {
            _options.FollowRedirects = false;
            return this;
        }

        #endregion

        #region Cookie Configuration

        /// <summary>
        /// Configures cookie handling.
        /// </summary>
        public HttpClientBuilder WithCookies(bool enabled = true)
        {
            _options.UseCookies = enabled;
            return this;
        }

        /// <summary>
        /// Disables cookie handling.
        /// </summary>
        public HttpClientBuilder WithoutCookies()
        {
            _options.UseCookies = false;
            return this;
        }

        #endregion

        #region Decompression Configuration

        /// <summary>
        /// Enables automatic decompression.
        /// </summary>
        public HttpClientBuilder WithDecompression(bool enabled = true)
        {
            _options.EnableDecompression = enabled;
            return this;
        }

        /// <summary>
        /// Disables automatic decompression.
        /// </summary>
        public HttpClientBuilder WithoutDecompression()
        {
            _options.EnableDecompression = false;
            return this;
        }

        #endregion

        /// <summary>
        /// Builds the HTTP client options.
        /// </summary>
        public HttpClientOptions Build() => _options;

        /// <summary>
        /// Implicit conversion to HttpClientOptions.
        /// </summary>
        public static implicit operator HttpClientOptions(HttpClientBuilder builder) => builder.Build();
    }
}

