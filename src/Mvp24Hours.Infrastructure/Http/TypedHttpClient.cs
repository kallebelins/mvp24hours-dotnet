//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Http.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http
{
    /// <summary>
    /// Strongly-typed HTTP client implementation.
    /// </summary>
    /// <typeparam name="TApi">The marker type that identifies the API.</typeparam>
    public class TypedHttpClient<TApi> : ITypedHttpClient<TApi> where TApi : class
    {
        private readonly ILogger<TypedHttpClient<TApi>>? _logger;
        private readonly IHttpClientSerializer _serializer;
        private static readonly ActivitySource ActivitySource = new("Mvp24Hours.Infrastructure.Http");

        /// <summary>
        /// Initializes a new instance of the TypedHttpClient.
        /// </summary>
        public TypedHttpClient(HttpClient httpClient)
            : this(httpClient, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TypedHttpClient with logger.
        /// </summary>
        public TypedHttpClient(HttpClient httpClient, ILogger<TypedHttpClient<TApi>>? logger)
            : this(httpClient, logger, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TypedHttpClient with custom serializer.
        /// </summary>
        public TypedHttpClient(HttpClient httpClient, ILogger<TypedHttpClient<TApi>>? logger, IHttpClientSerializer? serializer)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            _serializer = serializer ?? new JsonHttpClientSerializer();
        }

        /// <inheritdoc />
        public HttpClient HttpClient { get; }

        /// <inheritdoc />
        public Uri? BaseAddress => HttpClient.BaseAddress;

        /// <inheritdoc />
        public TimeSpan Timeout => HttpClient.Timeout;

        #region GET Methods

        /// <inheritdoc />
        public async Task<string?> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            return await SendAndReadStringAsync(HttpMethod.Get, url, null, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
        {
            return await GetAsync<T>(url, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where T : class
        {
            var content = await SendAndReadStringAsync(HttpMethod.Get, url, null, headers, cancellationToken);
            return DeserializeResponse<T>(content);
        }

        /// <inheritdoc />
        public async Task<Stream?> GetStreamAsync(string url, CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"HTTP GET Stream {typeof(TApi).Name}");
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.url", BuildUrl(url));

            try
            {
                return await HttpClient.GetStreamAsync(BuildUrl(url), cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("GetStreamAsync", url, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<byte[]?> GetBytesAsync(string url, CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"HTTP GET Bytes {typeof(TApi).Name}");
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.url", BuildUrl(url));

            try
            {
                return await HttpClient.GetByteArrayAsync(BuildUrl(url), cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("GetBytesAsync", url, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<byte[]> GetStreamAsync(
            string url,
            int bufferSize = 8192,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"HTTP GET Stream Enumerable {typeof(TApi).Name}");
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.url", BuildUrl(url));
            activity?.SetTag("buffer.size", bufferSize);

            LogRequest("GetStreamAsync (Enumerable)", url);

            HttpResponseMessage response;
            try
            {
                response = await HttpClient.GetAsync(BuildUrl(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                activity?.SetTag("http.status_code", (int)response.StatusCode);
                await EnsureSuccessResponseAsync(response, url);
            }
            catch (HttpStatusCodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("GetStreamAsync (Enumerable)", url, ex);
                throw;
            }

            if (response.Content == null)
            {
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, cancellationToken)) > 0)
            {
                if (bytesRead < bufferSize)
                {
                    var partialBuffer = new byte[bytesRead];
                    Array.Copy(buffer, partialBuffer, bytesRead);
                    yield return partialBuffer;
                }
                else
                {
                    yield return buffer;
                }
            }
        }

        #endregion

        #region POST Methods

        /// <inheritdoc />
        public async Task<string?> PostAsync(string url, object? data = null, CancellationToken cancellationToken = default)
        {
            return await SendAndReadStringAsync(HttpMethod.Post, url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PostAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class
        {
            return await PostAsync<TResponse>(url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PostAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class
        {
            var content = await SendAndReadStringAsync(HttpMethod.Post, url, data, headers, cancellationToken);
            return DeserializeResponse<TResponse>(content);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PostFormAsync<TResponse>(string url, Dictionary<string, string> formData, CancellationToken cancellationToken = default) where TResponse : class
        {
            using var activity = ActivitySource.StartActivity($"HTTP POST Form {typeof(TApi).Name}");
            activity?.SetTag("http.method", "POST");
            activity?.SetTag("http.url", BuildUrl(url));

            try
            {
                LogRequest("PostFormAsync", url);

                using var content = new FormUrlEncodedContent(formData);
                var response = await HttpClient.PostAsync(BuildUrl(url), content, cancellationToken);

                await EnsureSuccessResponseAsync(response, url);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return DeserializeResponse<TResponse>(responseContent);
            }
            catch (HttpStatusCodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("PostFormAsync", url, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TResponse?> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default) where TResponse : class
        {
            using var activity = ActivitySource.StartActivity($"HTTP POST Multipart {typeof(TApi).Name}");
            activity?.SetTag("http.method", "POST");
            activity?.SetTag("http.url", BuildUrl(url));

            try
            {
                LogRequest("PostMultipartAsync", url);

                var response = await HttpClient.PostAsync(BuildUrl(url), content, cancellationToken);

                await EnsureSuccessResponseAsync(response, url);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return DeserializeResponse<TResponse>(responseContent);
            }
            catch (HttpStatusCodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("PostMultipartAsync", url, ex);
                throw;
            }
        }

        #endregion

        #region PUT Methods

        /// <inheritdoc />
        public async Task<string?> PutAsync(string url, object? data = null, CancellationToken cancellationToken = default)
        {
            return await SendAndReadStringAsync(HttpMethod.Put, url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PutAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class
        {
            return await PutAsync<TResponse>(url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PutAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class
        {
            var content = await SendAndReadStringAsync(HttpMethod.Put, url, data, headers, cancellationToken);
            return DeserializeResponse<TResponse>(content);
        }

        #endregion

        #region PATCH Methods

        /// <inheritdoc />
        public async Task<string?> PatchAsync(string url, object? data = null, CancellationToken cancellationToken = default)
        {
            return await SendAndReadStringAsync(HttpMethod.Patch, url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PatchAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class
        {
            return await PatchAsync<TResponse>(url, data, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse?> PatchAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class
        {
            var content = await SendAndReadStringAsync(HttpMethod.Patch, url, data, headers, cancellationToken);
            return DeserializeResponse<TResponse>(content);
        }

        #endregion

        #region DELETE Methods

        /// <inheritdoc />
        public async Task<string?> DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            return await SendAndReadStringAsync(HttpMethod.Delete, url, null, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
        {
            return await DeleteAsync<T>(url, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T?> DeleteAsync<T>(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where T : class
        {
            var content = await SendAndReadStringAsync(HttpMethod.Delete, url, null, headers, cancellationToken);
            return DeserializeResponse<T>(content);
        }

        #endregion

        #region Send Methods

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"HTTP {request.Method} {typeof(TApi).Name}");
            activity?.SetTag("http.method", request.Method.ToString());
            activity?.SetTag("http.url", request.RequestUri?.ToString());

            try
            {
                LogRequest("SendAsync", request.RequestUri?.ToString() ?? "unknown");

                var response = await HttpClient.SendAsync(request, cancellationToken);

                activity?.SetTag("http.status_code", (int)response.StatusCode);

                return response;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError("SendAsync", request.RequestUri?.ToString() ?? "unknown", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class
        {
            var response = await SendAsync(request, cancellationToken);

            await EnsureSuccessResponseAsync(response, request.RequestUri?.ToString() ?? "unknown");

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return DeserializeResponse<T>(content);
        }

        #endregion

        #region Private Methods

        private async Task<string?> SendAndReadStringAsync(HttpMethod method, string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity($"HTTP {method} {typeof(TApi).Name}");
            activity?.SetTag("http.method", method.ToString());
            activity?.SetTag("http.url", BuildUrl(url));

            try
            {
                LogRequest($"{method}Async", url);

                using var request = new HttpRequestMessage(method, BuildUrl(url));

                // Add headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Add content for methods that support body
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    request.Content = _serializer.Serialize(data);
                }

                var response = await HttpClient.SendAsync(request, cancellationToken);

                activity?.SetTag("http.status_code", (int)response.StatusCode);

                await EnsureSuccessResponseAsync(response, url);

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpStatusCodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                LogError($"{method}Async", url, ex);
                throw;
            }
        }

        private string BuildUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BaseAddress?.ToString() ?? string.Empty;
            }

            // If URL is already absolute, return as is
            if (url.StartsWith("//") || url.Contains("://"))
            {
                return url;
            }

            // Combine with base address
            if (BaseAddress != null)
            {
                return new Uri(BaseAddress, url).ToString();
            }

            return url;
        }

        private async Task EnsureSuccessResponseAsync(HttpResponseMessage response, string url)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content != null
                    ? await response.Content.ReadAsStringAsync()
                    : string.Empty;

                                throw new HttpStatusCodeException(
                    response.ReasonPhrase ?? "Unknown error",
                    response.StatusCode,
                    response.RequestMessage?.Method ?? HttpMethod.Get,
                    response.RequestMessage?.RequestUri,
                    responseContent);
            }
        }

        private T? DeserializeResponse<T>(string? content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return _serializer.Deserialize<T>(content);
        }

        private void LogRequest(string method, string url)
        {
            _logger?.LogDebug("TypedHttpClient<{ApiType}>.{Method}: {Url}", typeof(TApi).Name, method, url);
                    }

        private void LogError(string method, string url, Exception ex)
        {
            _logger?.LogError(ex, "TypedHttpClient<{ApiType}>.{Method} failed: {Url}", typeof(TApi).Name, method, url);
                    }

        #endregion
    }
}

