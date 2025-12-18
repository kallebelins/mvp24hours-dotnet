//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler for adding authentication headers to HTTP requests.
    /// Supports Bearer token, API Key, and Basic authentication schemes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler automatically adds authentication headers to outgoing HTTP requests
    /// based on the configured authentication scheme and credentials.
    /// </para>
    /// <para>
    /// <strong>Supported Authentication Schemes:</strong>
    /// <list type="bullet">
    /// <item><strong>Bearer:</strong> JWT or OAuth2 token authentication</item>
    /// <item><strong>ApiKey:</strong> API key authentication via header</item>
    /// <item><strong>Basic:</strong> HTTP Basic authentication (username:password)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bearer token authentication
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler(sp => new AuthenticationDelegatingHandler(
    ///         sp.GetRequiredService&lt;ILogger&lt;AuthenticationDelegatingHandler&gt;&gt;(),
    ///         AuthenticationScheme.Bearer,
    ///         tokenProvider: () => Task.FromResult("your-jwt-token")));
    /// 
    /// // API Key authentication
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler(sp => new AuthenticationDelegatingHandler(
    ///         sp.GetRequiredService&lt;ILogger&lt;AuthenticationDelegatingHandler&gt;&gt;(),
    ///         AuthenticationScheme.ApiKey,
    ///         apiKey: "your-api-key",
    ///         apiKeyHeaderName: "X-API-Key"));
    /// 
    /// // Basic authentication
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler(sp => new AuthenticationDelegatingHandler(
    ///         sp.GetRequiredService&lt;ILogger&lt;AuthenticationDelegatingHandler&gt;&gt;(),
    ///         AuthenticationScheme.Basic,
    ///         username: "user",
    ///         password: "pass"));
    /// </code>
    /// </example>
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<AuthenticationDelegatingHandler> _logger;
        private readonly AuthenticationOptions _options;
        private readonly Func<Task<string?>>? _tokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The authentication options.</param>
        public AuthenticationDelegatingHandler(
            ILogger<AuthenticationDelegatingHandler> logger,
            AuthenticationOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _tokenProvider = options.TokenProvider;
        }

        /// <summary>
        /// Initializes a new instance for Bearer token authentication.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="tokenProvider">Function that provides the authentication token.</param>
        /// <param name="apiKey">The API key (for ApiKey scheme).</param>
        /// <param name="apiKeyHeaderName">The header name for API key (default: X-API-Key).</param>
        /// <param name="username">Username for Basic authentication.</param>
        /// <param name="password">Password for Basic authentication.</param>
        public AuthenticationDelegatingHandler(
            ILogger<AuthenticationDelegatingHandler> logger,
            AuthenticationScheme scheme,
            Func<Task<string?>>? tokenProvider = null,
            string? apiKey = null,
            string apiKeyHeaderName = "X-API-Key",
            string? username = null,
            string? password = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenProvider = tokenProvider;
            _options = new AuthenticationOptions
            {
                Scheme = scheme,
                ApiKey = apiKey,
                ApiKeyHeaderName = apiKeyHeaderName,
                Username = username,
                Password = password,
                TokenProvider = tokenProvider
            };
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                await ApplyAuthenticationAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply authentication header to request {Method} {Uri}",
                    request.Method, request.RequestUri);

                if (_options.ThrowOnAuthenticationFailure)
                {
                    throw;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task ApplyAuthenticationAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            switch (_options.Scheme)
            {
                case AuthenticationScheme.Bearer:
                    await ApplyBearerAuthenticationAsync(request, cancellationToken);
                    break;

                case AuthenticationScheme.ApiKey:
                    ApplyApiKeyAuthentication(request);
                    break;

                case AuthenticationScheme.Basic:
                    ApplyBasicAuthentication(request);
                    break;

                case AuthenticationScheme.None:
                default:
                    break;
            }
        }

        private async Task ApplyBearerAuthenticationAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_tokenProvider is null && string.IsNullOrWhiteSpace(_options.StaticToken))
            {
                _logger.LogWarning("No token provider or static token configured for Bearer authentication");
                return;
            }

            string? token;

            if (_tokenProvider != null)
            {
                token = await _tokenProvider();
            }
            else
            {
                token = _options.StaticToken;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token provider returned null or empty token for request to {Uri}",
                    request.RequestUri);
                return;
            }

            // Remove "Bearer " prefix if already present
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token["Bearer ".Length..];
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogDebug("Applied Bearer authentication to request {Method} {Uri}",
                request.Method, request.RequestUri);
        }

        private void ApplyApiKeyAuthentication(HttpRequestMessage request)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("No API key configured for API Key authentication");
                return;
            }

            var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
                ? "X-API-Key"
                : _options.ApiKeyHeaderName;

            if (_options.ApiKeyLocation == ApiKeyLocation.Header)
            {
                request.Headers.TryAddWithoutValidation(headerName, _options.ApiKey);
            }
            else if (_options.ApiKeyLocation == ApiKeyLocation.QueryString)
            {
                var uri = request.RequestUri!;
                var separator = string.IsNullOrWhiteSpace(uri.Query) ? "?" : "&";
                var paramName = string.IsNullOrWhiteSpace(_options.ApiKeyQueryParamName)
                    ? "api_key"
                    : _options.ApiKeyQueryParamName;
                request.RequestUri = new Uri($"{uri.OriginalString}{separator}{paramName}={Uri.EscapeDataString(_options.ApiKey)}");
            }

            _logger.LogDebug("Applied API Key authentication to request {Method} {Uri}",
                request.Method, request.RequestUri);
        }

        private void ApplyBasicAuthentication(HttpRequestMessage request)
        {
            if (string.IsNullOrWhiteSpace(_options.Username))
            {
                _logger.LogWarning("No username configured for Basic authentication");
                return;
            }

            var credentials = $"{_options.Username}:{_options.Password ?? string.Empty}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

            _logger.LogDebug("Applied Basic authentication to request {Method} {Uri}",
                request.Method, request.RequestUri);
        }
    }

    /// <summary>
    /// Defines the authentication scheme for HTTP requests.
    /// </summary>
    public enum AuthenticationScheme
    {
        /// <summary>
        /// No authentication.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bearer token authentication (JWT, OAuth2).
        /// </summary>
        Bearer = 1,

        /// <summary>
        /// API Key authentication via header or query string.
        /// </summary>
        ApiKey = 2,

        /// <summary>
        /// HTTP Basic authentication (username:password).
        /// </summary>
        Basic = 3
    }

    /// <summary>
    /// Defines where the API key should be placed in the request.
    /// </summary>
    public enum ApiKeyLocation
    {
        /// <summary>
        /// Place API key in request header.
        /// </summary>
        Header = 0,

        /// <summary>
        /// Place API key in query string.
        /// </summary>
        QueryString = 1
    }

    /// <summary>
    /// Configuration options for authentication.
    /// </summary>
    public class AuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the authentication scheme. Default is None.
        /// </summary>
        public AuthenticationScheme Scheme { get; set; } = AuthenticationScheme.None;

        /// <summary>
        /// Gets or sets the function that provides Bearer tokens.
        /// Called before each request to get a fresh token.
        /// </summary>
        public Func<Task<string?>>? TokenProvider { get; set; }

        /// <summary>
        /// Gets or sets a static token for Bearer authentication.
        /// Use TokenProvider for dynamic tokens.
        /// </summary>
        public string? StaticToken { get; set; }

        /// <summary>
        /// Gets or sets the API key for API Key authentication.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the header name for API key. Default is "X-API-Key".
        /// </summary>
        public string ApiKeyHeaderName { get; set; } = "X-API-Key";

        /// <summary>
        /// Gets or sets where to place the API key. Default is Header.
        /// </summary>
        public ApiKeyLocation ApiKeyLocation { get; set; } = ApiKeyLocation.Header;

        /// <summary>
        /// Gets or sets the query string parameter name for API key.
        /// Only used when ApiKeyLocation is QueryString. Default is "api_key".
        /// </summary>
        public string ApiKeyQueryParamName { get; set; } = "api_key";

        /// <summary>
        /// Gets or sets the username for Basic authentication.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the password for Basic authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets whether to throw exceptions on authentication failures.
        /// Default is false (continues without authentication).
        /// </summary>
        public bool ThrowOnAuthenticationFailure { get; set; } = false;
    }
}

