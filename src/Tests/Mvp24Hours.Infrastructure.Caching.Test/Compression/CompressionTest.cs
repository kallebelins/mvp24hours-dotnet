using System.IO.Compression;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Compression;

namespace Mvp24Hours.Infrastructure.Caching.Test.Compression;

[Trait("Category", "Unit")]
public class CacheCompressorTest
{
    [Fact]
    public async Task CompressAndDecompress_Brotli_ShouldRoundTrip()
    {
        var compressor = new CacheCompressor(CompressionAlgorithm.Brotli);
        byte[] original = "hello cache compression test payload"u8.ToArray();

        byte[] compressed = await compressor.CompressAsync(original);
        byte[] decompressed = await compressor.DecompressAsync(compressed);

        decompressed.Should().BeEquivalentTo(original);
        compressed[0].Should().Be((byte)CompressionAlgorithm.Brotli);
    }

    [Fact]
    public async Task CompressAndDecompress_Gzip_ShouldRoundTrip()
    {
        var compressor = new CacheCompressor(CompressionAlgorithm.Gzip, CompressionLevel.Fastest);
        byte[] original = new byte[256];
        Random.Shared.NextBytes(original);

        byte[] compressed = await compressor.CompressAsync(original);
        byte[] decompressed = await compressor.DecompressAsync(compressed);

        decompressed.Should().BeEquivalentTo(original);
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Gzip);
    }

    [Fact]
    public async Task CompressAsync_NullOrEmpty_ShouldReturnEmptyArray()
    {
        var compressor = new CacheCompressor();

        byte[] nullResult = await compressor.CompressAsync(null!);
        byte[] emptyResult = await compressor.CompressAsync([]);

        nullResult.Should().BeEmpty();
        emptyResult.Should().BeEmpty();
    }

    [Fact]
    public async Task DecompressAsync_DataTooSmall_ShouldReturnAsIs()
    {
        var compressor = new CacheCompressor();
        byte[] tiny = [1, 2, 3];

        byte[] result = await compressor.DecompressAsync(tiny);

        result.Should().BeEquivalentTo(tiny);
    }
}
