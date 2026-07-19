//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.IO.Compression;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class CompressionDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new CompressionDelegatingHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var handler = new CompressionDelegatingHandler(
            NullLogger<CompressionDelegatingHandler>.Instance,
            null!);
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldNotCompress()
    {
        string payload = new string('a', 2048);
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(new CompressionHandlerOptions { Enabled = false, MinimumSizeBytes = 100 });
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        await client.SendAsync(request);

        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Content-Encoding");
        inner.ReceivedRequests[0].Body.Should().Be(payload);
    }

    [Fact]
    public async Task SendAsync_WithSmallPayload_ShouldSkipCompression()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(new CompressionHandlerOptions { MinimumSizeBytes = 1024 });
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent("tiny", Encoding.UTF8, "application/json")
        };

        await client.SendAsync(request);

        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Content-Encoding");
        inner.ReceivedRequests[0].Body.Should().Be("tiny");
    }

    [Fact]
    public async Task SendAsync_WithLargePayload_ShouldCompressWithGzip()
    {
        string payload = new string('x', 4096);
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(new CompressionHandlerOptions
        {
            Algorithm = CompressionAlgorithm.Gzip,
            MinimumSizeBytes = 100,
            CompressionLevel = CompressionLevel.Optimal
        });
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        await client.SendAsync(request);

        RecordedRequest recorded = inner.ReceivedRequests.Single();
        recorded.Headers["Content-Encoding"].Should().Be("gzip");
        recorded.Body.Should().NotBe(payload);
        recorded.Body!.Length.Should().BeLessThan(payload.Length);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.Brotli, "br")]
    [InlineData(CompressionAlgorithm.Deflate, "deflate")]
    public async Task SendAsync_WithAlgorithms_ShouldSetContentEncoding(
        CompressionAlgorithm algorithm,
        string encoding)
    {
        string payload = new string('y', 4096);
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(new CompressionHandlerOptions
        {
            Algorithm = algorithm,
            MinimumSizeBytes = 100
        });
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        };

        await client.SendAsync(request);

        inner.ReceivedRequests[0].Headers["Content-Encoding"].Should().Be(encoding);
    }

    [Fact]
    public async Task SendAsync_WhenAlreadyEncoded_ShouldSkipCompression()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(new CompressionHandlerOptions { MinimumSizeBytes = 10 });
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(new string('z', 2048)))
        };
        request.Content.Headers.ContentEncoding.Add("gzip");
        request.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        await client.SendAsync(request);

        inner.ReceivedRequests[0].Headers["Content-Encoding"].Should().Be("gzip");
    }

    [Fact]
    public async Task SendAsync_WithoutContent_ShouldPassThrough()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.NoContent);
        var handler = CreateHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public void SkipCompression_WithNullRequest_ShouldThrowArgumentNullException()
    {
        Action act = () => CompressionExtensions.SkipCompression(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void SkipCompression_ShouldReturnSameRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource");
        HttpRequestMessage result = request.SkipCompression();
        result.Should().BeSameAs(request);
    }

    private static CompressionDelegatingHandler CreateHandler(CompressionHandlerOptions? options = null)
    {
        return options is null
            ? new CompressionDelegatingHandler(NullLogger<CompressionDelegatingHandler>.Instance)
            : new CompressionDelegatingHandler(
                NullLogger<CompressionDelegatingHandler>.Instance,
                options);
    }
}
