//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text;
using System.Text.Json;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Testing.Http;

[Trait("Category", "Unit")]
public class TestHttpMessageHandlerTest
{
    [Fact]
    public async Task SendAsync_WithNoConfiguration_ShouldReturnDefaultOkWithJsonBody()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Be("{}");
    }

    [Fact]
    public async Task RespondWith_StatusCode_ShouldChangeDefaultResponse()
    {
        using TestHttpMessageHandler handler = new();
        handler.RespondWith(HttpStatusCode.NoContent);
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/empty");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RespondWith_Object_ShouldSerializeJsonContent()
    {
        using TestHttpMessageHandler handler = new();
        handler.RespondWith(HttpStatusCode.OK, new { id = 42, name = "Test" });
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/item");

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(42);
        doc.RootElement.GetProperty("name").GetString().Should().Be("Test");
    }

    [Fact]
    public async Task WhenGet_ShouldReturnConfiguredResponseForGetRequests()
    {
        using TestHttpMessageHandler handler = new();
        handler.WhenGet("/users/1", HttpStatusCode.OK, new { id = 1 });
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"id\":1");
    }

    [Fact]
    public async Task WhenPost_ShouldReturnConfiguredResponseForPostRequests()
    {
        using TestHttpMessageHandler handler = new();
        handler.WhenPost("/users", HttpStatusCode.Created, new { id = 99 });
        using HttpClient client = new(handler);
        using StringContent content = new("{}", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("https://api.example.com/users", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task WhenPost_ShouldNotMatchGetRequests()
    {
        using TestHttpMessageHandler handler = new();
        handler.WhenPost("/users", HttpStatusCode.Created, new { id = 99 });
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_FirstMatcherWins_ShouldUseFirstMatchingResponse()
    {
        using TestHttpMessageHandler handler = new();
        handler.When(_ => true, new HttpResponseMessage(HttpStatusCode.Accepted));
        handler.When(_ => true, new HttpResponseMessage(HttpStatusCode.BadRequest));
        using HttpClient client = new(handler);

        HttpResponseMessage response = await client.GetAsync("https://api.example.com/any");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ThrowException_ShouldPropagateConfiguredException()
    {
        using TestHttpMessageHandler handler = new();
        var expected = new InvalidOperationException("boom");
        handler.ThrowException(expected);
        using HttpClient client = new(handler);

        Func<Task> act = async () => await client.GetAsync("https://api.example.com/fail");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task SimulateNetworkFailure_ShouldThrowHttpRequestException()
    {
        using TestHttpMessageHandler handler = new();
        handler.SimulateNetworkFailure();
        using HttpClient client = new(handler);

        Func<Task> act = async () => await client.GetAsync("https://api.example.com/offline");

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Network failure*");
    }

    [Fact]
    public async Task SimulateTimeout_ShouldThrowTaskCanceledException()
    {
        using TestHttpMessageHandler handler = new();
        handler.SimulateTimeout();
        using HttpClient client = new(handler);

        Func<Task> act = async () => await client.GetAsync("https://api.example.com/slow");

        await act.Should().ThrowAsync<TaskCanceledException>()
            .WithMessage("*timed out*");
    }

    [Fact]
    public async Task SendAsync_ShouldRecordRequestsWithMethodUriAndBody()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        using StringContent content = new("{\"name\":\"Alice\"}", Encoding.UTF8, "application/json");

        await client.PostAsync("https://api.example.com/users", content);

        handler.RequestCount.Should().Be(1);
        RecordedRequest recorded = handler.ReceivedRequests[0];
        recorded.Method.Should().Be("POST");
        recorded.RequestUri.Should().Contain("/users");
        recorded.Body.Should().Contain("Alice");
    }

    [Fact]
    public async Task ClearRequests_ShouldRemoveRecordedRequests()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/a");

        handler.ClearRequests();

        handler.RequestCount.Should().Be(0);
        handler.ReceivedRequests.Should().BeEmpty();
    }

    [Fact]
    public void RecordedRequest_GetBodyAs_ShouldDeserializeJsonBody()
    {
        // System.Text.Json is case-sensitive by default — match property names
        var recorded = new RecordedRequest(
            "POST",
            "https://api.example.com/users",
            new Dictionary<string, string>(),
            "{\"Id\":7,\"Active\":true}",
            DateTimeOffset.UtcNow);

        TestPayload? body = recorded.GetBodyAs<TestPayload>();

        body.Should().NotBeNull();
        body!.Id.Should().Be(7);
        body.Active.Should().BeTrue();
    }

    [Fact]
    public void RecordedRequest_GetBodyAs_WithNullBody_ShouldReturnDefault()
    {
        var recorded = new RecordedRequest(
            "GET",
            "https://api.example.com",
            new Dictionary<string, string>(),
            null,
            DateTimeOffset.UtcNow);

        recorded.GetBodyAs<TestPayload>().Should().BeNull();
    }

    [Fact]
    public async Task VerifyRequestUrl_ShouldReturnTrueWhenMatchingRequestWasMade()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/reports/2024");

        handler.VerifyRequestUrl("/reports/2024").Should().BeTrue();
        handler.VerifyRequestUrl("/missing").Should().BeFalse();
    }

    [Fact]
    public async Task GetGetRequests_ShouldFilterByHttpMethod()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/a");
        using StringContent content = new("{}", Encoding.UTF8, "application/json");
        await client.PostAsync("https://api.example.com/b", content);

        handler.GetGetRequests().Should().HaveCount(1);
        handler.GetPostRequests().Should().HaveCount(1);
    }

    private sealed class TestPayload
    {
        public int Id { get; set; }
        public bool Active { get; set; }
    }
}
