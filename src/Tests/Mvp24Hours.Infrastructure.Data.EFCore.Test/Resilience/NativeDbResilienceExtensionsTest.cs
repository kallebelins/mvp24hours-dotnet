using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using Polly;
using Polly.Registry;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Resilience;

[Trait("Category", "Unit")]
public class NativeDbResilienceExtensionsTest
{
    private static ResiliencePipeline GetPipeline(IServiceCollection services, string name = "database")
    {
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline(name);
    }

    [Fact]
    public void AddNativeDbResilience_Default_ShouldRegisterPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeDbResilience();

        ResiliencePipeline pipeline = GetPipeline(services);
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeDbResilience_WithCustomName_ShouldRegisterNamedPipeline()
    {
        var services = new ServiceCollection();

        services.AddNativeDbResilience("custom-db");

        ResiliencePipeline pipeline = GetPipeline(services, "custom-db");
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void AddNativeDbResilience_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();

        services.AddNativeDbResilience(options =>
        {
            options.EnableRetry = true;
            options.RetryMaxAttempts = 7;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });

        GetPipeline(services).Should().NotBeNull();
    }

    [Fact]
    public void AddNativeDbResilience_WithNullOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddNativeDbResilience("database", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(nameof(NativeDbResilienceOptions.SqlServer))]
    [InlineData(nameof(NativeDbResilienceOptions.PostgreSql))]
    [InlineData(nameof(NativeDbResilienceOptions.MySql))]
    public void NativeDbResilienceOptions_Presets_ShouldEnableCoreFeatures(string presetName)
    {
        NativeDbResilienceOptions options = presetName switch
        {
            nameof(NativeDbResilienceOptions.SqlServer) => NativeDbResilienceOptions.SqlServer,
            nameof(NativeDbResilienceOptions.PostgreSql) => NativeDbResilienceOptions.PostgreSql,
            _ => NativeDbResilienceOptions.MySql
        };

        options.EnableRetry.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableTimeout.Should().BeTrue();
    }

    [Fact]
    public async Task AddNativeDbResilience_ShouldExecuteOperationThroughPipeline()
    {
        var services = new ServiceCollection();
        services.AddNativeDbResilience(options =>
        {
            options.EnableRetry = false;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });

        ResiliencePipeline pipeline = GetPipeline(services);

        int result = await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return 42;
        });

        result.Should().Be(42);
    }
}
