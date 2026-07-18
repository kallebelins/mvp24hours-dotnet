//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.Compression;

/// <summary>
/// Compressor implementation using Brotli or Gzip algorithms.
/// </summary>
/// <remarks>
/// <para>
/// This compressor wraps .NET's built-in compression streams (BrotliStream, GZipStream)
/// to provide compression for cache values. It includes a header to identify the compression
/// algorithm used, allowing automatic decompression.
/// </para>
/// <para>
/// <strong>Compression Header Format:</strong>
/// <list type="bullet">
/// <item>Byte 0: Compression algorithm identifier (1 = Brotli, 2 = Gzip)</item>
/// <item>Bytes 1-4: Original data length (int32, little-endian)</item>
/// <item>Bytes 5+: Compressed data</item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of CacheCompressor.
/// </remarks>
/// <param name="algorithm">The compression algorithm to use.</param>
/// <param name="compressionLevel">The compression level (defaults to Optimal).</param>
/// <param name="logger">Optional logger.</param>
public class CacheCompressor(
    CompressionAlgorithm algorithm = CompressionAlgorithm.Brotli,
    CompressionLevel compressionLevel = CompressionLevel.Optimal,
    ILogger<CacheCompressor>? logger = null) : ICacheCompressor
{
    private readonly CompressionLevel _compressionLevel = compressionLevel;
    private readonly ILogger<CacheCompressor>? _logger = logger;

    /// <inheritdoc />
    public CompressionAlgorithm Algorithm { get; } = algorithm;

    /// <inheritdoc />
    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data ?? [];
        }

        try
        {
            using var outputStream = new MemoryStream();

            // Write compression header
            outputStream.WriteByte((byte)Algorithm);
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            await outputStream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);

            // Compress data
            Stream compressionStream = Algorithm switch
            {
                CompressionAlgorithm.Brotli => new BrotliStream(outputStream, _compressionLevel, leaveOpen: true),
                CompressionAlgorithm.Gzip => new GZipStream(outputStream, _compressionLevel, leaveOpen: true),
                _ => throw new NotSupportedException($"Compression algorithm {Algorithm} is not supported.")
            };

            using (compressionStream)
            {
                await compressionStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }

            byte[] compressed = outputStream.ToArray();
            double compressionRatio = (1.0 - (double)compressed.Length / data.Length) * 100;
            _logger?.LogDebug(
                "Compressed {OriginalSize} bytes to {CompressedSize} bytes ({Ratio:F2}% reduction) using {Algorithm}",
                data.Length, compressed.Length, compressionRatio, Algorithm);

            return compressed;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error compressing data using {Algorithm}", Algorithm);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        if (compressedData == null || compressedData.Length == 0)
        {
            return compressedData ?? [];
        }

        // Check minimum header size (1 byte algorithm + 4 bytes length)
        if (compressedData.Length < 5)
        {
            _logger?.LogWarning("Compressed data too small to contain header, returning as-is");
            return compressedData;
        }

        try
        {
            using var inputStream = new MemoryStream(compressedData);

            // Read compression header
            byte algorithmByte = (byte)inputStream.ReadByte();
            var algorithm = (CompressionAlgorithm)algorithmByte;

            byte[] lengthBytes = new byte[4];
            await inputStream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            int originalLength = BitConverter.ToInt32(lengthBytes, 0);

            // Decompress data
            Stream decompressionStream = algorithm switch
            {
                CompressionAlgorithm.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                CompressionAlgorithm.Gzip => new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                _ => throw new NotSupportedException($"Compression algorithm {algorithm} is not supported.")
            };

            using (decompressionStream)
            {
                byte[] decompressed = new byte[originalLength];
                int totalRead = 0;
                int bytesRead;

                while (totalRead < originalLength &&
                       (bytesRead = await decompressionStream.ReadAsync(decompressed, totalRead, originalLength - totalRead, cancellationToken)) > 0)
                {
                    totalRead += bytesRead;
                }

                if (totalRead != originalLength)
                {
                    _logger?.LogWarning(
                        "Decompressed size ({ActualSize}) doesn't match expected size ({ExpectedSize})",
                        totalRead, originalLength);
                }

                return decompressed;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decompressing data");
            throw;
        }
    }
}

