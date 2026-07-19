//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Providers;

/// <summary>
/// Azure Blob provider is currently a stub (Azure.Storage.Blobs not packaged).
/// Tests cover constructor guards and NotImplementedException on all operations.
/// </summary>
[Trait("Category", "Unit")]
public class AzureBlobStorageProviderTest
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private const string ContainerName = "test-container";

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AzureBlobStorageProvider(null!, ConnectionString, ContainerName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AzureBlobStorageProvider(
            FileStorageTestHelpers.CreateOptions(),
            null!,
            ContainerName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithNullContainerName_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AzureBlobStorageProvider(
            FileStorageTestHelpers.CreateOptions(),
            ConnectionString,
            null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("containerName");
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldCreateInstance()
    {
        AzureBlobStorageProvider provider = CreateProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task UploadFromStreamAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();
        using var stream = new MemoryStream(FileStorageTestHelpers.CreateContent());

        Func<Task> act = () => provider.UploadFromStreamAsync("file.txt", stream, "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task UploadFromChunksAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.UploadFromChunksAsync(
            "file.txt",
            FileStorageTestHelpers.CreateChunksAsync(FileStorageTestHelpers.CreateContent()),
            "text/plain");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task DownloadAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.DownloadAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task DownloadToStreamAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();
        using var stream = new MemoryStream();

        Func<Task> act = () => provider.DownloadToStreamAsync("file.txt", stream);

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public void DownloadAsChunksAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Action act = () => provider.DownloadAsChunksAsync("file.txt");

        act.Should().Throw<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.ExistsAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.DeleteAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetMetadataAsync("file.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public void ListFilesAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Action act = () => provider.ListFilesAsync();

        act.Should().Throw<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task CopyAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.CopyAsync("a.txt", "b.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    [Fact]
    public async Task MoveAsync_ShouldThrowNotImplementedException()
    {
        AzureBlobStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.MoveAsync("a.txt", "b.txt");

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*Azure.Storage.Blobs*");
    }

    private static AzureBlobStorageProvider CreateProvider()
    {
        return new AzureBlobStorageProvider(
            FileStorageTestHelpers.CreateOptions(),
            ConnectionString,
            ContainerName);
    }
}
