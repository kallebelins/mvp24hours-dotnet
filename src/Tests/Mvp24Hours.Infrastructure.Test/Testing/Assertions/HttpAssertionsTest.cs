//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Http;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class HttpAssertionsTest
{
    [Fact]
    public async Task AssertRequestMade_WithoutUrl_ShouldPassWhenRequestsExist()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/users");

        Action act = () => HttpAssertions.AssertRequestMade(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertRequestMade_WithoutUrl_ShouldThrowWhenNoRequests()
    {
        using TestHttpMessageHandler handler = new();

        Action act = () => HttpAssertions.AssertRequestMade(handler);

        act.Should().Throw<AssertionException>().WithMessage("*at least one HTTP request*");
    }

    [Fact]
    public async Task AssertRequestMade_WithUrlPart_ShouldPassWhenUrlMatches()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/api/users/1");

        Action act = () => HttpAssertions.AssertRequestMade(handler, "api/users");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestMade_WithUrlPart_ShouldThrowWhenUrlMissing()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/orders");

        Action act = () => HttpAssertions.AssertRequestMade(handler, "api/users");

        act.Should().Throw<AssertionException>().WithMessage("*api/users*");
    }

    [Fact]
    public async Task AssertRequestCount_ShouldPassWhenCountMatches()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/a");
        await client.GetAsync("https://api.example.com/b");

        Action act = () => HttpAssertions.AssertRequestCount(handler, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestCount_ShouldThrowWhenCountMismatch()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/a");

        Action act = () => HttpAssertions.AssertRequestCount(handler, 3);

        act.Should().Throw<AssertionException>().WithMessage("*Expected 3 HTTP request*");
    }

    [Fact]
    public async Task AssertGetRequestMade_ShouldPassForGetRequest()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/api/users/1");

        Action act = () => HttpAssertions.AssertGetRequestMade(handler, "users/1");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertPostRequestMade_ShouldPassForPostRequest()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        using StringContent content = new("{}", Encoding.UTF8, "application/json");
        await client.PostAsync("https://api.example.com/api/users", content);

        Action act = () => HttpAssertions.AssertPostRequestMade(handler, "api/users");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertPutRequestMade_ShouldPassForPutRequest()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        using StringContent content = new("{}", Encoding.UTF8, "application/json");
        await client.PutAsync("https://api.example.com/api/users/1", content);

        Action act = () => HttpAssertions.AssertPutRequestMade(handler, "users/1");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertDeleteRequestMade_ShouldPassForDeleteRequest()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.DeleteAsync("https://api.example.com/api/users/1");

        Action act = () => HttpAssertions.AssertDeleteRequestMade(handler, "users/1");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestWithMethodMade_ShouldThrowWhenMethodMismatch()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/api/users");

        Action act = () => HttpAssertions.AssertPostRequestMade(handler, "api/users");

        act.Should().Throw<AssertionException>().WithMessage("*POST*");
    }

    [Fact]
    public async Task AssertRequestWithHeader_ShouldPassWhenHeaderPresent()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer token123");
        await client.GetAsync("https://api.example.com/secure");

        Action act = () => HttpAssertions.AssertRequestWithHeader(handler, "Authorization", "Bearer");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestWithHeader_ShouldPassWhenOnlyHeaderNameRequired()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer token123");
        await client.GetAsync("https://api.example.com/secure");

        Action act = () => HttpAssertions.AssertRequestWithHeader(handler, "Authorization");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestWithHeader_ShouldThrowWhenHeaderMissing()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/data");

        Action act = () => HttpAssertions.AssertRequestWithHeader(handler, "Authorization", "Bearer");

        act.Should().Throw<AssertionException>().WithMessage("*Authorization*");
    }

    [Fact]
    public async Task AssertRequestWithBodyContaining_ShouldPassWhenBodyMatches()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        using StringContent content = new("{\"name\":\"Alice\"}", Encoding.UTF8, "application/json");
        await client.PostAsync("https://api.example.com/users", content);

        Action act = () => HttpAssertions.AssertRequestWithBodyContaining(handler, "Alice");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertRequestWithBodyContaining_ShouldThrowWhenBodyMissing()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        using StringContent content = new("{\"name\":\"Bob\"}", Encoding.UTF8, "application/json");
        await client.PostAsync("https://api.example.com/users", content);

        Action act = () => HttpAssertions.AssertRequestWithBodyContaining(handler, "Alice");

        act.Should().Throw<AssertionException>().WithMessage("*body containing 'Alice'*");
    }

    [Fact]
    public void AssertNoRequestsMade_ShouldPassWhenEmpty()
    {
        using TestHttpMessageHandler handler = new();

        Action act = () => HttpAssertions.AssertNoRequestsMade(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertNoRequestsMade_ShouldThrowWhenRequestsExist()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/a");

        Action act = () => HttpAssertions.AssertNoRequestsMade(handler);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no HTTP requests*");
    }

    [Fact]
    public async Task GetLastRequest_ShouldReturnMostRecentRequest()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/first");
        await client.GetAsync("https://api.example.com/last");

        RecordedRequest last = HttpAssertions.GetLastRequest(handler);

        last.RequestUri.Should().Contain("/last");
    }

    [Fact]
    public void GetLastRequest_ShouldThrowWhenNoRequests()
    {
        using TestHttpMessageHandler handler = new();

        Action act = () => HttpAssertions.GetLastRequest(handler);

        act.Should().Throw<AssertionException>().WithMessage("*No HTTP requests were recorded*");
    }

    [Fact]
    public async Task GetRequestsMatching_ShouldReturnFilteredRequests()
    {
        using TestHttpMessageHandler handler = new();
        using HttpClient client = new(handler);
        await client.GetAsync("https://api.example.com/users");
        using StringContent content = new("{}", Encoding.UTF8, "application/json");
        await client.PostAsync("https://api.example.com/orders", content);

        IReadOnlyList<RecordedRequest> getRequests =
            HttpAssertions.GetRequestsMatching(handler, r => r.Method == "GET");

        getRequests.Should().HaveCount(1);
        getRequests[0].RequestUri.Should().Contain("/users");
    }

    [Fact]
    public void NullArguments_ShouldThrowArgumentNullException()
    {
        using TestHttpMessageHandler handler = new();

        Action nullHandler = () => HttpAssertions.AssertRequestMade(null!);
        Action nullUrlPart = () => HttpAssertions.AssertRequestMade(handler, null!);
        Action nullMethod = () => HttpAssertions.AssertRequestWithMethodMade(handler, null!, "/x");
        Action nullPredicate = () => HttpAssertions.GetRequestsMatching(handler, null!);

        nullHandler.Should().Throw<ArgumentNullException>().WithParameterName("handler");
        nullUrlPart.Should().Throw<ArgumentNullException>().WithParameterName("urlPart");
        nullMethod.Should().Throw<ArgumentNullException>().WithParameterName("method");
        nullPredicate.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }
}
