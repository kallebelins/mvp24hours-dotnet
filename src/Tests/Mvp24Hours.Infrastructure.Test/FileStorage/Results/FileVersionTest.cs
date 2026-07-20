//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Results;

[Trait("Category", "Unit")]
public class FileVersionTest
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSetAllProperties()
    {
        DateTimeOffset createdAt = new(2024, 2, 14, 16, 45, 0, TimeSpan.Zero);
        IFileMetadata metadata = new FileMetadata("docs/file.txt", "file.txt", 256, "text/plain");

        FileVersion version = new(
            "version-1",
            versionNumber: 3,
            metadata,
            isCurrentVersion: true,
            isDeleted: false,
            createdAt,
            description: "manual save");

        version.VersionId.Should().Be("version-1");
        version.VersionNumber.Should().Be(3);
        version.Metadata.Should().BeSameAs(metadata);
        version.IsCurrentVersion.Should().BeTrue();
        version.IsDeleted.Should().BeFalse();
        version.CreatedAt.Should().Be(createdAt);
        version.Description.Should().Be("manual save");
    }

    [Fact]
    public void Constructor_WithNullVersionId_ShouldThrowArgumentNullException()
    {
        IFileMetadata metadata = new FileMetadata("docs/file.txt", "file.txt", 1);

        Action act = () => _ = new FileVersion(
            null!,
            1,
            metadata,
            isCurrentVersion: false,
            isDeleted: false,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("versionId");
    }

    [Fact]
    public void Constructor_WithNullMetadata_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileVersion(
            "version-1",
            1,
            null!,
            isCurrentVersion: false,
            isDeleted: false,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
    }
}
