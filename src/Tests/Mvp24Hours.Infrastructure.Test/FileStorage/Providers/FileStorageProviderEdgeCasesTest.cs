//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Providers;

[Trait("Category", "Unit")]
public class FileStorageProviderEdgeCasesTest : IDisposable
{
    private readonly string _tempRoot;
    private readonly List<string> _createdRoots = [];

    public FileStorageProviderEdgeCasesTest()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "mvp24hours-filestorage-edge", Guid.NewGuid().ToString("N"));
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
                // Best-effort cleanup.
            }
        }
    }

    [Fact]
    public async Task LocalFileStorageProvider_DownloadAsync_WhenMissing_ShouldFail()
    {
        LocalFileStorageProvider provider = CreateLocalProvider();

        FileDownloadResult result = await provider.DownloadAsync("missing/file.txt");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LocalFileStorageProvider_DeleteAsync_WhenMissing_ShouldReturnFalse()
    {
        LocalFileStorageProvider provider = CreateLocalProvider();

        (await provider.DeleteAsync("missing.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryFileStorageProvider_DeleteAsync_WhenMissing_ShouldReturnFalse()
    {
        InMemoryFileStorageProvider provider = CreateInMemoryProvider();

        (await provider.DeleteAsync("missing.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryFileStorageProvider_DownloadAsync_WhenMissing_ShouldFail()
    {
        InMemoryFileStorageProvider provider = CreateInMemoryProvider();

        FileDownloadResult result = await provider.DownloadAsync("missing.txt");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LocalFileStorageProvider_UploadAsync_WithPathTraversal_ShouldThrowUnauthorizedAccess()
    {
        LocalFileStorageProvider provider = CreateLocalProvider();

        Func<Task> act = () => provider.UploadAsync(
            "../outside.txt",
            FileStorageTestHelpers.CreateContent("escape"),
            "text/plain");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*outside base directory*");
    }

    private LocalFileStorageProvider CreateLocalProvider()
    {
        return new LocalFileStorageProvider(FileStorageTestHelpers.CreateOptions(
            basePath: _tempRoot,
            createDirectoriesIfNotExists: true));
    }

    private static InMemoryFileStorageProvider CreateInMemoryProvider()
    {
        return new InMemoryFileStorageProvider(FileStorageTestHelpers.CreateOptions());
    }
}
