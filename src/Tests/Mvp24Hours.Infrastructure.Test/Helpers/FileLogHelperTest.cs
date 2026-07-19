//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class FileLogHelperTest
{
    private sealed class LogDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Fact]
    public void WriteLog_WithValidPath_ShouldCreateDatedFolderAndLogFileWithJson()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();
        var dto = new LogDto { Name = "Test", Value = 42 };
        string expectedFolder = Path.Combine(temp.Path, $"{DateTime.Today:yyyy_MM_dd}");

        // Act
        FileLogHelper.WriteLog(dto, temp.Path);

        // Assert
        Directory.Exists(expectedFolder).Should().BeTrue();
        string[] logFiles = Directory.GetFiles(expectedFolder, "*.log");
        logFiles.Should().HaveCount(1);
        string content = File.ReadAllText(logFiles[0]);
        content.Should().Contain("\"Name\":\"Test\"");
        content.Should().Contain("\"Value\":42");
    }

    [Fact]
    public void WriteLog_WithSuffixFilename_ShouldPrefixFilename()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();
        var dto = new LogDto { Name = "Suffix", Value = 1 };
        const string suffix = "MyApp";
        string expectedFolder = Path.Combine(temp.Path, $"{DateTime.Today:yyyy_MM_dd}");

        // Act
        FileLogHelper.WriteLog(dto, temp.Path, suffixFilename: suffix);

        // Assert
        string[] logFiles = Directory.GetFiles(expectedFolder, "*.log");
        logFiles.Should().HaveCount(1);
        Path.GetFileName(logFiles[0]).Should().StartWith($"{suffix.ToLower()}_");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WriteLog_WithEmptyOrNullLogPath_ShouldBeNoOp(string? logPath)
    {
        // Arrange
        var dto = new LogDto { Name = "NoOp", Value = 0 };

        // Act
        Action act = () => FileLogHelper.WriteLog(dto, logPath!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WriteLogToken_AndReadLogToken_ShouldRoundTrip()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();
        const string token = "my-token";
        const string fileName = "payload";
        var dto = new LogDto { Name = "RoundTrip", Value = 99 };

        // Act
        FileLogHelper.WriteLogToken(token, fileName, dto, temp.Path);
        LogDto? read = FileLogHelper.ReadLogToken<LogDto>(token, fileName, temp.Path);

        // Assert
        read.Should().NotBeNull();
        read!.Name.Should().Be(dto.Name);
        read.Value.Should().Be(dto.Value);
    }

    [Fact]
    public void ReadLogToken_WithMissingFile_ShouldReturnDefault()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();

        // Act
        LogDto? read = FileLogHelper.ReadLogToken<LogDto>("missing", "file", temp.Path);

        // Assert
        read.Should().BeNull();
    }

    [Fact]
    public void WriteLogToken_WithNullObject_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var temp = new HelpersTestHelpers.TempDirectory();

        // Act
        Action act = () => FileLogHelper.WriteLogToken<LogDto>("token", "file", null!, temp.Path);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WriteLogToken_WithEmptyOrNullLogPath_ShouldBeNoOp(string? logPath)
    {
        // Arrange
        var dto = new LogDto { Name = "NoOp", Value = 0 };

        // Act
        Action act = () => FileLogHelper.WriteLogToken("token", "file", dto, logPath!);

        // Assert
        act.Should().NotThrow();
        string expectedPath = Path.Combine(logPath ?? string.Empty, "token", "file.json");
        File.Exists(expectedPath).Should().BeFalse();
    }
}
