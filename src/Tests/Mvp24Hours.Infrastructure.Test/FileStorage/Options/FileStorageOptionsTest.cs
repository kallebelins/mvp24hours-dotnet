//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Options;

[Trait("Category", "Unit")]
public class FileStorageOptionsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        FileStorageOptions options = FileStorageOptions.Default;

        options.BasePath.Should().BeEmpty();
        options.MaxFileSize.Should().Be(100 * 1024 * 1024);
        options.MinFileSize.Should().BeNull();
        options.AllowedExtensions.Should().BeNull();
        options.BlockedExtensions.Should().BeNull();
        options.AllowedContentTypes.Should().BeNull();
        options.BlockedContentTypes.Should().BeNull();
        options.CreateDirectoriesIfNotExists.Should().BeTrue();
        options.OverwriteExistingFiles.Should().BeTrue();
        options.DefaultContentType.Should().Be("application/octet-stream");
        options.ChunkSize.Should().Be(65536);
        options.ValidateFileContent.Should().BeFalse();
        options.ProviderOptions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Validate_WithDefaultOptions_ShouldReturnNoErrors()
    {
        FileStorageOptions options = FileStorageOptions.Default;

        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenMinFileSizeGreaterThanMaxFileSize_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.MinFileSize = 200;
        options.MaxFileSize = 100;

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Minimum file size cannot be greater than maximum file size.");
    }

    [Fact]
    public void Validate_WhenMinFileSizeIsNegative_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.MinFileSize = -1;

        IList<string> errors = options.Validate();

        errors.Should().Contain("Minimum file size cannot be negative.");
    }

    [Fact]
    public void Validate_WhenMaxFileSizeIsNegative_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.MaxFileSize = -1;

        IList<string> errors = options.Validate();

        errors.Should().Contain("Maximum file size cannot be negative.");
    }

    [Fact]
    public void Validate_WhenChunkSizeIsZero_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.ChunkSize = 0;

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Chunk size must be greater than zero.");
    }

    [Fact]
    public void Validate_WhenChunkSizeIsNegative_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.ChunkSize = -1024;

        options.Validate().Should().Contain("Chunk size must be greater than zero.");
    }

    [Fact]
    public void Validate_WhenExtensionIsBothAllowedAndBlocked_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.AllowedExtensions = ["pdf", "txt"];
        options.BlockedExtensions = ["exe", "PDF"];

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Contain("Extensions cannot be both allowed and blocked")
            .And.Contain("pdf");
    }

    [Fact]
    public void Validate_WhenContentTypeIsBothAllowedAndBlocked_ShouldReturnError()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.AllowedContentTypes = ["application/pdf", "text/plain"];
        options.BlockedContentTypes = ["application/x-executable", "APPLICATION/PDF"];

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Contain("Content types cannot be both allowed and blocked")
            .And.Contain("application/pdf");
    }

    [Fact]
    public void Validate_WithMultipleViolations_ShouldReturnAllErrors()
    {
        FileStorageOptions options = FileStorageTestHelpers.CreateOptions();
        options.MinFileSize = 500;
        options.MaxFileSize = 100;
        options.ChunkSize = 0;
        options.AllowedExtensions = ["pdf"];
        options.BlockedExtensions = ["pdf"];

        IList<string> errors = options.Validate();

        errors.Should().HaveCount(3);
        errors.Should().Contain("Minimum file size cannot be greater than maximum file size.");
        errors.Should().Contain("Chunk size must be greater than zero.");
        errors.Should().ContainMatch("*Extensions cannot be both allowed and blocked*");
    }

    [Fact]
    public void ForImages_ShouldConfigureImageValidationRules()
    {
        FileStorageOptions options = FileStorageOptions.ForImages;

        options.MaxFileSize.Should().Be(10 * 1024 * 1024);
        options.AllowedExtensions.Should().Contain(["jpg", "jpeg", "png", "gif", "webp", "bmp"]);
        options.AllowedContentTypes.Should().Contain("image/jpeg");
        options.DefaultContentType.Should().Be("image/jpeg");
        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void ForDocuments_ShouldConfigureDocumentValidationRules()
    {
        FileStorageOptions options = FileStorageOptions.ForDocuments;

        options.MaxFileSize.Should().Be(50 * 1024 * 1024);
        options.AllowedExtensions.Should().Contain("pdf");
        options.AllowedContentTypes.Should().Contain("application/pdf");
        options.DefaultContentType.Should().Be("application/pdf");
        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void ForSecureUploads_ShouldConfigureStrictSecurityRules()
    {
        FileStorageOptions options = FileStorageOptions.ForSecureUploads;

        options.MaxFileSize.Should().Be(5 * 1024 * 1024);
        options.BlockedExtensions.Should().Contain("exe");
        options.BlockedContentTypes.Should().Contain("application/x-executable");
        options.ValidateFileContent.Should().BeTrue();
        options.Validate().Should().BeEmpty();
    }
}
