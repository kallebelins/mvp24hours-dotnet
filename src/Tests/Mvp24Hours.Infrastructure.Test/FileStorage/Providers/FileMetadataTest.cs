//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Providers;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Providers;

[Trait("Category", "Unit")]
public class FileMetadataTest
{
    [Fact]
    public void Constructor_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileMetadata(null!, "file.txt", 100);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("filePath")
            .WithMessage("*File path cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileMetadata("  ", "file.txt", 100);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileMetadata("docs/file.txt", null!, 100);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name")
            .WithMessage("*File name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileMetadata("docs/file.txt", "  ", 100);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithMinimalArguments_ShouldSetRequiredProperties()
    {
        FileMetadata metadata = new("docs/file.txt", "file.txt", 2048);

        metadata.FilePath.Should().Be("docs/file.txt");
        metadata.Name.Should().Be("file.txt");
        metadata.Size.Should().Be(2048);
        metadata.ContentType.Should().BeNull();
        metadata.ETag.Should().BeNull();
        metadata.CustomProperties.Should().NotBeNull().And.BeEmpty();
        metadata.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        metadata.ModifiedAt.Should().Be(metadata.CreatedAt);
    }

    [Fact]
    public void Constructor_WithAllArguments_ShouldSetAllProperties()
    {
        DateTimeOffset createdAt = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset modifiedAt = new(2024, 6, 20, 14, 30, 0, TimeSpan.Zero);
        Dictionary<string, string> customProperties = new() { ["author"] = "tester" };

        FileMetadata metadata = new(
            "docs/report.pdf",
            "report.pdf",
            4096,
            "application/pdf",
            createdAt,
            modifiedAt,
            "etag-123",
            customProperties);

        metadata.FilePath.Should().Be("docs/report.pdf");
        metadata.Name.Should().Be("report.pdf");
        metadata.Size.Should().Be(4096);
        metadata.ContentType.Should().Be("application/pdf");
        metadata.CreatedAt.Should().Be(createdAt);
        metadata.ModifiedAt.Should().Be(modifiedAt);
        metadata.ETag.Should().Be("etag-123");
        metadata.CustomProperties.Should().BeSameAs(customProperties);
    }

    [Fact]
    public void Constructor_WithNullCustomProperties_ShouldCreateEmptyDictionary()
    {
        FileMetadata metadata = new("file.txt", "file.txt", 1, customProperties: null);

        metadata.CustomProperties.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithModifiedAtOnly_ShouldUseProvidedCreatedAtForModifiedAtDefault()
    {
        DateTimeOffset createdAt = new(2024, 3, 1, 8, 0, 0, TimeSpan.Zero);

        FileMetadata metadata = new("file.txt", "file.txt", 1, createdAt: createdAt);

        metadata.CreatedAt.Should().Be(createdAt);
        metadata.ModifiedAt.Should().Be(createdAt);
    }
}
