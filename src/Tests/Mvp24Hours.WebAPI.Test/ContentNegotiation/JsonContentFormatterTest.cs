using System.Text;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;

namespace Mvp24Hours.WebAPI.Test.ContentNegotiation;

[Trait("Category", "Unit")]
public class JsonContentFormatterTest
{
    public sealed class SampleModel
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }

    #region [ Constructors ]

    [Fact]
    public void Constructor_Parameterless_UsesDefaultOptions()
    {
        // Act
        var formatter = new JsonContentFormatter();

        // Assert
        formatter.PrimaryMediaType.Should().Be("application/json");
    }

    [Fact]
    public void Constructor_WithNullOptionsValue_FallsBackToDefaults()
    {
        // Act
        var formatter = new JsonContentFormatter((ContentNegotiationOptions)null!);

        // Assert
        formatter.Should().NotBeNull();
    }

    #endregion

    #region [ SupportedMediaTypes / CanWrite ]

    [Fact]
    public void SupportedMediaTypes_ContainsExpectedTypes()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.SupportedMediaTypes.Should().Contain(["application/json", "text/json", "application/problem+json"]);
    }

    [Fact]
    public void CanWrite_WithConcreteType_ReturnsTrue()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.CanWrite(typeof(SampleModel)).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_WithNullType_ReturnsFalse()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.CanWrite(null!).Should().BeFalse();
    }

    [Fact]
    public void CanWrite_WithStreamType_ReturnsFalse()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.CanWrite(typeof(Stream)).Should().BeFalse();
    }

    #endregion

    #region [ Serialize / SerializeAsync ]

    [Fact]
    public void Serialize_WithNullValue_ReturnsNullLiteral()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act
        string result = formatter.Serialize(null);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithCamelCaseDefault_UsesCamelCasePropertyNames()
    {
        // Arrange
        var formatter = new JsonContentFormatter();
        var model = new SampleModel { Name = "Alice", Age = 30 };

        // Act
        string result = formatter.Serialize(model);

        // Assert
        result.Should().Contain("\"name\"").And.Contain("\"age\"");
    }

    [Fact]
    public void Serialize_WithCamelCaseDisabled_PreservesPropertyNameCasing()
    {
        // Arrange
        var options = new ContentNegotiationOptions { JsonOptions = new JsonSerializationOptions { UseCamelCase = false } };
        var formatter = new JsonContentFormatter(options);
        var model = new SampleModel { Name = "Alice" };

        // Act
        string result = formatter.Serialize(model);

        // Assert
        result.Should().Contain("\"Name\"");
    }

    [Fact]
    public void Serialize_WithIgnoreNullValues_OmitsNullProperties()
    {
        // Arrange
        var options = new ContentNegotiationOptions { JsonOptions = new JsonSerializationOptions { IgnoreNullValues = true } };
        var formatter = new JsonContentFormatter(options);
        var model = new SampleModel { Name = "Alice", Age = null };

        // Act
        string result = formatter.Serialize(model);

        // Assert
        result.Should().NotContain("age");
    }

    [Fact]
    public void Serialize_WithWriteIndentedTrue_ProducesMultilineOutput()
    {
        // Arrange
        var options = new ContentNegotiationOptions { JsonOptions = new JsonSerializationOptions { WriteIndented = true } };
        var formatter = new JsonContentFormatter(options);
        var model = new SampleModel { Name = "Alice", Age = 30 };

        // Act
        string result = formatter.Serialize(model);

        // Assert
        result.Should().Contain("\n");
    }

    [Fact]
    public async Task SerializeAsync_WithNullValue_WritesNullLiteralToStream()
    {
        // Arrange
        var formatter = new JsonContentFormatter();
        using var stream = new MemoryStream();

        // Act
        await formatter.SerializeAsync(stream, null, Encoding.UTF8);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).Should().Be("null");
    }

    [Fact]
    public async Task SerializeAsync_WithValue_WritesJsonToStream()
    {
        // Arrange
        var formatter = new JsonContentFormatter();
        using var stream = new MemoryStream();
        var model = new SampleModel { Name = "Bob", Age = 22 };

        // Act
        await formatter.SerializeAsync(stream, model, Encoding.UTF8);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();
        content.Should().Contain("\"name\":\"Bob\"");
    }

    #endregion

    #region [ GetContentType ]

    [Fact]
    public void GetContentType_WithoutCharset_ReturnsPlainApplicationJson()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.GetContentType().Should().Be("application/json");
    }

    [Fact]
    public void GetContentType_WithCharset_AppendsCharsetParameter()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.GetContentType("utf-8").Should().Be("application/json; charset=utf-8");
    }

    #endregion

    #region [ ProblemDetails ]

    [Fact]
    public void SerializeProblemDetails_ReturnsJsonRepresentation()
    {
        // Arrange
        var formatter = new JsonContentFormatter();
        var problem = new ProblemDetails { Title = "Bad Request", Status = 400 };

        // Act
        string result = formatter.SerializeProblemDetails(problem);

        // Assert
        result.Should().Contain("Bad Request");
    }

    [Fact]
    public async Task SerializeProblemDetailsAsync_WritesJsonToStream()
    {
        // Arrange
        var formatter = new JsonContentFormatter();
        using var stream = new MemoryStream();
        var problem = new ProblemDetails { Title = "Not Found", Status = 404 };

        // Act
        await formatter.SerializeProblemDetailsAsync(stream, problem, Encoding.UTF8);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).Should().Contain("Not Found");
    }

    [Fact]
    public void GetProblemDetailsContentType_WhenRfc7807Enabled_ReturnsProblemJson()
    {
        // Arrange
        var options = new ContentNegotiationOptions { UseRfc7807ContentTypeForProblemDetails = true };
        var formatter = new JsonContentFormatter(options);

        // Act & Assert
        formatter.GetProblemDetailsContentType().Should().Be("application/problem+json");
    }

    [Fact]
    public void GetProblemDetailsContentType_WhenRfc7807Disabled_ReturnsApplicationJson()
    {
        // Arrange
        var options = new ContentNegotiationOptions { UseRfc7807ContentTypeForProblemDetails = false };
        var formatter = new JsonContentFormatter(options);

        // Act & Assert
        formatter.GetProblemDetailsContentType().Should().Be("application/json");
    }

    [Fact]
    public void GetProblemDetailsContentType_WithCharset_AppendsCharsetParameter()
    {
        // Arrange
        var formatter = new JsonContentFormatter();

        // Act & Assert
        formatter.GetProblemDetailsContentType("utf-8").Should().Be("application/problem+json; charset=utf-8");
    }

    #endregion
}
