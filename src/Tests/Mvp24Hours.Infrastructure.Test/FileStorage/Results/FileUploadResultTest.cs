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
public class FileUploadResultTest
{
    [Fact]
    public void Successful_WithValidPath_ShouldSetSuccessProperties()
    {
        DateTimeOffset uploadedAt = new(2024, 5, 10, 12, 0, 0, TimeSpan.Zero);
        IFileMetadata metadata = CreateMetadata();

        var result = FileUploadResult.Successful("docs/file.txt", metadata, uploadedAt);

        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.FilePath.Should().Be("docs/file.txt");
        result.Metadata.Should().BeSameAs(metadata);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
        result.UploadedAt.Should().Be(uploadedAt);
    }

    [Fact]
    public void Successful_WithNullOrEmptyPath_ShouldThrowArgumentNullException()
    {
        Action nullPath = () => FileUploadResult.Successful(null!);
        Action emptyPath = () => FileUploadResult.Successful("  ");

        nullPath.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
        emptyPath.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void Failed_WithErrorMessage_ShouldSetFailureProperties()
    {
        DateTimeOffset uploadedAt = new(2024, 5, 10, 12, 0, 0, TimeSpan.Zero);
        InvalidOperationException exception = new("inner failure");

        var result = FileUploadResult.Failed("upload failed", exception, uploadedAt);

        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.FilePath.Should().BeNull();
        result.Metadata.Should().BeNull();
        result.ErrorMessage.Should().Be("upload failed");
        result.Exception.Should().BeSameAs(exception);
        result.UploadedAt.Should().Be(uploadedAt);
    }

    [Fact]
    public void Failed_WithNullOrEmptyErrorMessage_ShouldThrowArgumentException()
    {
        Action nullMessage = () => FileUploadResult.Failed((string)null!);
        Action emptyMessage = () => FileUploadResult.Failed("  ");

        nullMessage.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
        emptyMessage.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_WithException_ShouldUseExceptionMessage()
    {
        InvalidOperationException exception = new("disk full");

        var result = FileUploadResult.Failed(exception);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("disk full");
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Failed_WithNullException_ShouldThrowArgumentNullException()
    {
        Action act = () => FileUploadResult.Failed((Exception)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    private static FileMetadata CreateMetadata()
    {
        return new FileMetadata("docs/file.txt", "file.txt", FileStorageTestHelpers.CreateContent().Length, "text/plain");
    }
}
