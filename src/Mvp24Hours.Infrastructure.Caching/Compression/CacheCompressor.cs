//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Compression
{
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
    public class CacheCompressor : ICacheCompressor
    {
        private readonly CompressionAlgorithm _algorithm;
        private readonly CompressionLevel _compressionLevel;
        private readonly ILogger<CacheCompressor>? _logger;

        /// <summary>
        /// Creates a new instance of CacheCompressor.
        /// </summary>
        /// <param name="algorithm">The compression algorithm to use.</param>
        /// <param name="compressionLevel">The compression level (defaults to Optimal).</param>
        /// <param name="logger">Optional logger.</param>
        public CacheCompressor(
            CompressionAlgorithm algorithm = CompressionAlgorithm.Brotli,
            CompressionLevel compressionLevel = CompressionLevel.Optimal,
            ILogger<CacheCompressor>? logger = null)
        {
            _algorithm = algorithm;
            _compressionLevel = compressionLevel;
            _logger = logger;
        }

        /// <inheritdoc />
        public CompressionAlgorithm Algorithm => _algorithm;

        /// <inheritdoc />
        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0)
                return data ?? Array.Empty<byte>();

            try
            {
                using var outputStream = new MemoryStream();
                
                // Write compression header
                outputStream.WriteByte((byte)_algorithm);
                var lengthBytes = BitConverter.GetBytes(data.Length);
                await outputStream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);

                // Compress data
                Stream compressionStream = _algorithm switch
                {
                    CompressionAlgorithm.Brotli => new BrotliStream(outputStream, _compressionLevel, leaveOpen: true),
                    CompressionAlgorithm.Gzip => new GZipStream(outputStream, _compressionLevel, leaveOpen: true),
                    _ => throw new NotSupportedException($"Compression algorithm {_algorithm} is not supported.")
                };

                using (compressionStream)
                {
                    await compressionStream.WriteAsync(data, 0, data.Length, cancellationToken);
                }

                var compressed = outputStream.ToArray();
                var compressionRatio = (1.0 - (double)compressed.Length / data.Length) * 100;
                _logger?.LogDebug(
                    "Compressed {OriginalSize} bytes to {CompressedSize} bytes ({Ratio:F2}% reduction) using {Algorithm}",
                    data.Length, compressed.Length, compressionRatio, _algorithm);

                return compressed;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error compressing data using {Algorithm}", _algorithm);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
        {
            if (compressedData == null || compressedData.Length == 0)
                return compressedData ?? Array.Empty<byte>();

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
                var algorithmByte = (byte)inputStream.ReadByte();
                var algorithm = (CompressionAlgorithm)algorithmByte;

                var lengthBytes = new byte[4];
                await inputStream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
                var originalLength = BitConverter.ToInt32(lengthBytes, 0);

                // Decompress data
                Stream decompressionStream = algorithm switch
                {
                    CompressionAlgorithm.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                    CompressionAlgorithm.Gzip => new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                    _ => throw new NotSupportedException($"Compression algorithm {algorithm} is not supported.")
                };

                using (decompressionStream)
                {
                    var decompressed = new byte[originalLength];
                    var totalRead = 0;
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
}

