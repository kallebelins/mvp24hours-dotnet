//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Resilience.Native;
using Polly;
using Polly.Registry;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Native;

[Trait("Category", "Unit")]
public class NativeResilienceServiceExtensionsTest
{
    [Fact]
    public void AddNativeResilience_Default_ShouldRegisterPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeResilience();

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline = provider.GetRequiredService<INativeResiliencePipeline>();
        NativeResilienceOptions options = provider.GetRequiredService<NativeResilienceOptions>();

        pipeline.Should().NotBeNull();
        options.Name.Should().Be("Mvp24Hours-Resilience");
    }

    [Fact]
    public void AddNativeResilience_WithOptions_ShouldUseProvidedOptions()
    {
        var services = new ServiceCollection();
        var options = new NativeResilienceOptions
        {
            Name = "custom",
            EnableRetry = false,
            EnableCircuitBreaker = false,
            EnableTimeout = false
        };

        services.AddNativeResilience(options);

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline = provider.GetRequiredService<INativeResiliencePipeline>();

        pipeline.Name.Should().Be("custom");
    }

    [Fact]
    public void AddNativeResilience_WithNullOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddNativeResilience((NativeResilienceOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddNativeResilience_WithConfigure_ShouldApplyConfiguration()
    {
        var services = new ServiceCollection();

        services.AddNativeResilience(o =>
        {
            o.Name = "configured";
            o.RetryMaxAttempts = 7;
            o.EnableCircuitBreaker = false;
            o.EnableTimeout = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        NativeResilienceOptions options = provider.GetRequiredService<NativeResilienceOptions>();

        options.Name.Should().Be("configured");
        options.RetryMaxAttempts.Should().Be(7);
    }

    [Fact]
    public void AddNativeResilience_Named_ShouldRegisterKeyedService()
    {
        var services = new ServiceCollection();

        services.AddNativeResilience("database", NativeResilienceOptions.Database);

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline = provider.GetRequiredKeyedService<INativeResiliencePipeline>("database");

        pipeline.Name.Should().Be("database");
    }

    [Fact]
    public void AddNativeResilience_NamedWithConfigure_ShouldApplyNameAndConfig()
    {
        var services = new ServiceCollection();

        services.AddNativeResilience("messaging", o =>
        {
            o.RetryMaxAttempts = 4;
            o.EnableTimeout = false;
            o.EnableCircuitBreaker = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline = provider.GetRequiredKeyedService<INativeResiliencePipeline>("messaging");

        pipeline.Name.Should().Be("messaging");
    }

    [Fact]
    public void AddNativeResilience_NamedWithNullName_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddNativeResilience(null!, NativeResilienceOptions.Default);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddNativeResilience_Typed_ShouldRegisterTypedPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeResilience<string>(o =>
        {
            o.Name = "typed";
            o.EnableRetry = false;
            o.EnableCircuitBreaker = false;
            o.EnableTimeout = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline<string> pipeline =
            provider.GetRequiredService<INativeResiliencePipeline<string>>();

        pipeline.Name.Should().Be("typed");
    }

    [Fact]
    public void AddNativeResilience_TypedWithOptions_ShouldRegister()
    {
        var services = new ServiceCollection();
        var options = new NativeResilienceOptions
        {
            Name = "typed-opts",
            EnableRetry = false,
            EnableCircuitBreaker = false,
            EnableTimeout = false
        };

        services.AddNativeResilience<int>(options);

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline<int> pipeline =
            provider.GetRequiredService<INativeResiliencePipeline<int>>();

        pipeline.Name.Should().Be("typed-opts");
    }

    [Fact]
    public void AddNativeDbResilience_ShouldRegisterDatabaseKeyedPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeDbResilience();

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline =
            provider.GetRequiredKeyedService<INativeResiliencePipeline>("database");

        pipeline.Name.Should().Be("database");
    }

    [Fact]
    public void AddNativeDbResilience_WithConfigure_ShouldAllowOverrides()
    {
        var services = new ServiceCollection();

        services.AddNativeDbResilience(o => o.RetryMaxAttempts = 9);

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline =
            provider.GetRequiredKeyedService<INativeResiliencePipeline>("database");

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeMessagingResilience_ShouldRegisterMessagingKeyedPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeMessagingResilience();

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline =
            provider.GetRequiredKeyedService<INativeResiliencePipeline>("messaging");

        pipeline.Name.Should().Be("messaging");
    }

    [Fact]
    public void AddNativeMessagingResilience_WithConfigure_ShouldAllowOverrides()
    {
        var services = new ServiceCollection();

        services.AddNativeMessagingResilience(o => o.EnableTimeout = false);

        using ServiceProvider provider = services.BuildServiceProvider();
        INativeResiliencePipeline pipeline =
            provider.GetRequiredKeyedService<INativeResiliencePipeline>("messaging");

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeResiliencePipeline_ShouldRegisterPollyPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeResiliencePipeline("direct", builder => builder.AddTimeout(TimeSpan.FromSeconds(5)));

        using ServiceProvider provider = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider =
            provider.GetRequiredService<ResiliencePipelineProvider<string>>();

        ResiliencePipeline pipeline = pipelineProvider.GetPipeline("direct");
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeResiliencePipeline_Typed_ShouldRegisterPollyPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeResiliencePipeline<string>("typed-direct", builder => builder.AddTimeout(TimeSpan.FromSeconds(5)));

        using ServiceProvider provider = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider =
            provider.GetRequiredService<ResiliencePipelineProvider<string>>();

        ResiliencePipeline<string> pipeline = pipelineProvider.GetPipeline<string>("typed-direct");
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeResiliencePipeline_WithNullName_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddNativeResiliencePipeline(null!, _ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStandardNativeResilience_ShouldRegisterStandardPipeline()
    {
        var services = new ServiceCollection();

        services.AddStandardNativeResilience("standard");

        using ServiceProvider provider = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider =
            provider.GetRequiredService<ResiliencePipelineProvider<string>>();

        ResiliencePipeline pipeline = pipelineProvider.GetPipeline("standard");
        pipeline.Should().NotBeNull();
    }
}
