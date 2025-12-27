//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MessagePack;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Serializers
{
    /// <summary>
    /// MessagePack-based cache serializer using MessagePack library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This serializer uses MessagePack for high-performance binary serialization.
    /// MessagePack is more compact and faster than JSON, making it ideal for caching scenarios
    /// where performance and storage efficiency are important.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Smaller payload size (typically 30-50% smaller than JSON)</item>
    /// <item>Faster serialization/deserialization</item>
    /// <item>Binary format (not human-readable)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong>
    /// Requires MessagePack NuGet package. For string serialization, MessagePack bytes
    /// are base64-encoded to ensure compatibility with string-based cache operations.
    /// </para>
    /// </remarks>
    public class MessagePackCacheSerializer : ICacheSerializer
    {
        private readonly MessagePackSerializerOptions _options;
        private readonly ILogger<MessagePackCacheSerializer>? _logger;

        /// <summary>
        /// Creates a new instance of MessagePackCacheSerializer.
        /// </summary>
        /// <param name="options">Optional MessagePack serializer options (defaults to Standard).</param>
        /// <param name="logger">Optional logger.</param>
        public MessagePackCacheSerializer(
            MessagePackSerializerOptions? options = null,
            ILogger<MessagePackCacheSerializer>? logger = null)
        {
            _options = options ?? MessagePackSerializerOptions.Standard;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<byte[]> SerializeAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var bytes = MessagePackSerializer.Serialize(value, _options);
                return Task.FromResult(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error serializing {Type} to MessagePack", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<T?> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (bytes == null || bytes.Length == 0)
                return Task.FromResult<T?>(default);

            try
            {
                var value = MessagePackSerializer.Deserialize<T>(bytes, _options);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing MessagePack to {Type}", typeof(T).Name);
                return Task.FromResult<T?>(default);
            }
        }

        /// <inheritdoc />
        public Task<string> SerializeToStringAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var bytes = MessagePackSerializer.Serialize(value, _options);
                // Base64 encode for string storage
                var base64 = Convert.ToBase64String(bytes);
                return Task.FromResult(base64);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error serializing {Type} to MessagePack string", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<T?> DeserializeFromStringAsync<T>(string value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<T?>(default);

            try
            {
                // Base64 decode from string storage
                var bytes = Convert.FromBase64String(value);
                var result = MessagePackSerializer.Deserialize<T>(bytes, _options);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing MessagePack string to {Type}", typeof(T).Name);
                return Task.FromResult<T?>(default);
            }
        }
    }
}

