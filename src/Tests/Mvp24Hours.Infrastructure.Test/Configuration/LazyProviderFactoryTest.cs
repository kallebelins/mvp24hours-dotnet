using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Configuration;

namespace Mvp24Hours.Infrastructure.Test.Configuration;

[Trait("Category", "Unit")]
public class LazyProviderFactoryTest
{
    private sealed class Marker;

    [Fact]
    public void CreateLazyFactory_WithLazyInitDisabled_ShouldCreateInstanceImmediately()
    {
        int callCount = 0;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        Func<Marker> factory = LazyProviderFactory.CreateLazyFactory(
            provider,
            _ =>
            {
                callCount++;
                return new Marker();
            },
            enableLazyInit: false);

        callCount.Should().Be(1, "eager initialization must create the instance during CreateLazyFactory itself");

        Marker first = factory();
        Marker second = factory();

        callCount.Should().Be(1);
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void CreateLazyFactory_WithLazyInitEnabled_ShouldDeferCreationUntilFirstInvocation()
    {
        int callCount = 0;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        Func<Marker> factory = LazyProviderFactory.CreateLazyFactory(
            provider,
            _ =>
            {
                callCount++;
                return new Marker();
            },
            enableLazyInit: true);

        callCount.Should().Be(0, "lazy initialization must not create the instance before it is first requested");

        Marker first = factory();

        callCount.Should().Be(1);

        Marker second = factory();

        callCount.Should().Be(1, "subsequent calls must reuse the lazily created instance");
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void CreateLazyFactory_WithLazyInitEnabled_ShouldOnlyInvokeFactoryOnceUnderConcurrentAccess()
    {
        int callCount = 0;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        Func<Marker> factory = LazyProviderFactory.CreateLazyFactory(
            provider,
            _ =>
            {
                Interlocked.Increment(ref callCount);
                return new Marker();
            },
            enableLazyInit: true);

        Parallel.For(0, 20, _ => factory());

        callCount.Should().Be(1);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void RegisterLazyService_WithLazyInitDisabled_ShouldRegisterWithRequestedLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();

        LazyProviderFactory.RegisterLazyService(services, _ => new Marker(), enableLazyInit: false, lifetime);

        services.Should().ContainSingle(d => d.ServiceType == typeof(Marker) && d.Lifetime == lifetime);
    }

    [Fact]
    public void RegisterLazyService_WithLazyInitDisabledAndDefaultLifetime_ShouldDefaultToSingleton()
    {
        var services = new ServiceCollection();

        LazyProviderFactory.RegisterLazyService(services, _ => new Marker(), enableLazyInit: false);

        services.Should().ContainSingle(d => d.ServiceType == typeof(Marker) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterLazyService_WithLazyInitEnabled_ShouldRegisterFactoryDescriptorWithRequestedLifetime()
    {
        var services = new ServiceCollection();

        LazyProviderFactory.RegisterLazyService(services, _ => new Marker(), enableLazyInit: true, ServiceLifetime.Scoped);

        services.Should().ContainSingle(d => d.ServiceType == typeof(Marker) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterLazyService_WithLazyInitEnabled_ShouldOnlyInvokeFactoryOncePerResolution()
    {
        int callCount = 0;
        var services = new ServiceCollection();
        LazyProviderFactory.RegisterLazyService(
            services,
            _ =>
            {
                callCount++;
                return new Marker();
            },
            enableLazyInit: true,
            ServiceLifetime.Singleton);
        ServiceProvider provider = services.BuildServiceProvider();

        Marker first = provider.GetRequiredService<Marker>();

        callCount.Should().Be(1);
        first.Should().NotBeNull();
    }
}
