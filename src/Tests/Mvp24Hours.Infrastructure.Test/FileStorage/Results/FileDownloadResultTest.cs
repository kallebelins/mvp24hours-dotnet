//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Results;

[Trait("Category", "Unit")]
public class FileDownloadResultTest
{
    [Fact]
    public void Successful_WithValidContent_ShouldSetSuccessProperties()
    {
        byte[] content = FileStorageTestHelpers.CreateContent("download");
        DateTimeOffset downloadedAt = new(2024, 5, 10, 12, 0, 0, TimeSpan.Zero);
        IFileMetadata metadata = CreateMetadata();

        FileDownloadResult result = FileDownloadResult.Successful(content, metadata, downloadedAt);

        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        result.Content.Should().BeSameAs(content);
        result.Metadata.Should().BeSameAs(metadata);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
        result.DownloadedAt.Should().Be(downloadedAt);
    }

    [Fact]
    public void Successful_WithNullContent_ShouldThrowArgumentNullException()
    {
        Action act = () => FileDownloadResult.Successful(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("content");
    }

    [Fact]
    public void Failed_WithErrorMessage_ShouldSetFailureProperties()
    {
        IOException exception = new("read error");
        DateTimeOffset downloadedAt = new(2024, 5, 10, 12, 0, 0, TimeSpan.Zero);

        FileDownloadResult result = FileDownloadResult.Failed("download failed", exception, downloadedAt);

        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();
        result.Content.Should().BeNull();
        result.Metadata.Should().BeNull();
        result.ErrorMessage.Should().Be("download failed");
        result.Exception.Should().BeSameAs(exception);
        result.DownloadedAt.Should().Be(downloadedAt);
    }

    [Fact]
    public void Failed_WithNullOrEmptyErrorMessage_ShouldThrowArgumentException()
    {
        Action nullMessage = () => FileDownloadResult.Failed((string)null!);
        Action emptyMessage = () => FileDownloadResult.Failed("  ");

        nullMessage.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
        emptyMessage.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_WithException_ShouldUseExceptionMessage()
    {
        IOException exception = new("network error");

        FileDownloadResult result = FileDownloadResult.Failed(exception);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("network error");
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Failed_WithNullException_ShouldThrowArgumentNullException()
    {
        Action act = () => FileDownloadResult.Failed((Exception)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    [Fact]
    public void NotFound_ShouldSetNotFoundProperties()
    {
        DateTimeOffset downloadedAt = new(2024, 5, 10, 12, 0, 0, TimeSpan.Zero);

        FileDownloadResult result = FileDownloadResult.NotFound("missing.txt", downloadedAt);

        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.IsNotFound.Should().BeTrue();
        result.Content.Should().BeNull();
        result.ErrorMessage.Should().Be("File not found: missing.txt");
        result.DownloadedAt.Should().Be(downloadedAt);
    }

    [Theory]
    [InlineData("File does not exist")]
    [InlineData("Resource NOT FOUND")]
    public void IsNotFound_WhenErrorMessageIndicatesMissingFile_ShouldBeTrue(string errorMessage)
    {
        FileDownloadResult result = FileDownloadResult.Failed(errorMessage);

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public void NotFound_WithNullOrEmptyPath_ShouldThrowArgumentException()
    {
        Action nullPath = () => FileDownloadResult.NotFound(null!);
        Action emptyPath = () => FileDownloadResult.NotFound("  ");

        nullPath.Should().Throw<ArgumentException>().WithParameterName("filePath");
        emptyPath.Should().Throw<ArgumentException>().WithParameterName("filePath");
    }

    private static FileMetadata CreateMetadata()
    {
        return new FileMetadata("docs/file.txt", "file.txt", 9, "text/plain");
    }
}
