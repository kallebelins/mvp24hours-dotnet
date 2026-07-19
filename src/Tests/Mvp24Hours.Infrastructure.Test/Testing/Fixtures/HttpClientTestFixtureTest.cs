//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Testing.Fixtures;

namespace Mvp24Hours.Infrastructure.Test.Testing.Fixtures;

[Trait("Category", "Unit")]
public class HttpClientTestFixtureTest
{
    [Fact]
    public void CreateClient_ShouldReturnHttpClientUsingHandler()
    {
        using HttpClientTestFixture fixture = new();

        using HttpClient client = fixture.CreateClient("https://api.example.com/");

        client.BaseAddress.Should().Be(new Uri("https://api.example.com/"));
    }

    [Fact]
    public async Task SetupGetResponse_ShouldConfigureHandlerForGetRequests()
    {
        using HttpClientTestFixture fixture = new();
        fixture.SetupGetResponse("/health", new { status = "ok" });
        using HttpClient client = fixture.CreateClient("https://api.example.com");

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ok");
    }

    [Fact]
    public async Task SetupPostResponse_ShouldConfigureHandlerForPostRequests()
    {
        using HttpClientTestFixture fixture = new();
        fixture.SetupPostResponse("/orders", new { id = 1001 }, HttpStatusCode.Created);
        using HttpClient client = fixture.CreateClient("https://api.example.com");
        using StringContent body = new("{}", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/orders", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task VerifyRequestMade_ShouldReturnTrueForMatchingUrl()
    {
        using HttpClientTestFixture fixture = new();
        using HttpClient client = fixture.CreateClient("https://api.example.com");
        await client.GetAsync("/reports/monthly");

        fixture.VerifyRequestMade("/reports/monthly").Should().BeTrue();
        fixture.VerifyRequestMade("/missing").Should().BeFalse();
    }

    [Fact]
    public async Task Reset_ShouldClearRequestsAndMatchers()
    {
        using HttpClientTestFixture fixture = new();
        fixture.SetupGetResponse("/temp", new { ok = true });
        using HttpClient client = fixture.CreateClient("https://api.example.com");
        await client.GetAsync("/temp");

        fixture.Reset();
        HttpResponseMessage response = await client.GetAsync("/temp");

        fixture.RequestCount.Should().Be(1);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("{}");
    }
}
