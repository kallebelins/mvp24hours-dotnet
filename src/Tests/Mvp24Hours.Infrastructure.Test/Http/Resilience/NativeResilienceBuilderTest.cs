//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Http.Resilience;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class NativeResilienceBuilderTest
{
    [Fact]
    public void Constructor_WithNullHttpClientBuilder_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new NativeResilienceBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithOptions_WithNull_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = [];
        IHttpClientBuilder clientBuilder = services.AddHttpClient("test");
        var builder = new NativeResilienceBuilder(clientBuilder);

        Action act = () => builder.WithOptions(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void ConfigureOptions_WithNull_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = [];
        IHttpClientBuilder clientBuilder = services.AddHttpClient("test");
        var builder = new NativeResilienceBuilder(clientBuilder);

        Action act = () => builder.ConfigureOptions(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_WhenAllStrategiesDisabled_ShouldReturnBuilderWithoutThrowing()
    {
        ServiceCollection services = [];
        IHttpClientBuilder clientBuilder = services.AddHttpClient("disabled");
        var builder = new NativeResilienceBuilder(clientBuilder);

        IHttpClientBuilder result = builder
            .WithOptions(NativeResilienceOptions.Disabled)
            .Build();

        result.Should().BeSameAs(clientBuilder);
        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("disabled");
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithDefaultOptions_ShouldRegisterResilienceHandler()
    {
        ServiceCollection services = [];
        IHttpClientBuilder clientBuilder = services.AddHttpClient("resilient");

        IHttpClientBuilder result = new NativeResilienceBuilder(clientBuilder)
            .ConfigureOptions(o =>
            {
                o.MaxRetryAttempts = 2;
                o.RetryDelay = TimeSpan.FromMilliseconds(10);
                o.AttemptTimeout = TimeSpan.FromSeconds(5);
                o.TotalRequestTimeout = TimeSpan.FromSeconds(15);
            })
            .OnRetry((_, _) => { })
            .OnCircuitBreak(_ => { })
            .OnCircuitReset(_ => { })
            .ConfigureRetry(_ => { })
            .ConfigureCircuitBreaker(_ => { })
            .ConfigureAttemptTimeout(_ => { })
            .ConfigureTotalTimeout(_ => { })
            .Build();

        result.Should().NotBeNull();
        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("resilient");
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithOnlyRetryEnabled_ShouldSucceed()
    {
        ServiceCollection services = [];
        IHttpClientBuilder clientBuilder = services.AddHttpClient("retry-only");

        new NativeResilienceBuilder(clientBuilder)
            .WithOptions(new NativeResilienceOptions
            {
                EnableRetry = true,
                EnableCircuitBreaker = false,
                EnableAttemptTimeout = false,
                EnableTotalTimeout = false,
                MaxRetryAttempts = 1,
                RetryDelay = TimeSpan.FromMilliseconds(5)
            })
            .Build();

        ServiceProvider provider = services.BuildServiceProvider();
        Action act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient("retry-only");
        act.Should().NotThrow();
    }

    [Fact]
    public void AddMvpResilience_WithConfigureAction_ShouldApplyOptions()
    {
        ServiceCollection services = [];

        services.AddHttpClient("mvp")
            .AddMvpResilience(r => r.WithOptions(NativeResilienceOptions.LowLatency));

        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("mvp");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpResilience_WithOptionsOverload_ShouldApplyPreset()
    {
        ServiceCollection services = [];

        services.AddHttpClient("ha")
            .AddMvpResilience(NativeResilienceOptions.HighAvailability);

        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ha");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpResilience_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        Action act = () => NativeResilienceBuilderExtensions.AddMvpResilience(
            null!,
            NativeResilienceOptions.Disabled);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvpResilience_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = [];
        IHttpClientBuilder builder = services.AddHttpClient("x");

        Action act = () => builder.AddMvpResilience((Action<NativeResilienceBuilder>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvpResilience_WithNullOptions_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = [];
        IHttpClientBuilder builder = services.AddHttpClient("x");

        Action act = () => builder.AddMvpResilience((NativeResilienceOptions)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvpStandardResilience_ShouldRegisterHandler()
    {
        ServiceCollection services = [];

        services.AddHttpClient("standard").AddMvpStandardResilience();

        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("standard");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpStandardResilience_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        Action act = () => NativeResilienceBuilderExtensions.AddMvpStandardResilience(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
