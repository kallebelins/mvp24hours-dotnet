using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Integration;

[Trait("Category", "Unit")]
public class WebApiPipelineIntegrationTest
{
    [Fact]
    public async Task ContentNegotiationPipeline_Should_Return406_WhenMediaTypeIsNotSupported()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursContentNegotiation(options =>
            {
                options.AddVaryHeader = false;
                options.Return406WhenNoMatch = true;
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursContentNegotiation())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/data", async context =>
            {
                await context.Response.WriteAsync("{\"name\":\"test\"}");
            }));

        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/data?format=application/pdf");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
        body.Should().Contain("Not Acceptable");
    }

    [Fact]
    public async Task IdempotencyPipeline_Should_ReplayResponse_WhenSameKeyIsUsed()
    {
        int counter = 0;

        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursIdempotencyInMemory())
            .ConfigurePipeline(app => app.UseMvp24HoursIdempotency())
            .ConfigureEndpoints(endpoints => endpoints.MapPost("/api/orders", async context =>
            {
                counter++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                await context.Response.WriteAsync($"count:{counter}");
            }));

        using HttpClient client = factory.CreateClient();
        using var request1 = CreatePost("/api/orders", "Idempotency-Key", "order-key-1");
        using var request2 = CreatePost("/api/orders", "Idempotency-Key", "order-key-1");

        using HttpResponseMessage first = await client.SendAsync(request1);
        using HttpResponseMessage second = await client.SendAsync(request2);
        string firstBody = await first.Content.ReadAsStringAsync();
        string secondBody = await second.Content.ReadAsStringAsync();

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        firstBody.Should().Be("count:1");
        secondBody.Should().Be("count:1");
        counter.Should().Be(1);
    }

    [Fact]
    public async Task RateLimitingPipeline_Should_Return429_WhenLimitExceeded()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursRateLimiting(options =>
            {
                options.UseProblemDetails = false;
                options.AddFixedWindowPolicy("default", 1, TimeSpan.FromMinutes(1));
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursRateLimiting())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/limited", () => "ok"));

        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync("/api/limited");
        using HttpResponseMessage second = await client.GetAsync("/api/limited");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OutputCachingPipeline_Should_ServeCachedResponse_OnSecondRequest()
    {
        int counter = 0;

        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursOutputCache())
            .ConfigurePipeline(app => app.UseMvp24HoursOutputCache())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/products", () => $"product-{++counter}")
                .CacheOutputFor(TimeSpan.FromMinutes(5), tags: ["products"]));

        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync("/api/products");
        using HttpResponseMessage second = await client.GetAsync("/api/products");
        string firstBody = await first.Content.ReadAsStringAsync();
        string secondBody = await second.Content.ReadAsStringAsync();

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        firstBody.Should().Be("product-1");
        secondBody.Should().Be("product-1");
        counter.Should().Be(1);
    }

    private static HttpRequestMessage CreatePost(string path, string headerName, string headerValue)
    {
        return new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            Headers = { { headerName, headerValue } }
        };
    }
}
