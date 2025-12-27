//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for compressing and decompressing cache values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts compression mechanisms for cache values. Compression is useful
    /// for large values to reduce memory usage and network bandwidth in distributed caches.
    /// </para>
    /// <para>
    /// <strong>Supported Algorithms:</strong>
    /// <list type="bullet">
    /// <item>Brotli (better compression ratio, slower)</item>
    /// <item>Gzip (faster compression, good ratio)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICacheCompressor
    {
        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The compressed data with compression header.</returns>
        Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="compressedData">The compressed data with compression header.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The decompressed data.</returns>
        Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the compression algorithm used by this compressor.
        /// </summary>
        CompressionAlgorithm Algorithm { get; }
    }

    /// <summary>
    /// Compression algorithms supported by cache compressors.
    /// </summary>
    public enum CompressionAlgorithm
    {
        /// <summary>
        /// Brotli compression (better ratio, slower).
        /// </summary>
        Brotli = 1,

        /// <summary>
        /// Gzip compression (faster, good ratio).
        /// </summary>
        Gzip = 2
    }
}

