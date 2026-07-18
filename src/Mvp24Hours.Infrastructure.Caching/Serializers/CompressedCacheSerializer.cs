//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.Serializers;

/// <summary>
/// Cache serializer wrapper that applies compression to large values.
/// </summary>
/// <remarks>
/// <para>
/// This serializer wraps another serializer and automatically compresses values that exceed
/// a configurable threshold. Compression reduces memory usage and network bandwidth for
/// distributed caches, especially beneficial for large objects.
/// </para>
/// <para>
/// <strong>Compression Strategy:</strong>
/// <list type="bullet">
/// <item>Values smaller than threshold are stored uncompressed</item>
/// <item>Values larger than threshold are compressed before storage</item>
/// <item>Decompression is automatic on retrieval</item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of CompressedCacheSerializer.
/// </remarks>
/// <param name="innerSerializer">The underlying serializer to use.</param>
/// <param name="compressor">The compressor to use for large values.</param>
/// <param name="compressionThresholdBytes">Minimum size in bytes to trigger compression (defaults to 1024).</param>
/// <param name="logger">Optional logger.</param>
public class CompressedCacheSerializer(
    ICacheSerializer innerSerializer,
    ICacheCompressor compressor,
    int compressionThresholdBytes = 1024,
    ILogger<CompressedCacheSerializer>? logger = null) : ICacheSerializer
{
    private readonly ICacheSerializer _innerSerializer = innerSerializer ?? throw new ArgumentNullException(nameof(innerSerializer));
    private readonly ICacheCompressor _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
    private readonly int _compressionThresholdBytes = compressionThresholdBytes > 0
            ? compressionThresholdBytes
            : throw new ArgumentException("Compression threshold must be greater than 0.", nameof(compressionThresholdBytes));
    private readonly ILogger<CompressedCacheSerializer>? _logger = logger;

    /// <inheritdoc />
    public async Task<byte[]> SerializeAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        byte[] serialized = await _innerSerializer.SerializeAsync(value, cancellationToken);

        // Compress if above threshold
        if (serialized.Length >= _compressionThresholdBytes)
        {
            _logger?.LogDebug(
                "Compressing serialized value of {Size} bytes (threshold: {Threshold})",
                serialized.Length, _compressionThresholdBytes);
            return await _compressor.CompressAsync(serialized, cancellationToken);
        }

        // Add header byte (0 = uncompressed) for small values
        byte[] result = new byte[serialized.Length + 1];
        result[0] = 0; // Uncompressed marker
        Buffer.BlockCopy(serialized, 0, result, 1, serialized.Length);
        return result;
    }

    /// <inheritdoc />
    public async Task<T?> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        byte[] dataToDeserialize;

        // Check compression header
        if (bytes.Length > 0 && bytes[0] == 0)
        {
            // Uncompressed - remove header byte
            dataToDeserialize = new byte[bytes.Length - 1];
            Buffer.BlockCopy(bytes, 1, dataToDeserialize, 0, dataToDeserialize.Length);
        }
        else
        {
            // Compressed - decompress first
            _logger?.LogDebug("Decompressing cached value");
            dataToDeserialize = await _compressor.DecompressAsync(bytes, cancellationToken);
        }

        return await _innerSerializer.DeserializeAsync<T>(dataToDeserialize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> SerializeToStringAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        // String serialization doesn't use compression
        return _innerSerializer.SerializeToStringAsync(value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> DeserializeFromStringAsync<T>(string value, CancellationToken cancellationToken = default)
    {
        // String deserialization doesn't use compression
        return _innerSerializer.DeserializeFromStringAsync<T>(value, cancellationToken);
    }
}

