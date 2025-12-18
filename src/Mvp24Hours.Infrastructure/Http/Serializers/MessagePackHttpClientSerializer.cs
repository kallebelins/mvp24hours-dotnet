//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MessagePack;
using Mvp24Hours.Infrastructure.Http.Contract;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Serializers
{
    /// <summary>
    /// MessagePack serializer for HTTP content using MessagePack library.
    /// </summary>
    /// <remarks>
    /// Requires MessagePack NuGet package. This serializer provides high-performance binary serialization.
    /// </remarks>
    public class MessagePackHttpClientSerializer : IHttpContentSerializer
    {
        private readonly MessagePackSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance with default MessagePack options.
        /// </summary>
        public MessagePackHttpClientSerializer()
            : this(MessagePackSerializerOptions.Standard)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom MessagePack options.
        /// </summary>
        public MessagePackHttpClientSerializer(MessagePackSerializerOptions options)
        {
            _options = options ?? MessagePackSerializerOptions.Standard;
        }

        /// <inheritdoc />
        public string MediaType => "application/x-msgpack";

        /// <inheritdoc />
        public HttpContent Serialize(object? value)
        {
            if (value == null)
            {
                return new ByteArrayContent(Array.Empty<byte>());
            }

            try
            {
                var bytes = MessagePackSerializer.Serialize(value.GetType(), value, _options);
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaType);
                return content;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize object to MessagePack: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default) where T : class
        {
            var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
            return DeserializeFromBytes<T>(bytes);
        }

        /// <inheritdoc />
        public T? Deserialize<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            // MessagePack is binary, so string content should be base64 encoded
            try
            {
                var bytes = Convert.FromBase64String(content);
                return DeserializeFromBytes<T>(bytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize MessagePack from string: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializes MessagePack from byte array.
        /// </summary>
        private T? DeserializeFromBytes<T>(byte[] bytes) where T : class
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            try
            {
                return MessagePackSerializer.Deserialize<T>(bytes, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize MessagePack to {typeof(T).Name}: {ex.Message}", ex);
            }
        }
    }
}

