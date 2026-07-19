//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Helpers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class DirectoryHelperTest
{
    [Fact]
    public void GetExecutingDirectory_ShouldReturnNonEmptyPath()
    {
        // Act
        string path = DirectoryHelper.GetExecutingDirectory();

        // Assert
        path.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void ExistsOrCreate_WithNewPath_ShouldCreateDirectoryAndReturnTrue()
    {
        // Arrange
        string tempPath = HelpersTestHelpers.CreateTempDirectory();

        try
        {
            // Act
            bool result = DirectoryHelper.ExistsOrCreate(tempPath);

            // Assert
            result.Should().BeTrue();
            Directory.Exists(tempPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }

    [Fact]
    public void ExistsOrCreate_WithExistingPath_ShouldReturnTrue()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();

        // Act
        bool result = DirectoryHelper.ExistsOrCreate(temp.Path);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(temp.Path).Should().BeTrue();
    }

    [Fact]
    public void ExistsOrCreate_WithNullPath_ShouldReturnFalse()
    {
        // Act
        bool result = DirectoryHelper.ExistsOrCreate(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExistsOrCreate_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        const string invalidPath = "C:\\invalid<>path";

        // Act
        bool result = DirectoryHelper.ExistsOrCreate(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DirectoryService_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => _ = new DirectoryService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void DirectoryService_ExistsOrCreate_ShouldDelegateToDirectoryHelper()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();
        var logger = new Mock<ILogger<DirectoryService>>();
        var service = new DirectoryService(logger.Object);

        // Act
        bool result = service.ExistsOrCreate(temp.Path);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(temp.Path).Should().BeTrue();
    }

    [Fact]
    public void DirectoryService_GetExecutingDirectory_ShouldDelegateToDirectoryHelper()
    {
        // Arrange
        var logger = new Mock<ILogger<DirectoryService>>();
        var service = new DirectoryService(logger.Object);

        // Act
        string servicePath = service.GetExecutingDirectory();
        string helperPath = DirectoryHelper.GetExecutingDirectory();

        // Assert
        servicePath.Should().Be(helperPath);
        servicePath.Should().NotBeNullOrWhiteSpace();
    }
}
