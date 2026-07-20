using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using StackExchange.Redis;

namespace Mvp24Hours.Infrastructure.Caching.Test.Extensions;

[Trait("Category", "Unit")]
public class RedisServiceExtensionsTest
{
    [Fact]
    public void AddMvp24HoursCachingRedis_WithNullConfigurationOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursCachingRedis((ConfigurationOptions)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configurationOptions");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddMvp24HoursCachingRedis_WithInvalidConnectionString_ShouldThrow(string? connectionString)
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursCachingRedis(connectionString!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void AddMvp24HoursCachingRedis_WithConnectionString_ShouldRegisterDistributedCacheAndOptions()
    {
        var services = new ServiceCollection();
        const string connectionString = "localhost:6379,abortConnect=false";
        const string instanceName = "MyApp";

        IServiceCollection result = services.AddMvp24HoursCachingRedis(connectionString, instanceName);
        ServiceProvider provider = services.BuildServiceProvider();

        result.Should().BeSameAs(services);
        provider.GetRequiredService<IDistributedCache>().Should().NotBeNull();

        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        options.Configuration.Should().Be(connectionString);
        options.InstanceName.Should().Be("myapp_");
    }

    [Fact]
    public void AddMvp24HoursCachingRedis_WithConfigurationOptions_ShouldRegisterDistributedCacheAndOptions()
    {
        var services = new ServiceCollection();
        ConfigurationOptions configurationOptions = ConfigurationOptions.Parse("localhost:6379,abortConnect=false");
        const string instanceName = "My.App";

        IServiceCollection result = services.AddMvp24HoursCachingRedis(configurationOptions, instanceName);
        ServiceProvider provider = services.BuildServiceProvider();

        result.Should().BeSameAs(services);
        provider.GetRequiredService<IDistributedCache>().Should().NotBeNull();

        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        options.ConfigurationOptions.Should().BeSameAs(configurationOptions);
        options.InstanceName.Should().Be("my.app");
    }

    [Fact]
    public void AddMvp24HoursCachingRedis_WithConnectionStringAndNullInstanceName_ShouldUseDefaultSuffix()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursCachingRedis("localhost:6379,abortConnect=false");
        ServiceProvider provider = services.BuildServiceProvider();

        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        options.InstanceName.Should().NotBeNullOrWhiteSpace();
        options.InstanceName.Should().EndWith("_");
        options.InstanceName.Should().Be(options.InstanceName!.ToLowerInvariant());
    }

    [Fact]
    public void AddMvp24HoursCachingRedis_WithConfigurationOptionsAndNullInstanceName_ShouldUseDefaultWithoutSuffix()
    {
        var services = new ServiceCollection();
        ConfigurationOptions configurationOptions = ConfigurationOptions.Parse("localhost:6379,abortConnect=false");

        services.AddMvp24HoursCachingRedis(configurationOptions);
        ServiceProvider provider = services.BuildServiceProvider();

        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        options.InstanceName.Should().NotBeNullOrWhiteSpace();
        options.InstanceName.Should().NotEndWith("_");
        options.InstanceName.Should().Be(options.InstanceName!.ToLowerInvariant());
    }
}
