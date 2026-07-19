//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class WebRequestHelperTest
{
    private sealed class QueryModel
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public List<string>? Tags { get; set; }
    }

    private sealed class SpecialCharsModel
    {
        public string? Query { get; set; }
    }

    [Fact]
    public void ToQueryString_WithNullObjects_ShouldReturnEmpty()
    {
        // Act
        string result = WebRequestHelper.ToQueryString(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToQueryString_WithObjectProperties_ShouldReturnNameValuePairs()
    {
        // Arrange
        var model = new QueryModel { Name = "Alice", Age = 30 };

        // Act
        string result = WebRequestHelper.ToQueryString(model);

        // Assert
        result.Should().Contain("Name=Alice");
        result.Should().Contain("Age=30");
    }

    [Fact]
    public void ToQueryString_WithNullProperties_ShouldSkipNullValues()
    {
        // Arrange
        var model = new QueryModel { Name = "Bob", Age = null };

        // Act
        string result = WebRequestHelper.ToQueryString(model);

        // Assert
        result.Should().Contain("Name=Bob");
        result.Should().NotContain("Age=");
    }

    [Fact]
    public void ToQueryString_WithCollectionProperty_ShouldReturnMultipleEntries()
    {
        // Arrange
        var model = new QueryModel
        {
            Tags = ["alpha", "beta"]
        };

        // Act
        string result = WebRequestHelper.ToQueryString(model);

        // Assert
        result.Should().Contain("Tags=alpha");
        result.Should().Contain("Tags=beta");
    }

    [Fact]
    public void ToQueryString_WithSpecialCharacters_ShouldUrlEncodeValues()
    {
        // Arrange
        var model = new SpecialCharsModel { Query = "a b&c=d" };

        // Act
        string result = WebRequestHelper.ToQueryString(model);

        // Assert
        result.Should().Be("Query=a+b%26c%3dd");
    }

    [Fact]
    public void EncodingRequest_Default_ShouldBeUtf8()
    {
        // Assert
        WebRequestHelper.EncodingRequest.Should().BeSameAs(Encoding.UTF8);
    }

    [Fact]
    public void SetLogger_WithValidLogger_ShouldNotThrow()
    {
        // Arrange
        var logger = new Mock<ILogger>();

        // Act
        Action act = () => WebRequestHelper.SetLogger(logger.Object);

        // Assert
        act.Should().NotThrow();
    }
}
