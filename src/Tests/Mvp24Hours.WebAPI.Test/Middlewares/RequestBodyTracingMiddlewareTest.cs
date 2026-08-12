using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class RequestBodyTracingMiddlewareTest
{
    [Fact]
    public async Task RequestBodyTracingMiddleware_Should_RedactAndTagJsonBody()
    {
        const string json = "{\"email\":\"user@test.com\",\"password\":\"123\"}";
        string? downstreamBody = null;

        var options = new RequestBodyTracingOptions
        {
            Enabled = true,
            MaxBodySizeBytes = 1024
        };

        var sut = new RequestBodyTracingMiddleware(async context =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            downstreamBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            await Task.CompletedTask;
        }, Options.Create(options));

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders", body: json);

        using var activity = new Activity("request-body-tracing").Start();

        await sut.InvokeAsync(context);

        downstreamBody.Should().Be(json);
        activity.GetTagItem(options.BodyTagName)?.ToString().Should().Contain("***REDACTED***");
        activity.GetTagItem(options.RedactedFieldsTagName).Should().NotBeNull();
    }

    [Fact]
    public async Task RequestBodyTracingMiddleware_Should_SkipExcludedPath()
    {
        var options = new RequestBodyTracingOptions
        {
            Enabled = true,
            ExcludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/health"
            }
        };

        bool called = false;
        var sut = new RequestBodyTracingMiddleware(context =>
        {
            called = true;
            return Task.CompletedTask;
        }, Options.Create(options));

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/health", body: "{\"password\":\"123\"}");

        using var activity = new Activity("request-body-tracing").Start();

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
        activity.GetTagItem(options.BodyTagName).Should().BeNull();
    }

    [Fact]
    public async Task RequestBodyTracingMiddleware_Should_TruncateLargeBody()
    {
        string body = new string('a', 200);
        var options = new RequestBodyTracingOptions
        {
            Enabled = true,
            MaxBodySizeBytes = 32,
            TracedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text/plain" }
        };

        var sut = new RequestBodyTracingMiddleware(_ => Task.CompletedTask, Options.Create(options));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/upload", body: body, contentType: "text/plain");

        using var activity = new Activity("request-body-tracing").Start();

        await sut.InvokeAsync(context);

        string tracedBody = activity.GetTagItem(options.BodyTagName)?.ToString() ?? string.Empty;
        tracedBody.Should().Contain(options.TruncationSuffix);
        activity.GetTagItem(options.TruncatedTagName).Should().Be(true);
    }
}
