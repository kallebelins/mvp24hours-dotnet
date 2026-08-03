using System.Net;
using System.Text;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Http.Extensions;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http.Extensions;

[Trait("Category", "Unit")]
public class HttpClientSerializationExtensionsTest
{
    private sealed class ApiPayload
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private static HttpClient CreateClient(TestHttpMessageHandler handler, Uri? baseAddress = null)
    {
        return new HttpClient(handler)
        {
            BaseAddress = baseAddress ?? new Uri("https://api.example.com/")
        };
    }

    [Fact]
    public async Task GetAsync_StringUri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenGet("/items/1", HttpStatusCode.OK, new ApiPayload { Id = 1, Name = "Alpha" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.GetAsync<ApiPayload>("/items/1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetAsync_Uri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenGet("/items/2", HttpStatusCode.OK, new ApiPayload { Id = 2, Name = "Beta" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.GetAsync<ApiPayload>(new Uri("https://api.example.com/items/2"));

        result.Should().NotBeNull();
        result!.Name.Should().Be("Beta");
    }

    [Fact]
    public async Task GetAsync_ShouldThrowHttpStatusCodeException_OnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenGet("/items/missing", HttpStatusCode.NotFound, new { error = "not found" });
        HttpClient client = CreateClient(handler);

        Func<Task> act = () => client.GetAsync<ApiPayload>("/items/missing");

        await act.Should().ThrowAsync<HttpStatusCodeException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAsync_StringUri_ShouldSerializeRequestAndDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/items", HttpStatusCode.Created, new ApiPayload { Id = 10, Name = "Created" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PostAsync<ApiPayload>("/items", new { Name = "Created" });

        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        handler.GetPostRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task PostAsync_Uri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/items", HttpStatusCode.OK, new ApiPayload { Id = 11, Name = "Posted" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PostAsync<ApiPayload>(
            new Uri("https://api.example.com/items"),
            new { Name = "Posted" });

        result!.Name.Should().Be("Posted");
    }

    [Fact]
    public async Task PutAsync_ShouldSendPutRequest()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPut("/items/1", HttpStatusCode.OK, new ApiPayload { Id = 1, Name = "Updated" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PutAsync<ApiPayload>("/items/1", new { Name = "Updated" });

        result!.Name.Should().Be("Updated");
        handler.GetPutRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task PutAsync_Uri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPut("/items/2", HttpStatusCode.OK, new ApiPayload { Id = 2, Name = "Replaced" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PutAsync<ApiPayload>(
            new Uri("https://api.example.com/items/2"),
            new { Name = "Replaced" });

        result!.Id.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSendDeleteRequest()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenDelete("/items/1", HttpStatusCode.OK, new ApiPayload { Id = 1, Name = "Deleted" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.DeleteAsync<ApiPayload>("/items/1");

        result.Should().NotBeNull();
        handler.GetDeleteRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteAsync_Uri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenDelete("/items/9", HttpStatusCode.OK, new ApiPayload { Id = 9, Name = "Removed" });
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.DeleteAsync<ApiPayload>(new Uri("https://api.example.com/items/9"));

        result!.Id.Should().Be(9);
    }

    [Fact]
    public async Task PatchAsync_StringUri_ShouldSendPatchRequest()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .When(
                req => req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath.Contains("/items/1"),
                CreateResponse(HttpStatusCode.OK, new ApiPayload { Id = 1, Name = "Patched" }));
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PatchAsync<ApiPayload>("/items/1", new { Name = "Patched" });

        result!.Name.Should().Be("Patched");
        handler.ReceivedRequests.Should().ContainSingle(r => r.Method == "PATCH");
    }

    [Fact]
    public async Task PatchAsync_Uri_ShouldDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .When(
                req => req.Method == HttpMethod.Patch,
                CreateResponse(HttpStatusCode.OK, new ApiPayload { Id = 3, Name = "Partial" }));
        HttpClient client = CreateClient(handler);

        ApiPayload? result = await client.PatchAsync<ApiPayload>(
            new Uri("https://api.example.com/items/3"),
            new { Name = "Partial" });

        result!.Name.Should().Be("Partial");
    }

    [Fact]
    public async Task GetStreamAsync_ShouldYieldByteChunks()
    {
        byte[] payload = Encoding.UTF8.GetBytes("hello-stream");
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenGet("/stream", HttpStatusCode.OK, payload, "application/octet-stream");
        HttpClient client = CreateClient(handler);

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in client.GetStreamAsync("/stream", bufferSize: 8192))
        {
            chunks.Add(chunk);
        }

        chunks.Should().NotBeEmpty();
        Encoding.UTF8.GetString([.. chunks.SelectMany(c => c)]).Should().Be("hello-stream");
    }

    [Fact]
    public async Task GetStreamAsync_Uri_ShouldDelegateToStringOverload()
    {
        byte[] payload = "uri-stream"u8.ToArray();
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenGet("/stream-uri", HttpStatusCode.OK, payload, "application/octet-stream");
        HttpClient client = CreateClient(handler);

        var chunks = new List<byte[]>();
        await foreach (byte[] chunk in client.GetStreamAsync(new Uri("https://api.example.com/stream-uri"), bufferSize: 8192))
        {
            chunks.Add(chunk);
        }

        Encoding.UTF8.GetString([.. chunks.SelectMany(c => c)]).Should().Be("uri-stream");
    }

    [Fact]
    public async Task PostStreamAsync_WithStream_ShouldUploadAndDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/upload", HttpStatusCode.OK, new ApiPayload { Id = 99, Name = "Uploaded" });
        HttpClient client = CreateClient(handler);

        await using var stream = new MemoryStream("file-data"u8.ToArray());
        ApiPayload? result = await client.PostStreamAsync<ApiPayload>("/upload", stream);

        result!.Id.Should().Be(99);
        handler.GetPostRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task PostStreamAsync_WithAsyncEnumerable_ShouldUploadAndDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/upload-stream", HttpStatusCode.OK, new ApiPayload { Id = 100, Name = "Streamed" });
        HttpClient client = CreateClient(handler);

        static async IAsyncEnumerable<byte[]> StreamData()
        {
            yield return "part1-"u8.ToArray();
            yield return "part2"u8.ToArray();
        }

        ApiPayload? result = await client.PostStreamAsync<ApiPayload>("/upload-stream", StreamData());

        result!.Name.Should().Be("Streamed");
        handler.GetPostRequests().Should().ContainSingle();
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, object? content)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(content);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }
}

internal static class TestHttpMessageHandlerStreamExtensions
{
    public static TestHttpMessageHandler WhenGet(
        this TestHttpMessageHandler handler,
        string url,
        HttpStatusCode statusCode,
        byte[] content,
        string mediaType)
    {
        return handler.When(
            req => req.Method == HttpMethod.Get &&
                   (req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false),
            new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType) }
                }
            });
    }
}
