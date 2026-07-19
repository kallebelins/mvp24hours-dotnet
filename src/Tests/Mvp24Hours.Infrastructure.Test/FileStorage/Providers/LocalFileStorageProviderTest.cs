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
public class LocalFileStorageProviderTest : IDisposable
{
    private readonly string _tempRoot;
    private readonly List<string> _createdRoots = [];

    public LocalFileStorageProviderTest()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "mvp24hours-filestorage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _createdRoots.Add(_tempRoot);
    }

    public void Dispose()
    {
        foreach (string root in _createdRoots)
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for temp test folders.
            }
        }
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new LocalFileStorageProvider(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithBasePath_ShouldCreateDirectoryWhenEnabled()
    {
        string basePath = Path.Combine(_tempRoot, "uploads");
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(
            basePath: basePath,
            createDirectoriesIfNotExists: true);

        _ = new LocalFileStorageProvider(options);

        Directory.Exists(basePath).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithValidContent_ShouldWriteFile()
    {
        LocalFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent("local-upload");

        FileUploadResult result = await provider.UploadAsync("docs/file.txt", content, "text/plain");

        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("docs/file.txt");
        result.Metadata!.Size.Should().Be(content.Length);
        File.Exists(Path.Combine(_tempRoot, "docs", "file.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithEmptyPath_ShouldFail()
    {
        LocalFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadAsync(" ", FileStorageTestHelpers.CreateContent(), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path");
    }

    [Fact]
    public async Task UploadAsync_WithNullContent_ShouldFail()
    {
        LocalFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("content");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyContentType_ShouldUseDefault()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(
            basePath: _tempRoot,
            defaultContentType: "application/custom");
        var provider = new LocalFileStorageProvider(options);

        FileUploadResult result = await provider.UploadAsync("file.bin", FileStorageTestHelpers.CreateContent(), "");

        result.Success.Should().BeTrue();
        result.Metadata!.ContentType.Should().Be("application/custom");
    }

    [Fact]
    public async Task UploadAsync_WhenOverwriteDisabledAndFileExists_ShouldFail()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions(
            basePath: _tempRoot,
            overwriteExistingFiles: false);
        var provider = new LocalFileStorageProvider(options);
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v1"), "text/plain");

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("v2"), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task UploadAsync_WhenValidatorFails_ShouldNotWriteFile()
    {
        var provider = new LocalFileStorageProvider(
            FileStorageTestHelpers.CreateOptions(basePath: _tempRoot),
            FileStorageTestHelpers.CreateFailingValidator("blocked"));

        FileUploadResult result = await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("validation failed");
        File.Exists(Path.Combine(_tempRoot, "file.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task UploadFromStreamAsync_ShouldWriteFile()
    {
        LocalFileStorageProvider provider = CreateProvider();
        using var stream = new MemoryStream(FileStorageTestHelpers.CreateContent("from-stream"));

        FileUploadResult result = await provider.UploadFromStreamAsync("stream.txt", stream, "text/plain");
        FileDownloadResult download = await provider.DownloadAsync("stream.txt");

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("from-stream");
    }

    [Fact]
    public async Task UploadFromStreamAsync_WithNullStream_ShouldFail()
    {
        LocalFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadFromStreamAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Stream");
    }

    [Fact]
    public async Task UploadFromChunksAsync_ShouldConcatenateContent()
    {
        LocalFileStorageProvider provider = CreateProvider();
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
        LocalFileStorageProvider provider = CreateProvider();

        FileUploadResult result = await provider.UploadFromChunksAsync("file.txt", null!, "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Chunks");
    }

    [Fact]
    public async Task DownloadAsync_WhenFileExists_ShouldReturnContent()
    {
        LocalFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent("download-me");
        await provider.UploadAsync("file.txt", content, "text/plain");

        FileDownloadResult result = await provider.DownloadAsync("file.txt");

        result.Success.Should().BeTrue();
        result.Content.Should().BeEquivalentTo(content);
        result.Metadata!.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task DownloadAsync_WhenMissing_ShouldReturnNotFound()
    {
        LocalFileStorageProvider provider = CreateProvider();

        FileDownloadResult result = await provider.DownloadAsync("missing.txt");

        result.IsNotFound.Should().BeTrue();
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("data.json", "application/json")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public async Task DownloadAsync_ShouldInferContentTypeFromExtension(string fileName, string expectedContentType)
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync(fileName, FileStorageTestHelpers.CreateContent(), "application/octet-stream");

        FileDownloadResult result = await provider.DownloadAsync(fileName);

        result.Success.Should().BeTrue();
        result.Metadata!.ContentType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task DownloadToStreamAsync_ShouldWriteDestination()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent("to-stream"), "text/plain");
        using var destination = new MemoryStream();

        FileDownloadResult result = await provider.DownloadToStreamAsync("file.txt", destination);

        result.Success.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(destination.ToArray()).Should().Be("to-stream");
    }

    [Fact]
    public async Task DownloadToStreamAsync_WithNullDestination_ShouldFail()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("file.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        FileDownloadResult result = await provider.DownloadToStreamAsync("file.txt", null!);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Destination stream");
    }

    [Fact]
    public async Task DownloadAsChunksAsync_ShouldYieldChunks()
    {
        LocalFileStorageProvider provider = CreateProvider();
        byte[] content = FileStorageTestHelpers.CreateContent("abcdefghij");
        await provider.UploadAsync("file.txt", content, "text/plain");

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in provider.DownloadAsChunksAsync("file.txt", chunkSize: 4))
        {
            // Local provider may reuse the read buffer; clone for assertion safety.
            chunks.Add([.. chunk]);
        }

        chunks.SelectMany(c => c).Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task Exists_Delete_GetMetadata_ShouldWork()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("meta.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        (await provider.ExistsAsync("meta.txt")).Should().BeTrue();
        IFileMetadata? metadata = await provider.GetMetadataAsync("meta.txt");
        metadata.Should().NotBeNull();
        metadata!.Name.Should().Be("meta.txt");

        (await provider.DeleteAsync("meta.txt")).Should().BeTrue();
        (await provider.ExistsAsync("meta.txt")).Should().BeFalse();
        (await provider.GetMetadataAsync("meta.txt")).Should().BeNull();
        (await provider.DeleteAsync("meta.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ListFilesAsync_Recursive_ShouldReturnRelativePaths()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("root.txt", FileStorageTestHelpers.CreateContent(), "text/plain");
        await provider.UploadAsync("sub/nested.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("", recursive: true))
        {
            files.Add(file);
        }

        files.Select(f => f.FilePath.Replace('\\', '/'))
            .Should().BeEquivalentTo(["root.txt", "sub/nested.txt"]);
    }

    [Fact]
    public async Task ListFilesAsync_NonRecursive_ShouldListTopLevelOnly()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("root.txt", FileStorageTestHelpers.CreateContent(), "text/plain");
        await provider.UploadAsync("sub/nested.txt", FileStorageTestHelpers.CreateContent(), "text/plain");

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("", recursive: false))
        {
            files.Add(file);
        }

        files.Should().ContainSingle(f => f.FilePath.Replace('\\', '/') == "root.txt");
    }

    [Fact]
    public async Task ListFilesAsync_WhenDirectoryMissing_ShouldYieldNothing()
    {
        LocalFileStorageProvider provider = CreateProvider();

        var files = new List<IFileMetadata>();
        await foreach (IFileMetadata file in provider.ListFilesAsync("does-not-exist", recursive: true))
        {
            files.Add(file);
        }

        files.Should().BeEmpty();
    }

    [Fact]
    public async Task CopyAsync_ShouldDuplicateFile()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("src.txt", FileStorageTestHelpers.CreateContent("copy"), "text/plain");

        bool copied = await provider.CopyAsync("src.txt", "folder/dest.txt");
        FileDownloadResult download = await provider.DownloadAsync("folder/dest.txt");

        copied.Should().BeTrue();
        (await provider.ExistsAsync("src.txt")).Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(download.Content!).Should().Be("copy");
    }

    [Fact]
    public async Task CopyAsync_WhenSourceMissing_ShouldReturnFalse()
    {
        LocalFileStorageProvider provider = CreateProvider();
        (await provider.CopyAsync("missing.txt", "dest.txt")).Should().BeFalse();
        (await provider.CopyAsync("", "dest.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        LocalFileStorageProvider provider = CreateProvider();
        await provider.UploadAsync("src.txt", FileStorageTestHelpers.CreateContent("move"), "text/plain");

        bool moved = await provider.MoveAsync("src.txt", "moved/dest.txt");

        moved.Should().BeTrue();
        (await provider.ExistsAsync("src.txt")).Should().BeFalse();
        (await provider.ExistsAsync("moved/dest.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_WhenSourceMissing_ShouldReturnFalse()
    {
        LocalFileStorageProvider provider = CreateProvider();
        (await provider.MoveAsync("missing.txt", "dest.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task GetFullPath_WhenOutsideBaseDirectory_ShouldThrowUnauthorizedAccessException()
    {
        LocalFileStorageProvider provider = CreateProvider();

        Func<Task> act = () => provider.ExistsAsync("../outside.txt");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*outside base directory*");
    }

    private LocalFileStorageProvider CreateProvider()
    {
        return new LocalFileStorageProvider(FileStorageTestHelpers.CreateOptions(basePath: _tempRoot));
    }
}
