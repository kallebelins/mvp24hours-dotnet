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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Helpers
{
    /// <summary>
    /// Helper class for creating MultipartFormDataContent with a fluent API.
    /// </summary>
    public class MultipartFormDataHelper
    {
        private readonly MultipartFormDataContent _content;
        private readonly string _boundary;

        /// <summary>
        /// Initializes a new instance of MultipartFormDataHelper.
        /// </summary>
        public MultipartFormDataHelper()
            : this($"----Mvp24Hours-{Guid.NewGuid():N}")
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom boundary.
        /// </summary>
        public MultipartFormDataHelper(string boundary)
        {
            _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            _content = new MultipartFormDataContent(_boundary);
        }

        /// <summary>
        /// Adds a string field to the multipart form data.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        /// <returns>The helper instance for method chaining.</returns>
        public MultipartFormDataHelper AddField(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
            }

            _content.Add(new StringContent(value ?? string.Empty, Encoding.UTF8), name);
            return this;
        }

        /// <summary>
        /// Adds a file field to the multipart form data from a stream.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="stream">The file stream.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The content type. Default is "application/octet-stream".</param>
        /// <returns>The helper instance for method chaining.</returns>
        public MultipartFormDataHelper AddFile(string name, Stream stream, string fileName, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            var streamContent = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            _content.Add(streamContent, name, fileName);
            return this;
        }

        /// <summary>
        /// Adds a file field to the multipart form data from a byte array.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="bytes">The file bytes.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The content type. Default is "application/octet-stream".</param>
        /// <returns>The helper instance for method chaining.</returns>
        public MultipartFormDataHelper AddFile(string name, byte[] bytes, string fileName, string? contentType = null)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using var stream = new MemoryStream(bytes);
            return AddFile(name, stream, fileName, contentType);
        }

        /// <summary>
        /// Adds a file field to the multipart form data from an async enumerable of bytes (for streaming large files).
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="stream">The async enumerable of bytes.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The content type. Default is "application/octet-stream".</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The helper instance for method chaining.</returns>
        public async Task<MultipartFormDataHelper> AddFileAsync(
            string name,
            IAsyncEnumerable<byte[]> stream,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            // Convert async enumerable to stream
            var memoryStream = new MemoryStream();
            await foreach (var chunk in stream.WithCancellation(cancellationToken))
            {
                await memoryStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken);
            }
            memoryStream.Position = 0;

            var streamContent = new StreamContent(memoryStream);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            _content.Add(streamContent, name, fileName);
            return this;
        }

        /// <summary>
        /// Adds a file field to the multipart form data from a file path.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="contentType">The content type. If null, will be inferred from file extension.</param>
        /// <returns>The helper instance for method chaining.</returns>
        public MultipartFormDataHelper AddFileFromPath(string name, string filePath, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            var fileName = Path.GetFileName(filePath);
            contentType ??= GetContentTypeFromExtension(Path.GetExtension(filePath));

            using var fileStream = File.OpenRead(filePath);
            return AddFile(name, fileStream, fileName, contentType);
        }

        /// <summary>
        /// Adds multiple string fields to the multipart form data.
        /// </summary>
        /// <param name="fields">Dictionary of field names and values.</param>
        /// <returns>The helper instance for method chaining.</returns>
        public MultipartFormDataHelper AddFields(Dictionary<string, string> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            foreach (var field in fields)
            {
                AddField(field.Key, field.Value);
            }

            return this;
        }

        /// <summary>
        /// Builds and returns the MultipartFormDataContent.
        /// </summary>
        /// <returns>The configured MultipartFormDataContent.</returns>
        public MultipartFormDataContent Build()
        {
            return _content;
        }

        /// <summary>
        /// Gets the content type from file extension.
        /// </summary>
        private static string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".zip" => "application/zip",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// Extension methods for creating MultipartFormDataContent easily.
    /// </summary>
    public static class MultipartFormDataExtensions
    {
        /// <summary>
        /// Creates a new MultipartFormDataHelper instance.
        /// </summary>
        /// <returns>A new MultipartFormDataHelper instance.</returns>
        public static MultipartFormDataHelper CreateMultipartFormData()
        {
            return new MultipartFormDataHelper();
        }

        /// <summary>
        /// Creates a new MultipartFormDataHelper instance with a custom boundary.
        /// </summary>
        /// <param name="boundary">The boundary string.</param>
        /// <returns>A new MultipartFormDataHelper instance.</returns>
        public static MultipartFormDataHelper CreateMultipartFormData(string boundary)
        {
            return new MultipartFormDataHelper(boundary);
        }
    }
}

