//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Results;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Results;

[Trait("Category", "Unit")]
public class ChunkedUploadStatusTest
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSetAllProperties()
    {
        DateTimeOffset initiatedAt = new(2024, 4, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresAt = initiatedAt.AddHours(1);

        ChunkedUploadStatus status = new(
            "upload-1",
            "docs/large.bin",
            totalSize: 1000,
            chunkSize: 250,
            totalChunks: 4,
            uploadedChunks: 2,
            bytesUploaded: 500,
            initiatedAt,
            expiresAt,
            isComplete: false);

        status.UploadId.Should().Be("upload-1");
        status.FilePath.Should().Be("docs/large.bin");
        status.TotalSize.Should().Be(1000);
        status.ChunkSize.Should().Be(250);
        status.TotalChunks.Should().Be(4);
        status.UploadedChunks.Should().Be(2);
        status.BytesUploaded.Should().Be(500);
        status.InitiatedAt.Should().Be(initiatedAt);
        status.ExpiresAt.Should().Be(expiresAt);
        status.IsComplete.Should().BeFalse();
        status.ProgressPercentage.Should().Be(50.0);
    }

    [Fact]
    public void Constructor_WithNullUploadId_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ChunkedUploadStatus(
            null!,
            "file.txt",
            100,
            50,
            2,
            0,
            0,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("uploadId");
    }

    [Fact]
    public void Constructor_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ChunkedUploadStatus(
            "upload-1",
            null!,
            100,
            50,
            2,
            0,
            0,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void ProgressPercentage_WhenTotalSizeIsZero_ShouldReturnZero()
    {
        ChunkedUploadStatus status = new(
            "upload-1",
            "file.txt",
            totalSize: 0,
            chunkSize: 100,
            totalChunks: 0,
            uploadedChunks: 0,
            bytesUploaded: 0,
            DateTimeOffset.UtcNow);

        status.ProgressPercentage.Should().Be(0);
    }
}
