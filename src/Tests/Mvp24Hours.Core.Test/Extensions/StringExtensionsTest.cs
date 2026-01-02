namespace Mvp24Hours.Core.Test.Extensions;

/// <summary>
/// Testes unitários para StringExtensions (métodos existentes).
/// </summary>
public class StringExtensionsTest
{
    #region [ RegexReplace Tests ]

    [Fact]
    public void RegexReplace_WithSimplePattern_ReplacesCorrectly()
    {
        // Arrange
        var input = "Hello World 123";

        // Act
        var result = input.RegexReplace(@"\d+", "456");

        // Assert
        result.Should().Be("Hello World 456");
    }

    [Fact]
    public void RegexReplace_WithComplexPattern_ReplacesMultiple()
    {
        // Arrange
        var input = "test123test456test";

        // Act
        var result = input.RegexReplace(@"\d+", "X");

        // Assert
        result.Should().Be("testXtestXtest");
    }

    #endregion

    #region [ ReplaceEnd Tests ]

    [Theory]
    [InlineData("HelloWorld", "World", "Universe", "HelloUniverse")]
    [InlineData("test.txt", ".txt", ".doc", "test.doc")]
    [InlineData("filename", "name", "path", "filepath")]
    public void ReplaceEnd_ReplacesEndOfString(string input, string value, string replacement, string expected)
    {
        // Act
        var result = input.ReplaceEnd(value, replacement);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ReplaceEnd_WhenValueNotAtEnd_DoesNotReplace()
    {
        // Arrange
        var input = "HelloWorld";

        // Act
        var result = input.ReplaceEnd("Hello", "Hi");

        // Assert
        result.Should().Be("HelloWorld");
    }

    #endregion

    #region [ RemoveEnd Tests ]

    [Theory]
    [InlineData("test.txt", ".txt", "test")]
    [InlineData("filename.doc", ".doc", "filename")]
    [InlineData("Hello World", " World", "Hello")]
    public void RemoveEnd_RemovesEndOfString(string input, string value, string expected)
    {
        // Act
        var result = input.RemoveEnd(value);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region [ Truncate Tests ]

    [Theory]
    [InlineData("Hello World", 5, "Hello")]
    [InlineData("Hello World", 11, "Hello World")]
    [InlineData("Hello World", 20, "Hello World")]
    [InlineData("", 5, "")]
    public void Truncate_ReturnsExpectedLength(string input, int size, string expected)
    {
        // Act
        var result = input.Truncate(size);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.Truncate(5);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ Reticence Tests ]

    [Theory]
    [InlineData("Hello World Example", 10, "Hello Worl...")]
    [InlineData("Test", 10, "Test")]
    [InlineData("12345", 3, "123...")]
    public void Reticence_AddsEllipsisWhenTruncated(string input, int size, string expected)
    {
        // Act
        var result = input.Reticence(size);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Reticence_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.Reticence(10);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ SubstringSafe Tests ]

    [Fact]
    public void SubstringSafe_WithValidRange_ReturnsSubstring()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.SubstringSafe(0, 5);

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void SubstringSafe_WhenStartBeyondLength_ReturnsEmpty()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.SubstringSafe(10, 5);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SubstringSafe_WhenLengthExceedsString_ReturnsToEnd()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.SubstringSafe(6, 100);

        // Assert
        result.Should().Be("World");
    }

    [Fact]
    public void SubstringSafe_WithDefaultLength_ReturnsToEnd()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.SubstringSafe(6);

        // Assert
        result.Should().Be("World");
    }

    #endregion

    #region [ SqlSafe Tests ]

    [Theory]
    [InlineData("SELECT * FROM table", "SELECT * FROM table")]
    [InlineData("test--comment", "testcomment")]
    [InlineData("It's a test", "It''s a test")]
    [InlineData("-- comment", " comment")]
    public void SqlSafe_SanitizesInput(string input, string expected)
    {
        // Act
        var result = input.SqlSafe();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SqlSafe_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.SqlSafe();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ Format Tests ]

    [Fact]
    public void Format_WithValidPlaceholders_FormatsCorrectly()
    {
        // Arrange
        var template = "Hello {0}, you are {1} years old";

        // Act
        var result = template.Format("John", 30);

        // Assert
        result.Should().Be("Hello John, you are 30 years old");
    }

    [Fact]
    public void Format_WithMultiplePlaceholders_FormatsCorrectly()
    {
        // Arrange
        var template = "{0} + {1} = {2}";

        // Act
        var result = template.Format(2, 3, 5);

        // Assert
        result.Should().Be("2 + 3 = 5");
    }

    #endregion

    #region [ Performance Tests ]

    [Fact]
    public void StringExtensions_WithLargeString_ShouldPerformEfficiently()
    {
        // Arrange
        var input = new string('a', 100000);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = input.Truncate(50);
        stopwatch.Stop();

        // Assert
        result.Should().HaveLength(50);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
    }

    #endregion
}
