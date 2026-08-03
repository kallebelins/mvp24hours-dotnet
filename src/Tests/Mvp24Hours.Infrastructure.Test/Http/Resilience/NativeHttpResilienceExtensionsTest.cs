//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Http.Resilience;
using Polly;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class NativeHttpResilienceExtensionsTest
{
    [Fact]
    public void AddHttpClientWithStandardResilience_ShouldRegisterNamedClient()
    {
        ServiceCollection services = [];

        services.AddHttpClientWithStandardResilience("api", client =>
            client.BaseAddress = new Uri("https://api.example.com"));

        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("api");
        client.BaseAddress.Should().Be(new Uri("https://api.example.com"));
    }

    [Fact]
    public void AddHttpClientWithStandardResilience_WithOptions_ShouldApplyConfiguration()
    {
        ServiceCollection services = [];

        services.AddHttpClientWithStandardResilience(
            "api-options",
            client => client.BaseAddress = new Uri("https://api.example.com"),
            options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromMilliseconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        ServiceProvider provider = services.BuildServiceProvider();
        Action act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient("api-options");
        act.Should().NotThrow();
    }

    [Fact]
    public void AddHttpClientWithCustomResilience_ShouldRegisterPipeline()
    {
        ServiceCollection services = [];

        services.AddHttpClientWithCustomResilience(
            "custom",
            "custom-pipeline",
            client => client.BaseAddress = new Uri("https://api.example.com"),
            pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(5)));

        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("custom");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddHttpClientWithStandardResilience_WithNullServices_ShouldThrow()
    {
        Action act = () => NativeHttpResilienceExtensions.AddHttpClientWithStandardResilience(
            null!,
            "name");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHttpClientWithStandardResilience_WithEmptyName_ShouldThrow()
    {
        ServiceCollection services = [];

        Action act = () => services.AddHttpClientWithStandardResilience("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTypedHttpClientWithStandardResilience_ShouldRegisterTypedClient()
    {
        ServiceCollection services = [];

        services.AddTypedHttpClientWithStandardResilience<SampleApiClient>(client =>
            client.BaseAddress = new Uri("https://typed.example.com"));

        ServiceProvider provider = services.BuildServiceProvider();
        SampleApiClient client = provider.GetRequiredService<SampleApiClient>();
        client.HttpClient.BaseAddress.Should().Be(new Uri("https://typed.example.com"));
    }

    [Fact]
    public void AddTypedHttpClientWithStandardResilience_WithOptions_ShouldRegister()
    {
        ServiceCollection services = [];

        services.AddTypedHttpClientWithStandardResilience<SampleApiClient>(
            client => client.BaseAddress = new Uri("https://typed.example.com"),
            options => options.Retry.MaxRetryAttempts = 1);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<SampleApiClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddTypedHttpClientWithCustomResilience_ShouldRegister()
    {
        ServiceCollection services = [];

        services.AddTypedHttpClientWithCustomResilience<SampleApiClient>(
            "typed-pipeline",
            client => client.BaseAddress = new Uri("https://typed.example.com"),
            pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(3)));

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<SampleApiClient>().Should().NotBeNull();
    }

    private sealed class SampleApiClient(HttpClient httpClient)
    {
        public HttpClient HttpClient { get; } = httpClient;
    }
}
