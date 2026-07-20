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
public class SoftDeletedFileTest
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSetAllProperties()
    {
        DateTimeOffset deletedAt = new(2024, 7, 1, 9, 0, 0, TimeSpan.Zero);
        IFileMetadata metadata = new FileMetadata("docs/file.txt", "file.txt", 128, "text/plain");

        SoftDeletedFile deletedFile = new(
            "docs/file.txt",
            metadata,
            deletedAt,
            deletionReason: "retention policy",
            deletedBy: "admin");

        deletedFile.FilePath.Should().Be("docs/file.txt");
        deletedFile.OriginalMetadata.Should().BeSameAs(metadata);
        deletedFile.DeletedAt.Should().Be(deletedAt);
        deletedFile.DeletionReason.Should().Be("retention policy");
        deletedFile.DeletedBy.Should().Be("admin");
    }

    [Fact]
    public void Constructor_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        IFileMetadata metadata = new FileMetadata("docs/file.txt", "file.txt", 1);

        Action act = () => _ = new SoftDeletedFile(null!, metadata, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void Constructor_WithNullOriginalMetadata_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new SoftDeletedFile("docs/file.txt", null!, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("originalMetadata");
    }
}
