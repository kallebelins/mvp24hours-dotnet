//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Http.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Extensions
{
    /// <summary>
    /// Extension methods for HttpClient to simplify serialization and deserialization.
    /// </summary>
    public static class HttpClientSerializationExtensions
    {
        /// <summary>
        /// Sends a GET request and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<T?> GetAsync<T>(
            this HttpClient client,
            string requestUri,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var response = await client.GetAsync(requestUri, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<T>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a GET request and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<T?> GetAsync<T>(
            this HttpClient client,
            Uri requestUri,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var response = await client.GetAsync(requestUri, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri.ToString());
            return await response.ReadAsAsync<T>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a POST request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PostAsync<TResponse>(
            this HttpClient client,
            string requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var response = await client.PostAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a POST request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PostAsync<TResponse>(
            this HttpClient client,
            Uri requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var response = await client.PostAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri.ToString());
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a PUT request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PutAsync<TResponse>(
            this HttpClient client,
            string requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var response = await client.PutAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a PUT request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PutAsync<TResponse>(
            this HttpClient client,
            Uri requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var response = await client.PutAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri.ToString());
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a DELETE request and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<T?> DeleteAsync<T>(
            this HttpClient client,
            string requestUri,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var response = await client.DeleteAsync(requestUri, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<T>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a DELETE request and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<T?> DeleteAsync<T>(
            this HttpClient client,
            Uri requestUri,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var response = await client.DeleteAsync(requestUri, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri.ToString());
            return await response.ReadAsAsync<T>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PatchAsync<TResponse>(
            this HttpClient client,
            string requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content };
            var response = await client.SendAsync(request, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request with serialized content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PatchAsync<TResponse>(
            this HttpClient client,
            Uri requestUri,
            object? value = null,
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            HttpContent? content = value != null ? serializer.Serialize(value) : null;
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content };
            var response = await client.SendAsync(request, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri.ToString());
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a GET request and returns the response as an async enumerable of bytes for streaming large responses.
        /// </summary>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="bufferSize">The buffer size for reading. Default is 8192 bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of byte arrays.</returns>
        public static async IAsyncEnumerable<byte[]> GetStreamAsync(
            this HttpClient client,
            string requestUri,
            int bufferSize = 8192,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);

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

        /// <summary>
        /// Sends a GET request and returns the response as an async enumerable of bytes for streaming large responses.
        /// </summary>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="bufferSize">The buffer size for reading. Default is 8192 bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of byte arrays.</returns>
        public static async IAsyncEnumerable<byte[]> GetStreamAsync(
            this HttpClient client,
            Uri requestUri,
            int bufferSize = 8192,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var chunk in client.GetStreamAsync(requestUri.ToString(), bufferSize, cancellationToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Sends a POST request with streaming content and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="stream">The stream to send.</param>
        /// <param name="contentType">The content type. Default is "application/octet-stream".</param>
        /// <param name="serializer">The serializer to use for response. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PostStreamAsync<TResponse>(
            this HttpClient client,
            string requestUri,
            Stream stream,
            string contentType = "application/octet-stream",
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            var response = await client.PostAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }

        /// <summary>
        /// Sends a POST request with streaming content from an async enumerable and deserializes the response.
        /// </summary>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="stream">The async enumerable of bytes to send.</param>
        /// <param name="contentType">The content type. Default is "application/octet-stream".</param>
        /// <param name="serializer">The serializer to use for response. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        public static async Task<TResponse?> PostStreamAsync<TResponse>(
            this HttpClient client,
            string requestUri,
            IAsyncEnumerable<byte[]> stream,
            string contentType = "application/octet-stream",
            IHttpContentSerializer? serializer = null,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            serializer ??= new JsonHttpClientSerializer();
            var content = new PushStreamContent(async (outputStream, httpContent, transportContext) =>
            {
                await foreach (var chunk in stream.WithCancellation(cancellationToken))
                {
                    await outputStream.WriteAsync(chunk, cancellationToken);
                }
                await outputStream.FlushAsync(cancellationToken);
            });
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            var response = await client.PostAsync(requestUri, content, cancellationToken);
            await response.EnsureSuccessStatusCodeAsync(requestUri);
            return await response.ReadAsAsync<TResponse>(serializer, cancellationToken);
        }
    }

    /// <summary>
    /// Helper class for creating push stream content from async enumerable.
    /// </summary>
    internal class PushStreamContent : HttpContent
    {
        private readonly Func<Stream, HttpContent, System.Net.TransportContext?, Task> _onStreamAvailable;

        public PushStreamContent(Func<Stream, HttpContent, System.Net.TransportContext?, Task> onStreamAvailable)
        {
            _onStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
        }

        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            await _onStreamAvailable(stream, this, context);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}

