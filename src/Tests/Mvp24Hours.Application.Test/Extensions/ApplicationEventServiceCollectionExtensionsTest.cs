//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ApplicationEventServiceCollectionExtensionsTest
{
    private sealed class RecordingEventHandler : IApplicationEventHandler<TestApplicationEvent>
    {
        public Task HandleAsync(TestApplicationEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void AddMvp24HoursApplicationEvents_Parameterless_ShouldRegisterDispatcher()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEvents();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationEventDispatcher>().Should().BeOfType<ApplicationEventDispatcher>();
    }

    [Fact]
    public void AddMvp24HoursApplicationEvents_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEvents(options => options.ContinueOnError = true);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ApplicationEventDispatcherOptions>>().Value.ContinueOnError.Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursApplicationEventOutboxInMemory_ShouldRegisterInMemoryOutbox()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventOutboxInMemory();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationEventOutbox>().Should().BeOfType<InMemoryApplicationEventOutbox>();
    }

    [Fact]
    public void AddMvp24HoursApplicationEventOutbox_Generic_ShouldRegisterCustomOutbox()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventOutbox<InMemoryApplicationEventOutbox>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationEventOutbox>().Should().BeOfType<InMemoryApplicationEventOutbox>();
    }

    [Fact]
    public void AddMvp24HoursApplicationEventOutboxProcessor_Parameterless_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursApplicationEventOutboxProcessor();

        services.Should().Contain(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(ApplicationEventOutboxProcessor));
    }

    [Fact]
    public void AddMvp24HoursApplicationEventOutboxProcessor_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursApplicationEventOutboxProcessor(options => options.BatchSize = 25);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ApplicationEventOutboxProcessorOptions>>().Value.BatchSize.Should().Be(25);
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandlers_WithNullAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplicationEventHandlers((Assembly[])null!);

        act.Should().Throw<ArgumentException>().WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandlers_WithEmptyAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplicationEventHandlers([]);

        act.Should().Throw<ArgumentException>().WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandlers_ShouldRegisterDiscoveredHandlers()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventHandlers(typeof(CapturingEventHandler).Assembly);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationEventHandler<TestApplicationEvent>) &&
            d.ImplementationType == typeof(CapturingEventHandler));
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandlers_WithLifetime_ShouldUseThatLifetime()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventHandlers(ServiceLifetime.Singleton, typeof(CapturingEventHandler).Assembly);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationEventHandler<TestApplicationEvent>) &&
            d.ImplementationType == typeof(CapturingEventHandler) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandlersFromAssemblyContaining_ShouldRegisterHandlersFromThatAssembly()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventHandlersFromAssemblyContaining<CapturingEventHandler>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationEventHandler<TestApplicationEvent>) &&
            d.ImplementationType == typeof(CapturingEventHandler));
    }

    [Fact]
    public void AddMvp24HoursApplicationEventHandler_ShouldRegisterSpecificHandler()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationEventHandler<RecordingEventHandler, TestApplicationEvent>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationEventHandler<TestApplicationEvent>>()
            .Should().BeOfType<RecordingEventHandler>();
    }

    [Fact]
    public void AddMvp24HoursApplicationEventsWithOutbox_ShouldRegisterDispatcherOutboxProcessorAndHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursApplicationEventsWithOutbox(
            configureDispatcher: options => options.ContinueOnError = true,
            configureProcessor: options => options.BatchSize = 10,
            typeof(CapturingEventHandler).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationEventDispatcher>().Should().NotBeNull();
        provider.GetRequiredService<IApplicationEventOutbox>().Should().BeOfType<InMemoryApplicationEventOutbox>();
        provider.GetRequiredService<IOptions<ApplicationEventDispatcherOptions>>().Value.UseOutbox.Should().BeTrue();
        provider.GetRequiredService<IOptions<ApplicationEventOutboxProcessorOptions>>().Value.BatchSize.Should().Be(10);
        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationEventHandler<TestApplicationEvent>) &&
            d.ImplementationType == typeof(CapturingEventHandler));
    }

    [Fact]
    public void AddMvp24HoursApplicationEventsWithOutbox_WithoutHandlerAssemblies_ShouldSkipHandlerRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursApplicationEventsWithOutbox();

        services.Should().NotContain(d => d.ServiceType.IsGenericType
            && d.ServiceType.GetGenericTypeDefinition() == typeof(IApplicationEventHandler<>));
    }
}
