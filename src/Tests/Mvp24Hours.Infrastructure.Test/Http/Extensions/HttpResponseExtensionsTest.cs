using System.Net;
using System.Text;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Http.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Http.Extensions;

[Trait("Category", "Unit")]
public class HttpResponseExtensionsTest
{
    private sealed class TestPayload
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task EnsureSuccessStatusCodeAsync_WithSuccessResponse_ShouldNotThrow()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        Func<Task> act = () => response.EnsureSuccessStatusCodeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureSuccessStatusCodeAsync_WithFailureResponse_ShouldThrowHttpStatusCodeExceptionWithContent()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent("error body"),
            RequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.test/resource")
        };

        Func<Task> act = () => response.EnsureSuccessStatusCodeAsync();

        HttpStatusCodeException exception = (await act.Should().ThrowAsync<HttpStatusCodeException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Method.Should().Be(HttpMethod.Post);
        exception.RequestUri.Should().Be(new Uri("https://api.test/resource"));
        exception.ResponseBody.Should().Be("error body");
    }

    [Fact]
    public async Task EnsureSuccessStatusCodeAsync_WithFailureAndNoRequestMessage_ShouldDefaultMethodToGet()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        Func<Task> act = () => response.EnsureSuccessStatusCodeAsync();

        HttpStatusCodeException exception = (await act.Should().ThrowAsync<HttpStatusCodeException>()).Which;
        exception.Method.Should().Be(HttpMethod.Get);
        exception.RequestUri.Should().BeNull();
        exception.ResponseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsAsync_WithNullContent_ShouldReturnDefault()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        TestPayload? result = await response.ReadAsAsync<TestPayload>();

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsAsync_WithJsonContent_ShouldDeserializeUsingDefaultSerializer()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"Alpha\"}", Encoding.UTF8, "application/json")
        };

        TestPayload? result = await response.ReadAsAsync<TestPayload>();

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task ReadAsAsyncWithUrl_WhenFailureStatus_ShouldThrowBeforeDeserializing()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"name\":\"Alpha\"}", Encoding.UTF8, "application/json")
        };

        Func<Task> act = () => response.ReadAsAsync<TestPayload>("https://api.test/resource");

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task ReadAsAsyncWithUrl_WhenSuccessStatus_ShouldDeserialize()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"Beta\"}", Encoding.UTF8, "application/json")
        };

        TestPayload? result = await response.ReadAsAsync<TestPayload>("https://api.test/resource");

        result!.Name.Should().Be("Beta");
    }

    [Fact]
    public async Task ReadAsStringAsync_WhenFailureStatus_ShouldThrow()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("forbidden")
        };

        Func<Task> act = () => response.ReadAsStringAsync("https://api.test");

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task ReadAsStringAsync_WhenSuccessWithContent_ShouldReturnContentString()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("hello world")
        };

        string result = await response.ReadAsStringAsync("https://api.test");

        result.Should().Be("hello world");
    }

    [Fact]
    public async Task ReadAsStringAsync_WhenSuccessWithNullContent_ShouldReturnEmptyString()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        string result = await response.ReadAsStringAsync("https://api.test");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenFailureStatus_ShouldThrow()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("no")
        };

        Func<Task> act = () => response.ReadAsByteArrayAsync("https://api.test");

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenSuccessWithContent_ShouldReturnBytes()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };

        byte[] result = await response.ReadAsByteArrayAsync("https://api.test");

        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenSuccessWithNullContent_ShouldReturnEmptyArray()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        byte[] result = await response.ReadAsByteArrayAsync("https://api.test");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsStreamAsync_Task_WhenFailureStatus_ShouldThrow()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("bad gateway")
        };

        // Disambiguated from the IAsyncEnumerable<byte[]> overload (which has an int bufferSize
        // as its second parameter) by passing the CancellationToken positionally: it cannot
        // bind to an int parameter, so only the Task<Stream> overload applies.
        Func<Task> act = () => response.ReadAsStreamAsync("https://api.test", CancellationToken.None);

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task ReadAsStreamAsync_Task_WhenSuccessWithContent_ShouldReturnReadableStream()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("stream-data")
        };

        await using Stream stream = await response.ReadAsStreamAsync("https://api.test", CancellationToken.None);
        using var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();

        content.Should().Be("stream-data");
    }

    [Fact]
    public async Task ReadAsStreamAsync_Task_WhenSuccessWithNoContentAssigned_ShouldReturnEmptyReadableStream()
    {
        // HttpResponseMessage.Content's getter lazily backs a null field with an internal
        // EmptyContent instance, so `response.Content == null` is never actually true for a
        // real HttpResponseMessage - the extension's "content == null" branch is unreachable
        // via this type. What IS reachable, and what this test verifies, is that an
        // unassigned/empty content still round-trips through the stream branch as a valid,
        // readable, zero-length stream.
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        Stream result = await response.ReadAsStreamAsync("https://api.test", CancellationToken.None);

        using var reader = new StreamReader(result);
        (await reader.ReadToEndAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsStreamAsync_AsyncEnumerable_WhenFailureStatus_ShouldThrowBeforeYieldingAnyChunk()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("unavailable")
        };

        Func<Task> act = async () =>
        {
            await foreach (byte[] _ in response.ReadAsStreamAsync("https://api.test", bufferSize: 4))
            {
            }
        };

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task ReadAsStreamAsync_AsyncEnumerable_WhenNullContent_ShouldYieldNoChunks()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in response.ReadAsStreamAsync("https://api.test", bufferSize: 8192))
        {
            chunks.Add(chunk);
        }

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsStreamAsync_AsyncEnumerable_WithContentSmallerThanBuffer_ShouldYieldSinglePartialChunk()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("abc")
        };

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in response.ReadAsStreamAsync("https://api.test", bufferSize: 8192))
        {
            chunks.Add(chunk);
        }

        chunks.Should().ContainSingle();
        chunks[0].Should().Equal(Encoding.UTF8.GetBytes("abc"));
    }

    [Fact]
    public async Task ReadAsStreamAsync_AsyncEnumerable_WithContentLargerThanBuffer_ShouldYieldMultipleChunksIncludingFullBufferChunks()
    {
        string payload = new string('x', 20);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload)
        };

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in response.ReadAsStreamAsync("https://api.test", bufferSize: 8))
        {
            chunks.Add(chunk);
        }

        // 20 bytes with an 8-byte buffer => two full 8-byte chunks (the "bytesRead == bufferSize"
        // branch, which returns the shared buffer array directly) plus one 4-byte partial chunk
        // (the "bytesRead < bufferSize" branch, which copies into a right-sized array).
        chunks.Should().HaveCount(3);
        chunks[0].Should().HaveCount(8);
        chunks[1].Should().HaveCount(8);
        chunks[2].Should().HaveCount(4);
        string reassembled = string.Concat(chunks.Select(Encoding.UTF8.GetString));
        reassembled.Should().Be(payload);
    }
}
