using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Http;
using Mvp24Hours.Infrastructure.Http.Serializers;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http;

[Trait("Category", "Unit")]
public class TypedHttpClientTest
{
    private sealed class TestApi;

    private sealed class ApiResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private static TypedHttpClient<TestApi> CreateClient(TestHttpMessageHandler handler, Uri? baseAddress = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = baseAddress ?? new Uri("https://api.example.com/")
        };
        return new TypedHttpClient<TestApi>(httpClient, NullLogger<TypedHttpClient<TestApi>>.Instance);
    }

    [Fact]
    public void Constructor_ShouldExposeBaseAddressAndTimeout()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var client = new TypedHttpClient<TestApi>(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/"),
            Timeout = TimeSpan.FromSeconds(42)
        });

        client.BaseAddress!.ToString().Should().Contain("api.example.com");
        client.Timeout.Should().Be(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenHttpClientIsNull()
    {
        Action act = () => _ = new TypedHttpClient<TestApi>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStringContent()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "{\"ok\":true}");
        TypedHttpClient<TestApi> client = CreateClient(handler);

        string? content = await client.GetAsync("/items");

        content.Should().Contain("ok");
        handler.VerifyRequestUrl("/items");
    }

    [Fact]
    public async Task GetAsync_Generic_ShouldDeserializeJson()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenGet("/items/1", HttpStatusCode.OK, new ApiResponse { Id = 1, Name = "Alpha" });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.GetAsync<ApiResponse>("/items/1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetAsync_WithHeaders_ShouldForwardHeaders()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "{}");
        TypedHttpClient<TestApi> client = CreateClient(handler);
        var headers = new Dictionary<string, string> { ["X-Custom"] = "value" };

        await client.GetAsync<ApiResponse>("/secure", headers);

        handler.ReceivedRequests.Should().ContainSingle(r => r.GetHeader("X-Custom") == "value");
    }

    [Fact]
    public async Task GetStreamAsync_ShouldReturnStream()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "stream-data");
        TypedHttpClient<TestApi> client = CreateClient(handler);

        await using Stream? stream = await client.GetStreamAsync("/stream", cancellationToken: CancellationToken.None);

        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream!);
        (await reader.ReadToEndAsync()).Should().Be("stream-data");
    }

    [Fact]
    public async Task GetBytesAsync_ShouldReturnByteArray()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "bytes");
        TypedHttpClient<TestApi> client = CreateClient(handler);

        byte[]? bytes = await client.GetBytesAsync("/bytes");

        bytes.Should().NotBeNull();
        Encoding.UTF8.GetString(bytes!).Should().Be("bytes");
    }

    [Fact]
    public async Task GetStreamAsync_Enumerable_ShouldYieldChunks()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "abcdefghij");
        TypedHttpClient<TestApi> client = CreateClient(handler);
        var chunks = new List<byte[]>();

        await foreach (byte[] chunk in client.GetStreamAsync("/chunks", bufferSize: 4))
        {
            chunks.Add(chunk);
        }

        chunks.Should().NotBeEmpty();
        chunks.Sum(c => c.Length).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostAsync_ShouldSendBodyAndDeserializeResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenPost("/items", HttpStatusCode.OK, new ApiResponse { Id = 99, Name = "Created" });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.PostAsync<ApiResponse>("/items", new { name = "Created" });

        result!.Id.Should().Be(99);
        handler.GetPostRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task PostFormAsync_ShouldPostFormUrlEncodedContent()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenPost("/form", HttpStatusCode.OK, new ApiResponse { Id = 1, Name = "Form" });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.PostFormAsync<ApiResponse>(
            "/form",
            new Dictionary<string, string> { ["name"] = "Form" });

        result!.Name.Should().Be("Form");
    }

    [Fact]
    public async Task PostMultipartAsync_ShouldPostMultipartContent()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenPost("/upload", HttpStatusCode.OK, new ApiResponse { Id = 2, Name = "File" });
        TypedHttpClient<TestApi> client = CreateClient(handler);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("hello"), "field");

        ApiResponse? result = await client.PostMultipartAsync<ApiResponse>("/upload", content);

        result!.Id.Should().Be(2);
    }

    [Fact]
    public async Task PutAsync_ShouldSendPutRequest()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenPut("/items/1", HttpStatusCode.OK, new ApiResponse { Id = 1, Name = "Updated" });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.PutAsync<ApiResponse>("/items/1", new { name = "Updated" });

        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task PatchAsync_ShouldSendPatchRequest()
    {
        var handler = new TestHttpMessageHandler();
        handler.When(
            req => req.Method == HttpMethod.Patch,
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":1,\"name\":\"Patched\"}", Encoding.UTF8, "application/json")
            });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.PatchAsync<ApiResponse>("/items/1", new { name = "Patched" });

        result!.Name.Should().Be("Patched");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSendDeleteRequest()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().WhenDelete("/items/1", HttpStatusCode.OK, new ApiResponse { Id = 1, Name = "Deleted" });
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.DeleteAsync<ApiResponse>("/items/1");

        result!.Name.Should().Be("Deleted");
    }

    [Fact]
    public async Task SendAsync_ShouldReturnRawResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.Accepted);
        TypedHttpClient<TestApi> client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/status");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SendAsync_Generic_ShouldDeserializeSuccessfulResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, new ApiResponse { Id = 5, Name = "Sent" });
        TypedHttpClient<TestApi> client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/items/5");

        ApiResponse? result = await client.SendAsync<ApiResponse>(request);

        result!.Id.Should().Be(5);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowHttpStatusCodeException_OnFailureStatus()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.BadRequest, "bad request");
        TypedHttpClient<TestApi> client = CreateClient(handler);

        Func<Task> act = () => client.GetAsync("/fail");

        await act.Should().ThrowAsync<HttpStatusCodeException>();
    }

    [Fact]
    public async Task GetAsync_ShouldCombineRelativeUrlWithBaseAddress()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "ok");
        TypedHttpClient<TestApi> client = CreateClient(handler, new Uri("https://api.example.com/v1/"));

        await client.GetAsync("items");

        handler.ReceivedRequests.Should().ContainSingle(r => r.RequestUri.Contains("/v1/items", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAsync_ShouldUseAbsoluteUrl_WhenProvided()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "ok");
        TypedHttpClient<TestApi> client = CreateClient(handler);

        await client.GetAsync("https://other.example.com/data");

        handler.ReceivedRequests.Should().ContainSingle(r => r.RequestUri.Contains("other.example.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAsync_Generic_ShouldReturnNull_WhenResponseBodyIsEmpty()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, string.Empty);
        TypedHttpClient<TestApi> client = CreateClient(handler);

        ApiResponse? result = await client.GetAsync<ApiResponse>("/empty");

        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCustomSerializer_ShouldUseSerializer()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
        var serializer = new JsonHttpClientSerializer();
        var client = new TypedHttpClient<TestApi>(httpClient, NullLogger<TypedHttpClient<TestApi>>.Instance, serializer);

        client.Should().NotBeNull();
    }
}
