//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Contract
{
    /// <summary>
    /// Represents a strongly-typed HTTP client for a specific API.
    /// </summary>
    /// <typeparam name="TApi">The marker type that identifies the API.</typeparam>
    public interface ITypedHttpClient<TApi> where TApi : class
    {
        /// <summary>
        /// Gets the underlying HttpClient instance.
        /// </summary>
        HttpClient HttpClient { get; }

        /// <summary>
        /// Gets the base address of the API.
        /// </summary>
        Uri? BaseAddress { get; }

        /// <summary>
        /// Gets the default timeout for requests.
        /// </summary>
        TimeSpan Timeout { get; }

        #region GET Methods

        /// <summary>
        /// Sends a GET request to the specified URL and returns the response as string.
        /// </summary>
        Task<string?> GetAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a GET request to the specified URL and returns the deserialized response.
        /// </summary>
        Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sends a GET request with custom headers.
        /// </summary>
        Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sends a GET request and returns the response as a stream.
        /// </summary>
        Task<Stream?> GetStreamAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a GET request and returns the response as byte array.
        /// </summary>
        Task<byte[]?> GetBytesAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a GET request and returns the response as an async enumerable of bytes for streaming large responses.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="bufferSize">The buffer size for reading. Default is 8192 bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of byte arrays.</returns>
        IAsyncEnumerable<byte[]> GetStreamAsync(string url, int bufferSize = 8192, CancellationToken cancellationToken = default);

        #endregion

        #region POST Methods

        /// <summary>
        /// Sends a POST request with the specified data.
        /// </summary>
        Task<string?> PostAsync(string url, object? data = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a POST request and returns the deserialized response.
        /// </summary>
        Task<TResponse?> PostAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Sends a POST request with custom headers.
        /// </summary>
        Task<TResponse?> PostAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Sends a POST request with form data.
        /// </summary>
        Task<TResponse?> PostFormAsync<TResponse>(string url, Dictionary<string, string> formData, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Sends a POST request with multipart form data (file upload).
        /// </summary>
        Task<TResponse?> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default) where TResponse : class;

        #endregion

        #region PUT Methods

        /// <summary>
        /// Sends a PUT request with the specified data.
        /// </summary>
        Task<string?> PutAsync(string url, object? data = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a PUT request and returns the deserialized response.
        /// </summary>
        Task<TResponse?> PutAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Sends a PUT request with custom headers.
        /// </summary>
        Task<TResponse?> PutAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class;

        #endregion

        #region PATCH Methods

        /// <summary>
        /// Sends a PATCH request with the specified data.
        /// </summary>
        Task<string?> PatchAsync(string url, object? data = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a PATCH request and returns the deserialized response.
        /// </summary>
        Task<TResponse?> PatchAsync<TResponse>(string url, object? data = null, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Sends a PATCH request with custom headers.
        /// </summary>
        Task<TResponse?> PatchAsync<TResponse>(string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where TResponse : class;

        #endregion

        #region DELETE Methods

        /// <summary>
        /// Sends a DELETE request to the specified URL.
        /// </summary>
        Task<string?> DeleteAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a DELETE request and returns the deserialized response.
        /// </summary>
        Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sends a DELETE request with custom headers.
        /// </summary>
        Task<T?> DeleteAsync<T>(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken = default) where T : class;

        #endregion

        #region Send Methods

        /// <summary>
        /// Sends a custom HTTP request.
        /// </summary>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a custom HTTP request and returns the deserialized response.
        /// </summary>
        Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class;

        #endregion
    }
}

