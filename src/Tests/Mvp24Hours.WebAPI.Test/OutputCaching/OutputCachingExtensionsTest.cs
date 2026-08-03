using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;

namespace Mvp24Hours.WebAPI.Test.OutputCaching;

[Trait("Category", "Unit")]
public class OutputCachingExtensionsTest
{
    [Fact]
    public void AddMvp24HoursOutputCache_Should_RegisterInvalidatorAndPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursOutputCache(options =>
        {
            options.AddStandardPolicies();
            options.AddPolicy("Products", p => p.Expire(TimeSpan.FromMinutes(5)).SetTags("products"));
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IOutputCacheInvalidator>().Should().NotBeNull();
        provider.GetService<IOptions<OutputCachingOptions>>()!.Value.Policies.Should().ContainKey("Products");
        provider.GetService<IOutputCacheStore>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursOutputCache_Should_SkipRegistration_WhenDisabled()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursOutputCache(options => options.Enabled = false);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IOutputCacheInvalidator>().Should().BeNull();
        provider.GetService<IOutputCacheStore>().Should().BeNull();
    }

    [Fact]
    public void UseMvp24HoursOutputCache_Should_Bypass_WhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<OutputCachingOptions>(options => options.Enabled = false);
        IApplicationBuilder app = new ApplicationBuilder(services.BuildServiceProvider());

        IApplicationBuilder result = app.UseMvp24HoursOutputCache();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursOutputCache_Should_Bypass_WhenExplicitlyDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<OutputCachingOptions>(options => options.Enabled = true);
        IApplicationBuilder app = new ApplicationBuilder(services.BuildServiceProvider());

        IApplicationBuilder result = app.UseMvp24HoursOutputCache(enabled: false);

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void CacheOutputWithPolicy_Should_Throw_WhenPolicyNameIsNull()
    {
        var builder = new TestEndpointConventionBuilder();

        Action act = () => builder.CacheOutputWithPolicy(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NoCacheOutput_Should_Throw_WhenBuilderIsNull()
    {
        TestEndpointConventionBuilder? builder = null;

        Action act = () => builder!.NoCacheOutput();

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
        }
    }
}
