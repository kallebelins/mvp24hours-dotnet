using System.Runtime.Serialization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;

namespace Mvp24Hours.WebAPI.Test.ContentNegotiation;

[Trait("Category", "Unit")]
public class XmlContentFormatterTest
{
    [Fact]
    public void SupportedMediaTypes_ShouldIncludeXmlVariants()
    {
        var formatter = new XmlContentFormatter();

        formatter.SupportedMediaTypes.Should().Contain("application/xml");
        formatter.SupportedMediaTypes.Should().Contain("text/xml");
        formatter.SupportedMediaTypes.Should().Contain("application/problem+xml");
        formatter.PrimaryMediaType.Should().Be("application/xml");
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(DayOfWeek), true)]
    [InlineData(typeof(List<string>), true)]
    [InlineData(typeof(Stream), false)]
    public void CanWrite_ShouldEvaluateTypeSupport(Type type, bool expected)
    {
        var formatter = new XmlContentFormatter();

        formatter.CanWrite(type).Should().Be(expected);
    }

    [Fact]
    public void CanWrite_WithNullOrAbstractType_ShouldReturnFalse()
    {
        var formatter = new XmlContentFormatter();

        formatter.CanWrite(null!).Should().BeFalse();
        formatter.CanWrite(typeof(AbstractXmlDto)).Should().BeFalse();
    }

    [Fact]
    public void Serialize_WithNull_ShouldReturnNullElement()
    {
        var formatter = new XmlContentFormatter();

        string result = formatter.Serialize(null);

        result.Should().Contain("<null />");
    }

    [Fact]
    public void Serialize_WithObject_ShouldProduceXml()
    {
        var formatter = new XmlContentFormatter();
        var dto = new XmlTestDto { Name = "test", Value = 42 };

        string result = formatter.Serialize(dto);

        result.Should().Contain("test");
        result.Should().Contain("42");
    }

    [Fact]
    public async Task SerializeAsync_ShouldWriteToStream()
    {
        var formatter = new XmlContentFormatter();
        var dto = new XmlTestDto { Name = "async", Value = 7 };
        await using var stream = new MemoryStream();

        await formatter.SerializeAsync(stream, dto, Encoding.UTF8);

        string result = Encoding.UTF8.GetString(stream.ToArray());
        result.Should().Contain("async");
    }

    [Fact]
    public async Task SerializeAsync_WithNull_ShouldWriteNullElement()
    {
        var formatter = new XmlContentFormatter();
        await using var stream = new MemoryStream();

        await formatter.SerializeAsync(stream, null, Encoding.UTF8);

        string result = Encoding.UTF8.GetString(stream.ToArray());
        result.Should().Contain("<null />");
    }

    [Fact]
    public void Serialize_WithDataContractSerializer_ShouldProduceXml()
    {
        var options = new ContentNegotiationOptions
        {
            XmlOptions = new XmlSerializationOptions { UseDataContractSerializer = true }
        };
        var formatter = new XmlContentFormatter(options);
        var dto = new DataContractXmlDto { Title = "dc" };

        string result = formatter.Serialize(dto);

        result.Should().Contain("dc");
    }

    [Fact]
    public void Serialize_WithCollection_ShouldUseCollectionRootName()
    {
        var options = new ContentNegotiationOptions
        {
            XmlOptions = new XmlSerializationOptions { CollectionRootName = "Items" }
        };
        var formatter = new XmlContentFormatter(options);
        var items = new List<XmlTestDto> { new() { Name = "a", Value = 1 } };

        string result = formatter.Serialize(items);

        result.Should().Contain("Items");
        result.Should().Contain("a");
    }

    [Fact]
    public void Serialize_WithDefaultNamespace_ShouldApplyNamespace()
    {
        var options = new ContentNegotiationOptions
        {
            XmlOptions = new XmlSerializationOptions { DefaultNamespace = "http://test.example" }
        };
        var formatter = new XmlContentFormatter(options);
        var dto = new XmlTestDto { Name = "ns", Value = 1 };

        string result = formatter.Serialize(dto);

        result.Should().Contain("http://test.example");
    }

    [Fact]
    public void Constructor_WithIOptions_ShouldUseOptions()
    {
        IOptions<ContentNegotiationOptions> options = Options.Create(new ContentNegotiationOptions
        {
            UseRfc7807ContentTypeForProblemDetails = true
        });
        var formatter = new XmlContentFormatter(options);

        formatter.GetProblemDetailsContentType().Should().Be("application/problem+xml");
    }

    [Fact]
    public void GetContentType_WithCharset_ShouldIncludeCharset()
    {
        var formatter = new XmlContentFormatter();

        formatter.GetContentType("utf-8").Should().Be("application/xml; charset=utf-8");
        formatter.GetContentType().Should().Be("application/xml");
    }

    [Fact]
    public void SerializeProblemDetails_ShouldIncludeCoreFields()
    {
        var formatter = new XmlContentFormatter();
        var details = new ProblemDetails
        {
            Type = "https://example.com/errors/not-found",
            Title = "Not Found",
            Status = 404,
            Detail = "Resource missing",
            Instance = "/api/items/1"
        };

        string result = formatter.SerializeProblemDetails(details);

        result.Should().Contain("Not Found");
        result.Should().Contain("404");
        result.Should().Contain("Resource missing");
    }

    [Fact]
    public void SerializeProblemDetails_WithExtensions_ShouldSerializeValues()
    {
        var formatter = new XmlContentFormatter();
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Status = 400
        };
        details.Extensions["traceId"] = "abc-123";
        details.Extensions["errors"] = new[] { "field1", "field2" };
        details.Extensions["meta"] = new Dictionary<string, object> { ["key"] = "value" };

        string result = formatter.SerializeProblemDetails(details);

        result.Should().Contain("traceId");
        result.Should().Contain("abc-123");
        result.Should().Contain("field1");
        result.Should().Contain("value");
    }

    [Fact]
    public async Task SerializeProblemDetailsAsync_ShouldWriteToStream()
    {
        var formatter = new XmlContentFormatter();
        var details = new ProblemDetails { Title = "Error", Status = 500 };
        await using var stream = new MemoryStream();

        await formatter.SerializeProblemDetailsAsync(stream, details, Encoding.UTF8);

        string result = Encoding.UTF8.GetString(stream.ToArray());
        result.Should().Contain("Error");
    }

    [Fact]
    public void GetProblemDetailsContentType_WithRfc7807Disabled_ShouldUseApplicationXml()
    {
        var options = new ContentNegotiationOptions { UseRfc7807ContentTypeForProblemDetails = false };
        var formatter = new XmlContentFormatter(options);

        formatter.GetProblemDetailsContentType("utf-8").Should().Be("application/xml; charset=utf-8");
    }

    [Fact]
    public void GetOrCreateSerializer_ShouldCacheRepeatedTypes()
    {
        var formatter = new XmlContentFormatter();
        var dto = new XmlTestDto { Name = "cache", Value = 1 };

        string first = formatter.Serialize(dto);
        string second = formatter.Serialize(dto);

        first.Should().Be(second);
    }

    [DataContract(Namespace = "http://test.example")]
    private sealed class DataContractXmlDto
    {
        [DataMember]
        public string Title { get; set; } = string.Empty;
    }

    private abstract class AbstractXmlDto;

    public class XmlTestDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
