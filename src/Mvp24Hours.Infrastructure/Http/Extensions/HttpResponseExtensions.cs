//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Http.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Extensions
{
    /// <summary>
    /// Extension methods for HttpResponseMessage to simplify deserialization.
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Ensures the response is successful, otherwise throws an exception.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response, string? url = null)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

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

        /// <summary>
        /// Deserializes the response content to the specified type using the default serializer.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized object, or null if content is empty.</returns>
        public static async Task<T?> ReadAsAsync<T>(
            this HttpResponseMessage response,
            IHttpClientSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (response.Content == null)
            {
                return default;
            }

            serializer ??= new Serializers.JsonHttpClientSerializer();
            return await serializer.DeserializeAsync<T>(response.Content, cancellationToken);
        }

        /// <summary>
        /// Deserializes the response content to the specified type, ensuring success status code first.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <param name="serializer">The serializer to use. If null, uses default JSON serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async Task<T?> ReadAsAsync<T>(
            this HttpResponseMessage response,
            string? url,
            IHttpClientSerializer? serializer = null,
            CancellationToken cancellationToken = default) where T : class
        {
            await response.EnsureSuccessStatusCodeAsync(url);
            return await response.ReadAsAsync<T>(serializer, cancellationToken);
        }

        /// <summary>
        /// Reads the response content as a string, ensuring success status code first.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response content as string.</returns>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async Task<string> ReadAsStringAsync(
            this HttpResponseMessage response,
            string? url,
            CancellationToken cancellationToken = default)
        {
            await response.EnsureSuccessStatusCodeAsync(url);
            return response.Content != null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : string.Empty;
        }

        /// <summary>
        /// Reads the response content as a byte array, ensuring success status code first.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response content as byte array.</returns>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async Task<byte[]> ReadAsByteArrayAsync(
            this HttpResponseMessage response,
            string? url,
            CancellationToken cancellationToken = default)
        {
            await response.EnsureSuccessStatusCodeAsync(url);
            return response.Content != null
                ? await response.Content.ReadAsByteArrayAsync(cancellationToken)
                : Array.Empty<byte>();
        }

        /// <summary>
        /// Reads the response content as a stream, ensuring success status code first.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response content as stream.</returns>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async Task<Stream> ReadAsStreamAsync(
            this HttpResponseMessage response,
            string? url,
            CancellationToken cancellationToken = default)
        {
            await response.EnsureSuccessStatusCodeAsync(url);
            return response.Content != null
                ? await response.Content.ReadAsStreamAsync(cancellationToken)
                : Stream.Null;
        }

        /// <summary>
        /// Reads the response content as an async enumerable of bytes for streaming large responses.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="url">The request URL for error context.</param>
        /// <param name="bufferSize">The buffer size for reading. Default is 8192 bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of byte arrays.</returns>
        /// <exception cref="HttpStatusCodeException">Thrown when the response is not successful.</exception>
        public static async IAsyncEnumerable<byte[]> ReadAsStreamAsync(
            this HttpResponseMessage response,
            string? url,
            int bufferSize = 8192,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await response.EnsureSuccessStatusCodeAsync(url);

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
    }
}

