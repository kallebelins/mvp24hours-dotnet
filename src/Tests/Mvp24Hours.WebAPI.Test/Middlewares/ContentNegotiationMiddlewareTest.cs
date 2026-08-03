using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class ContentNegotiationMiddlewareTest
{
    [Fact]
    public async Task ContentNegotiationMiddleware_Should_Bypass_WhenDisabled()
    {
        bool called = false;
        var options = new ContentNegotiationOptions { Enabled = false };
        var registry = new ContentFormatterRegistry(options);
        var negotiator = new AcceptHeaderNegotiator(options, registry);
        var sut = new ContentNegotiationMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<ContentNegotiationMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext(), negotiator);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ContentNegotiationMiddleware_Should_StoreNegotiationResult_AndAddVaryHeader()
    {
        var options = new ContentNegotiationOptions { Enabled = true, AddVaryHeader = true };
        var registry = new ContentFormatterRegistry(options);
        var negotiator = new AcceptHeaderNegotiator(options, registry);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers.Accept = "application/json";
        var sut = new ContentNegotiationMiddleware(async c => await c.Response.WriteAsync("{\"ok\":true}"), Options.Create(options), NullLogger<ContentNegotiationMiddleware>.Instance);

        await sut.InvokeAsync(context, negotiator);

        context.Items.Should().ContainKey("ContentNegotiationResult");
        context.Items.Should().ContainKey("ContentFormatter");
        context.Items.Should().ContainKey("NegotiatedMediaType");
        context.Response.Headers["Vary"].ToString().Should().Be("Accept");
    }

    [Fact]
    public async Task ContentNegotiationMiddleware_Should_Return406_WhenMediaTypeNotSupported()
    {
        var options = new ContentNegotiationOptions
        {
            Enabled = true,
            Return406WhenNoMatch = true
        };
        var registry = new ContentFormatterRegistry(options);
        var negotiator = new AcceptHeaderNegotiator(options, registry);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.QueryString = new QueryString("?format=application/pdf");
        bool nextCalled = false;
        var sut = new ContentNegotiationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<ContentNegotiationMiddleware>.Instance);

        await sut.InvokeAsync(context, negotiator);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status406NotAcceptable);
        body.Should().Contain("Not Acceptable");
        body.Should().Contain("application/pdf");
    }

    [Fact]
    public async Task ResponseTransformMiddleware_Should_Bypass_WhenDisabled()
    {
        bool called = false;
        var options = new ContentNegotiationOptions { Enabled = false };
        var registry = new ContentFormatterRegistry(options);
        var sut = new ResponseTransformMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<ResponseTransformMiddleware>.Instance, registry);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ResponseTransformMiddleware_Should_UpdateContentType_WhenFormatterDiffers()
    {
        var options = new ContentNegotiationOptions { Enabled = true };
        var registry = new ContentFormatterRegistry(options);
        IContentFormatter xmlFormatter = registry.GetFormatter("application/xml")!;
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Items["ContentFormatter"] = xmlFormatter;
        context.Response.ContentType = "application/json";
        var sut = new ResponseTransformMiddleware(async c =>
        {
            c.Response.ContentType = "application/json";
            await c.Response.WriteAsync("{\"name\":\"test\"}");
        }, Options.Create(options), NullLogger<ResponseTransformMiddleware>.Instance, registry);

        await sut.InvokeAsync(context);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);

        context.Response.ContentType.Should().Contain("application/xml");
        body.Should().Contain("name");
    }

    [Fact]
    public async Task ResponseTransformMiddleware_Should_PassThrough_WhenContentTypeMatches()
    {
        var options = new ContentNegotiationOptions { Enabled = true };
        var registry = new ContentFormatterRegistry(options);
        IContentFormatter jsonFormatter = registry.GetFormatter("application/json")!;
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Items["ContentFormatter"] = jsonFormatter;
        bool called = false;
        var sut = new ResponseTransformMiddleware(async c =>
        {
            called = true;
            c.Response.ContentType = "application/json; charset=utf-8";
            await c.Response.WriteAsync("{\"ok\":true}");
        }, Options.Create(options), NullLogger<ResponseTransformMiddleware>.Instance, registry);

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task ResponseTransformMiddleware_Should_PassThrough_WhenNoFormatterInContext()
    {
        bool called = false;
        var options = new ContentNegotiationOptions { Enabled = true };
        var registry = new ContentFormatterRegistry(options);
        var sut = new ResponseTransformMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<ResponseTransformMiddleware>.Instance, registry);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }
}
