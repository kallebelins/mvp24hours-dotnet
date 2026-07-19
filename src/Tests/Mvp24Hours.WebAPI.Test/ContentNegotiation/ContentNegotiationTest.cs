using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.ContentNegotiation;

[Trait("Category", "Unit")]
public class ContentNegotiationTest
{
    [Fact]
    public void AcceptHeaderNegotiator_Should_ChooseJson_FromAcceptHeader()
    {
        var options = new ContentNegotiationOptions();
        var registry = new ContentFormatterRegistry(options);
        var sut = new AcceptHeaderNegotiator(options, registry);
        var context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["Accept"] = "application/json";

        ContentNegotiationResult result = sut.Negotiate(context);

        result.Success.Should().BeTrue();
        result.MediaType.Should().Be("application/json");
    }

    [Fact]
    public void AcceptHeaderNegotiator_Should_UseFormatQueryParameter()
    {
        var options = new ContentNegotiationOptions();
        var registry = new ContentFormatterRegistry(options);
        var sut = new AcceptHeaderNegotiator(options, registry);
        var context = WebApiTestHelpers.CreateHttpContext();
        context.Request.QueryString = new QueryString("?format=xml");

        ContentNegotiationResult result = sut.Negotiate(context);

        result.Success.Should().BeTrue();
        result.MediaType.Should().Be("application/xml");
    }

    [Fact]
    public void ContentFormatterRegistry_Should_ResolveDefaultFormatter()
    {
        var registry = new ContentFormatterRegistry(new ContentNegotiationOptions());

        IContentFormatter formatter = registry.DefaultFormatter;

        formatter.Should().NotBeNull();
        registry.IsSupported("application/json").Should().BeTrue();
    }

    [Fact]
    public void ContentFormatterRegistry_Should_HandleWildcard()
    {
        var registry = new ContentFormatterRegistry(new ContentNegotiationOptions());

        IContentFormatter? formatter = registry.GetFormatter("*/*");

        formatter.Should().NotBeNull();
    }

    [Fact]
    public void ProblemDetailsJsonFormatter_Should_SerializeProblemDetails()
    {
        var formatter = new ProblemDetailsJsonFormatter();
        var details = new ProblemDetails { Status = 400, Title = "Bad request" };

        string result = formatter.Serialize(details);

        result.Should().Contain("Bad request");
        formatter.GetContentType("utf-8").Should().Contain("application/problem+json");
    }

    [Fact]
    public void ProblemDetailsXmlFormatter_Should_SerializeProblemDetails()
    {
        var formatter = new ProblemDetailsXmlFormatter();
        var details = new ProblemDetails { Status = 404, Title = "Not found" };

        string result = formatter.Serialize(details);

        result.Should().Contain("Not found");
        formatter.GetContentType().Should().Be("application/problem+xml");
    }
}
