//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.Testing.Fakes;

namespace Mvp24Hours.Infrastructure.Test.Testing.Fakes;

[Trait("Category", "Unit")]
public class FakeFileStorageTest
{
    [Fact]
    public async Task UploadAndDownload_ShouldRoundTripContent()
    {
        FakeFileStorage storage = new();
        byte[] content = "hello world"u8.ToArray();

        FileUploadResult upload = await storage.UploadAsync("docs/readme.txt", content, "text/plain");
        FileDownloadResult download = await storage.DownloadAsync("docs/readme.txt");

        upload.Success.Should().BeTrue();
        download.Success.Should().BeTrue();
        download.Content.Should().Equal(content);
    }

    [Fact]
    public async Task UploadAsync_ShouldNormalizePathSeparatorsAndTrimSlashes()
    {
        FakeFileStorage storage = new();
        byte[] content = [1, 2, 3];

        await storage.UploadAsync("\\folder\\file.bin\\", content, "application/octet-stream");

        storage.HasFile("folder/file.bin").Should().BeTrue();
        storage.StoredFilePaths.Should().ContainSingle().Which.Should().Be("folder/file.bin");
    }

    [Fact]
    public async Task UploadAsync_WhenShouldUploadFail_ShouldReturnFailedWithoutStoring()
    {
        FakeFileStorage storage = new()
        {
            ShouldUploadFail = true,
            FailureMessage = "Upload rejected"
        };

        FileUploadResult result = await storage.UploadAsync("fail.txt", [1], "text/plain");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Upload rejected");
        storage.FileCount.Should().Be(0);
    }

    [Fact]
    public async Task DownloadAsync_WhenShouldDownloadFail_ShouldReturnFailed()
    {
        FakeFileStorage storage = new() { ShouldDownloadFail = true };
        storage.SeedFile("seed.txt", [1, 2, 3]);

        FileDownloadResult result = await storage.DownloadAsync("seed.txt");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAsync_WhenFileNotFound_ShouldReturnNotFound()
    {
        FakeFileStorage storage = new();

        FileDownloadResult result = await storage.DownloadAsync("missing/file.txt");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("missing/file.txt");
    }

    [Fact]
    public void SeedFile_HasFile_GetFileContent_ShouldWorkTogether()
    {
        FakeFileStorage storage = new();
        byte[] content = [10, 20, 30];

        storage.SeedFile("seed/data.bin", content, "application/octet-stream");

        storage.HasFile("seed/data.bin").Should().BeTrue();
        storage.GetFileContent("seed/data.bin").Should().Equal(content);
        storage.FileCount.Should().Be(1);
    }

    [Fact]
    public async Task CopyAsync_ShouldDuplicateFileContent()
    {
        FakeFileStorage storage = new();
        byte[] content = "copy me"u8.ToArray();
        storage.SeedFile("source.txt", content, "text/plain");

        bool copied = await storage.CopyAsync("source.txt", "destination.txt");

        copied.Should().BeTrue();
        storage.GetFileContent("destination.txt").Should().Equal(content);
        storage.HasFile("source.txt").Should().BeTrue();
    }

    [Fact]
    public async Task CopyAsync_WhenSourceMissing_ShouldReturnFalse()
    {
        FakeFileStorage storage = new();

        bool copied = await storage.CopyAsync("missing.txt", "dest.txt");

        copied.Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        FakeFileStorage storage = new();
        byte[] content = "move me"u8.ToArray();
        storage.SeedFile("old/path.txt", content, "text/plain");

        bool moved = await storage.MoveAsync("old/path.txt", "new/path.txt");

        moved.Should().BeTrue();
        storage.HasFile("old/path.txt").Should().BeFalse();
        storage.GetFileContent("new/path.txt").Should().Equal(content);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveExistingFile()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("delete-me.txt", [1], "text/plain");

        bool deleted = await storage.DeleteAsync("delete-me.txt");

        deleted.Should().BeTrue();
        storage.HasFile("delete-me.txt").Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFileMissing_ShouldReturnFalse()
    {
        FakeFileStorage storage = new();

        bool deleted = await storage.DeleteAsync("missing.txt");

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task ListFilesAsync_NonRecursive_ShouldListOnlyDirectChildren()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("reports/january/summary.txt", [1], "text/plain");
        storage.SeedFile("reports/february/summary.txt", [2], "text/plain");
        storage.SeedFile("reports/readme.txt", [3], "text/plain");

        List<string> files = [];
        await foreach (IFileMetadata metadata in storage.ListFilesAsync("reports", recursive: false))
        {
            files.Add(metadata.FilePath);
        }

        files.Should().ContainSingle().Which.Should().Be("reports/readme.txt");
    }

    [Fact]
    public async Task ListFilesAsync_Recursive_ShouldIncludeNestedFiles()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("data/a.txt", [1], "text/plain");
        storage.SeedFile("data/nested/b.txt", [2], "text/plain");

        List<string> files = [];
        await foreach (IFileMetadata metadata in storage.ListFilesAsync("data", recursive: true))
        {
            files.Add(metadata.FilePath);
        }

        files.Should().HaveCount(2);
        files.Should().Contain("data/a.txt");
        files.Should().Contain("data/nested/b.txt");
    }

    [Fact]
    public void ClearFiles_ShouldRemoveAllStoredFiles()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("a.txt", [1], "text/plain");
        storage.SeedFile("b.txt", [2], "text/plain");

        storage.ClearFiles();

        storage.FileCount.Should().Be(0);
        storage.StoredFilePaths.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReflectStoredFiles()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("exists.txt", [1], "text/plain");

        (await storage.ExistsAsync("exists.txt")).Should().BeTrue();
        (await storage.ExistsAsync("missing.txt")).Should().BeFalse();
    }
}
