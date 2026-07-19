//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Providers;

/// <summary>
/// AWS S3 provider is currently a stub (AWSSDK.S3 not packaged).
/// Task 2.5 names it S3FileStorageProvider; the implementation is AwsS3StorageProvider.
/// Tests cover constructor guards and NotImplementedException on all operations.
/// </summary>
[Trait("Category", "Unit")]
public class AwsS3StorageProviderTest
{
    private const string BucketName = "test-bucket";

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AwsS3StorageProvider(null!, BucketName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullBucketName_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AwsS3StorageProvider(FileStorageTestHelpers.CreateOptions(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bucketName");
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldCreateInstance()
    {
        AwsS3StorageProvider provider = CreateProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCredentialsAndRegion_ShouldCreateInstance()
    {
        var provider = new AwsS3StorageProvider(
            FileStorageTestHelpers.CreateOptions(),
            BucketName,
            accessKeyId: "AKIATEST",
            secretAccessKey: "secret",
            region: "us-east-1");

        provider.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task UploadFromStreamAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();
        using var stream = new MemoryStream(FileStorageTestHelpers.CreateContent());

        Func<Task> act = () => provider.UploadFromStreamAsync("file.txt", stream, "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task UploadFromChunksAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.UploadFromChunksAsync(
            "file.txt",
            FileStorageTestHelpers.CreateChunksAsync(FileStorageTestHelpers.CreateContent()),
            "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task DownloadAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.DownloadAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task DownloadToStreamAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();
        using var stream = new MemoryStream();

        Func<Task> act = () => provider.DownloadToStreamAsync("file.txt", stream);

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public void DownloadAsChunksAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Action act = () => provider.DownloadAsChunksAsync("file.txt");

        act.Should().Throw<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.ExistsAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.DeleteAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetMetadataAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public void ListFilesAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Action act = () => provider.ListFilesAsync();

        act.Should().Throw<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task CopyAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.CopyAsync("a.txt", "b.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    [Fact]
    public async Task MoveAsync_ShouldThrowNotImplementedException()
    {
        AwsS3StorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.MoveAsync("a.txt", "b.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*AWSSDK.S3*");
    }

    private static AwsS3StorageProvider CreateProvider()
    {
        return new AwsS3StorageProvider(FileStorageTestHelpers.CreateOptions(), BucketName);
    }
}
