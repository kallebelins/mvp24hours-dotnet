//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Providers;

[Trait("Category", "Unit")]
public class InMemoryFileStorageProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new InMemoryFileStorageProvider(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task UploadAsync_WithValidContent_ShouldSucceed()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent();

        FileUploadResult result = await provider.UploadAsync("docs/file.txt", content, "text/plain");

        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("docs/file.txt");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Size.Should().Be(content.Length);
        result.Metadata.ContentType.Should().Be("text/plain");
        result.Metadata.Name.Should().Be("file.txt");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyPath_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadAsync("  ", FileStorageTestHelpers.CreateContent(), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path");
    }

    [Fact]
    public async Task UploadAsync_WithNullContent_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("content");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyContentType_ShouldUseDefault()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(defaultContentType: "application/custom");
        var provider = new InMemoryFileStorageProvider(options);

        FileUploadResult result = await provider.UploadAsync("file.bin", FileStorageTestHelpers.CreateContent(), "  ");

        result.Success.Should().BeTrue();
        result.Metadata!.ContentType.Should().Be("application/custom");
    }

    [Fact]
    public async Task UploadAsync_WithBasePath_ShouldPrefixPath()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(basePath: "uploads");
        var provider = new InMemoryFileStorageProvider(options);

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("uploads/file.txt");
        (await provider.ExistsAsync("file.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_WhenOverwriteDisabledAndFileExists_ShouldFail()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(overwriteExistingFiles: false);
        var provider = new InMemoryFileStorageProvider(options);
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v1"), "text/plain");

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v2"), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task UploadAsync_WhenOverwriteEnabled_ShouldReplaceContent()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v1"), "text/plain");

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v2"), "text/plain");
        FileDownloadResult download = await provider.DownloadAsync("file.txt");

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("v2");
    }

    [Fact]
    public async Task UploadAsync_WhenValidatorFails_ShouldReturnFailure()
    {
        var provider = new InMemoryFileStorageProvider(
            FileStorageTestHelpers.CreateOptions(),
            FileStorageTestHelpers.CreateFailingValidator("too large"));

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("validation failed");
        result.ErrorMessage.Should().Contain("too large");
        (await provider.ExistsAsync("file.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_WithMetadata_ShouldPersistCustomProperties()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        var metadata = new Dictionary<string, string> { ["author"] = "mvp" };

        FileUploadResult result = await provider.UploadAsync(
            "file.txt",
            FileStorageTestHelpers.CreateContent(),
            "text/plain",
            metadata);

        result.Success.Should().BeTrue();
        IFileMetadata? stored = await provider.GetMetadataAsync("file.txt");
        stored!.CustomProperties["author"].Should().Be("mvp");
    }

    [Fact]
    public async Task UploadFromStreamAsync_WithValidStream_ShouldSucceed()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        using var stream = new MemoryStream(FileStorageTestHelpers.CreateContent("from-stream"));

        FileUploadResult result = await provider.UploadFromStreamAsync("stream.txt", stream, "text/plain");
        FileDownloadResult download = await provider.DownloadAsync("stream.txt");

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("from-stream");
    }

    [Fact]
    public async Task UploadFromStreamAsync_WithNullStream_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadFromStreamAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Stream");
    }

    [Fact]
    public async Task UploadFromStreamAsync_WithEmptyPath_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        using var stream = new MemoryStream(FileStorageTestHelpers.CreateContent());

        FileUploadResult result = await provider.UploadFromStreamAsync("", stream, "text/plain");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UploadFromChunksAsync_WithValidChunks_ShouldConcatenateContent()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        IAsyncEnumerable<byte[]> chunks = FileStorageTestHelpers.CreateChunksAsync(
            FileStorageTestHelpers.CreateContent("hel"),
            FileStorageTestHelpers.CreateContent("lo"));

        FileUploadResult result = await provider.UploadFromChunksAsync("chunk.txt", chunks, "text/plain");
        FileDownloadResult download = await provider.DownloadAsync("chunk.txt");

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("hello");
    }

    [Fact]
    public async Task UploadFromChunksAsync_WithNullChunks_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadFromChunksAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Chunks");
    }

    [Fact]
    public async Task DownloadAsync_WhenFileExists_ShouldReturnContent()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent("download-me");
        await provider.UploadAsync("file.txt", content, "text/plain");

        FileDownloadResult result = await provider.DownloadAsync("file.txt");

        result.Success.Should().BeTrue();
        result.Content.Should().BeEquivalentTo(content);
        result.Metadata!.Name.Should().Be("file.txt");
    }

    [Fact]
    public async Task DownloadAsync_WhenFileMissing_ShouldReturnNotFound()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileDownloadResult result = await provider.DownloadAsync("missing.txt");

        result.Success.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyPath_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileDownloadResult result = await provider.DownloadAsync(" ");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path");
    }

    [Fact]
    public async Task DownloadToStreamAsync_WhenFileExists_ShouldWriteContent()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("stream-out"), "text/plain");
        using var destination = new MemoryStream();

        FileDownloadResult result = await provider.DownloadToStreamAsync("file.txt", destination);

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(destination.ToArray()).Should().Be("stream-out");
    }

    [Fact]
    public async Task DownloadToStreamAsync_WithNullDestination_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        FileDownloadResult result = await provider.DownloadToStreamAsync("file.txt", null!);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Destination stream");
    }

    [Fact]
    public async Task DownloadToStreamAsync_WhenFileMissing_ShouldReturnNotFound()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        using var destination = new MemoryStream();

        FileDownloadResult result = await provider.DownloadToStreamAsync("missing.txt", destination);

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsChunksAsync_ShouldYieldChunks()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent("abcdefghij");
        await provider.UploadAsync("file.txt", content, "text/plain");

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in provider.DownloadAsChunksAsync("file.txt", chunkSize: 4))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(3);
        chunks.SelectMany(c => c).Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task DownloadAsChunksAsync_WithEmptyPath_ShouldYieldNothing()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in provider.DownloadAsChunksAsync(""))
        {
            chunks.Add(chunk);
        }

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsAsync_And_DeleteAsync_ShouldWork()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        (await provider.ExistsAsync("file.txt")).Should().BeTrue();
        (await provider.DeleteAsync("file.txt")).Should().BeTrue();
        (await provider.ExistsAsync("file.txt")).Should().BeFalse();
        (await provider.DeleteAsync("file.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyPath_ShouldReturnFalse()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        (await provider.ExistsAsync(" ")).Should().BeFalse();
    }

    [Fact]
    public async Task GetMetadataAsync_WhenMissing_ShouldReturnNull()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        (await provider.GetMetadataAsync("missing.txt")).Should().BeNull();
        (await provider.GetMetadataAsync("")).Should().BeNull();
    }

    [Fact]
    public async Task ListFilesAsync_NonRecursive_ShouldSkipSubdirectories()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("root.txt", FileStorageTestHelpers.CreateContent(), "text/plain");
        await provider.UploadAsync("sub/nested.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("", recursive: false))
        {
            files.Add(file);
        }

        files.Should().ContainSingle(f => f.FilePath == "root.txt");
        files.Should().NotContain(f => f.FilePath == "sub/nested.txt");
    }

    [Fact]
    public async Task ListFilesAsync_Recursive_ShouldIncludeNestedFiles()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("root.txt", FileStorageTestHelpers.CreateContent(), "text/plain");
        await provider.UploadAsync("sub/nested.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("", recursive: true))
        {
            files.Add(file);
        }

        files.Select(f => f.FilePath).Should().BeEquivalentTo(["root.txt", "sub/nested.txt"]);
    }

    [Fact]
    public async Task ListFilesAsync_WithDirectoryPrefix_ShouldFilter()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("docs/a.txt", FileStorageTestHelpers.CreateContent(), "text/plain");
        await provider.UploadAsync("other/b.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("docs", recursive: true))
        {
            files.Add(file);
        }

        files.Should().ContainSingle(f => f.FilePath == "docs/a.txt");
    }

    [Fact]
    public async Task CopyAsync_ShouldDuplicateFile()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("src.txt", FileStorageTestHelpers.CreateContent("copy"), "text/plain");

        bool copied = await provider.CopyAsync("src.txt", "dest.txt");
        FileDownloadResult download = await provider.DownloadAsync("dest.txt");

        copied.Should().BeTrue();
        (await provider.ExistsAsync("src.txt")).Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("copy");
    }

    [Fact]
    public async Task CopyAsync_WhenSourceMissing_ShouldReturnFalse()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        (await provider.CopyAsync("missing.txt", "dest.txt")).Should().BeFalse();
        (await provider.CopyAsync("", "dest.txt")).Should().BeFalse();
        (await provider.CopyAsync("src.txt", "")).Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("src.txt", FileStorageTestHelpers.CreateContent("move"), "text/plain");

        bool moved = await provider.MoveAsync("src.txt", "dest.txt");

        moved.Should().BeTrue();
        (await provider.ExistsAsync("src.txt")).Should().BeFalse();
        (await provider.ExistsAsync("dest.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_WhenSourceMissing_ShouldReturnFalse()
    {
        InMemoryFileStorageProvider provider = CreateProvider();
        (await provider.MoveAsync("missing.txt", "dest.txt")).Should().BeFalse();
        (await provider.MoveAsync("", "dest.txt")).Should().BeFalse();
        (await provider.MoveAsync("src.txt", "")).Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_ShouldNormalizeBackslashes()
    {
        InMemoryFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadAsync(
            @"folder\file.txt",
            FileStorageTestHelpers.CreateContent(),
            "text/plain");

        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("folder/file.txt");
        (await provider.ExistsAsync("folder/file.txt")).Should().BeTrue();
    }

    private static InMemoryFileStorageProvider CreateProvider()
    {
        return new InMemoryFileStorageProvider(FileStorageTestHelpers.CreateOptions());
    }
}
